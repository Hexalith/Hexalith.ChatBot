using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

using Aspire.Hosting;
using Aspire.Hosting.Testing;

using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Contracts.Identities;
using Hexalith.ChatBot.Server.Gateway;

using Shouldly;

namespace Hexalith.ChatBot.IntegrationTests.Recovery;

/// <summary>
/// Seeds the owner evidence the live mailbox admission probe needs: tenant ownership, the validator's Party identity,
/// and the recovery mailbox client's ChatBot authority for the probe's fixed intake only. The drill's own mailbox
/// intakes carry fresh intake identities and are not covered.
/// </summary>
/// <remarks>
/// ChatBot authority can only be issued through the ChatBot command gateway, which refuses every command with a
/// non-retryable <c>503 canonical-execution-unavailable</c> while the A6/A13 owner-atomic acceptance gates stay open.
/// Seeding then fails fast and names that blocker instead of retrying a refusal that cannot change.
/// </remarks>
internal static class RecoveryMailboxOwnerProvisioner
{
    /// <summary>The resource the operation grant targets: the admission probe's fixed intake identity.</summary>
    internal const string ProbeIntakeId = LiveContinuityAspireE2eTests.MailboxAdmissionProbeIntakeId;

    /// <summary>The command the admission probe submits and the operation grant authorizes.</summary>
    internal const string ProbeOperation = nameof(CaptureMailboxMessageIntake);

    private const string Tenant = RecoveryValidationTopology.StorageTenantRef;
    private const string Actor = "recovery-validator";
    private const int MaxDiagnosticLength = 512;
    private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(500);
    private static readonly TimeSpan CommandBudget = TimeSpan.FromMinutes(2);

    // The owner aggregate refuses expiries more than 30 days out and never accepts a grant id twice, so the service
    // grant gets its own id and a lease that outlives the 250-minute lane instead of mirroring the token's claims.
    private static readonly TimeSpan ServiceGrantLease = TimeSpan.FromDays(1);

    /// <summary>Gets the whole seeding budget; the live test's startup deadline reserves it.</summary>
    internal static TimeSpan SeedBudget { get; } = TimeSpan.FromMinutes(10);

