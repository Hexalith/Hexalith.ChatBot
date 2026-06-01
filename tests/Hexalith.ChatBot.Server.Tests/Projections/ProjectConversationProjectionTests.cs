using System.Net;
using System.Security.Claims;
using System.Text.Json;

using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Server.Association.Intake;
using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Gateway.Stages;
using Hexalith.ChatBot.Server.Operations;
using Hexalith.ChatBot.Server.Projections;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Projections;

public sealed class ProjectConversationProjectionTests
{
    private const string Tenant = "tenant-alpha";
    private const string OtherTenant = "tenant-beta";
    private const string AssociationId = "01ARZ3NDEKTSV4RRFFQ69G5FAV";
    private const string CorrelationId = "01ARZ3NDEKTSV4RRFFQ69G5FAW";
    private static readonly DateTimeOffset DetectedAt = new(2026, 6, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task AssociationHandlerShouldProjectTenantProjectPartitionedConversationItem()
    {
        InMemoryAssociationProjectionStore associationStore = new();
        InMemoryProjectConversationProjectionStore conversationStore = new();
        AssociationProjectionHandler handler = new(associationStore, new FixedClock(), conversationStore);

        AssociationProjectionHandler.ProjectionOutcome outcome = await handler.HandleAsync(Notification(1), TestContext.Current.CancellationToken);
        ProjectConversationPage page = await conversationStore.ReadPageAsync(Tenant, "project-001", null, 25, TestContext.Current.CancellationToken);
        ProjectConversationPage foreign = await conversationStore.ReadPageAsync(OtherTenant, "project-001", null, 25, TestContext.Current.CancellationToken);

        outcome.ShouldBe(AssociationProjectionHandler.ProjectionOutcome.Applied);
        ProjectConversationItemView item = page.Items.ShouldHaveSingleItem();
        item.TenantId.ShouldBe(Tenant);
        item.ProjectId.ShouldBe("project-001");
        item.SourceMailboxId.ShouldBe("controlled-mailbox-001");
        item.SourceConversationId.ShouldBe("conversation-001");
        item.SourceProviderMessageId.ShouldBeNull();
        item.InternetMessageId.ShouldBeNull();
        item.Kind.ShouldBe(ProjectConversationItemKind.EmailDerived);
        item.ActorKind.ShouldBe(ProjectConversationActorKind.Mailbox);
        foreign.Items.ShouldBeEmpty();
        ProjectConversationItemView.KeyFor(Tenant, "project-001", AssociationId).ShouldStartWith("tenant-alpha:project-conversation:project-001:");
    }

    [Fact]
    public async Task ConversationProjectionShouldMergeIntakeSourceIdentityWhenIntakeArrivesBeforeAssociation()
    {
        InMemoryProjectConversationProjectionStore conversationStore = new();
        AssociationProjectionHandler handler = new(new InMemoryAssociationProjectionStore(), new FixedClock(), conversationStore);

        await handler.HandleAsync(IntakeCaptured(), Tenant, 1, CorrelationId, TestContext.Current.CancellationToken);
        await handler.HandleAsync(Notification(2), TestContext.Current.CancellationToken);

        ProjectConversationItemView item = (await conversationStore.ReadPageAsync(Tenant, "project-001", null, 25, TestContext.Current.CancellationToken)).Items.ShouldHaveSingleItem();
        item.SourceProviderMessageId.ShouldBe("graph-message-001");
        item.InternetMessageId.ShouldBe("<internet-message-001@example.test>");
        item.SourceReceivedAtUtc.ShouldBe(new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero));
        item.SourceSentAtUtc.ShouldBe(new DateTimeOffset(2026, 5, 31, 23, 58, 0, TimeSpan.Zero));
        item.SourceCreatedAtUtc.ShouldBe(new DateTimeOffset(2026, 5, 31, 23, 57, 0, TimeSpan.Zero));
        item.SourceTimezone.ShouldBe("UTC");
        item.SourceProvenanceDisplayToken.ShouldBe("Microsoft 365 mailbox");
    }

    [Fact]
    public async Task ConversationProjectionShouldEnrichExistingAssociationWhenIntakeArrivesAfterAssociation()
    {
        InMemoryProjectConversationProjectionStore conversationStore = new();
        AssociationProjectionHandler handler = new(new InMemoryAssociationProjectionStore(), new FixedClock(), conversationStore);

        await handler.HandleAsync(Notification(2), TestContext.Current.CancellationToken);
        await handler.HandleAsync(IntakeCaptured(), Tenant, 3, CorrelationId, TestContext.Current.CancellationToken);

        ProjectConversationItemView item = (await conversationStore.ReadPageAsync(Tenant, "project-001", null, 25, TestContext.Current.CancellationToken)).Items.ShouldHaveSingleItem();
        item.SourceProviderMessageId.ShouldBe("graph-message-001");
        item.InternetMessageId.ShouldBe("<internet-message-001@example.test>");
        item.SourceVersion.ShouldBe(2);
    }

