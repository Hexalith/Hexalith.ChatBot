using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

using Aspire.Hosting;
using Aspire.Hosting.Testing;

using Hexalith.ChatBot.Contracts.Identities;

using Shouldly;

namespace Hexalith.ChatBot.IntegrationTests;

/// <summary>
/// Tier-3 end-to-end proof that the trivial governed command flows through the REAL DAPR topology
/// (ChatBot + sidecars + EventStore + Tenants + Keycloak). The full vertical is wired and this runs GREEN against
/// the live topology: an unauthenticated submit is rejected fail-closed; a tenant-bound Keycloak bearer is minted
/// and an authenticated <c>RecordGovernedNote</c> (origin=ui) is dispatched into EventStore, whose aggregate
/// actor round-trips to the chatbot <c>/process</c> domain service, persists + publishes the governed event, and
/// the chatbot projection subscriber materialises the durable <c>GovernedOperationView</c> in
/// <c>chatbot-statestore</c>; the view is read back (tenant-partitioned, metadata-only, source version 1),
/// idempotent replay yields one durable effect with an identical body, and no restricted evidence leaks.
/// It self-skips unless deliberately opted in, keeping the runnable suite honest (never a spurious pass or fail).
/// </summary>
/// <remarks>
/// To run it green, opt in with <c>HEXALITH_CHATBOT_TIER3=1</c> on a host that has a Docker runtime and the DAPR
/// CLI/runtime (<c>dapr init</c>), with <c>~/.dapr/bin</c> on PATH. The test mints the tenant-bound token from the
/// provisioned Keycloak realm itself (the <c>hexalith-chatbot</c> direct-access-grant client + the
/// <c>actor-alpha</c> user whose <c>tenants:[tenant-alpha]</c> attribute maps to the <c>eventstore:tenant</c>
/// claim), so no out-of-band token is needed (an <c>HEXALITH_CHATBOT_TIER3_TOKEN</c> override is still honoured).
/// Two host-specific knobs cover a non-standard <c>dapr init</c>: <c>Dapr:PlacementHostAddress</c> /
/// <c>Dapr:SchedulerHostAddress</c> point daprd at the placement/scheduler control-plane when it was mapped to
/// non-default host ports (unset → daprd defaults). The chatbot sidecar loads the default-allow
/// <c>accesscontrol.local.yaml</c>: the deployed deny-by-default <c>accesscontrol.yaml</c> can only match callers
/// under mTLS (verified SPIFFE identity), which this self-hosted/mTLS-off topology does not provide.
/// </remarks>
[Trait("Category", "E2E")]
public sealed class TrivialGovernedCommandAspireE2eTests
{
    private const string ChatBotResourceName = "chatbot";
    private const string EventStoreResourceName = "eventstore";
    private const string TenantsResourceName = "tenants";

    private static readonly TimeSpan ProjectionTimeout = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan StartupTimeout = TimeSpan.FromMinutes(3);
    private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(500);

    private static string RecordGovernedNoteBody(string noteId, string commandId)
        => RecordGovernedNoteBody(noteId, commandId, "ui");

    private static string RecordGovernedNoteBody(string noteId, string commandId, string origin)
        => $$"""
            {"commandId":"{{commandId}}","commandType":"RecordGovernedNote","command":{"noteId":"{{noteId}}"},"origin":"{{origin}}","requestSchemaVersion":"v1"}
            """;