    /// <summary>Seeds the probe's owner evidence inside <see cref="SeedBudget"/>.</summary>
    /// <param name="application">The composed topology.</param>
    /// <param name="mailboxToken">The recovery mailbox client's bearer, checked for the identity the grants name.</param>
    /// <param name="cancellationToken">Cancels seeding.</param>
    /// <returns>A task that completes once every owner command and grant completed in EventStore.</returns>
    public static async Task SeedAsync(DistributedApplication application, string mailboxToken, CancellationToken cancellationToken)
    {
        AssertMailboxTokenIdentity(mailboxToken);
        using CancellationTokenSource seed = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        seed.CancelAfter(SeedBudget);
        using HttpClient eventStore = application.CreateHttpClient("eventstore", "http");
        using HttpClient chatBot = application.CreateHttpClient("chatbot");
        eventStore.Timeout = chatBot.Timeout = TimeSpan.FromSeconds(30);
        string step = "token acquisition";
        try
        {
            string adminToken = await RecoveryAccessTokenProvider.AcquireGlobalAdministratorAsync(application, seed.Token).ConfigureAwait(false);
            string actorEventStoreToken = await RecoveryAccessTokenProvider.AcquireEventStoreValidatorAsync(application, seed.Token).ConfigureAwait(false);
            string actorChatBotToken = await RecoveryAccessTokenProvider.AcquireAsync(application, seed.Token).ConfigureAwait(false);

            step = "global-administrator bootstrap";
            await SubmitOwnerAsync(eventStore, adminToken, "platform:global-administrators", "global-administrators", "global-administrators",
                "BootstrapGlobalAdmin", new { userId = "admin-user" }, seed.Token,
                "global-admin-already-bootstrapped-rejection").ConfigureAwait(false);
            step = "global-administrator projection";
            await WaitForGlobalAdministratorAsync(eventStore, adminToken, seed.Token).ConfigureAwait(false);
            step = "tenant creation";
            await SubmitOwnerAsync(eventStore, adminToken, "platform:tenants", "tenants", Tenant,
                "CreateTenant", new { tenantId = Tenant, name = "Recovery validation", description = "Live recovery owner authority" },
                seed.Token, "tenant-already-exists-rejection").ConfigureAwait(false);
            step = "tenant membership";
            await SubmitOwnerAsync(eventStore, adminToken, "platform:tenants", "tenants", Tenant,
                "AddUserToTenant", new { tenantId = Tenant, userId = Actor, role = "TenantOwner" },
                seed.Token, "user-already-in-tenant-rejection").ConfigureAwait(false);
            step = "validator Party";
            await SubmitOwnerAsync(eventStore, actorEventStoreToken, Tenant, "party", Actor,
                "Hexalith.Parties.Contracts.Commands.CreateParty",
                new { partyId = Actor, type = 1, personDetails = new { firstName = "Recovery", lastName = "Validator" } },
                seed.Token).ConfigureAwait(false);

            step = "mailbox operation grant";
            string operationKey = ChatBotAuthorityGrantKey.For(Tenant, RecoveryValidationTopology.MailboxClientId,
                ProbeIntakeId, ProbeOperation, "operation", "service", ChatBotSurfaceOrigin.Mailbox);
            var operationGrant = new GrantChatBotAuthority(operationKey, ChatBotCommandId.New().ToString(),
                RecoveryValidationTopology.MailboxClientId, ProbeIntakeId, ProbeOperation, "operation", "service",
                ChatBotSurfaceOrigin.Mailbox, DateTimeOffset.UtcNow.AddHours(1));
            await SubmitChatBotGrantAsync(chatBot, actorChatBotToken, eventStore, actorEventStoreToken,
                operationGrant, seed.Token).ConfigureAwait(false);

            step = "mailbox service grant";
            string serviceGrantId = ChatBotCommandId.New().ToString();
            DateTimeOffset serviceGrantExpiresAt = DateTimeOffset.UtcNow.Add(ServiceGrantLease);
            var serviceGrant = new ServiceClientGrant(serviceGrantId, Tenant, RecoveryValidationTopology.MailboxClientId,
                ServiceClientClass.MailboxIngestion, [ProbeOperation], [], ChatBotSurfaceOrigin.Mailbox, serviceGrantExpiresAt, false,
                ["mailbox.ingest"], "command-set-v1");
            string serviceKey = ChatBotAuthorityGrantKey.For(Tenant, RecoveryValidationTopology.MailboxClientId,
                RecoveryValidationTopology.MailboxClientId, ProbeOperation, "service-grant", "service", ChatBotSurfaceOrigin.Mailbox);
            var serviceAuthority = new GrantChatBotAuthority(serviceKey, serviceGrantId,
                RecoveryValidationTopology.MailboxClientId, RecoveryValidationTopology.MailboxClientId,
                ProbeOperation, "service-grant", "service", ChatBotSurfaceOrigin.Mailbox, serviceGrantExpiresAt, serviceGrant);
            await SubmitChatBotGrantAsync(chatBot, actorChatBotToken, eventStore, actorEventStoreToken,
                serviceAuthority, seed.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (seed.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException($"Recovery mailbox owner seeding exceeded its {SeedBudget} budget during the {step} step.");
        }
    }

    /// <summary>
    /// Recognizes ChatBot's fail-closed canonical-execution refusal, which no retry can change while A6/A13 stay open.
    /// </summary>
    /// <param name="statusCode">The observed status.</param>
    /// <param name="body">The verbatim response body.</param>
    /// <returns><see langword="true"/> for a 503 whose problem type is canonical-execution-unavailable.</returns>
    internal static bool IsCanonicalExecutionRefusal(HttpStatusCode statusCode, string body)
    {
        if (statusCode != HttpStatusCode.ServiceUnavailable || string.IsNullOrWhiteSpace(body))
        {
            return false;
        }

        try
        {
            using JsonDocument problem = JsonDocument.Parse(body);
            return problem.RootElement.ValueKind == JsonValueKind.Object
                && problem.RootElement.TryGetProperty("type", out JsonElement type)
                && type.ValueKind == JsonValueKind.String
                && string.Equals(type.GetString(), ChatBotProblemTypes.CanonicalExecutionUnavailable, StringComparison.Ordinal);
        }
        catch (JsonException)
        {
            return false;
        }
    }

    // The bearer must name the identity the grants are keyed to. The grant id and expiry claims are deliberately not
    // read: no server code consumes them, and copying them broke the aggregate's 30-day cap and grant-id reuse rule.
    private static void AssertMailboxTokenIdentity(string token)
    {
        string[] parts = token.Split('.');
        if (parts.Length != 3) { throw new InvalidOperationException("The mailbox token is not a JWT."); }
        string encoded = parts[1].Replace('-', '+').Replace('_', '/');
        encoded = encoded.PadRight(encoded.Length + (4 - encoded.Length % 4) % 4, '=');
        using JsonDocument claims = JsonDocument.Parse(Convert.FromBase64String(encoded));
        JsonElement root = claims.RootElement;
        root.GetProperty("chatbot:service-client-id").GetString().ShouldBe(RecoveryValidationTopology.MailboxClientId);
        root.GetProperty("eventstore:tenant").GetString().ShouldBe(Tenant);
        root.GetProperty("chatbot:service-client-command").GetString().ShouldBe(ProbeOperation);
    }

    private static async Task SubmitChatBotGrantAsync(HttpClient client, string token, HttpClient eventStore,
        string eventStoreToken, GrantChatBotAuthority grant, CancellationToken cancellationToken)
    {
        string commandId = ChatBotCommandId.New().ToString();
        Stopwatch timer = Stopwatch.StartNew();
        string last = "no response";
        bool accepted = false;
        while (timer.Elapsed < CommandBudget)
        {
            try
            {
                using HttpRequestMessage request = new(HttpMethod.Post, "/api/v1/commands")
                {
                    Content = JsonContent.Create(new { commandId, commandType = nameof(GrantChatBotAuthority),
                        command = grant, origin = "ui", requestSchemaVersion = "v1" }),
                };
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                using HttpResponseMessage response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);
                if (response.StatusCode == HttpStatusCode.Accepted)
                {
                    accepted = true;
                    break;
                }

                string body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                last = $"status {(int)response.StatusCode}: {Bounded(body)}";
                if (IsCanonicalExecutionRefusal(response.StatusCode, body))
                {
                    throw new InvalidOperationException(
                        $"ChatBot refused the recovery mailbox {grant.Authority} grant with the non-retryable canonical-execution-unavailable "
                        + "problem. ChatBot authority can only be issued through the command gateway, which stays fail-closed while the "
                        + "A6/A13 owner-atomic acceptance gates are open, so the live mailbox admission probe cannot be seeded. "
                        + $"Last response: {last}");
                }

                if (response.StatusCode is not (HttpStatusCode.Forbidden or HttpStatusCode.ServiceUnavailable
                    or HttpStatusCode.InternalServerError or HttpStatusCode.GatewayTimeout)) { break; }
            }
            catch (HttpRequestException exception)
            {
                // The command path can still be warming up; the admission probe tolerates the same failure.
                last = $"transport failure: {exception.GetType().Name}";
            }
            catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                last = "per-request timeout";
            }

            await Task.Delay(PollInterval, cancellationToken).ConfigureAwait(false);
        }

        if (!accepted)
        {
            throw new InvalidOperationException($"The recovery mailbox {grant.Authority} grant was not accepted: {last}");
        }

        // Outside the submission retry: a status-poll timeout must not resubmit an accepted grant.
        await WaitForCommandAsync(eventStore, eventStoreToken, commandId, cancellationToken).ConfigureAwait(false);
    }

