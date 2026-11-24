using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Server.Governance.Conversations;
using Hexalith.ChatBot.Server.Projections;
using Hexalith.EventStore.ServiceDefaults.Authentication;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Projections;

/// <summary>
/// Verifies the single ChatBot projection subscription. Dapr.AspNetCore keeps only the first default route it finds
/// for a pub/sub topic, so the shared events topic must resolve to one fan-out route that runs every projection family.
/// </summary>
public sealed class ChatBotProjectionFanOutEndpointsTests
{
    private const string FanOutRoute = ChatBotProjectionFanOutEndpoints.FanOutRoute;
    private const string Tenant = "tenant-alpha";
    private const string ProjectId = "project-001";
    private const string NoteId = "01ARZ3NDEKTSV4RRFFQ69G5FAZ";
    private const string MessageId = "01ARZ3NDEKTSV4RRFFQ69G5FAY";
    private const string CorrelationId = "01ARZ3NDEKTSV4RRFFQ69G5FAW";
    private static readonly DateTimeOffset OccurredAt = new(2026, 6, 1, 0, 0, 0, TimeSpan.Zero);

    /// <summary>Gets every sidecar-originated projection route: the fan-out subscription and the seven family routes.</summary>
    public static TheoryData<string> SidecarRoutes => new()
    {
        FanOutRoute,
        GovernedOperationProjectionEndpoints.GovernedNoteRecordedRoute,
        MailboxIntakeProjectionEndpoints.MailboxIntakeRoute,
        AssociationProjectionEndpoints.AssociationRoute,
        ParticipantResolutionProjectionEndpoints.ParticipantResolutionRoute,
        AiOutcomeProjectionEndpoints.AiOutcomeRecordedRoute,
        TaskIntentProjectionEndpoints.TaskIntentCapturedRoute,
        ApprovalProjectionEndpoints.ApprovalRecordedRoute,
    };

    /// <summary>
    /// The sidecar discovers exactly one subscription for the events topic, routed to the fan-out route with its
    /// dead-letter topic, plus the dead-letter drain. Before the fan-out, seven routes collided on the topic and the
    /// subscribe handler kept only the governed-operation route.
    /// </summary>
    [Fact]
    public async Task SubscriptionDiscoveryShouldRouteTheEventsTopicToOneFanOutSubscription()
    {
        using WebApplicationFactory<Program> factory = new WebApplicationFactory<Program>().WithAppChannelToken();
        using HttpClient client = factory.CreateAppChannelClient();

        using HttpResponseMessage response = await client
            .GetAsync("/dapr/subscribe", TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        string json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);
        using JsonDocument document = JsonDocument.Parse(json);
        JsonElement[] subscriptions = [.. document.RootElement.EnumerateArray()];

        JsonElement projection = subscriptions
            .Where(static subscription => Property(subscription, "topic") == ChatBotProjectionSubscriptionDefaults.Topic)
            .ShouldHaveSingleItem(json);
        Property(projection, "pubsubname").ShouldBe(ChatBotProjectionSubscriptionDefaults.PubSubName, json);
        Route(projection).ShouldBe(Route(FanOutRoute), json);
        Property(projection, "deadLetterTopic").ShouldBe(ChatBotProjectionSubscriptionDefaults.DeadLetterTopic, json);

        JsonElement drain = subscriptions
            .Where(static subscription => Property(subscription, "topic") == ChatBotProjectionSubscriptionDefaults.DeadLetterTopic)
            .ShouldHaveSingleItem(json);
        Property(drain, "pubsubname").ShouldBe(ChatBotProjectionSubscriptionDefaults.PubSubName, json);
        Route(drain).ShouldBe(Route(ChatBotDeadLetterProjectionEndpoints.DeadLetterDrainRoute), json);
        subscriptions.Length.ShouldBe(2, json);
    }