    [Fact]
    public async Task ConversationStoreShouldRejectOlderAssociationAfterSourceEmailEnrichment()
    {
        InMemoryProjectConversationProjectionStore store = new();

        await store.UpsertAsync(Item("item-a", 5, DetectedAt), TestContext.Current.CancellationToken);
        await store.UpsertSourceEmailAsync(SourceEmail(10, "graph-message-current"), TestContext.Current.CancellationToken);
        await store.UpsertAsync(
            Item("item-a", 4, DetectedAt.AddMinutes(-5)) with
            {
                LifecycleState = LifecycleState.Correcting,
                SafeNextAction = "wait-for-propagation",
            },
            TestContext.Current.CancellationToken);

        ProjectConversationItemView item = (await store.ReadPageAsync(Tenant, "project-001", null, 25, TestContext.Current.CancellationToken)).Items.ShouldHaveSingleItem();
        item.SourceVersion.ShouldBe(5);
        item.LifecycleState.ShouldBe(LifecycleState.Associated);
        item.SourceProviderMessageId.ShouldBe("graph-message-current");
    }

    [Fact]
    public async Task ConversationStoreShouldIgnoreStaleSourceEmailReplayWhenEnrichingExistingItems()
    {
        InMemoryProjectConversationProjectionStore store = new();

        await store.UpsertAsync(Item("item-a", 5, DetectedAt), TestContext.Current.CancellationToken);
        await store.UpsertSourceEmailAsync(SourceEmail(10, "graph-message-current"), TestContext.Current.CancellationToken);
        await store.UpsertSourceEmailAsync(SourceEmail(9, "graph-message-stale"), TestContext.Current.CancellationToken);

        ProjectConversationItemView item = (await store.ReadPageAsync(Tenant, "project-001", null, 25, TestContext.Current.CancellationToken)).Items.ShouldHaveSingleItem();
        item.SourceProviderMessageId.ShouldBe("graph-message-current");
    }

    [Fact]
    public async Task ConversationStoreShouldOrderByUtcSourceTimeAndIgnoreOlderReplays()
    {
        InMemoryProjectConversationProjectionStore store = new();

        await store.UpsertAsync(Item("item-b", 2, DetectedAt.AddMinutes(2)), TestContext.Current.CancellationToken);
        await store.UpsertAsync(Item("item-a", 1, DetectedAt), TestContext.Current.CancellationToken);
        await store.UpsertAsync(Item("item-b", 1, DetectedAt.AddMinutes(-5)), TestContext.Current.CancellationToken);

        ProjectConversationPage page = await store.ReadPageAsync(Tenant, "project-001", null, 25, TestContext.Current.CancellationToken);

        page.Items.Select(static item => item.ItemId).ShouldBe(["item-a", "item-b"], ignoreOrder: false);
        page.Items.Last().SourceVersion.ShouldBe(2);
        page.Items.Last().OccurredAt.ShouldBe(DetectedAt.AddMinutes(2));
    }

    [Fact]
    public void ConversationItemReplacementShouldRejectOlderReplays()
    {
        ProjectConversationItemView current = Item("item-b", 2, DetectedAt.AddMinutes(2));
        ProjectConversationItemView older = Item("item-b", 1, DetectedAt.AddMinutes(-5));
        ProjectConversationItemView newer = Item("item-b", 3, DetectedAt.AddMinutes(3));

        ProjectConversationItemView.ShouldReplace(current, older).ShouldBeFalse();
        ProjectConversationItemView.ShouldReplace(current, newer).ShouldBeTrue();
    }

    [Fact]
    public static void MailboxIntakeTranslatorShouldRejectUnsafeOrIncompleteEnvelope()
    {
        MailboxIntakeProjectionNotification notification = MailboxIntakeProjectionTranslator.TryCreateNotification(PublishedIntake(3)).ShouldNotBeNull();

        notification.TenantId.ShouldBe(Tenant);
        notification.Captured.ProviderMessageId.ShouldBe("graph-message-001");
        notification.SourceVersion.ShouldBe(3);

        MailboxIntakeProjectionTranslator.TryCreateNotification(PublishedIntake(3) with { Domain = "folders" }).ShouldBeNull();
        MailboxIntakeProjectionTranslator.TryCreateNotification(PublishedIntake(3) with { ReceivedAtUtc = default }).ShouldBeNull();
        MailboxIntakeProjectionTranslator.TryCreateNotification(PublishedIntake(0)).ShouldBeNull();
    }