    private static async Task WaitForCommandAsync(HttpClient eventStore, string token, string messageId,
        CancellationToken cancellationToken)
    {
        Stopwatch timer = Stopwatch.StartNew();
        string last = "no status";
        while (timer.Elapsed < CommandBudget)
        {
            using HttpRequestMessage request = new(HttpMethod.Get,
                $"/api/v1/commands/status/{Uri.EscapeDataString(messageId)}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Headers.Add("X-Hexalith-Tenant", Tenant);
            using HttpResponseMessage response = await eventStore.SendAsync(request, cancellationToken).ConfigureAwait(false);
            string body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                using JsonDocument status = JsonDocument.Parse(body);
                string? state = status.RootElement.GetProperty("status").GetString();
                if (state == "Completed") { return; }
                if (state is "Rejected" or "PublishFailed" or "TimedOut")
                {
                    throw new InvalidOperationException($"Recovery mailbox authority command ended {state}: {Bounded(body)}");
                }

                last = $"state {state}";
            }
            else if (response.StatusCode != HttpStatusCode.NotFound)
            {
                throw new InvalidOperationException(
                    $"Recovery mailbox authority status returned {(int)response.StatusCode}: {Bounded(body)}");
            }
            else
            {
                last = "status 404";
            }

            await Task.Delay(PollInterval, cancellationToken).ConfigureAwait(false);
        }

        throw new TimeoutException($"Recovery mailbox authority did not complete in EventStore within {CommandBudget}; last observed {last}.");
    }

