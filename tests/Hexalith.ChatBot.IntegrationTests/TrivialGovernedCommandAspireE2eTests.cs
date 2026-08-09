using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Text.Json;

using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing;

using CommunityToolkit.Aspire.Hosting.Dapr;

using Hexalith.ChatBot.Contracts.Identities;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

using Shouldly;

using Xunit;

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

    // Mirrors src/Hexalith.ChatBot.AppHost/Program.cs. The gate endpoint is not mapped at all without a token, so an
    // unmapped endpoint (404) is a topology misconfiguration rather than a "gate is clear" result.
    private const string M2ReleaseGateTokenHeader = "X-ChatBot-M2-Release-Gate-Token";
    private const string M2ReleaseGateToken = "local-topology-m2-release-gate-token";

    private static readonly string[] IsolatedDaprHttpResourceNames =
    [
        EventStoreResourceName,
        TenantsResourceName,
        ChatBotResourceName,
        "eventstore-admin",
    ];

    private static readonly string[] RequiredTopologyResources =
    [
        "security",
        EventStoreResourceName,
        TenantsResourceName,
        ChatBotResourceName,
        "chatbot-ui",
        "eventstore-admin",
        "eventstore-admin-ui",
    ];

    // Program.cs fails closed without a configured recovery mailbox secret; this suite never exercises the
    // recovery mailbox client, so a fixed, well-formed placeholder satisfies PrepareKeycloakRealmImport without
    // any of these tests needing to know about the live-recovery validation lane.
    private static readonly string[] MailboxSecretArgs =
    [
        $"--ChatBot:LiveRecoveryValidation:MailboxClientSecret={new string('a', 32)}",
    ];

    private static readonly TimeSpan ProjectionTimeout = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan ProjectionStabilityWindow = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan StartupTimeout = TimeSpan.FromMinutes(3);
    private static readonly TimeSpan SelectedResourceValidationTimeout = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(500);

    private readonly ITestOutputHelper _output;

    public TrivialGovernedCommandAspireE2eTests(ITestOutputHelper output)
    {
        _output = output;
    }

    private static string RecordGovernedNoteBody(string noteId, string commandId)
        => RecordGovernedNoteBody(noteId, commandId, "ui");

    private static string RecordGovernedNoteBody(string noteId, string commandId, string origin)
        => $$"""
            {"commandId":"{{commandId}}","commandType":"RecordGovernedNote","command":{"noteId":"{{noteId}}"},"origin":"{{origin}}","requestSchemaVersion":"v1"}
            """;

    [Fact]
    public async Task TrivialGovernedCommandShouldFlowEndToEndThroughTheRealDaprTopology()
    {
        RequireTier3Runtime(
            "Tier-3 Aspire E2E requires a Docker runtime and the DAPR CLI/runtime (dapr init). Set "
            + "HEXALITH_CHATBOT_TIER3=1 (with ~/.dapr/bin on PATH) to run it; the test mints its own tenant-bound "
            + "Keycloak token from the provisioned realm.");

        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        // Fresh, valid ULIDs PER RUN so the durable state of an earlier (possibly failed) topology run in Redis
        // cannot dedup/poison this run: EventStore caches a command outcome keyed by command/causation id, so a
        // fixed id would make every run after the first replay a stale prior result. The SAME fresh ids are reused
        // for the idempotent replay (proving one durable effect for a repeated submission).
        string unauthenticatedNoteId = GovernedNoteId.New().ToString();
        string unauthenticatedCommandId = ChatBotCommandId.New().ToString();
        string unauthenticatedTaskId = ChatBotCommandId.New().ToString();
        string unauthenticatedCorrelationId = ChatBotCommandId.New().ToString();

        string noteId = GovernedNoteId.New().ToString();
        string commandId = ChatBotCommandId.New().ToString();
        string taskId = ChatBotCommandId.New().ToString();
        string correlationId = ChatBotCommandId.New().ToString();

        string betaNoteId = GovernedNoteId.New().ToString();
        string betaCommandId = ChatBotCommandId.New().ToString();
        string betaTaskId = ChatBotCommandId.New().ToString();
        string betaCorrelationId = ChatBotCommandId.New().ToString();

        DistributedApplication app = await StartTestingApplicationAsync(cancellationToken).ConfigureAwait(true);
        try
        {
            await WaitForAndRecordRequiredTopologyAsync(app, cancellationToken).ConfigureAwait(true);

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
            using HttpResponseMessage unauthenticated = await SubmitUnauthenticatedGovernedNoteAsync(
                client,
                unauthenticatedNoteId,
                unauthenticatedCommandId,
                unauthenticatedTaskId,
                unauthenticatedCorrelationId,
                cancellationToken).ConfigureAwait(true);
            unauthenticated.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);

            string accessToken = await AcquireTenantBoundAccessTokenAsync(app, cancellationToken).ConfigureAwait(true);

            await AssertNoDurableStateWasCreatedAsync(
                client,
                accessToken,
                unauthenticatedNoteId,
                unauthenticatedTaskId,
                unauthenticatedCorrelationId,
                cancellationToken).ConfigureAwait(true);

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

            // Establish an independently authenticated second production tenant before the next M2 partition. The
            // derived-store probe draws its population from the union of the derived store and WORM audit store, then
            // seeds its own metadata-only sentinels. This command therefore creates real two-tenant pair coverage
            // without test-only product data or an empty-population exemption.
            string betaAccessToken = await AcquireTenantBoundAccessTokenAsync(
                app,
                "actor-beta",
                "actor-beta-pass",
                cancellationToken).ConfigureAwait(true);
            CommandSubmissionOutcome beta = await SubmitGovernedNoteUntilAcceptedAsync(
                client,
                betaAccessToken,
                betaNoteId,
                betaCommandId,
                betaTaskId,
                betaCorrelationId,
                cancellationToken).ConfigureAwait(true);
            beta.StatusCode.ShouldBe(HttpStatusCode.Accepted);

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
            await AssertGovernedOperationViewRemainsStableAsync(
                client,
                accessToken,
                noteId,
                correlationId,
                viewAfterReplay.GetRawText(),
                cancellationToken).ConfigureAwait(true);

            // No restricted evidence leaks across the durable surfaces.
            foreach (string body in new[]
            {
                first.Body,
                status.GetRawText(),
                view.GetRawText(),
                auditHistory.GetRawText(),
                viewAfterReplay.GetRawText(),
            })
            {
                body.ShouldNotContain("tenant-alpha", Case.Insensitive);
                body.ShouldNotContain("restricted-file.txt", Case.Insensitive);
                body.ShouldNotContain("Secret Project", Case.Insensitive);
                body.ShouldNotContain("raw exception", Case.Insensitive);
            }

            // AC3 (Story 12.14): the M2 stop-ship gate, asserted against the live topology. This is deliberately the
            // last leg of the *required* acceptance test rather than a separate job: this test already gates
            // `semantic-release` in release.yml, and by this point a real governed command has flowed through the real
            // spine, so the WORM chain and outbound-trace stores hold genuine tenant state for the sweeps to verify.
            // A real breach here fails this test, fails the job, and blocks the release — which is what "block release
            // on a real (not merely provable) breach signal" asks for.
            await AssertM2ReleaseGateIsClearAsync(client, cancellationToken).ConfigureAwait(true);
        }
        finally
        {
            await app.DisposeAsync().ConfigureAwait(true);
        }
    }

    /// <summary>
    /// Polls the token-gated M2 release-gate endpoint until every sweep has reported, then asserts the stop-ship
    /// verdict is clear.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Polling is required because the sweeps are cadence-gated, not on-demand. The first sweep fires at startup —
    /// before this test has submitted anything — so it necessarily sees empty stores; the gate is only meaningful
    /// once a sweep has run *after* the governed command landed. The acceptance topology therefore shortens
    /// <c>M2SweepCadence</c> (see <see cref="ConfigureAcceptanceM2SweepCadence"/>) so a second partition arrives
    /// within the test's lifetime instead of 24 hours later.
    /// </para>
    /// <para>
    /// The test submits independently authenticated commands for tenant-alpha and tenant-beta. The derived-store probe
    /// therefore owes real ordered-pair coverage; zero coverage is never accepted as a substitute for verification.
    /// </para>
    /// </remarks>
    private static async Task AssertM2ReleaseGateIsClearAsync(HttpClient client, CancellationToken cancellationToken)
    {
        using CancellationTokenSource deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(TimeSpan.FromMinutes(6));

        string? lastBody = null;
        HttpStatusCode? lastStatus = null;
        while (!deadline.IsCancellationRequested)
        {
            using HttpRequestMessage request = new(HttpMethod.Get, "/health/chatbot/periodic-enforcement/m2");
            request.Headers.TryAddWithoutValidation(M2ReleaseGateTokenHeader, M2ReleaseGateToken);
            using HttpResponseMessage response = await client
                .SendAsync(request, deadline.Token)
                .ConfigureAwait(true);
            lastStatus = response.StatusCode;
            lastBody = await response.Content.ReadAsStringAsync(deadline.Token).ConfigureAwait(true);

            // 404 would mean the endpoint was never mapped, i.e. the topology did not supply a gate token — that is a
            // configuration failure, not a transient state, so fail immediately rather than burning the deadline.
            response.StatusCode.ShouldNotBe(
                HttpStatusCode.NotFound,
                "The M2 release-gate endpoint is unmapped: the topology did not configure "
                + "ChatBot__PeriodicEnforcement__M2ReleaseGateToken.");
            response.StatusCode.ShouldNotBe(
                HttpStatusCode.Unauthorized,
                "The M2 release-gate token presented by the acceptance test does not match the topology's.");

            if (response.StatusCode == HttpStatusCode.OK)
            {
                using JsonDocument gate = JsonDocument.Parse(lastBody);
                JsonElement root = gate.RootElement;
                root.GetProperty("isStopShip").GetBoolean().ShouldBeFalse();
                root.GetProperty("m2SweepsEnabled").GetBoolean().ShouldBeTrue();

                // Positive coverage on every release-gated sweep. Without this the assertion could pass on a topology
                // where an empty or misbound store verified nothing.
                JsonElement sweeps = root.GetProperty("m2SweepStatuses");
                sweeps.GetProperty("worm-audit-chain").GetProperty("hasCoverage").GetBoolean().ShouldBeTrue();
                sweeps.GetProperty("replay-isolation-probe").GetProperty("hasCoverage").GetBoolean().ShouldBeTrue();
                sweeps.GetProperty("derived-store-isolation-probe").GetProperty("hasCoverage").GetBoolean().ShouldBeTrue();
                return;
            }

            await Task.Delay(TimeSpan.FromSeconds(5), deadline.Token).ConfigureAwait(true);
        }

        throw new InvalidOperationException(
            $"The M2 release gate never cleared within its deadline. Last status: {lastStatus}. Last body: {lastBody}");
    }

    [Fact]
    public async Task GovernedNoteShouldProjectIdenticalDurableEndStateRegardlessOfDeclaredOrigin()
    {
        // Stretch leg (AC7): the live cross-origin differential. The same semantic intent submitted with
        // origin=ui then origin=cli then origin=mcp against the REAL DAPR topology must materialise an identical
        // projected GovernedOperationView derived-record shape (the projection is origin-free). A fresh note id
        // per origin avoids fine-idempotency collapsing the three submissions into one durable effect.
        RequireTier3Runtime(
            "Tier-3 cross-origin Aspire E2E requires a Docker runtime and the DAPR CLI/runtime (dapr init). Set "
            + "HEXALITH_CHATBOT_TIER3=1 (with ~/.dapr/bin on PATH) to run it; the Tier-2 in-process arms cover "
            + "AC1-AC6 without it.");

        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        string correlationId = ChatBotCommandId.New().ToString();

        DistributedApplication app = await StartTestingApplicationAsync(cancellationToken).ConfigureAwait(true);
        try
        {
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

    [Fact]
    public async Task CorrectionPropagationWorkflowRuntimeShouldBeHealthyInRealDaprTopology()
    {
        RequireTier3Runtime(
            "Tier-3 workflow smoke requires a Docker runtime and the DAPR CLI/runtime (dapr init). Set "
            + "HEXALITH_CHATBOT_TIER3=1 (with ~/.dapr/bin on PATH) to validate the hosted workflow runtime.");

        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        DistributedApplication app = await StartTestingApplicationAsync(cancellationToken).ConfigureAwait(true);
        try
        {
            foreach (string resource in new[] { EventStoreResourceName, TenantsResourceName, ChatBotResourceName })
            {
                await app.ResourceNotifications
                    .WaitForResourceHealthyAsync(resource, cancellationToken)
                    .WaitAsync(TimeSpan.FromMinutes(5), cancellationToken)
                    .ConfigureAwait(true);
            }

            using HttpClient client = app.CreateHttpClient(ChatBotResourceName);
            client.Timeout = TimeSpan.FromSeconds(30);
            await WaitForChatBotListeningAsync(client, cancellationToken).ConfigureAwait(true);

            using HttpResponseMessage response = await client
                .GetAsync("/health/chatbot/workflows", cancellationToken)
                .ConfigureAwait(true);
            response.StatusCode.ShouldBe(HttpStatusCode.OK);

            using JsonDocument body = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(true));
            body.RootElement.GetProperty("isAvailable").GetBoolean().ShouldBeTrue();
            body.RootElement.GetProperty("status").GetString().ShouldBe("available");

            // Primary-path schedule + inspect: start a deterministic correction-propagation instance through the
            // chatbot Dapr sidecar workflow HTTP API, then read its runtime status metadata.
            Uri daprHttp = ResolveChatBotDaprHttpEndpoint(app);
            string instanceId =
                $"tier3:correction-propagation:smoke:{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";
            using HttpClient daprClient = new() { BaseAddress = daprHttp, Timeout = TimeSpan.FromSeconds(30) };
            using StringContent scheduleBody = new(
                """
                {
                  "TenantId": "tenant-alpha",
                  "ActorId": "actor-alpha",
                  "AssociationId": "01ARZ3NDEKTSV4RRFFQ69G5FAV",
                  "IntakeId": "01ARZ3NDEKTSV4RRFFQ69G5FAY",
                  "CorrectionId": "01ARZ3NDEKTSV4RRFFQ69G5FAV:correction:3",
                  "WorkflowInstanceId": "REPLACE_INSTANCE",
                  "PriorProjectId": "project-001",
                  "CorrectedProjectId": "project-002",
                  "SourceVersion": 3,
                  "CorrelationId": "01ARZ3NDEKTSV4RRFFQ69G5FAW",
                  "StartedAtUtc": "2026-05-31T09:00:00Z",
                  "EstimatedCompletionAtUtc": "2026-05-31T09:10:00Z",
                  "OperationId": "01ARZ3NDEKTSV4RRFFQ69G5FAX"
                }
                """.Replace("REPLACE_INSTANCE", instanceId, StringComparison.Ordinal),
                Encoding.UTF8,
                "application/json");

            using HttpResponseMessage scheduleResponse = await daprClient
                .PostAsync(
                    $"/v1.0-beta1/workflows/dapr/CorrectionPropagationWorkflow/start?instanceID={Uri.EscapeDataString(instanceId)}",
                    scheduleBody,
                    cancellationToken)
                .ConfigureAwait(true);
            scheduleResponse.StatusCode.ShouldBeOneOf(HttpStatusCode.Accepted, HttpStatusCode.OK);

            using HttpResponseMessage statusResponse = await daprClient
                .GetAsync(
                    $"/v1.0-beta1/workflows/dapr/{Uri.EscapeDataString(instanceId)}",
                    cancellationToken)
                .ConfigureAwait(true);
            statusResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
            string statusPayload = await statusResponse.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(true);
            using JsonDocument statusDoc = JsonDocument.Parse(statusPayload);
            statusDoc.RootElement.GetProperty("instanceID").GetString().ShouldBe(instanceId);
            statusPayload.ShouldNotContain("sender@", Case.Insensitive);
            statusPayload.ShouldNotContain("rawBody", Case.Insensitive);
        }
        finally
        {
            await app.DisposeAsync().ConfigureAwait(true);
        }
    }

    private static Uri ResolveChatBotDaprHttpEndpoint(DistributedApplication app)
    {
        foreach (string candidate in new[] { "chatbot-dapr", "chatbot-dapr-cli", $"{ChatBotResourceName}-dapr" })
        {
            try
            {
                return app.GetEndpoint(candidate, "http");
            }
            catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException)
            {
                // try next candidate
            }
        }

        throw new InvalidOperationException(
            "Could not resolve the ChatBot Dapr HTTP endpoint required to schedule a correction-propagation workflow.");
    }

    [Fact]
    public async Task TierThreeEndpointIsolationShouldOnlyAssignHeldDistinctReservationsToNamedDaprHttpEndpoints()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        IDistributedApplicationTestingBuilder builder = await DistributedApplicationTestingBuilder
            .CreateAsync<global::Projects.Hexalith_ChatBot_AppHost>(MailboxSecretArgs, cancellationToken)
            .ConfigureAwait(true);

        EndpointSnapshot[] before = CaptureEndpointSnapshots(builder);
        IReadOnlyList<ReservedEndpoint> selected = GetIsolatedDaprHttpEndpoints(builder);
        IReadOnlySet<int> unselectedConcretePorts = GetUnselectedConcreteEndpointPorts(builder, selected);
        using PortReservationSet reservations = PortReservationSet.Reserve(
            IsolatedDaprHttpResourceNames.Length,
            unselectedConcretePorts);
        selected.Select(static endpoint => endpoint.Resource.Name)
            .ShouldBe(IsolatedDaprHttpResourceNames, ignoreOrder: true);

        ConfigureReservedDaprHttpEndpoints(builder, reservations.Ports);

        reservations.Ports.Distinct().Count().ShouldBe(IsolatedDaprHttpResourceNames.Length);
        reservations.Ports.ShouldAllBe(port => !unselectedConcretePorts.Contains(port));
        foreach (ReservedEndpoint selectedEndpoint in selected)
        {
            EndpointAnnotation endpoint = selectedEndpoint.Endpoint;
            endpoint.Port.ShouldNotBeNull();
            endpoint.TargetPort.ShouldBe(endpoint.Port);
            reservations.Ports.ShouldContain(endpoint.Port.Value);

            EndpointSnapshot original = before.Single(snapshot =>
                string.Equals(snapshot.ResourceName, selectedEndpoint.Resource.Name, StringComparison.Ordinal)
                && string.Equals(snapshot.EndpointName, endpoint.Name, StringComparison.Ordinal));
            (SnapshotEndpoint(selectedEndpoint.Resource, endpoint, original.EndpointIndex) with
            {
                Port = original.Port,
                TargetPort = original.TargetPort,
            }).ShouldBe(original, $"{selectedEndpoint.Resource.Name}/http must preserve every non-port semantic.");
        }

        EndpointSnapshot[] unchangedBefore = before.Where(static snapshot => !IsReservedEndpoint(snapshot)).ToArray();
        EndpointSnapshot[] unchangedAfter = CaptureEndpointSnapshots(builder)
            .Where(static snapshot => !IsReservedEndpoint(snapshot))
            .ToArray();
        unchangedAfter.ShouldBe(unchangedBefore, "Unrelated, proxied, non-HTTP, container, and management endpoints must remain untouched.");

        DistributedApplication app = await builder.BuildAsync(cancellationToken).ConfigureAwait(true);
        try
        {
            reservations.IsReleased.ShouldBeFalse("Reservations must remain held through application-model construction.");
            foreach (int port in reservations.Ports)
            {
                SocketException collision = Should.Throw<SocketException>(() =>
                {
                    using TcpListener competingListener = WildcardTcpListener.Start(port);
                });
                WildcardTcpListener.IsExclusiveBindCollision(collision.SocketErrorCode, OperatingSystem.IsWindows())
                    .ShouldBeTrue();
            }
        }
        finally
        {
            await app.DisposeAsync().ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task TierThreeSelectedReservationOverlapShouldBeRejectedBeforeBuild()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        IDistributedApplicationTestingBuilder builder = await DistributedApplicationTestingBuilder
            .CreateAsync<global::Projects.Hexalith_ChatBot_AppHost>(MailboxSecretArgs, cancellationToken)
            .ConfigureAwait(true);
        EndpointSnapshot[] before = CaptureEndpointSnapshots(builder);
        IReadOnlyList<ReservedEndpoint> selected = GetIsolatedDaprHttpEndpoints(builder);
        IReadOnlySet<int> unselectedConcretePorts = GetUnselectedConcreteEndpointPorts(builder, selected);
        unselectedConcretePorts.ShouldNotBeEmpty("The AppHost must declare at least one concrete unselected endpoint port.");

        int conflictingPort = unselectedConcretePorts.First();
        int[] candidatePorts = new[] { conflictingPort }
            .Concat(Enumerable.Range(1, ushort.MaxValue)
                .Where(port => port != conflictingPort && !unselectedConcretePorts.Contains(port)))
            .Take(IsolatedDaprHttpResourceNames.Length)
            .ToArray();

        InvalidOperationException exception = Should.Throw<InvalidOperationException>(
            () => ConfigureReservedDaprHttpEndpoints(builder, candidatePorts));
        exception.Message.ShouldContain(conflictingPort.ToString(System.Globalization.CultureInfo.InvariantCulture));
        CaptureEndpointSnapshots(builder).ShouldBe(before, "An overlap must be rejected before model mutation or build.");
    }

    [Fact]
    public void TierThreePortReservationShouldBoundRejectedCandidates()
    {
        TcpListener[] excludedListeners = Enumerable.Range(0, 3)
            .Select(static _ => WildcardTcpListener.Start(0))
            .ToArray();
        Queue<TcpListener> candidates = new(excludedListeners);
        HashSet<int> excludedPorts = excludedListeners
            .Select(static listener => ((IPEndPoint)listener.LocalEndpoint).Port)
            .ToHashSet();
        int candidateCalls = 0;
        List<int> inspectedPorts = [];
        try
        {
            InvalidOperationException exception = Should.Throw<InvalidOperationException>(() =>
                PortReservationSet.Reserve(
                    count: 1,
                    excludedPorts,
                    () =>
                    {
                        foreach (int inspectedPort in inspectedPorts)
                        {
                            AssertWildcardPortIsHeld(inspectedPort);
                        }

                        candidateCalls++;
                        TcpListener candidate = candidates.Dequeue();
                        inspectedPorts.Add(((IPEndPoint)candidate.LocalEndpoint).Port);
                        return candidate;
                    },
                    maximumCandidateCount: 3));

            candidateCalls.ShouldBe(3);
            exception.Message.ShouldContain("after inspecting 3 candidates", Case.Sensitive);
            foreach (int excludedPort in excludedPorts)
            {
                using TcpListener rebound = WildcardTcpListener.Start(excludedPort);
            }
        }
        finally
        {
            foreach (TcpListener listener in excludedListeners)
            {
                listener.Stop();
            }
        }
    }

    [Fact]
    public void TierThreePortReservationShouldHoldExcludedCandidatesUntilASelectionSucceeds()
    {
        TcpListener[] candidateListeners = Enumerable.Range(0, 3)
            .Select(static _ => WildcardTcpListener.Start(0))
            .ToArray();
        Queue<TcpListener> candidates = new(candidateListeners);
        int[] candidatePorts = candidateListeners
            .Select(static listener => ((IPEndPoint)listener.LocalEndpoint).Port)
            .ToArray();
        HashSet<int> excludedPorts = candidatePorts.Take(2).ToHashSet();
        List<int> inspectedPorts = [];
        try
        {
            using PortReservationSet reservations = PortReservationSet.Reserve(
                count: 1,
                excludedPorts,
                () =>
                {
                    foreach (int inspectedPort in inspectedPorts)
                    {
                        AssertWildcardPortIsHeld(inspectedPort);
                    }

                    TcpListener candidate = candidates.Dequeue();
                    inspectedPorts.Add(((IPEndPoint)candidate.LocalEndpoint).Port);
                    return candidate;
                },
                maximumCandidateCount: 3);

            reservations.Ports.ShouldBe([candidatePorts[2]], ignoreOrder: false);
            foreach (int excludedPort in excludedPorts)
            {
                using TcpListener rebound = WildcardTcpListener.Start(excludedPort);
            }

            AssertWildcardPortIsHeld(candidatePorts[2]);
        }
        finally
        {
            foreach (TcpListener listener in candidateListeners)
            {
                listener.Stop();
            }
        }
    }

    [Fact]
    public void TierThreePortReservationShouldRejectDuplicateAndInvalidCandidatePorts()
    {
        TcpListener duplicateListener = WildcardTcpListener.Start(0);
        try
        {
            InvalidOperationException duplicate = Should.Throw<InvalidOperationException>(() =>
                PortReservationSet.Reserve(
                    count: 2,
                    startListener: () => duplicateListener,
                    maximumCandidateCount: 2));
            duplicate.Message.ShouldContain("duplicates an already-held candidate", Case.Sensitive);
        }
        finally
        {
            duplicateListener.Stop();
        }

        TcpListener invalidListener = WildcardTcpListener.Start(0);
        try
        {
            InvalidOperationException invalid = Should.Throw<InvalidOperationException>(() =>
                PortReservationSet.Reserve(
                    count: 1,
                    startListener: () => invalidListener,
                    getCandidatePort: static _ => 0));
            invalid.Message.ShouldContain("outside the valid TCP range", Case.Sensitive);
        }
        finally
        {
            invalidListener.Stop();
        }
    }

    [Fact]
    public async Task TierThreeEndpointConfigurationShouldRejectDuplicateAndInvalidPorts()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        IDistributedApplicationTestingBuilder builder = await DistributedApplicationTestingBuilder
            .CreateAsync<global::Projects.Hexalith_ChatBot_AppHost>(MailboxSecretArgs, cancellationToken)
            .ConfigureAwait(true);

        foreach (int[] invalidPorts in new[]
        {
            new[] { 41001, 41001, 41002, 41003 },
            new[] { 0, 41001, 41002, 41003 },
            new[] { IPEndPoint.MaxPort + 1, 41001, 41002, 41003 },
        })
        {
            Should.Throw<ArgumentException>(() => ConfigureReservedDaprHttpEndpoints(builder, invalidPorts));
        }
    }

    [Theory]
    [InlineData(SocketError.AddressNotAvailable)]
    [InlineData(SocketError.OperationNotSupported)]
    public void TierThreeWildcardReservationShouldFallBackToIpv4ForUnavailableDualModeBinding(
        SocketError ipv6Error)
    {
        List<IPAddress> attemptedAddresses = [];
        using TcpListener listener = WildcardTcpListener.Start(
            0,
            (address, port) =>
            {
                attemptedAddresses.Add(address);
                if (address.Equals(IPAddress.IPv6Any))
                {
                    throw new SocketException((int)ipv6Error);
                }

                return new TcpListener(address, port);
            },
            supportsIpv6: true);

        attemptedAddresses.ShouldBe([IPAddress.IPv6Any, IPAddress.Any], ignoreOrder: false);
        ((IPEndPoint)listener.LocalEndpoint).AddressFamily.ShouldBe(AddressFamily.InterNetwork);
    }

    [Theory]
    [InlineData(SocketError.AddressAlreadyInUse, false, true)]
    [InlineData(SocketError.AddressAlreadyInUse, true, true)]
    [InlineData(SocketError.AccessDenied, false, false)]
    [InlineData(SocketError.AccessDenied, true, true)]
    public void TierThreeExclusiveBindCollisionShouldHonorWindowsAccessDeniedSemantics(
        SocketError socketError,
        bool isWindows,
        bool expected)
        => WildcardTcpListener.IsExclusiveBindCollision(socketError, isWindows).ShouldBe(expected);

    [Fact]
    public void TierThreePortReservationReleaseShouldBeIdempotentWithoutARebindRace()
    {
        using PortReservationSet reservations = PortReservationSet.Reserve(IsolatedDaprHttpResourceNames.Length);
        int[] ports = reservations.Ports.ToArray();
        ports.Distinct().Count().ShouldBe(ports.Length);

        foreach (int port in ports)
        {
            SocketException collision = Should.Throw<SocketException>(() =>
            {
                using TcpListener competingListener = WildcardTcpListener.Start(port);
            });
            WildcardTcpListener.IsExclusiveBindCollision(collision.SocketErrorCode, OperatingSystem.IsWindows())
                .ShouldBeTrue();
        }

        reservations.Release();
        reservations.IsReleased.ShouldBeTrue();
        reservations.Release();
        reservations.IsReleased.ShouldBeTrue("A repeated release must remain a no-op.");
    }

    [Fact]
    public async Task TierThreeStartupRetryShouldUseAFreshSecondAttemptForSelectedTerminalLogContention()
    {
        const int selectedPort = 43123;
        IReadOnlyDictionary<string, int> selectedPorts = new Dictionary<string, int>(StringComparer.Ordinal)
        {
            [EventStoreResourceName] = selectedPort,
        };
        List<object> createdAttempts = [];
        List<object> startedAttempts = [];
        List<object> disposedAttempts = [];
        int validationCalls = 0;

        object result = await TopologyStartupOrchestrator.StartAsync(
            (attemptNumber, _) =>
            {
                object attempt = new();
                createdAttempts.Add(attempt);
                attemptNumber.ShouldBe(createdAttempts.Count);
                return Task.FromResult(attempt);
            },
            (attempt, _) =>
            {
                createdAttempts.ShouldContain(attempt);
                startedAttempts.Add(attempt);
                return Task.CompletedTask;
            },
            (attempt, _) =>
            {
                startedAttempts.ShouldContain(attempt, "Address contention is injected only after startup returns.");
                validationCalls++;
                if (validationCalls == 1)
                {
                    SelectedEndpointStartupException failure = new(
                        EventStoreResourceName,
                        selectedPort,
                        KnownResourceStates.FailedToStart,
                        healthStatus: null,
                        isTerminal: true);
                    failure.RecordCorrelatedBindEvidence(hasCorrelatedBindEvidence: true);
                    failure.InnerException.ShouldBeNull("Terminal correlation must not fabricate a socket failure.");
                    throw failure;
                }

                return Task.CompletedTask;
            },
            (_, exception) => TopologyFailureCorrelation.IsSelectedAddressInUse(exception, selectedPorts),
            attempt =>
            {
                disposedAttempts.Add(attempt);
                return ValueTask.CompletedTask;
            },
            TestContext.Current.CancellationToken).ConfigureAwait(true);

        createdAttempts.Count.ShouldBe(2);
        ReferenceEquals(createdAttempts[0], createdAttempts[1]).ShouldBeFalse("The retry must rebuild an entirely fresh attempt.");
        result.ShouldBeSameAs(createdAttempts[1]);
        startedAttempts.ShouldBe(createdAttempts, ignoreOrder: false);
        disposedAttempts.ShouldBe([createdAttempts[0]], ignoreOrder: false);
        validationCalls.ShouldBe(2);
    }

    [Theory]
    [InlineData("chatbot-ui")]
    [InlineData("eventstore-admin-ui")]
    public void TierThreeStartupRetryShouldNotCorrelatePrefixResourceNames(string unrelatedResourceName)
    {
        IReadOnlyDictionary<string, int> selectedPorts = new Dictionary<string, int>(StringComparer.Ordinal)
        {
            [ChatBotResourceName] = 43123,
            ["eventstore-admin"] = 43124,
        };
        InvalidOperationException exception = new(
            $"Resource '{unrelatedResourceName}' failed because its address is already in use.");

        TopologyFailureCorrelation.IsSelectedAddressInUse(exception, selectedPorts).ShouldBeFalse();
    }

    [Fact]
    public void TierThreeStartupRetryShouldNotCombineEvidenceAcrossAggregateBranches()
    {
        IReadOnlyDictionary<string, int> selectedPorts = new Dictionary<string, int>(StringComparer.Ordinal)
        {
            [EventStoreResourceName] = 43123,
        };
        AggregateException exception = new(
            new SocketException((int)SocketError.AddressAlreadyInUse),
            new InvalidOperationException("Selected endpoint port 43123 failed."));

        TopologyFailureCorrelation.IsSelectedAddressInUse(exception, selectedPorts).ShouldBeFalse();
    }

    [Fact]
    public void TierThreeStartupRetryShouldCorrelateExactPortOnTheSameExceptionBranch()
    {
        IReadOnlyDictionary<string, int> selectedPorts = new Dictionary<string, int>(StringComparer.Ordinal)
        {
            [EventStoreResourceName] = 43123,
        };
        InvalidOperationException exception = new(
            "Selected endpoint port 43123 failed.",
            new SocketException((int)SocketError.AddressAlreadyInUse));

        TopologyFailureCorrelation.IsSelectedAddressInUse(exception, selectedPorts).ShouldBeTrue();
    }

    [Theory]
    [InlineData("Selected endpoint failed without a reported port.")]
    [InlineData("Selected endpoint port 43124 failed.")]
    public void TierThreeStartupRetryShouldRejectMissingOrWrongSelectedPortEvidence(string message)
    {
        IReadOnlyDictionary<string, int> selectedPorts = new Dictionary<string, int>(StringComparer.Ordinal)
        {
            [EventStoreResourceName] = 43123,
        };
        InvalidOperationException exception = new(
            message,
            new SocketException((int)SocketError.AddressAlreadyInUse));

        TopologyFailureCorrelation.IsSelectedAddressInUse(exception, selectedPorts).ShouldBeFalse();
    }

    [Fact]
    public async Task TierThreeStartupRetryShouldNeverReclassifyUnrelatedAddressFailures()
    {
        IReadOnlyDictionary<string, int> selectedPorts = new Dictionary<string, int>(StringComparer.Ordinal)
        {
            [EventStoreResourceName] = 43123,
        };
        InvalidOperationException original = new(
            "A different child failed.",
            new SocketException((int)SocketError.AddressAlreadyInUse));
        int attempts = 0;

        InvalidOperationException thrown = await Should.ThrowAsync<InvalidOperationException>(() =>
            TopologyStartupOrchestrator.StartAsync(
                (_, _) => Task.FromResult(++attempts),
                (_, _) => Task.CompletedTask,
                (_, _) => throw original,
                (_, exception) => TopologyFailureCorrelation.IsSelectedAddressInUse(exception, selectedPorts),
                _ => ValueTask.CompletedTask,
                TestContext.Current.CancellationToken)).ConfigureAwait(true);

        thrown.ShouldBeSameAs(original);
        attempts.ShouldBe(1);
    }

    [Fact]
    public async Task TierThreeStartupCleanupFailureShouldPreserveTheOriginalStartupError()
    {
        InvalidOperationException original = new("Application startup failed.");
        InvalidOperationException cleanup = new("Cleanup failed.");

        InvalidOperationException thrown = await Should.ThrowAsync<InvalidOperationException>(() =>
            TopologyStartupOrchestrator.StartAsync(
                (_, _) => Task.FromResult(new object()),
                (_, _) => throw original,
                (_, _) => Task.CompletedTask,
                (_, _) => false,
                _ => ValueTask.FromException(cleanup),
                TestContext.Current.CancellationToken)).ConfigureAwait(true);

        thrown.ShouldBeSameAs(original);
        thrown.Data["TopologyCleanupException"].ShouldBeSameAs(cleanup);
    }

    [Fact]
    public async Task TierThreeStartupRetryClassifierFailureShouldDisposeAndPreserveTheOriginalStartupError()
    {
        InvalidOperationException original = new("Application startup failed.");
        InvalidOperationException classification = new("Retry classification failed.");
        bool disposed = false;

        InvalidOperationException thrown = await Should.ThrowAsync<InvalidOperationException>(() =>
            TopologyStartupOrchestrator.StartAsync(
                (_, _) => Task.FromResult(new object()),
                (_, _) => throw original,
                (_, _) => Task.CompletedTask,
                (_, _) => throw classification,
                _ =>
                {
                    disposed = true;
                    return ValueTask.CompletedTask;
                },
                TestContext.Current.CancellationToken)).ConfigureAwait(true);

        disposed.ShouldBeTrue();
        thrown.ShouldBeSameAs(original);
        thrown.Data["TopologyRetryClassificationException"].ShouldBeSameAs(classification);
    }

    [Fact]
    public async Task TierThreeParallelValidationShouldDetectALaterTerminalResourcePromptly()
    {
        IReadOnlyDictionary<string, int> selectedPorts = FourSelectedPorts();
        int canceledSiblings = 0;
        Stopwatch stopwatch = Stopwatch.StartNew();

        InvalidOperationException thrown = await Should.ThrowAsync<InvalidOperationException>(() =>
            TopologyReadinessCoordinator.ValidateAsync(
                selectedPorts,
                async (resourceName, _, token) =>
                {
                    if (string.Equals(resourceName, "eventstore-admin", StringComparison.Ordinal))
                    {
                        await Task.Delay(TimeSpan.FromMilliseconds(50), token).ConfigureAwait(false);
                        throw new InvalidOperationException("The later selected resource became terminal.");
                    }

                    try
                    {
                        await Task.Delay(Timeout.InfiniteTimeSpan, token).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException) when (token.IsCancellationRequested)
                    {
                        Interlocked.Increment(ref canceledSiblings);
                        throw;
                    }
                },
                static (_, _, _) => Task.CompletedTask,
                TestContext.Current.CancellationToken)).ConfigureAwait(true);

        stopwatch.Stop();
        thrown.Message.ShouldContain("later selected resource became terminal", Case.Sensitive);
        canceledSiblings.ShouldBe(3);
        stopwatch.Elapsed.ShouldBeLessThan(TimeSpan.FromSeconds(2));
    }

    [Fact]
    public async Task TierThreeParallelValidationShouldRetainEveryNonCancellationSiblingFailure()
    {
        IReadOnlyDictionary<string, int> selectedPorts = FourSelectedPorts();
        TaskCompletionSource allEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        int entered = 0;

        InvalidOperationException thrown = await Should.ThrowAsync<InvalidOperationException>(() =>
            TopologyReadinessCoordinator.ValidateAsync(
                selectedPorts,
                async (resourceName, _, token) =>
                {
                    if (Interlocked.Increment(ref entered) == selectedPorts.Count)
                    {
                        allEntered.TrySetResult();
                    }

                    await allEntered.Task.WaitAsync(token).ConfigureAwait(false);
                    throw new InvalidOperationException($"{resourceName} failed concurrently.");
                },
                static (_, _, _) => Task.CompletedTask,
                TestContext.Current.CancellationToken)).ConfigureAwait(true);

        AggregateException siblingFailures = thrown.Data["TopologySiblingValidationExceptions"]
            .ShouldBeOfType<AggregateException>();
        siblingFailures.InnerExceptions.Count.ShouldBe(selectedPorts.Count - 1);
        new[] { thrown.Message }
            .Concat(siblingFailures.InnerExceptions.Select(static failure => failure.Message))
            .ShouldBe(
                selectedPorts.Keys.Select(static resourceName => $"{resourceName} failed concurrently."),
                ignoreOrder: true);
    }

    [Fact]
    public async Task TierThreeFinalRecheckShouldEnterAllFourResourcesBeforeAnyCompletes()
    {
        IReadOnlyDictionary<string, int> selectedPorts = FourSelectedPorts();
        TaskCompletionSource allEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        int entered = 0;

        await TopologyReadinessCoordinator.ValidateAsync(
            selectedPorts,
            static (_, _, _) => Task.CompletedTask,
            async (_, _, token) =>
            {
                if (Interlocked.Increment(ref entered) == selectedPorts.Count)
                {
                    allEntered.TrySetResult();
                }

                await allEntered.Task.WaitAsync(token).ConfigureAwait(false);
            },
            TestContext.Current.CancellationToken).ConfigureAwait(true);

        entered.ShouldBe(selectedPorts.Count);
    }

    [Fact]
    public async Task TierThreeRunningPublishedEndpointShouldNotBeReadyWithoutATcpListener()
    {
        using Socket boundButNotListening = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        boundButNotListening.Bind(new IPEndPoint(IPAddress.Loopback, 0));
        int port = ((IPEndPoint)boundButNotListening.LocalEndPoint!).Port;
        Uri endpoint = new($"http://127.0.0.1:{port}");
        ResourceEvent resourceEvent = CreateResourceEvent(
            EventStoreResourceName,
            KnownResourceStates.Running,
            endpoint);

        HasPublishedRunningHttpEndpoint(resourceEvent).ShouldBeTrue();
        GetExactAssignedHttpEndpoint(EventStoreResourceName, port, resourceEvent).ShouldBe(endpoint);
        (await CanConnectToAssignedEndpointAsync(
            endpoint,
            port,
            TestContext.Current.CancellationToken).ConfigureAwait(true)).ShouldBeFalse();
    }

    [Fact]
    public void TierThreeNamedHttpEndpointShouldRejectANonHttpRuntimeUri()
    {
        const int selectedPort = 43123;
        ResourceEvent resourceEvent = CreateResourceEvent(
            EventStoreResourceName,
            KnownResourceStates.Running,
            new Uri($"https://127.0.0.1:{selectedPort}"));

        InvalidOperationException exception = Should.Throw<InvalidOperationException>(() =>
            GetExactAssignedHttpEndpoint(EventStoreResourceName, selectedPort, resourceEvent));
        exception.Message.ShouldContain("https://", Case.Sensitive);
    }

    [Fact]
    public async Task TierThreeFinalTcpSuccessShouldBeFollowedByACurrentSnapshotRevalidation()
    {
        const int selectedPort = 43123;
        Uri endpoint = new($"http://127.0.0.1:{selectedPort}");
        Queue<ResourceEvent> currentStates = new(
        [
            CreateResourceEvent(EventStoreResourceName, KnownResourceStates.Running, endpoint),
            CreateResourceEvent(EventStoreResourceName, KnownResourceStates.FailedToStart, endpoint),
        ]);
        int probeCalls = 0;

        _ = await Should.ThrowAsync<SelectedEndpointStartupException>(() =>
            RecheckSelectedResourceAsync(
                EventStoreResourceName,
                selectedPort,
                currentStates.Dequeue,
                (_, _, _) =>
                {
                    probeCalls++;
                    return Task.FromResult(result: true);
                },
                TestContext.Current.CancellationToken)).ConfigureAwait(true);

        probeCalls.ShouldBe(1);
        currentStates.ShouldBeEmpty("The final state must be reacquired immediately after the successful TCP probe.");
    }

    [Fact]
    public async Task TierThreeTerminalLogWatcherShouldReturnOnEvidenceWhileTheStreamRemainsOpen()
    {
        const int selectedPort = 43123;
        TaskCompletionSource streamEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource publishFinalLine = new(TaskCreationOptions.RunContinuationsAsynchronously);
        SelectedResourceLogWatcher watcher = await SelectedResourceLogWatcher.StartAsync(
            DelayedTerminalLogBatchesAsync(
                streamEntered,
                publishFinalLine,
                selectedPort,
                TestContext.Current.CancellationToken),
            selectedPort,
            TestContext.Current.CancellationToken).ConfigureAwait(true);
        try
        {
            streamEntered.Task.IsCompletedSuccessfully.ShouldBeTrue("The exact-resource watch must be active before readiness begins.");
            publishFinalLine.TrySetResult();
            Stopwatch stopwatch = Stopwatch.StartNew();
            bool hasEvidence = await watcher
                .WaitForEvidenceOrCompletionAsync(TestContext.Current.CancellationToken)
                .ConfigureAwait(true);
            stopwatch.Stop();

            hasEvidence.ShouldBeTrue();
            watcher.HasCorrelatedBindEvidence.ShouldBeTrue();
            stopwatch.Elapsed.ShouldBeLessThan(TimeSpan.FromSeconds(2));
        }
        finally
        {
            await watcher.DisposeAsync().ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task TierThreeTerminalLogWatcherShouldPreserveAStreamFaultCapturedAfterEvidence()
    {
        const int selectedPort = 43123;
        InvalidOperationException streamFailure = new("The exact-resource log stream failed after evidence.");
        TaskCompletionSource faultReached = new(TaskCreationOptions.RunContinuationsAsynchronously);
        SelectedResourceLogWatcher watcher = await SelectedResourceLogWatcher.StartAsync(
            EvidenceThenFaultLogBatchesAsync(
                selectedPort,
                streamFailure,
                faultReached,
                TestContext.Current.CancellationToken),
            selectedPort,
            TestContext.Current.CancellationToken).ConfigureAwait(true);

        (await watcher
            .WaitForEvidenceOrCompletionAsync(TestContext.Current.CancellationToken)
            .ConfigureAwait(true)).ShouldBeTrue();
        await faultReached.Task.WaitAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);
        InvalidOperationException thrown = await Should.ThrowAsync<InvalidOperationException>(
            () => watcher.DisposeAsync().AsTask()).ConfigureAwait(true);

        thrown.ShouldBeSameAs(streamFailure);
        watcher.HasCorrelatedBindEvidence.ShouldBeTrue();
    }

    [Theory]
    [InlineData("listen tcp 0.0.0.0:43123: address already in use", true)]
    [InlineData("listen tcp 0.0.0.0:43124: address already in use", false)]
    [InlineData("listen tcp: address already in use", false)]
    [InlineData("Port 43123: An attempt was made to access a socket in a way forbidden by its access permissions", true)]
    public void TierThreeTerminalLogCorrelationShouldRequireBindAndExactPortOnOneLine(
        string line,
        bool expected)
        => TopologyFailureCorrelation.IsCorrelatedLogLine(line, 43123).ShouldBe(expected);

    [Fact]
    public async Task TierThreeStartupCancellationShouldNeverRetry()
    {
        OperationCanceledException cancellation = new("Port 43123 failed with EADDRINUSE during cancellation.");
        int attempts = 0;
        int classificationCalls = 0;
        int disposals = 0;

        _ = await Should.ThrowAsync<OperationCanceledException>(() =>
            TopologyStartupOrchestrator.StartAsync(
                (_, _) => Task.FromResult(++attempts),
                (_, _) => throw cancellation,
                (_, _) => Task.CompletedTask,
                (_, _) =>
                {
                    classificationCalls++;
                    return true;
                },
                _ =>
                {
                    disposals++;
                    return ValueTask.CompletedTask;
                },
                TestContext.Current.CancellationToken)).ConfigureAwait(true);

        attempts.ShouldBe(1);
        classificationCalls.ShouldBe(0);
        disposals.ShouldBe(1);
        TopologyFailureCorrelation.IsSelectedAddressInUse(
            cancellation,
            new Dictionary<string, int> { [EventStoreResourceName] = 43123 }).ShouldBeFalse();
    }

    [Theory]
    [InlineData("before-classification", 0)]
    [InlineData("during-classification", 1)]
    [InlineData("during-cleanup", 1)]
    public async Task TierThreeCallerCancellationAtAnyRetryBoundaryShouldPreventASecondAttempt(
        string cancellationStage,
        int expectedClassificationCalls)
    {
        using CancellationTokenSource cancellationSource = CancellationTokenSource.CreateLinkedTokenSource(
            TestContext.Current.CancellationToken);
        InvalidOperationException original = new("The first attempt failed with otherwise retryable evidence.");
        int attempts = 0;
        int classificationCalls = 0;
        int disposals = 0;

        InvalidOperationException thrown = await Should.ThrowAsync<InvalidOperationException>(() =>
            TopologyStartupOrchestrator.StartAsync(
                (_, _) => Task.FromResult(++attempts),
                (_, _) => Task.CompletedTask,
                (_, _) =>
                {
                    if (string.Equals(cancellationStage, "before-classification", StringComparison.Ordinal))
                    {
                        cancellationSource.Cancel();
                    }

                    throw original;
                },
                (_, _) =>
                {
                    classificationCalls++;
                    if (string.Equals(cancellationStage, "during-classification", StringComparison.Ordinal))
                    {
                        cancellationSource.Cancel();
                    }

                    return true;
                },
                _ =>
                {
                    disposals++;
                    if (string.Equals(cancellationStage, "during-cleanup", StringComparison.Ordinal))
                    {
                        cancellationSource.Cancel();
                    }

                    return ValueTask.CompletedTask;
                },
                cancellationSource.Token)).ConfigureAwait(true);

        thrown.ShouldBeSameAs(original);
        attempts.ShouldBe(1);
        classificationCalls.ShouldBe(expectedClassificationCalls);
        disposals.ShouldBe(1);
    }

    [Fact]
    public async Task TierThreeSecondAttemptCreationFailureShouldRetainTheFirstCorrelatedFailure()
    {
        InvalidOperationException first = new("First correlated startup failure.");
        InvalidOperationException second = new("Second attempt creation failed.");
        int disposals = 0;

        InvalidOperationException thrown = await Should.ThrowAsync<InvalidOperationException>(() =>
            TopologyStartupOrchestrator.StartAsync(
                (attemptNumber, _) => attemptNumber == 1
                    ? Task.FromResult(attemptNumber)
                    : Task.FromException<int>(second),
                static (_, _) => Task.CompletedTask,
                (_, _) => throw first,
                (_, exception) => ReferenceEquals(exception, first),
                _ =>
                {
                    disposals++;
                    return ValueTask.CompletedTask;
                },
                TestContext.Current.CancellationToken)).ConfigureAwait(true);

        thrown.ShouldBeSameAs(second);
        thrown.Data["TopologyFirstAttemptException"].ShouldBeSameAs(first);
        disposals.ShouldBe(1);
    }

    [Fact]
    public async Task TierThreeSecondAttemptStartFailureShouldRetainTheFirstCorrelatedFailureAndDisposeBothAttempts()
    {
        InvalidOperationException first = new("First correlated startup failure.");
        InvalidOperationException second = new("Second attempt start failed.");
        List<int> disposals = [];

        InvalidOperationException thrown = await Should.ThrowAsync<InvalidOperationException>(() =>
            TopologyStartupOrchestrator.StartAsync(
                static (attemptNumber, _) => Task.FromResult(attemptNumber),
                (attemptNumber, _) => attemptNumber == 2 ? throw second : Task.CompletedTask,
                (attemptNumber, _) => attemptNumber == 1 ? throw first : Task.CompletedTask,
                (_, exception) => ReferenceEquals(exception, first),
                attemptNumber =>
                {
                    disposals.Add(attemptNumber);
                    return ValueTask.CompletedTask;
                },
                TestContext.Current.CancellationToken)).ConfigureAwait(true);

        thrown.ShouldBeSameAs(second);
        thrown.Data["TopologyFirstAttemptException"].ShouldBeSameAs(first);
        disposals.ShouldBe([1, 2], ignoreOrder: false);
    }

    private static IReadOnlyDictionary<string, int> FourSelectedPorts()
        => new Dictionary<string, int>(StringComparer.Ordinal)
        {
            [EventStoreResourceName] = 43121,
            [TenantsResourceName] = 43122,
            [ChatBotResourceName] = 43123,
            ["eventstore-admin"] = 43124,
        };

    private static void AssertWildcardPortIsHeld(int port)
    {
        SocketException collision = Should.Throw<SocketException>(() =>
        {
            using TcpListener competingListener = WildcardTcpListener.Start(port);
        });
        WildcardTcpListener.IsExclusiveBindCollision(collision.SocketErrorCode, OperatingSystem.IsWindows())
            .ShouldBeTrue();
    }

    private static ResourceEvent CreateResourceEvent(
        string resourceName,
        string state,
        Uri endpoint)
        => new(
            new ProjectResource(resourceName),
            resourceName,
            new CustomResourceSnapshot
            {
                ResourceType = "project",
                Properties = [],
                State = state,
                Urls = [new UrlSnapshot("http", endpoint.AbsoluteUri, IsInternal: false)],
            });

    private static async IAsyncEnumerable<IReadOnlyList<LogLine>> DelayedTerminalLogBatchesAsync(
        TaskCompletionSource streamEntered,
        TaskCompletionSource publishFinalLine,
        int selectedPort,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        streamEntered.TrySetResult();
        await publishFinalLine.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
        yield return
        [
            new LogLine(
                1,
                $"listen tcp 0.0.0.0:{selectedPort}: address already in use",
                IsErrorMessage: true),
        ];

        await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken).ConfigureAwait(false);
    }

    private static async IAsyncEnumerable<IReadOnlyList<LogLine>> EvidenceThenFaultLogBatchesAsync(
        int selectedPort,
        InvalidOperationException streamFailure,
        TaskCompletionSource faultReached,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask.ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        yield return
        [
            new LogLine(
                1,
                $"listen tcp 0.0.0.0:{selectedPort}: address already in use",
                IsErrorMessage: true),
        ];

        faultReached.TrySetResult();
        throw streamFailure;
    }

    private async Task<DistributedApplication> StartTestingApplicationAsync(CancellationToken cancellationToken)
    {
        TopologyStartupAttempt attempt = await TopologyStartupOrchestrator.StartAsync(
            CreateTopologyStartupAttemptAsync,
            StartTopologyAttemptAsync,
            ValidateTopologyAttemptAsync,
            static (currentAttempt, exception) => TopologyFailureCorrelation.IsSelectedAddressInUse(
                exception,
                currentAttempt.SelectedPorts),
            static currentAttempt => currentAttempt.DisposeAsync(),
            cancellationToken).ConfigureAwait(true);

        return attempt.Application;
    }

    private static async Task<TopologyStartupAttempt> CreateTopologyStartupAttemptAsync(
        int attemptNumber,
        CancellationToken cancellationToken)
    {
        IDistributedApplicationTestingBuilder builder = await DistributedApplicationTestingBuilder
            .CreateAsync<global::Projects.Hexalith_ChatBot_AppHost>(MailboxSecretArgs, cancellationToken)
            .ConfigureAwait(false);
        IReadOnlyList<ReservedEndpoint> selected = GetIsolatedDaprHttpEndpoints(builder);
        IReadOnlySet<int> unselectedConcretePorts = GetUnselectedConcreteEndpointPorts(builder, selected);
        PortReservationSet reservations = PortReservationSet.Reserve(
            IsolatedDaprHttpResourceNames.Length,
            unselectedConcretePorts);
        try
        {
            ConfigureReservedDaprHttpEndpoints(builder, reservations.Ports);
            ConfigureAcceptanceM2SweepCadence(builder);
            IReadOnlyDictionary<string, int> selectedPorts = selected
                .Select((endpoint, index) => new KeyValuePair<string, int>(endpoint.Resource.Name, reservations.Ports[index]))
                .ToDictionary(static pair => pair.Key, static pair => pair.Value, StringComparer.Ordinal);
            DistributedApplication application = await builder.BuildAsync(cancellationToken).ConfigureAwait(false);
            return new TopologyStartupAttempt(application, reservations, selectedPorts);
        }
        catch
        {
            reservations.Dispose();
            throw;
        }
    }

    private static async Task StartTopologyAttemptAsync(
        TopologyStartupAttempt attempt,
        CancellationToken cancellationToken)
    {
        attempt.Reservations.Release();
        await attempt.Application.StartAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task ValidateTopologyAttemptAsync(
        TopologyStartupAttempt attempt,
        CancellationToken cancellationToken)
    {
        using CancellationTokenSource deadlineSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadlineSource.CancelAfter(SelectedResourceValidationTimeout);
        Dictionary<string, SelectedResourceLogWatcher> logWatchers = new(StringComparer.Ordinal);
        Exception? validationFailure = null;
        try
        {
            ResourceLoggerService resourceLogger = attempt.Application.Services.GetRequiredService<ResourceLoggerService>();
            foreach ((string resourceName, int expectedPort) in attempt.SelectedPorts)
            {
                SelectedResourceLogWatcher watcher = await SelectedResourceLogWatcher.StartAsync(
                    resourceLogger.WatchAsync(resourceName),
                    expectedPort,
                    deadlineSource.Token).ConfigureAwait(false);
                logWatchers.Add(resourceName, watcher);
            }

            await TopologyReadinessCoordinator.ValidateAsync(
                attempt.SelectedPorts,
                (resourceName, expectedPort, token) => WaitForSelectedResourceReadyAsync(
                    attempt.Application,
                    resourceName,
                    expectedPort,
                    token),
                (resourceName, expectedPort, token) => RecheckSelectedResourceAsync(
                    attempt.Application,
                    resourceName,
                    expectedPort,
                    token),
                deadlineSource.Token).ConfigureAwait(false);

            _output.WriteLine(
                "ASPIRE_RESERVED_HTTP_PORT_EVIDENCE {0}",
                JsonSerializer.Serialize(attempt.SelectedPorts));
        }
        catch (Exception exception)
        {
            if (exception is SelectedEndpointStartupException selectedFailure
                && selectedFailure.IsTerminal
                && logWatchers.TryGetValue(selectedFailure.ResourceName, out SelectedResourceLogWatcher? watcher))
            {
                try
                {
                    bool hasCorrelatedBindEvidence = await watcher
                        .WaitForEvidenceOrCompletionAsync(deadlineSource.Token)
                        .ConfigureAwait(false);
                    selectedFailure.RecordCorrelatedBindEvidence(hasCorrelatedBindEvidence);
                }
                catch (Exception watcherException)
                {
                    selectedFailure.Data["TopologyLogEvidenceException"] = watcherException;
                }
            }

            validationFailure = exception;
        }

        Exception? watcherFailure = await DisposeLogWatchersAsync(logWatchers.Values).ConfigureAwait(false);
        if (watcherFailure is not null)
        {
            if (validationFailure is null)
            {
                validationFailure = watcherFailure;
            }
            else
            {
                validationFailure.Data["TopologyLogWatcherException"] = watcherFailure;
            }
        }

        if (validationFailure is not null)
        {
            ExceptionDispatchInfo.Capture(validationFailure).Throw();
        }
    }

    private static async Task WaitForSelectedResourceReadyAsync(
        DistributedApplication application,
        string resourceName,
        int expectedPort,
        CancellationToken cancellationToken)
    {
        using CancellationTokenSource failureSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        Task<ResourceEvent> failureTask = application.ResourceNotifications.WaitForResourceAsync(
            resourceName,
            IsSelectedResourceFailure,
            failureSource.Token);
        try
        {
            while (true)
            {
                ResourceEvent resourceEvent = await application.ResourceNotifications
                    .WaitForResourceAsync(
                        resourceName,
                        static current => IsSelectedResourceFailure(current) || HasPublishedRunningHttpEndpoint(current),
                        cancellationToken)
                    .ConfigureAwait(false);
                if (IsSelectedResourceFailure(resourceEvent))
                {
                    throw CreateSelectedResourceStateFailure(resourceName, expectedPort, resourceEvent);
                }

                Uri realizedEndpoint = GetExactAssignedHttpEndpoint(resourceName, expectedPort, resourceEvent);
                Task<bool> probeTask = CanConnectToAssignedEndpointAsync(
                    realizedEndpoint,
                    expectedPort,
                    cancellationToken);
                Task completed = await Task.WhenAny(failureTask, probeTask).ConfigureAwait(false);
                if (ReferenceEquals(completed, failureTask))
                {
                    ResourceEvent failureEvent = await failureTask.ConfigureAwait(false);
                    throw CreateSelectedResourceStateFailure(resourceName, expectedPort, failureEvent);
                }

                if (await probeTask.ConfigureAwait(false))
                {
                    return;
                }

                Task retryDelay = Task.Delay(PollInterval, cancellationToken);
                completed = await Task.WhenAny(failureTask, retryDelay).ConfigureAwait(false);
                if (ReferenceEquals(completed, failureTask))
                {
                    ResourceEvent failureEvent = await failureTask.ConfigureAwait(false);
                    throw CreateSelectedResourceStateFailure(resourceName, expectedPort, failureEvent);
                }

                await retryDelay.ConfigureAwait(false);
            }
        }
        finally
        {
            await failureSource.CancelAsync().ConfigureAwait(false);
            try
            {
                await failureTask.ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (failureSource.IsCancellationRequested)
            {
                // The resource became ready or sibling validation ended, so its failure observer is no longer needed.
            }
            catch (Exception)
            {
                // A completed observer failure has already won a readiness race and been surfaced above.
            }
        }
    }

    private static async Task RecheckSelectedResourceAsync(
        DistributedApplication application,
        string resourceName,
        int expectedPort,
        CancellationToken cancellationToken)
        => await RecheckSelectedResourceAsync(
            resourceName,
            expectedPort,
            () => GetCurrentSelectedResource(application, resourceName),
            CanConnectToAssignedEndpointAsync,
            cancellationToken).ConfigureAwait(false);

    private static async Task RecheckSelectedResourceAsync(
        string resourceName,
        int expectedPort,
        Func<ResourceEvent> getCurrentResource,
        Func<Uri, int, CancellationToken, Task<bool>> canConnectAsync,
        CancellationToken cancellationToken)
    {
        ResourceEvent resourceEvent = getCurrentResource();
        Uri realizedEndpoint = ValidateSelectedResourceSnapshot(resourceName, expectedPort, resourceEvent);
        if (!await canConnectAsync(
            realizedEndpoint,
            expectedPort,
            cancellationToken).ConfigureAwait(false))
        {
            throw new InvalidOperationException(
                $"Selected resource '{resourceName}/http' stopped accepting TCP connections on assigned port {expectedPort} "
                + "during final readiness recheck.");
        }

        _ = ValidateSelectedResourceSnapshot(resourceName, expectedPort, getCurrentResource());
    }

    private static bool HasPublishedRunningHttpEndpoint(ResourceEvent resourceEvent)
        => string.Equals(resourceEvent.Snapshot.State?.Text, KnownResourceStates.Running, StringComparison.Ordinal)
            && resourceEvent.Snapshot.HealthStatus is null or HealthStatus.Healthy
            && resourceEvent.Snapshot.Urls.Any(
                static url => !url.IsInactive && string.Equals(url.Name, "http", StringComparison.Ordinal));

    private static Uri GetExactAssignedHttpEndpoint(
        string resourceName,
        int expectedPort,
        ResourceEvent resourceEvent)
    {
        if (!string.Equals(resourceEvent.Snapshot.State?.Text, KnownResourceStates.Running, StringComparison.Ordinal)
            || resourceEvent.Snapshot.HealthStatus is not (null or HealthStatus.Healthy))
        {
            throw new InvalidOperationException(
                $"Selected resource '{resourceName}' was not Running and healthy during readiness validation.");
        }

        UrlSnapshot[] activeHttpEndpoints = resourceEvent.Snapshot.Urls
            .Where(static url => !url.IsInactive && string.Equals(url.Name, "http", StringComparison.Ordinal))
            .ToArray();
        if (activeHttpEndpoints.Length != 1
            || !Uri.TryCreate(activeHttpEndpoints[0].Url, UriKind.Absolute, out Uri? realizedEndpoint)
            || !string.Equals(realizedEndpoint.Scheme, Uri.UriSchemeHttp, StringComparison.Ordinal)
            || realizedEndpoint.Port != expectedPort)
        {
            string realized = string.Join(", ", activeHttpEndpoints.Select(static endpoint => endpoint.Url));
            throw new InvalidOperationException(
                $"Selected resource '{resourceName}/http' was assigned port {expectedPort}, but realized '{realized}'.");
        }

        return realizedEndpoint;
    }

    private static ResourceEvent GetCurrentSelectedResource(
        DistributedApplication application,
        string resourceName)
    {
        if (!application.ResourceNotifications.TryGetCurrentState(resourceName, out ResourceEvent? resourceEvent)
            || resourceEvent is null)
        {
            throw new InvalidOperationException(
                $"Selected resource '{resourceName}' had no current Aspire state during final readiness recheck.");
        }

        return resourceEvent;
    }

    private static Uri ValidateSelectedResourceSnapshot(
        string resourceName,
        int expectedPort,
        ResourceEvent resourceEvent)
    {
        if (IsSelectedResourceFailure(resourceEvent))
        {
            throw CreateSelectedResourceStateFailure(resourceName, expectedPort, resourceEvent);
        }

        return GetExactAssignedHttpEndpoint(resourceName, expectedPort, resourceEvent);
    }

    private static async Task<bool> CanConnectToAssignedEndpointAsync(
        Uri endpoint,
        int expectedPort,
        CancellationToken cancellationToken)
    {
        if (endpoint.Port != expectedPort)
        {
            return false;
        }

        using CancellationTokenSource probeSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        probeSource.CancelAfter(PollInterval);
        using TcpClient client = new();
        try
        {
            await client.ConnectAsync(endpoint.DnsSafeHost, endpoint.Port, probeSource.Token).ConfigureAwait(false);
            return client.Connected;
        }
        catch (SocketException)
        {
            return false;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return false;
        }
    }

    private static bool IsTerminal(ResourceEvent resourceEvent)
        => resourceEvent.Snapshot.State?.Text is string state
            && KnownResourceStates.TerminalStates.Contains(state, StringComparer.Ordinal);

    private static bool IsSelectedResourceFailure(ResourceEvent resourceEvent)
        => IsTerminal(resourceEvent)
            || string.Equals(
                resourceEvent.Snapshot.State?.Text,
                KnownResourceStates.RuntimeUnhealthy,
                StringComparison.Ordinal)
            || resourceEvent.Snapshot.HealthStatus == HealthStatus.Unhealthy;

    private static SelectedEndpointStartupException CreateSelectedResourceStateFailure(
        string resourceName,
        int expectedPort,
        ResourceEvent resourceEvent)
        => new(
            resourceName,
            expectedPort,
            resourceEvent.Snapshot.State?.Text,
            resourceEvent.Snapshot.HealthStatus?.ToString(),
            IsTerminal(resourceEvent));

    private static async Task<Exception?> DisposeLogWatchersAsync(
        IEnumerable<SelectedResourceLogWatcher> watchers)
    {
        Task[] shutdownTasks = watchers.Select(static watcher => watcher.DisposeAsync().AsTask()).ToArray();
        List<Exception> failures = [];
        foreach (Task shutdownTask in shutdownTasks)
        {
            try
            {
                await shutdownTask.ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                failures.Add(exception);
            }
        }

        return failures.Count switch
        {
            0 => null,
            1 => failures[0],
            _ => new AggregateException(failures),
        };
    }

    /// <summary>
    /// Shortens the M2 sweep cadence for the acceptance topology only.
    /// </summary>
    /// <remarks>
    /// The production cadence is nightly, which is correct for a long-lived deployment and useless for a topology that
    /// lives for minutes: the first sweep fires at startup against empty stores, commits its partition, and never runs
    /// again inside the test. Overriding here rather than in <c>AppHost/Program.cs</c> keeps the shipped default
    /// nightly — changing the AppHost would change real deployments, since Aspire generates deployment manifests from
    /// it. The values satisfy the runtime's own cross-field validation (retry &lt; cadence, timeout ≤ cadence), so a
    /// bad choice here fails fast at startup instead of silently disabling the gate.
    /// </remarks>
    private static void ConfigureAcceptanceM2SweepCadence(IDistributedApplicationTestingBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        IResource chatBot = builder.Resources.Single(resource =>
            string.Equals(resource.Name, ChatBotResourceName, StringComparison.Ordinal));

        // Appended last, so it wins over the AppHost's own environment callbacks.
        chatBot.Annotations.Add(new EnvironmentCallbackAnnotation(context =>
        {
            context.EnvironmentVariables["ChatBot__PeriodicEnforcement__M2SweepCadence"] = "00:02:00";
            context.EnvironmentVariables["ChatBot__PeriodicEnforcement__M2SweepRetryAfter"] = "00:00:15";
            context.EnvironmentVariables["ChatBot__PeriodicEnforcement__M2SweepTimeout"] = "00:01:00";
            context.EnvironmentVariables["ChatBot__PeriodicEnforcement__MissedCadenceAlertAfter"] = "00:01:00";
        }));
    }

    private static void ConfigureReservedDaprHttpEndpoints(
        IDistributedApplicationTestingBuilder builder,
        IReadOnlyList<int> ports)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(ports);
        if (ports.Count != IsolatedDaprHttpResourceNames.Length
            || ports.Any(static port => port is <= IPEndPoint.MinPort or > IPEndPoint.MaxPort)
            || ports.Distinct().Count() != ports.Count)
        {
            throw new ArgumentException(
                $"Exactly {IsolatedDaprHttpResourceNames.Length} distinct valid reserved ports are required.",
                nameof(ports));
        }

        IReadOnlyList<ReservedEndpoint> endpoints = GetIsolatedDaprHttpEndpoints(builder);
        IReadOnlySet<int> unselectedConcretePorts = GetUnselectedConcreteEndpointPorts(builder, endpoints);
        int[] overlaps = ports.Where(unselectedConcretePorts.Contains).Distinct().Order().ToArray();
        if (overlaps.Length > 0)
        {
            throw new InvalidOperationException(
                $"Selected reservations overlap concrete unselected endpoint ports: {string.Join(", ", overlaps)}.");
        }

        for (int index = 0; index < endpoints.Count; index++)
        {
            EndpointAnnotation endpoint = endpoints[index].Endpoint;
            endpoint.Port = ports[index];
            endpoint.TargetPort = ports[index];
        }
    }

    private static IReadOnlyList<ReservedEndpoint> GetIsolatedDaprHttpEndpoints(
        IDistributedApplicationTestingBuilder builder)
    {
        List<ReservedEndpoint> selected = [];
        foreach (string resourceName in IsolatedDaprHttpResourceNames)
        {
            ProjectResource[] resources = builder.Resources
                .OfType<ProjectResource>()
                .Where(resource => string.Equals(resource.Name, resourceName, StringComparison.Ordinal))
                .ToArray();
            if (resources.Length != 1)
            {
                throw new InvalidOperationException(
                    $"Expected exactly one project resource named '{resourceName}', but found {resources.Length}.");
            }

            ProjectResource resource = resources[0];
            int sidecarCount = resource.Annotations.OfType<DaprSidecarAnnotation>().Count();
            if (sidecarCount != 1)
            {
                throw new InvalidOperationException(
                    $"Expected project resource '{resourceName}' to have exactly one DAPR sidecar, but found {sidecarCount}.");
            }

            EndpointAnnotation[] endpoints = resource.Annotations
                .OfType<EndpointAnnotation>()
                .Where(static endpoint => string.Equals(endpoint.Name, "http", StringComparison.Ordinal))
                .ToArray();
            if (endpoints.Length != 1)
            {
                throw new InvalidOperationException(
                    $"Expected project resource '{resourceName}' to have exactly one 'http' endpoint, but found {endpoints.Length}.");
            }

            EndpointAnnotation endpoint = endpoints[0];
            if (endpoint.Protocol != ProtocolType.Tcp
                || !string.Equals(endpoint.UriScheme, "http", StringComparison.Ordinal)
                || endpoint.IsProxied)
            {
                throw new InvalidOperationException(
                    $"The isolated endpoint '{resourceName}/http' must remain a proxyless HTTP-over-TCP endpoint.");
            }

            selected.Add(new ReservedEndpoint(resource, endpoint));
        }

        return selected;
    }

    private static IReadOnlySet<int> GetUnselectedConcreteEndpointPorts(
        IDistributedApplicationTestingBuilder builder,
        IReadOnlyList<ReservedEndpoint> selectedEndpoints)
    {
        HashSet<int> ports = [];
        foreach (IResource resource in builder.Resources)
        {
            foreach (EndpointAnnotation endpoint in resource.Annotations.OfType<EndpointAnnotation>())
            {
                if (selectedEndpoints.Any(selected => ReferenceEquals(selected.Endpoint, endpoint)))
                {
                    continue;
                }

                if (endpoint.Port is > 0)
                {
                    ports.Add(endpoint.Port.Value);
                }

                if (endpoint.TargetPort is > 0)
                {
                    ports.Add(endpoint.TargetPort.Value);
                }
            }
        }

        return ports;
    }

    private static EndpointSnapshot[] CaptureEndpointSnapshots(IDistributedApplicationTestingBuilder builder)
    {
        List<EndpointSnapshot> snapshots = [];
        foreach (IResource resource in builder.Resources.OrderBy(static resource => resource.Name, StringComparer.Ordinal))
        {
            EndpointAnnotation[] endpoints = resource.Annotations.OfType<EndpointAnnotation>().ToArray();
            for (int index = 0; index < endpoints.Length; index++)
            {
                snapshots.Add(SnapshotEndpoint(resource, endpoints[index], index));
            }
        }

        return snapshots.ToArray();
    }

    private static EndpointSnapshot SnapshotEndpoint(
        IResource resource,
        EndpointAnnotation endpoint,
        int endpointIndex = 0)
        => new(
            resource.Name,
            resource.GetType().FullName ?? resource.GetType().Name,
            endpointIndex,
            endpoint.Name,
            endpoint.Protocol.ToString(),
            endpoint.UriScheme,
            endpoint.Transport,
            endpoint.Port,
            endpoint.TargetPort,
            endpoint.IsExternal,
            endpoint.IsProxied,
            endpoint.IsExplicitlyProxied,
            endpoint.TargetHost,
            endpoint.TlsEnabled,
            endpoint.ExcludeReferenceEndpoint);

    private static bool IsReservedEndpoint(EndpointSnapshot snapshot)
        => IsolatedDaprHttpResourceNames.Contains(snapshot.ResourceName, StringComparer.Ordinal)
            && string.Equals(snapshot.EndpointName, "http", StringComparison.Ordinal);

    // The origin-free derived-record fields of a projected view (provenance/derivation/redaction/retention/
    // schema + source version), excluding the per-note id and per-run timestamps, rendered for cross-origin
    // equality.
    private static string DerivedRecordShape(JsonElement view)
    {
        return string.Join(
            "|",
            view.GetProperty("schemaVersion").ToString(),
            view.GetProperty("sourceProvenance").ToString(),
            view.GetProperty("derivationKernelVersion").ToString(),
            view.GetProperty("redactionState").ToString(),
            view.GetProperty("retentionClass").ToString(),
            view.GetProperty("sourceVersion").ToString());
    }

    private async Task WaitForAndRecordRequiredTopologyAsync(DistributedApplication app, CancellationToken cancellationToken)
    {
        Dictionary<string, int> isolatedHttpPorts = new(StringComparer.Ordinal);
        foreach (string resource in RequiredTopologyResources)
        {
            ResourceEvent resourceEvent = await app.ResourceNotifications
                .WaitForResourceHealthyAsync(resource, cancellationToken)
                .WaitAsync(TimeSpan.FromMinutes(5), cancellationToken)
                .ConfigureAwait(true);

            string[] endpoints = resourceEvent.Snapshot.Urls
                .Where(static url => !url.IsInactive)
                .Select(static url => $"{url.Name ?? "endpoint"}={url.Url}")
                .ToArray();
            _output.WriteLine(
                "ASPIRE_RESOURCE_EVIDENCE {0}",
                JsonSerializer.Serialize(new
                {
                    resource,
                    state = resourceEvent.Snapshot.State?.Text,
                    health = resourceEvent.Snapshot.HealthStatus?.ToString(),
                    endpoints,
                }));

            resourceEvent.Snapshot.State.ShouldNotBeNull($"{resource} must expose its actual runtime state.");
            if (string.Equals(resource, ChatBotResourceName, StringComparison.Ordinal))
            {
                endpoints.ShouldNotBeEmpty("The chatbot must expose an externally reachable runtime endpoint.");
            }

            if (IsolatedDaprHttpResourceNames.Contains(resource, StringComparer.Ordinal))
            {
                var activeHttpEndpoints = resourceEvent.Snapshot.Urls
                    .Where(static url => !url.IsInactive && string.Equals(url.Name, "http", StringComparison.Ordinal))
                    .ToArray();
                activeHttpEndpoints.Length.ShouldBe(1, $"{resource} must expose exactly one active HTTP endpoint.");
                Uri.TryCreate(activeHttpEndpoints[0].Url, UriKind.Absolute, out Uri? endpointUri).ShouldBeTrue(
                    $"{resource}/http must expose an absolute runtime URI.");
                endpointUri.ShouldNotBeNull();
                endpointUri.Port.ShouldBeGreaterThan(0);
                isolatedHttpPorts.Add(resource, endpointUri.Port);
            }
        }

        isolatedHttpPorts.Keys.ShouldBe(IsolatedDaprHttpResourceNames, ignoreOrder: true);
        isolatedHttpPorts.Values.Distinct().Count().ShouldBe(
            IsolatedDaprHttpResourceNames.Length,
            "Every selected sidecar-backed project must run on its own concrete HTTP port.");
        _output.WriteLine("ASPIRE_RESERVED_HTTP_PORT_EVIDENCE {0}", JsonSerializer.Serialize(isolatedHttpPorts));
    }

    private static async Task<HttpResponseMessage> SubmitUnauthenticatedGovernedNoteAsync(
        HttpClient client,
        string noteId,
        string commandId,
        string taskId,
        string correlationId,
        CancellationToken cancellationToken)
    {
        using HttpRequestMessage request = new(HttpMethod.Post, "/api/v1/commands")
        {
            Content = new StringContent(RecordGovernedNoteBody(noteId, commandId), Encoding.UTF8, "application/json"),
        };
        request.Headers.Add("X-Correlation-Id", correlationId);
        request.Headers.Add("X-Hexalith-Task-Id", taskId);
        return await client.SendAsync(request, cancellationToken).ConfigureAwait(false);
    }

    private static async Task AssertNoDurableStateWasCreatedAsync(
        HttpClient client,
        string accessToken,
        string noteId,
        string taskId,
        string correlationId,
        CancellationToken cancellationToken)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        while (stopwatch.Elapsed < ProjectionStabilityWindow)
        {
            using HttpResponseMessage view = await GetAuthorizedAsync(
                client,
                accessToken,
                $"/api/v1/governed-operations/{noteId}",
                correlationId,
                cancellationToken).ConfigureAwait(false);
            view.StatusCode.ShouldNotBe(HttpStatusCode.OK, "An unauthenticated command must not create a durable projection.");

            using HttpResponseMessage status = await GetAuthorizedAsync(
                client,
                accessToken,
                $"/api/v1/operations/{taskId}",
                correlationId,
                cancellationToken).ConfigureAwait(false);
            status.StatusCode.ShouldNotBe(HttpStatusCode.OK, "An unauthenticated command must not create durable operation status.");
            await Task.Delay(PollInterval, cancellationToken).ConfigureAwait(false);
        }
    }

    private static async Task AssertGovernedOperationViewRemainsStableAsync(
        HttpClient client,
        string accessToken,
        string noteId,
        string correlationId,
        string expectedBody,
        CancellationToken cancellationToken)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        while (stopwatch.Elapsed < ProjectionStabilityWindow)
        {
            JsonElement current = await ReadGovernedOperationViewAsync(
                client,
                accessToken,
                noteId,
                correlationId,
                cancellationToken).ConfigureAwait(false);
            current.GetProperty("sourceVersion").GetInt64().ShouldBe(1);
            current.GetRawText().ShouldBe(expectedBody, "A delayed duplicate delivery must not mutate the durable projection.");
            await Task.Delay(PollInterval, cancellationToken).ConfigureAwait(false);
        }
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

                if (!IsTransientStatusCode(outcome.StatusCode))
                {
                    throw new InvalidOperationException(
                        $"The governed command failed permanently with {(int)outcome.StatusCode} {outcome.StatusCode}: {outcome.Body}");
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

        throw new TimeoutException(
            $"The governed command was not accepted within {StartupTimeout}. Last response: "
            + $"{(int)outcome.StatusCode} {outcome.StatusCode}: {outcome.Body}");
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

        return await AcquireTenantBoundAccessTokenAsync(
            app,
            "actor-alpha",
            "actor-alpha-pass",
            cancellationToken).ConfigureAwait(false);
    }

    private static async Task<string> AcquireTenantBoundAccessTokenAsync(
        DistributedApplication app,
        string username,
        string password,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(app);
        ArgumentException.ThrowIfNullOrWhiteSpace(username);
        ArgumentException.ThrowIfNullOrWhiteSpace(password);

        // Keycloak finishes its realm import + becomes ready asynchronously after the container reports Running,
        // so the token endpoint can hang or 503 briefly. Retry with a SHORT per-attempt timeout (so a not-ready
        // Keycloak fails fast instead of stalling the default 100s HttpClient timeout) until it issues the token.
        Uri security = app.GetEndpoint("security", "http");
        using FormUrlEncodedContent form = new(new Dictionary<string, string>
        {
            ["grant_type"] = "password",
            ["client_id"] = "hexalith-chatbot",
            ["username"] = username,
            ["password"] = password,
            ["scope"] = "openid",
        });
        string formContent = await form.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        Stopwatch stopwatch = Stopwatch.StartNew();
        while (stopwatch.Elapsed < StartupTimeout)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                using HttpClient http = new() { BaseAddress = security, Timeout = TimeSpan.FromSeconds(15) };
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

                else if (!IsTransientStatusCode(response.StatusCode))
                {
                    string error = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    throw new InvalidOperationException(
                        $"Keycloak rejected the tenant-bound token request permanently with "
                        + $"{(int)response.StatusCode} {response.StatusCode}: {error}");
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
    private static void RequireTier3Runtime(string skipReason)
    {
        bool available = string.Equals(
            Environment.GetEnvironmentVariable("HEXALITH_CHATBOT_TIER3"),
            "1",
            StringComparison.Ordinal)
            && CommandSucceeds("docker", "info")
            && CommandSucceeds("dapr", "--version");
        bool required = string.Equals(
            Environment.GetEnvironmentVariable("HEXALITH_CHATBOT_TIER3_REQUIRED"),
            "1",
            StringComparison.Ordinal);

        if (required && !available)
        {
            throw new InvalidOperationException(
                "The required Tier-3 acceptance lane is missing Docker, DAPR, or HEXALITH_CHATBOT_TIER3=1.");
        }

        Assert.SkipUnless(available, skipReason);
    }

    private static bool IsTransientStatusCode(HttpStatusCode statusCode)
        => statusCode is HttpStatusCode.RequestTimeout
            or HttpStatusCode.TooManyRequests
            or HttpStatusCode.BadGateway
            or HttpStatusCode.ServiceUnavailable
            or HttpStatusCode.GatewayTimeout
        || (int)statusCode >= 500;

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