    [Fact]
    public static void SourceEmailDisplayTokenShouldUseSafeFallbackForUnknownProvenance()
    {
        ProjectConversationSourceEmailView source = ProjectConversationSourceEmailView.FromIntake(
            Tenant,
            IntakeCaptured() with { SourceProvenance = "raw provider/source context" },
            3,
            CorrelationId);

        source.SourceProvenanceDisplayToken.ShouldBe("source-provenance-unavailable");
        source.SourceProvenanceDisplayToken.ShouldNotContain("raw provider", Case.Insensitive);
    }

    [Fact]
    public async Task CursorShouldBeProjectScopedAndNotExposeTenantOrProjectText()
    {
        InMemoryProjectConversationProjectionStore store = new();
        await store.UpsertAsync(Item("item-a", 1, DetectedAt), TestContext.Current.CancellationToken);
        await store.UpsertAsync(Item("item-b", 2, DetectedAt.AddMinutes(1)), TestContext.Current.CancellationToken);

        ProjectConversationPage first = await store.ReadPageAsync(Tenant, "project-001", null, 1, TestContext.Current.CancellationToken);
        first.HasMore.ShouldBeTrue();
        first.NextCursor.ShouldNotBeNull();
        first.NextCursor.ShouldNotContain(Tenant, Case.Sensitive);
        first.NextCursor.ShouldNotContain("project-001", Case.Sensitive);

        ProjectConversationPage second = await store.ReadPageAsync(Tenant, "project-001", first.NextCursor, 1, TestContext.Current.CancellationToken);
        second.Items.ShouldHaveSingleItem().ItemId.ShouldBe("item-b");

        ProjectConversationPage wrongProject = await store.ReadPageAsync(Tenant, "project-002", first.NextCursor, 1, TestContext.Current.CancellationToken);
        wrongProject.Items.ShouldBeEmpty();
    }

    [Fact]
    public async Task CorrectingStateShouldRenderAsSystemDecisionWithStaleSafeAction()
    {
        InMemoryProjectConversationProjectionStore conversationStore = new();
        AssociationProjectionHandler handler = new(new InMemoryAssociationProjectionStore(), new FixedClock(), conversationStore);

        await handler.HandleAsync(
            Notification(3) with
            {
                LifecycleState = LifecycleState.Correcting,
                CorrectionKind = AssociationCorrectionKind.ProjectReassignment,
                SafeNextAction = "wait-for-propagation",
                IsCorrectedContextStale = true,
            },
            TestContext.Current.CancellationToken);

        ProjectConversationItemView item = (await conversationStore.ReadPageAsync(Tenant, "project-001", null, 25, TestContext.Current.CancellationToken)).Items.ShouldHaveSingleItem();
        item.Kind.ShouldBe(ProjectConversationItemKind.SystemDecision);
        item.ActorKind.ShouldBe(ProjectConversationActorKind.SystemDecision);
        item.SafeNextAction.ShouldBe("wait-for-propagation");
        item.LifecycleState.ShouldBe(LifecycleState.Correcting);
    }