    /// <summary>
    /// Every projection route stays sidecar-guarded: a missing or forged application-channel token is refused before
    /// any projection runs.
    /// </summary>
    /// <param name="route">The projection route.</param>
    [Theory]
    [MemberData(nameof(SidecarRoutes))]
    public async Task ProjectionRoutesShouldRefuseUntrustedIngressWithoutProjecting(string route)
    {
        using WebApplicationFactory<Program> factory = new WebApplicationFactory<Program>().WithAppChannelToken();
        using HttpClient anonymous = factory.CreateClient();
        using HttpClient forged = factory.CreateClient();
        forged.DefaultRequestHeaders.Add(DaprAppChannelToken.HeaderName, "forged-app-channel-token");

        using HttpResponseMessage missing = await anonymous
            .PostAsJsonAsync(route, ProjectConversationProjectionTests.ApprovalRequestPublished(8), TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        using HttpResponseMessage wrong = await forged
            .PostAsJsonAsync(route, ProjectConversationProjectionTests.ApprovalRequestPublished(8), TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        missing.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        wrong.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await ConversationItemsAsync(factory).ConfigureAwait(true)).ShouldBeEmpty();
    }

    /// <summary>
    /// A family's event delivered on the fan-out route lands once in that family's read model, the other families
    /// ignore it, and a redelivery stays idempotent.
    /// </summary>
    /// <param name="family">The projection family whose published event is delivered.</param>
    [Theory]
    [InlineData("approval")]
    [InlineData("task-intent")]
    [InlineData("ai-outcome")]
    public async Task FanOutShouldProjectEachFamilyOnceAndStayIdempotentOnReplay(string family)
    {
        using WebApplicationFactory<Program> factory = new WebApplicationFactory<Program>().WithAppChannelToken();
        using HttpClient client = factory.CreateAppChannelClient();
        object published = family switch
        {
            "approval" => ProjectConversationProjectionTests.ApprovalRequestPublished(8),
            "task-intent" => TaskIntentMessagePublished(),
            _ => AiOutcomePublished(),
        };

        using HttpResponseMessage first = await client
            .PostAsJsonAsync(FanOutRoute, published, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        using HttpResponseMessage replay = await client
            .PostAsJsonAsync(FanOutRoute, published, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        first.StatusCode.ShouldBe(HttpStatusCode.OK);
        replay.StatusCode.ShouldBe(HttpStatusCode.OK);
        ProjectConversationItemView item = (await ConversationItemsAsync(factory).ConfigureAwait(true)).ShouldHaveSingleItem();
        switch (family)
        {
            case "approval":
                item.Kind.ShouldBe(ProjectConversationItemKind.ApprovalEvent);
                item.ApprovalId.ShouldBe("approval-001");
                item.SourceVersion.ShouldBe(8);
                break;
            case "task-intent":
                item.ItemId.ShouldBe("ui-message:001");
                item.SurfaceOrigin.ShouldBe("ui");
                item.SourceVersion.ShouldBe(12);
                break;
            default:
                item.Kind.ShouldBe(ProjectConversationItemKind.AiOutcome);
                item.SourceVersion.ShouldBe(60);
                break;
        }
    }

    /// <summary>
    /// A governed-operation event delivered on the fan-out route reaches the governed-operation read model, stays
    /// idempotent on redelivery, and leaves the conversation families untouched.
    /// </summary>
    [Fact]
    public async Task FanOutShouldProjectGovernedOperationEventsIdempotently()
    {
        using WebApplicationFactory<Program> factory = new WebApplicationFactory<Program>().WithAppChannelToken();
        using HttpClient client = factory.CreateAppChannelClient();

        using HttpResponseMessage first = await client
            .PostAsJsonAsync(FanOutRoute, GovernedNotePublished(), TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        using HttpResponseMessage replay = await client
            .PostAsJsonAsync(FanOutRoute, GovernedNotePublished(), TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        first.StatusCode.ShouldBe(HttpStatusCode.OK);
        replay.StatusCode.ShouldBe(HttpStatusCode.OK);
        IGovernedOperationProjectionStore store = factory.Services.GetRequiredService<IGovernedOperationProjectionStore>();
        GovernedOperationView view = (await store.GetAsync(Tenant, NoteId, TestContext.Current.CancellationToken).ConfigureAwait(true))
            .ShouldNotBeNull();
        view.SourceVersion.ShouldBe(1);
        (await ConversationItemsAsync(factory).ConfigureAwait(true)).ShouldBeEmpty();
    }

    /// <summary>
    /// A family whose envelope cannot bind the delivered body is skipped while the families that bind still project.
    /// </summary>
    [Fact]
    public async Task FanOutShouldSkipAFamilyWhoseEnvelopeCannotBind()
    {
        using WebApplicationFactory<Program> factory = new WebApplicationFactory<Program>().WithAppChannelToken();
        using HttpClient client = factory.CreateAppChannelClient();
        JsonObject body = JsonSerializer
            .SerializeToNode(ProjectConversationProjectionTests.ApprovalRequestPublished(8), JsonSerializerOptions.Web)
            .ShouldNotBeNull()
            .AsObject();

        // The mailbox-intake envelope types "sender" as a participant identity, so a string cannot bind to it.
        body["sender"] = "not-a-participant-identity";
        using HttpResponseMessage mailbox = await client
            .PostAsJsonAsync(MailboxIntakeProjectionEndpoints.MailboxIntakeRoute, body, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        using HttpResponseMessage fanOut = await client
            .PostAsJsonAsync(FanOutRoute, body, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        mailbox.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        fanOut.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await ConversationItemsAsync(factory).ConfigureAwait(true)).ShouldHaveSingleItem().Kind.ShouldBe(ProjectConversationItemKind.ApprovalEvent);
    }

    /// <summary>A body that is not a JSON object is refused before any family runs.</summary>
    /// <param name="body">The raw request body.</param>
    [Theory]
    [InlineData("")]
    [InlineData("{\"tenantId\":")]
    [InlineData("not json")]
    [InlineData("null")]
    [InlineData("[]")]
    public async Task FanOutShouldRejectABodyThatIsNotAJsonObject(string body)
    {
        using WebApplicationFactory<Program> factory = new WebApplicationFactory<Program>().WithAppChannelToken();
        using HttpClient client = factory.CreateAppChannelClient();
        using StringContent content = new(body, Encoding.UTF8, "application/json");

        using HttpResponseMessage response = await client
            .PostAsync(FanOutRoute, content, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await ConversationItemsAsync(factory).ConfigureAwait(true)).ShouldBeEmpty();
    }

    /// <summary>
    /// A family fault is not swallowed: the delivery fails so Dapr redelivers, and the redelivery projects once.
    /// </summary>
    [Fact]
    public async Task FanOutShouldPropagateAFamilyFaultSoDaprRedelivers()
    {
        FaultingGovernedOperationProjectionStore store = new(failures: 1);
        using WebApplicationFactory<Program> factory = new WebApplicationFactory<Program>().WithAppChannelToken()
            .WithWebHostBuilder(builder => builder.ConfigureServices(services =>
                services.AddSingleton<IGovernedOperationProjectionStore>(store)));
        using HttpClient client = factory.CreateAppChannelClient();

        using HttpResponseMessage faulted = await client
            .PostAsJsonAsync(FanOutRoute, GovernedNotePublished(), TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        (await store.GetAsync(Tenant, NoteId, TestContext.Current.CancellationToken).ConfigureAwait(true)).ShouldBeNull();
        using HttpResponseMessage redelivered = await client
            .PostAsJsonAsync(FanOutRoute, GovernedNotePublished(), TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        faulted.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
        redelivered.StatusCode.ShouldBe(HttpStatusCode.OK);
        store.SaveAttempts.ShouldBe(2);
        GovernedOperationView view = (await store.GetAsync(Tenant, NoteId, TestContext.Current.CancellationToken).ConfigureAwait(true))
            .ShouldNotBeNull();
        view.SourceVersion.ShouldBe(1);
    }

    // The subscribe handler emits routes without their leading slash, which the sidecar accepts either way.
    private static string? Route(JsonElement subscription) => Route(Property(subscription, "route"));

    private static string? Route(string? route) => route?.TrimStart('/');

    private static string? Property(JsonElement element, string name)
    {
        foreach (JsonProperty property in element.EnumerateObject())
        {
            if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                return property.Value.ValueKind == JsonValueKind.String ? property.Value.GetString() : property.Value.GetRawText();
            }
        }

        return null;
    }

    private static async Task<IReadOnlyList<ProjectConversationItemView>> ConversationItemsAsync(WebApplicationFactory<Program> factory)
    {
        ProjectConversationPage page = await factory.Services
            .GetRequiredService<IProjectConversationProjectionStore>()
            .ReadPageAsync(Tenant, ProjectId, null, 25, TestContext.Current.CancellationToken)
            .ConfigureAwait(false);
        return page.Items;
    }

    private static PublishedGovernedOperationEvent GovernedNotePublished()
        => new(
            Tenant,
            "chatbot",
            NoteId,
            GovernedOperationProjectionTranslator.GovernedNoteRecordedEventType,
            1,
            CorrelationId,
            MessageId,
            OccurredAt);

    private static PublishedTaskIntentEvent TaskIntentMessagePublished()
        => new(
            Tenant,
            "chatbot",
            "ui-message:001",
            typeof(ProjectConversationMessageAppended).FullName,
            12,
            OccurredAt,
            CorrelationId,
            null,
            UserMessage: new ProjectConversationMessageAppended(
                Tenant,
                ProjectId,
                "ui-message:001",
                "actor-001",
                "sha256:abcdef",
                13,
                "en-US",
                OccurredAt,
                CorrelationId,
                "metadata_only",
                "collaboration_input",
                "chatbot.project-conversation-message.v1",
                9,
                "wait-for-projection"));

    private static PublishedAiOutcomeEvent AiOutcomePublished()
        => new(
            Tenant,
            AiOutcomeProjectionTranslator.AiOutcomeDomain,
            "ai-aggregate-001",
            60,
            OccurredAt,
            CorrelationId,
            ProjectId,
            AiOutcomeKind.Proposal,
            AiOutcomeStatus.Proposed,
            "ai-actor-001",
            "ai",
            ProposalId: "proposal-001",
            RequestId: "request-001",
            RequesterId: "requester-001",
            SourceConversationItemId: "decision:source:001",
            SourceMessageId: "graph-message-001",
            OperationId: "operation-001",
            RiskClass: AiActionRiskClass.ApprovalRequired,
            RiskActionClasses: ["invokes-tools", "modifies-state"],
            PolicySnapshotId: "policy-snap-001",
            PolicySnapshotVisibility: "authorized",
            ContextPackageId: "context-package-001",
            AuthorizedContextReferences: ["evidence-001", "evidence-002"],
            CommandName: "send-summary",
            CommandAllowlistVersion: "allowlist.v1",
            ApprovalId: "approval-001",
            ApprovalStatus: "approved",
            ExecutionStatus: "pending",
            AuditOperationId: "audit-op-001",
            AuditStatus: "available",
            SafeNextAction: "review-ai-action");
}