    private static async Task SubmitOwnerAsync(HttpClient client, string token, string tenant, string domain,
        string aggregate, string type, object payload, CancellationToken cancellationToken, string? alreadySatisfied = null)
    {
        string messageId = ChatBotCommandId.New().ToString();
        string correlationId = string.Empty;
        string result = string.Empty;
        HttpStatusCode submissionStatus = 0;
        for (int attempt = 1; attempt <= 3; attempt++)
        {
            try
            {
                using HttpRequestMessage request = new(HttpMethod.Post, "/api/v1/commands")
                {
                    Content = JsonContent.Create(new { messageId, tenant, domain, aggregateId = aggregate,
                        commandType = type, payload = JsonSerializer.SerializeToElement(payload) }),
                };
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                request.Headers.Add(tenant.StartsWith("platform:", StringComparison.Ordinal)
                    ? "X-Hexalith-Scope" : "X-Hexalith-Tenant", tenant);
                using HttpResponseMessage response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);
                submissionStatus = response.StatusCode;
                result = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                if (submissionStatus == HttpStatusCode.Conflict && alreadySatisfied is not null)
                {
                    using JsonDocument conflict = JsonDocument.Parse(result);
                    if (conflict.RootElement.TryGetProperty("reasonCode", out JsonElement reason)
                        && reason.GetString() == alreadySatisfied) { return; }
                }
                if (submissionStatus == HttpStatusCode.Accepted)
                {
                    using JsonDocument receipt = JsonDocument.Parse(result);
                    correlationId = receipt.RootElement.GetProperty("correlationId").GetString()!;
                    break;
                }
                if (attempt == 3 || submissionStatus is not (HttpStatusCode.InternalServerError or
                    HttpStatusCode.ServiceUnavailable or HttpStatusCode.GatewayTimeout)) { break; }
            }
            catch (HttpRequestException exception) when (attempt < 3)
            {
                // The same message id is resent, so EventStore deduplicates an attempt that did reach it.
                result = $"transport failure: {exception.GetType().Name}";
            }
            catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested && attempt < 3)
            {
                result = "per-request timeout";
            }

            await Task.Delay(PollInterval, cancellationToken).ConfigureAwait(false);
        }
        submissionStatus.ShouldBe(HttpStatusCode.Accepted, $"Owner command {domain}/{type} failed: {Bounded(result)}");
        Stopwatch timer = Stopwatch.StartNew();
        string last = "no status";
        while (timer.Elapsed < CommandBudget)
        {
            using HttpRequestMessage statusRequest = new(HttpMethod.Get,
                $"/api/v1/commands/status/{Uri.EscapeDataString(correlationId)}");
            statusRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            statusRequest.Headers.Add(tenant.StartsWith("platform:", StringComparison.Ordinal)
                ? "X-Hexalith-Scope" : "X-Hexalith-Tenant", tenant);
            using HttpResponseMessage statusResponse = await client.SendAsync(statusRequest, cancellationToken).ConfigureAwait(false);
            string statusBody = await statusResponse.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            statusResponse.StatusCode.ShouldBe(HttpStatusCode.OK,
                $"Owner command {domain}/{type} status was unavailable: {Bounded(statusBody)}");
            using JsonDocument status = JsonDocument.Parse(statusBody);
            string? state = status.RootElement.GetProperty("status").GetString();
            if (state == "Completed") { return; }
            if (state is "Rejected" or "PublishFailed" or "TimedOut")
            {
                throw new InvalidOperationException($"Owner command {domain}/{type} ended {state}: {Bounded(statusBody)}");
            }

            last = $"state {state}";
            await Task.Delay(PollInterval, cancellationToken).ConfigureAwait(false);
        }
        throw new TimeoutException($"Owner command {domain}/{type} did not complete within {CommandBudget}; last observed {last}.");
    }

    private static async Task WaitForGlobalAdministratorAsync(HttpClient client, string token, CancellationToken cancellationToken)
    {
        Stopwatch timer = Stopwatch.StartNew();
        string last = "no response";
        while (timer.Elapsed < CommandBudget)
        {
            using HttpRequestMessage request = new(HttpMethod.Post, "/api/v1/queries")
            {
                Content = JsonContent.Create(new { tenant = "platform:global-administrators", domain = "global-administrators",
                    aggregateId = "global-administrators", queryType = "get-global-administrators",
                    projectionType = "global-administrators", payload = new { pageSize = 10 } }),
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Headers.Add("X-Hexalith-Scope", "platform:global-administrators");
            using HttpResponseMessage response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);
            if (response.StatusCode == HttpStatusCode.OK) { return; }
            last = $"status {(int)response.StatusCode}: "
                + Bounded(await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false));
            await Task.Delay(PollInterval, cancellationToken).ConfigureAwait(false);
        }
        throw new TimeoutException($"The recovery owner setup did not observe the current global administrator within {CommandBudget}; last {last}.");
    }

    private static string Bounded(string text)
        => text.Length > MaxDiagnosticLength ? text[..MaxDiagnosticLength] : text;
}
