using System.Net.Http.Json;
using System.Security.Claims;

using Hexalith.ChatBot.Contracts.Messages;
using Hexalith.ChatBot.Contracts.Queries;
using Hexalith.ChatBot.Server.Governance.AiMediation;
using Hexalith.ChatBot.Server.Projections;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Http.Connections.Client;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Projections;

/// <summary>
/// Story 10.6b transport E2E (single-host, in-sandbox — the ChatBot-owned hub makes this verifiable without DAPR or a
/// second host): a real SignalR <see cref="HubConnection"/> joins the tenant group on the mapped
/// <see cref="ChatBotProjectConversationHub"/>; projecting a server-verified AI response progress row (the low-risk
/// execution lifecycle) broadcasts the advisory metadata-only change signal; the client receives it (its cue to
/// re-query). Tenant isolation: a different tenant's group never receives it.
/// </summary>
public sealed class ChatBotProjectConversationHubE2ETests
{
    private const string Tenant = "tenant-alpha";
    private const string OtherTenant = "tenant-beta";
    private const string CorrelationId = "01ARZ3NDEKTSV4RRFFQ69G5FAW";
    private static readonly DateTimeOffset OccurredAt = new(2026, 6, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task AiResponseProgressProjectionShouldBroadcastTenantScopedChangeSignalToHubSubscribers()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using WebApplicationFactory<Program> factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder => builder.UseSetting("ChatBot:ProjectionChangeNotifications:Enabled", "true"));

        TestServer server = factory.Server;

        await using HubConnection tenantConnection = BuildHubConnection(server);
        await using HubConnection otherTenantConnection = BuildHubConnection(server);

        TaskCompletionSource<string> tenantSignal = new(TaskCreationOptions.RunContinuationsAsynchronously);
        bool otherTenantSignalled = false;
        _ = tenantConnection.On<string>(
            ChatBotProjectConversationHub.ProjectConversationChangedClientMethod,
            signalTenantId => tenantSignal.TrySetResult(signalTenantId));
        _ = otherTenantConnection.On<string>(
            ChatBotProjectConversationHub.ProjectConversationChangedClientMethod,
            _ => otherTenantSignalled = true);

        await tenantConnection.StartAsync(cancellationToken);
        await otherTenantConnection.StartAsync(cancellationToken);
        await tenantConnection.InvokeAsync("JoinTenant", Tenant, cancellationToken);
        await otherTenantConnection.InvokeAsync("JoinTenant", OtherTenant, cancellationToken);

        // Project a server-verified low-risk AI assistance outcome for Tenant (the producer sets AI response progress),
        // which materializes a progress-bearing row and broadcasts the advisory change signal to the tenant group.
        using HttpClient client = factory.CreateClient();
        using HttpResponseMessage response = await client.PostAsJsonAsync(
            AiOutcomeProjectionEndpoints.AiOutcomeRecordedRoute,
            LowRiskSucceededEvent(),
            cancellationToken);
        response.EnsureSuccessStatusCode();