    [Fact]
    public async Task TrivialGovernedCommandShouldFlowEndToEndThroughTheRealDaprTopology()
    {
        Assert.SkipUnless(
            Tier3RuntimeIsAvailable(),
            "Tier-3 Aspire E2E requires a Docker runtime and the DAPR CLI/runtime (dapr init). Set "
            + "HEXALITH_CHATBOT_TIER3=1 (with ~/.dapr/bin on PATH) to run it; the test mints its own tenant-bound "
            + "Keycloak token from the provisioned realm.");

        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        // Fresh, valid ULIDs PER RUN so the durable state of an earlier (possibly failed) topology run in Redis
        // cannot dedup/poison this run: EventStore caches a command outcome keyed by command/causation id, so a
        // fixed id would make every run after the first replay a stale prior result. The SAME fresh ids are reused
        // for the idempotent replay (proving one durable effect for a repeated submission).
        string noteId = GovernedNoteId.New().ToString();
        string commandId = ChatBotCommandId.New().ToString();
        string taskId = ChatBotCommandId.New().ToString();
        string correlationId = ChatBotCommandId.New().ToString();

        IDistributedApplicationTestingBuilder builder = await DistributedApplicationTestingBuilder
            .CreateAsync<global::Projects.Hexalith_ChatBot_AppHost>(cancellationToken)
            .ConfigureAwait(true);

        DistributedApplication app = await builder.BuildAsync(cancellationToken).ConfigureAwait(true);
        try
        {
            await app.StartAsync(cancellationToken).ConfigureAwait(true);

            // Wait for the spine resources to report Running before exercising them.
            foreach (string resource in new[] { EventStoreResourceName, TenantsResourceName, ChatBotResourceName })
            {
                await app.ResourceNotifications
                    .WaitForResourceHealthyAsync(resource, cancellationToken)
                    .WaitAsync(TimeSpan.FromMinutes(5), cancellationToken)
                    .ConfigureAwait(true);
            }

            using HttpClient client = app.CreateHttpClient(ChatBotResourceName);
            client.Timeout = TimeSpan.FromSeconds(30);
            using HttpClient eventStoreClient = app.CreateHttpClient(EventStoreResourceName, "http");
            eventStoreClient.Timeout = TimeSpan.FromSeconds(15);
            using HttpClient tenantsClient = app.CreateHttpClient(TenantsResourceName, "http");
            tenantsClient.Timeout = TimeSpan.FromSeconds(15);

            // `WaitForResourceHealthyAsync` resolves when a resource is *Running* (process launched), NOT when its
            // Kestrel listener is accepting — and with the dapr app-port endpoint proxy disabled there is no DCP
            // proxy to buffer the first connection. The chatbot dispatches the command to EventStore over DAPR
            // service invocation, so EventStore's Kestrel + actor host must be listening BEFORE the submit or the
            // dispatch hangs. Poll each spine app's /health until it answers (any response proves it is listening).
            await WaitForListenerAsync(eventStoreClient, cancellationToken).ConfigureAwait(true);
            await WaitForListenerAsync(tenantsClient, cancellationToken).ConfigureAwait(true);
            await WaitForChatBotListeningAsync(client, cancellationToken).ConfigureAwait(true);

            // Fail-closed proof through the real spine: an unauthenticated governed-command submission is
            // rejected (tenant bound only from Keycloak claims), writing no durable state.
            using StringContent unauthBody = new(RecordGovernedNoteBody(noteId, commandId), Encoding.UTF8, "application/json");
            using HttpResponseMessage unauthenticated = await client
                .PostAsync("/api/v1/commands", unauthBody, cancellationToken)
                .ConfigureAwait(true);
            unauthenticated.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);

            string accessToken = await AcquireTenantBoundAccessTokenAsync(app, cancellationToken).ConfigureAwait(true);

            // The chatbot's command dispatch and projection read both go THROUGH its DAPR sidecar; if the sidecar
            // failed to start (e.g. a bad access-control spec) the chatbot app still serves /health but every
            // sidecar-backed call hangs. Verify the sidecar's state-store path is live BEFORE submitting so that
            // failure surfaces here with a clear message instead of as an opaque submit timeout.
            await WaitForChatBotDaprSidecarAsync(client, accessToken, correlationId, cancellationToken).ConfigureAwait(true);

            // 1) Authenticated submit of the allowlisted trivial command declaring origin=ui → 202 Accepted. The
            //    durable spine (EventStore actor host + chatbot domain processor) finishes starting after the
            //    chatbot adapter; while EventStore is not yet reachable the dispatch fails closed (503), so retry
            //    the first submit until the spine accepts it (idempotent: a failed dispatch aborts its admission).
            CommandSubmissionOutcome first = await SubmitGovernedNoteUntilAcceptedAsync(client, accessToken, noteId, commandId, taskId, correlationId, cancellationToken).ConfigureAwait(true);
            first.StatusCode.ShouldBe(HttpStatusCode.Accepted);

            // 2) Poll the operation status until it is no longer pending — but never a premature "completed":
            //    a freshly accepted command stays accepted-projection-pending; the durable effect is asserted
            //    against the state store below, not from a status flip.
            JsonElement status = await PollOperationStatusAsync(client, accessToken, taskId, correlationId, cancellationToken).ConfigureAwait(true);
            status.GetProperty("completionStatus").GetString().ShouldNotBe("completed");

            // 3) Read the projected GovernedOperationView from chatbot-statestore (tenant-partitioned) and assert
            //    the durable read model materialized with the derived-record shape — not just an HTTP 202.
            JsonElement view = await PollGovernedOperationViewAsync(client, accessToken, noteId, correlationId, cancellationToken).ConfigureAwait(true);
            view.GetProperty("noteId").GetString().ShouldBe(noteId);
            view.GetProperty("sourceVersion").GetInt64().ShouldBe(1);
            view.GetProperty("redactionState").GetString().ShouldBe("metadata_only");

            // 4) Read the post-commit audit envelope summary through the tenant-scoped audit-history surface,
            //    proving the live topology carries surface origin and metadata-only audit fields end-to-end.
            JsonElement auditHistory = await PollOperationAuditHistoryAsync(client, accessToken, taskId, correlationId, cancellationToken).ConfigureAwait(true);
            auditHistory.GetProperty("operationId").GetString().ShouldBe(taskId);
            auditHistory.GetProperty("auditStatus").GetString().ShouldBe("committed");
            JsonElement entries = auditHistory.GetProperty("entries");
            entries.GetArrayLength().ShouldBe(1);
            JsonElement auditEntry = entries[0];
            auditEntry.GetProperty("phase").GetString().ShouldBe("post-commit");
            auditEntry.GetProperty("decision").GetString().ShouldBe("allow");
            auditEntry.GetProperty("reasonCode").GetString().ShouldBe("eventstore_dispatch_accepted");
            auditEntry.GetProperty("outcome").GetString().ShouldBe("proposed");
            auditEntry.GetProperty("redactionDecision").GetString().ShouldBe("metadata_only");
            auditEntry.GetProperty("surfaceOrigin").GetString().ShouldBe("ui");
            auditEntry.GetProperty("resourceId").GetString().ShouldBe(noteId);
            auditEntry.GetProperty("correlationId").GetString().ShouldBe(correlationId);

            // 5) Idempotent replay: the same submission (same fresh ids) yields one durable effect (the source
            //    version does not advance) and an identical response body.
            CommandSubmissionOutcome replay = await SubmitGovernedNoteAsync(client, accessToken, noteId, commandId, taskId, correlationId, cancellationToken).ConfigureAwait(true);
            replay.StatusCode.ShouldBe(HttpStatusCode.Accepted);
            replay.Body.ShouldBe(first.Body);
            JsonElement viewAfterReplay = await ReadGovernedOperationViewAsync(client, accessToken, noteId, correlationId, cancellationToken).ConfigureAwait(true);
            viewAfterReplay.GetProperty("sourceVersion").GetInt64().ShouldBe(1);

            // No restricted evidence leaks across the durable surfaces.
            foreach (string body in new[] { first.Body, view.GetRawText(), auditHistory.GetRawText(), viewAfterReplay.GetRawText() })
            {
                body.ShouldNotContain("tenant-alpha", Case.Insensitive);
                body.ShouldNotContain("restricted-file.txt", Case.Insensitive);
                body.ShouldNotContain("Secret Project", Case.Insensitive);
                body.ShouldNotContain("raw exception", Case.Insensitive);
            }
        }
        finally
        {
            await app.DisposeAsync().ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task GovernedNoteShouldProjectIdenticalDurableEndStateRegardlessOfDeclaredOrigin()
    {
        // Stretch leg (AC7): the live cross-origin differential. The same semantic intent submitted with
        // origin=ui then origin=cli then origin=mcp against the REAL DAPR topology must materialise an identical
        // projected GovernedOperationView derived-record shape (the projection is origin-free). A fresh note id
        // per origin avoids fine-idempotency collapsing the three submissions into one durable effect.
        Assert.SkipUnless(
            Tier3RuntimeIsAvailable(),
            "Tier-3 cross-origin Aspire E2E requires a Docker runtime and the DAPR CLI/runtime (dapr init). Set "
            + "HEXALITH_CHATBOT_TIER3=1 (with ~/.dapr/bin on PATH) to run it; the Tier-2 in-process arms cover "
            + "AC1-AC6 without it.");

        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        string correlationId = ChatBotCommandId.New().ToString();

        IDistributedApplicationTestingBuilder builder = await DistributedApplicationTestingBuilder
            .CreateAsync<global::Projects.Hexalith_ChatBot_AppHost>(cancellationToken)
            .ConfigureAwait(true);

        DistributedApplication app = await builder.BuildAsync(cancellationToken).ConfigureAwait(true);
        try
        {
            await app.StartAsync(cancellationToken).ConfigureAwait(true);
            foreach (string resource in new[] { EventStoreResourceName, TenantsResourceName, ChatBotResourceName })
            {
                await app.ResourceNotifications
                    .WaitForResourceHealthyAsync(resource, cancellationToken)
                    .WaitAsync(TimeSpan.FromMinutes(5), cancellationToken)
                    .ConfigureAwait(true);
            }

            using HttpClient client = app.CreateHttpClient(ChatBotResourceName);
            client.Timeout = TimeSpan.FromSeconds(30);
            using HttpClient eventStoreClient = app.CreateHttpClient(EventStoreResourceName, "http");
            eventStoreClient.Timeout = TimeSpan.FromSeconds(15);
            using HttpClient tenantsClient = app.CreateHttpClient(TenantsResourceName, "http");
            tenantsClient.Timeout = TimeSpan.FromSeconds(15);

            await WaitForListenerAsync(eventStoreClient, cancellationToken).ConfigureAwait(true);
            await WaitForListenerAsync(tenantsClient, cancellationToken).ConfigureAwait(true);
            await WaitForChatBotListeningAsync(client, cancellationToken).ConfigureAwait(true);

            string accessToken = await AcquireTenantBoundAccessTokenAsync(app, cancellationToken).ConfigureAwait(true);
            await WaitForChatBotDaprSidecarAsync(client, accessToken, correlationId, cancellationToken).ConfigureAwait(true);

            // Capture the origin-free derived-record shape of the projected view for each declared origin.
            List<(string Origin, string Shape)> shapes = [];
            foreach (string origin in new[] { "ui", "cli", "mcp" })
            {
                string noteId = GovernedNoteId.New().ToString();
                string commandId = ChatBotCommandId.New().ToString();
                string taskId = ChatBotCommandId.New().ToString();

                CommandSubmissionOutcome accepted = await SubmitGovernedNoteUntilAcceptedAsync(
                    client, accessToken, noteId, commandId, taskId, correlationId, origin, cancellationToken).ConfigureAwait(true);
                accepted.StatusCode.ShouldBe(HttpStatusCode.Accepted);

                JsonElement view = await PollGovernedOperationViewAsync(client, accessToken, noteId, correlationId, cancellationToken).ConfigureAwait(true);
                view.GetProperty("noteId").GetString().ShouldBe(noteId);
                view.GetProperty("sourceVersion").GetInt64().ShouldBe(1);
                view.GetProperty("redactionState").GetString().ShouldBe("metadata_only");
                view.GetRawText().ShouldNotContain("tenant-alpha", Case.Insensitive);

                // The derived-record shape excluding the per-note id (and per-run timestamps), so the only thing
                // compared across origins is the surface-invariant projection shape.
                shapes.Add((origin, DerivedRecordShape(view)));
            }

            // The projected end-state shape is identical regardless of the declared surface origin.
            shapes.Select(static entry => entry.Shape).Distinct(StringComparer.Ordinal).Count().ShouldBe(1);
        }
        finally
        {
            await app.DisposeAsync().ConfigureAwait(true);
        }
    }

    // The origin-free derived-record fields of a projected view (provenance/derivation/redaction/retention/
    // schema + source version), excluding the per-note id and per-run timestamps, rendered for cross-origin
    // equality.
    private static string DerivedRecordShape(JsonElement view)
    {
        string Read(string name) => view.TryGetProperty(name, out JsonElement value) ? value.ToString() : "<absent>";
        return string.Join(
            "|",
            Read("schemaVersion"),
            Read("sourceProvenance"),
            Read("derivationKernelVersion"),
            Read("redactionState"),
            Read("retentionClass"),
            Read("sourceVersion"));
    }

    private static async Task WaitForChatBotListeningAsync(HttpClient client, CancellationToken cancellationToken)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        while (stopwatch.Elapsed < StartupTimeout)
        {
            try
            {
                using HttpResponseMessage health = await client.GetAsync("/health", cancellationToken).ConfigureAwait(false);
                if (health.StatusCode == HttpStatusCode.OK)
                {
                    return;
                }
            }
            catch (HttpRequestException)
            {
                // Kestrel is not accepting connections yet; keep polling.
            }
            catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                // The per-request timeout fired before Kestrel answered; keep polling.
            }

            await Task.Delay(PollInterval, cancellationToken).ConfigureAwait(false);
        }