    [Fact]
    public async Task ProjectConversationEndpointShouldReturnEmptyStateOnlyForAuthorizedEmptyProject()
    {
        using WebApplicationFactory<Program> factory = CreateFactoryWithProjectClaim("empty-project");
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage authorized = await client
            .GetAsync("/api/v1/projects/empty-project/conversation", TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        string authorizedBody = await authorized.Content
            .ReadAsStringAsync(TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        authorized.StatusCode.ShouldBe(HttpStatusCode.OK);
        using JsonDocument document = JsonDocument.Parse(authorizedBody);
        document.RootElement.GetProperty("projectId").GetString().ShouldBe("empty-project");
        document.RootElement.GetProperty("status").GetString().ShouldBe("empty");
        document.RootElement.GetProperty("items").GetArrayLength().ShouldBe(0);

        using HttpResponseMessage unauthorized = await client
            .GetAsync("/api/v1/projects/other-project/conversation", TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        unauthorized.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    private static AssociationNotification Notification(long sourceVersion)
        => new(
            Tenant,
            AssociationId,
            "01ARZ3NDEKTSV4RRFFQ69G5FAY",
            "controlled-mailbox-001",
            "conversation-001",
            "thread-001",
            "project-001",
            "Project One",
            LifecycleState.Associated,
            AssociationScoringOutcome.AutoAssociated,
            AssociationThresholdBand.Auto,
            0.9,
            [new AssociationCandidate("project-001", "Project One", 0.9, 1, [AssociationReasonCode.ExplicitProjectIdentifierMatched], [], [], true)],
            [],
            "association-thresholds.m0.default.v1",
            "association-deterministic.kernel.m0.v1",
            "metadata_only",
            "collaboration_input",
            sourceVersion,
            DetectedAt,
            CorrelationId);

    private static ProjectConversationItemView Item(string itemId, long sourceVersion, DateTimeOffset occurredAt)
        => new(
            Tenant,
            "project-001",
            "Project One",
            itemId,
            "01ARZ3NDEKTSV4RRFFQ69G5FAY",
            ProjectConversationItemKind.EmailDerived,
            ProjectConversationActorKind.Mailbox,
            "Mailbox event",
            occurredAt,
            LifecycleState.Associated,
            AssociationThresholdBand.Auto,
            0.9,
            AssociationId,
            "controlled-mailbox-001",
            null,
            null,
            "conversation-001",
            "thread-001",
            null,
            null,
            null,
            null,
            null,
            AssociationCandidateView.MailboxSourceProvenance,
            "metadata_only",
            "collaboration_input",
            ProjectConversationItemView.CurrentSchemaVersion,
            sourceVersion,
            CorrelationId);

    private static MailboxMessageIntakeCaptured IntakeCaptured()
        => new(
            "01ARZ3NDEKTSV4RRFFQ69G5FAY",
            "graph-message-001",
            "<internet-message-001@example.test>",
            "conversation-001",
            "thread-001",
            "controlled-mailbox-001",
            new MailboxParticipantIdentity("sender-safe-label", "redacted"),
            [],
            new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 5, 31, 23, 58, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 5, 31, 23, 57, 0, TimeSpan.Zero),
            [],
            "UTC",
            "opaque-graph-delta-context",
            "m365-mailbox-intake",
            "association-deterministic.kernel.m0.v1",
            "metadata_only",
            "collaboration_input",
            1);

    private static ProjectConversationSourceEmailView SourceEmail(long sourceVersion, string providerMessageId)
        => ProjectConversationSourceEmailView.FromIntake(
            Tenant,
            IntakeCaptured() with { ProviderMessageId = providerMessageId },
            sourceVersion,
            CorrelationId);

    private static PublishedMailboxIntakeEvent PublishedIntake(long sequenceNumber)
        => new(
            Tenant,
            ChatBotEventStore.DomainName,
            MailboxIntakeProjectionTranslator.IntakeCapturedEventType,
            sequenceNumber,
            CorrelationId,
            "01ARZ3NDEKTSV4RRFFQ69G5FAY",
            "graph-message-001",
            "<internet-message-001@example.test>",
            "conversation-001",
            "thread-001",
            "controlled-mailbox-001",
            new MailboxParticipantIdentity("sender-safe-label", "redacted"),
            [],
            new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 5, 31, 23, 58, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 5, 31, 23, 57, 0, TimeSpan.Zero),
            [],
            "UTC",
            "opaque-graph-delta-context",
            AssociationCandidateView.MailboxSourceProvenance,
            "association-deterministic.kernel.m0.v1",
            "metadata_only",
            "collaboration_input",
            1);

    private static WebApplicationFactory<Program> CreateFactoryWithProjectClaim(string projectId)
    {
        InMemoryProjectConversationProjectionStore conversationStore = new();
        return new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder => builder.ConfigureServices(services =>
            {
                services.AddSingleton<IProjectConversationProjectionStore>(conversationStore);
                services.AddSingleton<IStartupFilter>(new TestPrincipalStartupFilter(projectId));
            }));
    }

    private sealed class FixedClock : ISystemClock
    {
        public DateTimeOffset UtcNow { get; } = new(2026, 6, 1, 1, 0, 0, TimeSpan.Zero);
    }

    private sealed class TestPrincipalStartupFilter(string projectId) : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
            => app =>
            {
                app.Use(async (context, continuation) =>
                {
                    context.User = new ClaimsPrincipal(new ClaimsIdentity(
                        [
                            new Claim("sub", "actor-001"),
                            new Claim("eventstore:tenant", Tenant),
                            new Claim(ParticipantAuthorizationStage.ProjectOwnerClaim, projectId),
                        ],
                        "test"));
                    await continuation().ConfigureAwait(false);
                });
                next(app);
            };
    }
}
