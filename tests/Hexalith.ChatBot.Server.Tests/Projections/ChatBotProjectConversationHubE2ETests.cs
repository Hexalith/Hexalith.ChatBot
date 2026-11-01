using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;

using Hexalith.ChatBot.Contracts.Messages;
using Hexalith.ChatBot.Contracts.Queries;
using Hexalith.ChatBot.Server.Governance.AiMediation;
using Hexalith.ChatBot.Server.Projections;
using Hexalith.ChatBot.Server.Tests.Authentication;
using Hexalith.ChatBot.Tests.TrustedAuthority;

using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Http.Connections.Client;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;

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
        using WebApplicationFactory<Program> factory = new WebApplicationFactory<Program>().WithAppChannelToken()
            .WithWebHostBuilder(builder =>
            {
                ChatBotSignedBearerFixture.Configure(builder);
                _ = builder.UseSetting("ChatBot:ProjectionChangeNotifications:Enabled", "true");
            });

        TestServer server = factory.Server;

        await using HubConnection tenantConnection = BuildHubConnection(server, ChatBotSignedBearerFixture.CreateToken(Tenant));
        await using HubConnection otherTenantConnection = BuildHubConnection(server, ChatBotSignedBearerFixture.CreateToken(OtherTenant));

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
        using HttpClient client = factory.CreateAppChannelClient();
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
        using WebApplicationFactory<Program> factory = new WebApplicationFactory<Program>().WithAppChannelToken()
            .WithWebHostBuilder(builder =>
            {
                _ = builder.UseSetting("ChatBot:ProjectionChangeNotifications:Enabled", "true");
                ChatBotSignedBearerFixture.Configure(builder);
            });

        await using HubConnection connection = BuildHubConnection(factory.Server, ChatBotSignedBearerFixture.CreateToken(Tenant));
        await connection.StartAsync(cancellationToken);

        HubException error = await Should.ThrowAsync<HubException>(
            () => connection.InvokeAsync("JoinTenant", OtherTenant, cancellationToken));
        error.Message.ShouldContain("tenant-forbidden");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SupplementalIdentityCannotJoinAnotherTenant(bool authenticatedSupplement)
    {
        ClaimsPrincipal principal = RegressionAuthorityFixture.Principal(new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim("sub", "actor-001"), new Claim("eventstore:tenant", Tenant)], "test")));
        principal.AddIdentity(new ClaimsIdentity([new Claim("tenant", OtherTenant)], authenticatedSupplement ? "authenticated-supplement" : null));
        RecordingProjectionHubGroups groups = new();
        using ChatBotProjectConversationHub hub = new(new ConfigurationBuilder().Build())
        {
            Context = new ProjectionHubCallerContext(principal),
            Groups = groups,
        };
        (await Should.ThrowAsync<HubException>(() => hub.JoinTenant(OtherTenant))).Message.ShouldContain("tenant-forbidden");
        groups.Joined.ShouldBeEmpty();
        if (authenticatedSupplement)
        {
            (await Should.ThrowAsync<HubException>(() => hub.JoinTenant(Tenant))).Message.ShouldContain("tenant-forbidden");
            groups.Joined.ShouldBeEmpty();
        }
        else
        {
            await hub.JoinTenant(Tenant);
            groups.Joined.ShouldBe([ChatBotProjectConversationHub.GroupFor(Tenant)]);
        }
    }

    [Fact]
    public async Task AuthenticatedSameTenantJoinShouldBeAuthorizedAndReceiveItsOwnTenantSignal()
    {
        // [AC5 authorized path] An authenticated caller whose token binds tenant-alpha joins its own group and receives
        // the tenant-scoped change signal — the positive complement of the cross-tenant fail-closed case.
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using WebApplicationFactory<Program> factory = new WebApplicationFactory<Program>().WithAppChannelToken()
            .WithWebHostBuilder(builder =>
            {
                _ = builder.UseSetting("ChatBot:ProjectionChangeNotifications:Enabled", "true");
                ChatBotSignedBearerFixture.Configure(builder);
            });

        TestServer server = factory.Server;
        await using HubConnection connection = BuildHubConnection(server, ChatBotSignedBearerFixture.CreateToken(Tenant));
        TaskCompletionSource<string> tenantSignal = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _ = connection.On<string>(
            ChatBotProjectConversationHub.ProjectConversationChangedClientMethod,
            signalTenantId => tenantSignal.TrySetResult(signalTenantId));

        await connection.StartAsync(cancellationToken);
        await connection.InvokeAsync("JoinTenant", Tenant, cancellationToken);

        using HttpClient client = factory.CreateAppChannelClient();
        using HttpResponseMessage response = await client.PostAsJsonAsync(
            AiOutcomeProjectionEndpoints.AiOutcomeRecordedRoute,
            LowRiskSucceededEvent(),
            cancellationToken);
        response.EnsureSuccessStatusCode();

        string received = await tenantSignal.Task.WaitAsync(TimeSpan.FromSeconds(15), cancellationToken);
        received.ShouldBe(Tenant);
    }

    [Theory]
    [InlineData("anonymous", HttpStatusCode.Unauthorized)]
    [InlineData("invalid", HttpStatusCode.Unauthorized)]
    [InlineData("tampered", HttpStatusCode.Unauthorized)]
    [InlineData("workload", HttpStatusCode.Forbidden)]
    public async Task HubNegotiationShouldRejectMissingInvalidAndNonHumanBearer(string credential, HttpStatusCode expectedStatus)
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using WebApplicationFactory<Program> factory = new WebApplicationFactory<Program>().WithAppChannelToken()
            .WithWebHostBuilder(builder =>
            {
                _ = builder.UseSetting("ChatBot:ProjectionChangeNotifications:Enabled", "true");
                ChatBotSignedBearerFixture.Configure(builder);
            });

        using HttpClient client = factory.CreateClient();
        using HttpRequestMessage request = new(HttpMethod.Post, ChatBotProjectConversationHub.HubPath + "/negotiate?negotiateVersion=1");
        string? token = credential switch
        {
            "invalid" => "invalid-bearer",
            "tampered" => ChatBotSignedBearerFixture.CreateToken(Tenant, invalidSignature: true),
            "workload" => ChatBotSignedBearerFixture.CreateToken(Tenant, workload: true),
            _ => null,
        };
        if (token is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        using HttpResponseMessage response = await client.SendAsync(request, cancellationToken);
        response.StatusCode.ShouldBe(expectedStatus);
        (await response.Content.ReadAsStringAsync(cancellationToken)).ShouldNotContain("connectionToken");
    }

    [Fact]
    public async Task UnauthenticatedJoinShouldFailClosedWhenJwtAuthenticationIsConfigured()
    {
        // Preserve the original JoinTenant guard assertion at its actual method boundary. HTTP never admits this
        // anonymous connection: the SDK human policy rejects it during negotiation first.
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Authentication:JwtBearer:SigningKey"] = "test-signing-key-0123456789-abcdef-ghijkl",
        }).Build();
        RecordingProjectionHubGroups groups = new();
        using ChatBotProjectConversationHub hub = new(configuration)
        {
            Context = new ProjectionHubCallerContext(new ClaimsPrincipal(new ClaimsIdentity())),
            Groups = groups,
        };
        (await Should.ThrowAsync<HubException>(() => hub.JoinTenant(Tenant))).Message.ShouldContain("tenant-forbidden");
        groups.Joined.ShouldBeEmpty();
    }

    private static HubConnection BuildHubConnection(TestServer server, string token)
        => new HubConnectionBuilder()
            .WithUrl(
                new Uri(server.BaseAddress, ChatBotProjectConversationHub.HubPath),
                options =>
                {
                    options.HttpMessageHandlerFactory = _ => server.CreateHandler();
                    options.Transports = HttpTransportType.LongPolling;
                    options.AccessTokenProvider = () => Task.FromResult<string?>(token);
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

}