        throw new TimeoutException($"The chatbot app did not start listening within {StartupTimeout}.");
    }

    // Verifies the chatbot's DAPR sidecar (state-store path) is live by exercising the tenant-scoped
    // governed-operations read for a random, absent note: a 403 safe-not-found (or 200) proves the
    // chatbot -> sidecar -> chatbot-statestore round-trip works. A crashed sidecar makes this hang, so the
    // per-request timeout + retry surfaces it as a clear readiness failure rather than an opaque submit timeout.
    private static async Task WaitForChatBotDaprSidecarAsync(HttpClient client, string accessToken, string correlationId, CancellationToken cancellationToken)
    {
        string probeNoteId = GovernedNoteId.New().ToString();
        Stopwatch stopwatch = Stopwatch.StartNew();
        while (stopwatch.Elapsed < StartupTimeout)
        {
            try
            {
                using HttpResponseMessage response = await GetAuthorizedAsync(
                    client, accessToken, $"/api/v1/governed-operations/{probeNoteId}", correlationId, cancellationToken).ConfigureAwait(false);
                if (response.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.OK)
                {
                    return;
                }
            }
            catch (HttpRequestException)
            {
                // Sidecar not reachable yet; keep polling.
            }
            catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                // The per-request timeout fired (a dead/unready sidecar makes the state read hang); keep polling.
            }

            await Task.Delay(PollInterval, cancellationToken).ConfigureAwait(false);
        }

        throw new TimeoutException("The chatbot DAPR sidecar (state-store path) did not become ready within the timeout.");
    }

    // Polls until the app returns ANY HTTP response — proof its Kestrel listener is accepting connections (the
    // status code is irrelevant; even a 404/503 means it is up). EventStore/Tenants expose no app-readiness
    // signal through Aspire's Running/healthy state under IsProxied=false, and the chatbot's command dispatch
    // round-trips into EventStore's actor host, so EventStore must be listening before the first submit.
    private static async Task WaitForListenerAsync(HttpClient client, CancellationToken cancellationToken)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        while (stopwatch.Elapsed < StartupTimeout)
        {
            try
            {
                using HttpResponseMessage response = await client.GetAsync("/health", cancellationToken).ConfigureAwait(false);
                return;
            }
            catch (HttpRequestException)
            {
                // Not accepting connections yet; keep polling.
            }
            catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                // The per-request timeout fired before the listener answered; keep polling.
            }

            await Task.Delay(PollInterval, cancellationToken).ConfigureAwait(false);
        }

        throw new TimeoutException($"A spine app did not start listening within {StartupTimeout}.");
    }

    private static Task<CommandSubmissionOutcome> SubmitGovernedNoteUntilAcceptedAsync(
        HttpClient client,
        string accessToken,
        string noteId,
        string commandId,
        string taskId,
        string correlationId,
        CancellationToken cancellationToken)
        => SubmitGovernedNoteUntilAcceptedAsync(client, accessToken, noteId, commandId, taskId, correlationId, "ui", cancellationToken);

    private static async Task<CommandSubmissionOutcome> SubmitGovernedNoteUntilAcceptedAsync(
        HttpClient client,
        string accessToken,
        string noteId,
        string commandId,
        string taskId,
        string correlationId,
        string origin,
        CancellationToken cancellationToken)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        CommandSubmissionOutcome outcome = new(HttpStatusCode.ServiceUnavailable, string.Empty);
        while (stopwatch.Elapsed < StartupTimeout)
        {
            try
            {
                outcome = await SubmitGovernedNoteAsync(client, accessToken, noteId, commandId, taskId, correlationId, origin, cancellationToken).ConfigureAwait(false);
                if (outcome.StatusCode == HttpStatusCode.Accepted)
                {
                    return outcome;
                }
            }
            catch (HttpRequestException)
            {
                // The durable spine is still coming up; keep retrying (a failed dispatch aborts its admission).
            }
            catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                // The per-submit timeout fired (the dispatch into EventStore is still warming up); keep retrying.
            }

            await Task.Delay(PollInterval, cancellationToken).ConfigureAwait(false);
        }

        return outcome;
    }

    private static Task<CommandSubmissionOutcome> SubmitGovernedNoteAsync(
        HttpClient client,
        string accessToken,
        string noteId,
        string commandId,
        string taskId,
        string correlationId,
        CancellationToken cancellationToken)
        => SubmitGovernedNoteAsync(client, accessToken, noteId, commandId, taskId, correlationId, "ui", cancellationToken);

    private static async Task<CommandSubmissionOutcome> SubmitGovernedNoteAsync(
        HttpClient client,
        string accessToken,
        string noteId,
        string commandId,
        string taskId,
        string correlationId,
        string origin,
        CancellationToken cancellationToken)
    {
        using HttpRequestMessage request = new(HttpMethod.Post, "/api/v1/commands")
        {
            Content = new StringContent(RecordGovernedNoteBody(noteId, commandId, origin), Encoding.UTF8, "application/json"),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Headers.Add("X-Correlation-Id", correlationId);
        request.Headers.Add("X-Hexalith-Task-Id", taskId);
        using HttpResponseMessage response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);
        string body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        return new CommandSubmissionOutcome(response.StatusCode, body);
    }

    private static async Task<JsonElement> PollOperationStatusAsync(HttpClient client, string accessToken, string operationId, string correlationId, CancellationToken cancellationToken)
    {
        JsonElement last = default;
        Stopwatch stopwatch = Stopwatch.StartNew();
        while (stopwatch.Elapsed < ProjectionTimeout)
        {
            using HttpResponseMessage response = await GetAuthorizedAsync(client, accessToken, $"/api/v1/operations/{operationId}", correlationId, cancellationToken).ConfigureAwait(false);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                using JsonDocument document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false));
                last = document.RootElement.Clone();
                if (!string.IsNullOrEmpty(last.GetProperty("completionStatus").GetString()))
                {
                    return last;
                }
            }

            await Task.Delay(PollInterval, cancellationToken).ConfigureAwait(false);
        }

        return last;
    }

    private static async Task<JsonElement> PollGovernedOperationViewAsync(HttpClient client, string accessToken, string noteId, string correlationId, CancellationToken cancellationToken)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        while (stopwatch.Elapsed < ProjectionTimeout)
        {
            using HttpResponseMessage response = await GetAuthorizedAsync(client, accessToken, $"/api/v1/governed-operations/{noteId}", correlationId, cancellationToken).ConfigureAwait(false);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                using JsonDocument document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false));
                return document.RootElement.Clone();
            }

            await Task.Delay(PollInterval, cancellationToken).ConfigureAwait(false);
        }

        throw new TimeoutException($"The governed-operation projection for note '{noteId}' did not materialize within {ProjectionTimeout}.");
    }

    private static async Task<JsonElement> ReadGovernedOperationViewAsync(HttpClient client, string accessToken, string noteId, string correlationId, CancellationToken cancellationToken)
    {
        using HttpResponseMessage response = await GetAuthorizedAsync(client, accessToken, $"/api/v1/governed-operations/{noteId}", correlationId, cancellationToken).ConfigureAwait(false);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        using JsonDocument document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false));
        return document.RootElement.Clone();
    }

    private static async Task<JsonElement> PollOperationAuditHistoryAsync(HttpClient client, string accessToken, string operationId, string correlationId, CancellationToken cancellationToken)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        while (stopwatch.Elapsed < ProjectionTimeout)
        {
            using HttpResponseMessage response = await GetAuthorizedAsync(client, accessToken, $"/api/v1/operations/{operationId}/audit-history", correlationId, cancellationToken).ConfigureAwait(false);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                using JsonDocument document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false));
                JsonElement root = document.RootElement.Clone();
                if (root.TryGetProperty("entries", out JsonElement entries) && entries.GetArrayLength() > 0)
                {
                    return root;
                }
            }

            await Task.Delay(PollInterval, cancellationToken).ConfigureAwait(false);
        }

        throw new TimeoutException($"The audit history for operation '{operationId}' did not materialize within {ProjectionTimeout}.");
    }

    private static async Task<HttpResponseMessage> GetAuthorizedAsync(HttpClient client, string accessToken, string path, string correlationId, CancellationToken cancellationToken)
    {
        using HttpRequestMessage request = new(HttpMethod.Get, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Headers.Add("X-Correlation-Id", correlationId);
        return await client.SendAsync(request, cancellationToken).ConfigureAwait(false);
    }

    // Mints a tenant-bound bearer from the provisioned Keycloak realm via the Direct Access Grant (password)
    // flow: client hexalith-chatbot, user actor-alpha whose `tenants: [tenant-alpha]` attribute maps to the
    // `eventstore:tenant` claim the gateway binds the tenant from, with audience hexalith-chatbot. An out-of-band
    // HEXALITH_CHATBOT_TIER3_TOKEN override is honored first for harnesses that seed the realm differently.
    private static async Task<string> AcquireTenantBoundAccessTokenAsync(DistributedApplication app, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(app);

        string? overrideToken = Environment.GetEnvironmentVariable("HEXALITH_CHATBOT_TIER3_TOKEN");
        if (!string.IsNullOrWhiteSpace(overrideToken))
        {
            return overrideToken;
        }

        // Keycloak finishes its realm import + becomes ready asynchronously after the container reports Running,
        // so the token endpoint can hang or 503 briefly. Retry with a SHORT per-attempt timeout (so a not-ready
        // Keycloak fails fast instead of stalling the default 100s HttpClient timeout) until it issues the token.
        Uri keycloak = app.GetEndpoint("keycloak", "http");
        using FormUrlEncodedContent form = new(new Dictionary<string, string>
        {
            ["grant_type"] = "password",
            ["client_id"] = "hexalith-chatbot",
            ["username"] = "actor-alpha",
            ["password"] = "actor-alpha-pass",
            ["scope"] = "openid",
        });
        string formContent = await form.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        Stopwatch stopwatch = Stopwatch.StartNew();
        while (stopwatch.Elapsed < StartupTimeout)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                using HttpClient http = new() { BaseAddress = keycloak, Timeout = TimeSpan.FromSeconds(15) };
                using StringContent attempt = new(formContent, Encoding.UTF8, "application/x-www-form-urlencoded");
                using HttpResponseMessage response = await http
                    .PostAsync("/realms/hexalith/protocol/openid-connect/token", attempt, cancellationToken)
                    .ConfigureAwait(false);
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    using JsonDocument token = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false));
                    if (token.RootElement.TryGetProperty("access_token", out JsonElement accessToken)
                        && accessToken.GetString() is string value && !string.IsNullOrWhiteSpace(value))
                    {
                        return value;
                    }
                }
            }
            catch (HttpRequestException)
            {
                // Keycloak not accepting connections yet; retry.
            }
            catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                // The per-attempt 15s timeout fired (Keycloak still starting); retry.
            }

            await Task.Delay(PollInterval, cancellationToken).ConfigureAwait(false);
        }

        throw new InvalidOperationException(
            "Keycloak did not issue a tenant-bound token within the timeout. Ensure the hexalith realm provisioned "
            + "the hexalith-chatbot client (direct access grants) and the actor-alpha user.");
    }

    // The topology needs a Docker container runtime AND the DAPR CLI AND a deliberately-provisioned topology
    // (Keycloak tenant token + EventStore/Tenants sidecars). Requiring an explicit opt-in env var on top of the
    // CLIs prevents a sandbox that merely has Docker+DAPR from running the test against an unprovisioned topology.
    private static bool Tier3RuntimeIsAvailable()
        => string.Equals(Environment.GetEnvironmentVariable("HEXALITH_CHATBOT_TIER3"), "1", StringComparison.Ordinal)
        && CommandSucceeds("docker", "info")
        && CommandSucceeds("dapr", "--version");

    private static bool CommandSucceeds(string fileName, string arguments)
    {
        try
        {
            using Process? process = Process.Start(new ProcessStartInfo(fileName, arguments)
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            });

            if (process is null)
            {
                return false;
            }

            if (!process.WaitForExit(10_000))
            {
                process.Kill(entireProcessTree: true);
                return false;
            }

            return process.ExitCode == 0;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private readonly record struct CommandSubmissionOutcome(HttpStatusCode StatusCode, string Body);
}
