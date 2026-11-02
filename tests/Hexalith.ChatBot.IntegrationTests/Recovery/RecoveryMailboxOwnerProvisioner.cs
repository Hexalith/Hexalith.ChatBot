using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

using Aspire.Hosting;
using Aspire.Hosting.Testing;

using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Contracts.Identities;

using Shouldly;

namespace Hexalith.ChatBot.IntegrationTests.Recovery;

/// <summary>Seeds the real owner evidence required by the recovery mailbox service client.</summary>
internal static class RecoveryMailboxOwnerProvisioner
{
    private const string Tenant = RecoveryValidationTopology.StorageTenantRef;
    private const string Actor = "recovery-validator";
    private const string Intake = "not-a-ulid";
    private const string Operation = nameof(CaptureMailboxMessageIntake);
    private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(500);
    private static readonly TimeSpan CommandBudget = TimeSpan.FromMinutes(2);

    public static async Task SeedAsync(DistributedApplication application, string mailboxToken, CancellationToken cancellationToken)
    {
        using HttpClient eventStore = application.CreateHttpClient("eventstore", "http");
        using HttpClient chatBot = application.CreateHttpClient("chatbot");
        eventStore.Timeout = chatBot.Timeout = TimeSpan.FromSeconds(30);
        string adminToken = await RecoveryAccessTokenProvider.AcquireGlobalAdministratorAsync(application, cancellationToken).ConfigureAwait(false);
        string actorEventStoreToken = await RecoveryAccessTokenProvider.AcquireEventStoreValidatorAsync(application, cancellationToken).ConfigureAwait(false);
        string actorChatBotToken = await RecoveryAccessTokenProvider.AcquireAsync(application, cancellationToken).ConfigureAwait(false);

        await SubmitOwnerAsync(eventStore, adminToken, "platform:global-administrators", "global-administrators", "global-administrators",
            "BootstrapGlobalAdmin", new { userId = "admin-user" }, cancellationToken,
            "global-admin-already-bootstrapped-rejection").ConfigureAwait(false);
        await WaitForGlobalAdministratorAsync(eventStore, adminToken, cancellationToken).ConfigureAwait(false);
        await SubmitOwnerAsync(eventStore, adminToken, "platform:tenants", "tenants", Tenant,
            "CreateTenant", new { tenantId = Tenant, name = "Recovery validation", description = "Live recovery owner authority" },
            cancellationToken, "tenant-already-exists-rejection").ConfigureAwait(false);
        await SubmitOwnerAsync(eventStore, adminToken, "platform:tenants", "tenants", Tenant,
            "AddUserToTenant", new { tenantId = Tenant, userId = Actor, role = "TenantOwner" },
            cancellationToken, "user-already-in-tenant-rejection").ConfigureAwait(false);
        await SubmitOwnerAsync(eventStore, actorEventStoreToken, Tenant, "party", Actor,
            "Hexalith.Parties.Contracts.Commands.CreateParty",
            new { partyId = Actor, type = 1, personDetails = new { firstName = "Recovery", lastName = "Validator" } },
            cancellationToken).ConfigureAwait(false);

        string operationKey = ChatBotAuthorityGrantKey.For(Tenant, RecoveryValidationTopology.MailboxClientId,
            Intake, Operation, "operation", "service", ChatBotSurfaceOrigin.Mailbox);
        var operationGrant = new GrantChatBotAuthority(operationKey, ChatBotCommandId.New().ToString(),
            RecoveryValidationTopology.MailboxClientId, Intake, Operation, "operation", "service",
            ChatBotSurfaceOrigin.Mailbox, DateTimeOffset.UtcNow.AddHours(1));
        await SubmitChatBotGrantAsync(chatBot, actorChatBotToken, eventStore, actorEventStoreToken,
            operationGrant, cancellationToken).ConfigureAwait(false);

        (string grantId, DateTimeOffset expiresAt) = ReadMailboxGrantMetadata(mailboxToken);
        var serviceGrant = new ServiceClientGrant(grantId, Tenant, RecoveryValidationTopology.MailboxClientId,
            ServiceClientClass.MailboxIngestion, [Operation], [], ChatBotSurfaceOrigin.Mailbox, expiresAt, false,
            ["mailbox.ingest"], "command-set-v1");
        string serviceKey = ChatBotAuthorityGrantKey.For(Tenant, RecoveryValidationTopology.MailboxClientId,
            RecoveryValidationTopology.MailboxClientId, Operation, "service-grant", "service", ChatBotSurfaceOrigin.Mailbox);
        var serviceAuthority = new GrantChatBotAuthority(serviceKey, grantId,
            RecoveryValidationTopology.MailboxClientId, RecoveryValidationTopology.MailboxClientId,
            Operation, "service-grant", "service", ChatBotSurfaceOrigin.Mailbox, expiresAt, serviceGrant);
        await SubmitChatBotGrantAsync(chatBot, actorChatBotToken, eventStore, actorEventStoreToken,
            serviceAuthority, cancellationToken).ConfigureAwait(false);
    }