        string received = await tenantSignal.Task.WaitAsync(TimeSpan.FromSeconds(15), cancellationToken);
        received.ShouldBe(Tenant);
        otherTenantSignalled.ShouldBeFalse();
    }

    [Fact]
    public async Task AuthenticatedCrossTenantJoinShouldFailClosedAsTenantForbidden()
    {
        // [AC3/AC5 fail-closed] An authenticated caller whose token binds tenant-alpha must NOT be able to join another
        // tenant's project-conversation change group; the hub throws tenant-forbidden before AddToGroupAsync.
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using WebApplicationFactory<Program> factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                _ = builder.UseSetting("ChatBot:ProjectionChangeNotifications:Enabled", "true");
                _ = builder.ConfigureServices(services =>
                    services.AddSingleton<IStartupFilter>(new AuthenticatedTenantStartupFilter(Tenant)));
            });

        await using HubConnection connection = BuildHubConnection(factory.Server);
        await connection.StartAsync(cancellationToken);

        HubException error = await Should.ThrowAsync<HubException>(
            () => connection.InvokeAsync("JoinTenant", OtherTenant, cancellationToken));
        error.Message.ShouldContain("tenant-forbidden");
    }

    [Fact]
    public async Task AuthenticatedSameTenantJoinShouldBeAuthorizedAndReceiveItsOwnTenantSignal()
    {
        // [AC5 authorized path] An authenticated caller whose token binds tenant-alpha joins its own group and receives
        // the tenant-scoped change signal — the positive complement of the cross-tenant fail-closed case.
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using WebApplicationFactory<Program> factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                _ = builder.UseSetting("ChatBot:ProjectionChangeNotifications:Enabled", "true");
                _ = builder.ConfigureServices(services =>
                    services.AddSingleton<IStartupFilter>(new AuthenticatedTenantStartupFilter(Tenant)));
            });

        TestServer server = factory.Server;
        await using HubConnection connection = BuildHubConnection(server);
        TaskCompletionSource<string> tenantSignal = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _ = connection.On<string>(
            ChatBotProjectConversationHub.ProjectConversationChangedClientMethod,
            signalTenantId => tenantSignal.TrySetResult(signalTenantId));

        await connection.StartAsync(cancellationToken);
        await connection.InvokeAsync("JoinTenant", Tenant, cancellationToken);

        using HttpClient client = factory.CreateClient();
        using HttpResponseMessage response = await client.PostAsJsonAsync(
            AiOutcomeProjectionEndpoints.AiOutcomeRecordedRoute,
            LowRiskSucceededEvent(),
            cancellationToken);
        response.EnsureSuccessStatusCode();

        string received = await tenantSignal.Task.WaitAsync(TimeSpan.FromSeconds(15), cancellationToken);
        received.ShouldBe(Tenant);
    }

    [Fact]
    public async Task UnauthenticatedJoinShouldFailClosedWhenJwtAuthenticationIsConfigured()
    {
        // [F1 fail-open fix] When the deployment configures JWT bearer auth, an anonymous hub connection must fail
        // closed: UseAuthentication only populates the principal from a present token (it does not reject anonymous
        // requests), so the hub's own env-gate must reject an unauthenticated JoinTenant rather than allow any tenant.
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using WebApplicationFactory<Program> factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                _ = builder.UseSetting("ChatBot:ProjectionChangeNotifications:Enabled", "true");
                _ = builder.UseSetting("Authentication:JwtBearer:SigningKey", "test-signing-key-0123456789-abcdef-ghijkl");
            });

        await using HubConnection connection = BuildHubConnection(factory.Server);
        await connection.StartAsync(cancellationToken);

        HubException error = await Should.ThrowAsync<HubException>(
            () => connection.InvokeAsync("JoinTenant", Tenant, cancellationToken));
        error.Message.ShouldContain("tenant-forbidden");
    }

    private static HubConnection BuildHubConnection(TestServer server)
        => new HubConnectionBuilder()
            .WithUrl(
                new Uri(server.BaseAddress, ChatBotProjectConversationHub.HubPath),
                options =>
                {
                    options.HttpMessageHandlerFactory = _ => server.CreateHandler();
                    options.Transports = HttpTransportType.LongPolling;
                })
            .Build();

    private static PublishedAiActionExecutionEvent LowRiskSucceededEvent()
    {
        LowRiskAiAssistanceExecutionRecord record = new(
            "ai-execution-001",
            "proposal-001",
            "summarize-visible-context",
            "success",
            "deterministic-test",
            "test-model-v1",
            OccurredAt.AddSeconds(5),
            ["evidence-001"],
            "context-package-001",
            "v1",
            "metadata_only",
            "policy-snap-001",
            "low-risk-execute-allowed",
            "audit:ai-execution-001",
            "available",
            CorrelationId,
            "metadata_only",
            "metadata_only",
            "none");

        return new PublishedAiActionExecutionEvent(
            Tenant,
            ApprovedAiActionOutcomeProjectionTranslator.ChatBotDomain,
            "graph-message-001",
            typeof(LowRiskAiAssistanceExecutionSucceeded).FullName,
            71,
            OccurredAt.AddSeconds(5),
            CorrelationId,
            LowRiskSucceeded: new LowRiskAiAssistanceExecutionSucceeded(
                record,
                "project-001",
                "requester-001",
                "graph-message-001",
                "conversation-item-001",
                ["evidence-001"],
                ["redacted", "policy-denied"]));
    }

    // Injects an authenticated principal carrying the eventstore:tenant claim into every request (mirrors the
    // TestPrincipalStartupFilter used by ProjectConversationProjectionTests) so the hub's authenticated authorization
    // branch is exercised without configuring a real JWT issuer. SignalR populates HubCallerContext.User from it.
    private sealed class AuthenticatedTenantStartupFilter(string tenantId) : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
            => app =>
            {
                _ = app.Use(async (context, continuation) =>
                {
                    context.User = new ClaimsPrincipal(new ClaimsIdentity(
                        [
                            new Claim("sub", "actor-001"),
                            new Claim("eventstore:tenant", tenantId),
                        ],
                        "test"));
                    await continuation().ConfigureAwait(false);
                });
                next(app);
            };
    }
}