    private static (string GrantId, DateTimeOffset ExpiresAt) ReadMailboxGrantMetadata(string token)
    {
        string[] parts = token.Split('.');
        if (parts.Length != 3) { throw new InvalidOperationException("The mailbox token is not a JWT."); }
        string encoded = parts[1].Replace('-', '+').Replace('_', '/');
        encoded = encoded.PadRight(encoded.Length + (4 - encoded.Length % 4) % 4, '=');
        using JsonDocument claims = JsonDocument.Parse(Convert.FromBase64String(encoded));
        JsonElement root = claims.RootElement;
        string grantId = root.GetProperty("chatbot:service-client-grant-id").GetString()!;
        string expiryText = root.GetProperty("chatbot:service-client-grant-expiry").GetString()!;
        root.GetProperty("chatbot:service-client-id").GetString().ShouldBe(RecoveryValidationTopology.MailboxClientId);
        root.GetProperty("eventstore:tenant").GetString().ShouldBe(Tenant);
        root.GetProperty("chatbot:service-client-command").GetString().ShouldBe(Operation);
        DateTimeOffset expiry = DateTimeOffset.Parse(expiryText, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
        expiry.Offset.ShouldBe(TimeSpan.Zero);
        expiry.ShouldBeGreaterThan(DateTimeOffset.UtcNow.AddMinutes(5));
        return (grantId, expiry);
    }

    private static async Task SubmitChatBotGrantAsync(HttpClient client, string token, HttpClient eventStore,
        string eventStoreToken, GrantChatBotAuthority grant, CancellationToken cancellationToken)
    {
        string commandId = ChatBotCommandId.New().ToString();
        Stopwatch timer = Stopwatch.StartNew();
        string last = "no response";
        while (timer.Elapsed < CommandBudget)
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
                await WaitForCommandAsync(eventStore, eventStoreToken, commandId, cancellationToken).ConfigureAwait(false);
                return;
            }
            last = $"status {(int)response.StatusCode}: {await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false)}";
            if (response.StatusCode is not (HttpStatusCode.Forbidden or HttpStatusCode.ServiceUnavailable
                or HttpStatusCode.InternalServerError or HttpStatusCode.GatewayTimeout)) { break; }
            await Task.Delay(PollInterval, cancellationToken).ConfigureAwait(false);
        }
        throw new InvalidOperationException($"The recovery mailbox {grant.Authority} grant was not accepted: {last}");
    }

    private static async Task WaitForCommandAsync(HttpClient eventStore, string token, string messageId,
        CancellationToken cancellationToken)
    {
        Stopwatch timer = Stopwatch.StartNew();
        while (timer.Elapsed < CommandBudget)
        {
            using HttpRequestMessage request = new(HttpMethod.Get,
                $"/api/v1/commands/status/{Uri.EscapeDataString(messageId)}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Headers.Add("X-Hexalith-Tenant", Tenant);
            using HttpResponseMessage response = await eventStore.SendAsync(request, cancellationToken).ConfigureAwait(false);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                using JsonDocument status = JsonDocument.Parse(
                    await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false));
                string? state = status.RootElement.GetProperty("status").GetString();
                if (state == "Completed") { return; }
                if (state is "Rejected" or "Failed" or "PublishFailed")
                {
                    throw new InvalidOperationException($"Recovery mailbox authority command ended {state}.");
                }
            }
            else if (response.StatusCode != HttpStatusCode.NotFound)
            {
                throw new InvalidOperationException($"Recovery mailbox authority status returned {(int)response.StatusCode}.");
            }
            await Task.Delay(PollInterval, cancellationToken).ConfigureAwait(false);
        }
        throw new TimeoutException("Recovery mailbox authority did not complete in EventStore.");
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
            await Task.Delay(PollInterval, cancellationToken).ConfigureAwait(false);
        }
        submissionStatus.ShouldBe(HttpStatusCode.Accepted, $"Owner command {domain}/{type} failed: {result}");
        Stopwatch timer = Stopwatch.StartNew();
        while (timer.Elapsed < CommandBudget)
        {
            using HttpRequestMessage statusRequest = new(HttpMethod.Get,
                $"/api/v1/commands/status/{Uri.EscapeDataString(correlationId)}");
            statusRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            statusRequest.Headers.Add(tenant.StartsWith("platform:", StringComparison.Ordinal)
                ? "X-Hexalith-Scope" : "X-Hexalith-Tenant", tenant);
            using HttpResponseMessage statusResponse = await client.SendAsync(statusRequest, cancellationToken).ConfigureAwait(false);
            statusResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
            using JsonDocument status = JsonDocument.Parse(await statusResponse.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false));
            string? state = status.RootElement.GetProperty("status").GetString();
            if (state == "Completed") { return; }
            if (state is "Rejected" or "Failed" or "PublishFailed")
            {
                throw new InvalidOperationException($"Owner command {domain}/{type} ended {state}.");
            }
            await Task.Delay(PollInterval, cancellationToken).ConfigureAwait(false);
        }
        throw new TimeoutException($"Owner command {domain}/{type} did not complete.");
    }

    private static async Task WaitForGlobalAdministratorAsync(HttpClient client, string token, CancellationToken cancellationToken)
    {
        Stopwatch timer = Stopwatch.StartNew();
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
            await Task.Delay(PollInterval, cancellationToken).ConfigureAwait(false);
        }
        throw new TimeoutException("The recovery owner setup did not observe the current global administrator.");
    }
}
