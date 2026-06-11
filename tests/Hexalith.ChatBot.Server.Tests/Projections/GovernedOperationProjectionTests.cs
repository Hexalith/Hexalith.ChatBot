using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Gateway.Stages;
using Hexalith.ChatBot.Server.Governance.AiActor;
using Hexalith.ChatBot.Server.Governance.CommandCapability;
using Hexalith.ChatBot.Server.Governance.Mailbox;
using Hexalith.ChatBot.Server.Governance.Outbound;
using Hexalith.ChatBot.Server.Governance.ServiceClient;
using Hexalith.ChatBot.Server.Projections;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Projections;

public sealed class GovernedOperationProjectionTests
{
    private const string Tenant = "tenant-alpha";
    private const string OtherTenant = "tenant-beta";
    private const string NoteId = "01ARZ3NDEKTSV4RRFFQ69G5FAZ";
    private const string MessageId = "01ARZ3NDEKTSV4RRFFQ69G5FAY";
    private const string CorrelationId = "01ARZ3NDEKTSV4RRFFQ69G5FAW";

    private static readonly DateTimeOffset RecordedAt = new(2026, 5, 31, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task HandleNewNotificationShouldProjectDerivedRecordShape()
    {
        InMemoryGovernedOperationProjectionStore store = new();
        FixedClock clock = new();
        GovernedOperationProjectionHandler handler = new(store, clock);

        GovernedOperationProjectionHandler.ProjectionOutcome outcome = await handler.HandleAsync(
            Notification(sourceVersion: 1),
            TestContext.Current.CancellationToken);

        outcome.ShouldBe(GovernedOperationProjectionHandler.ProjectionOutcome.Applied);
        GovernedOperationView view = (await store.GetAsync(Tenant, NoteId, TestContext.Current.CancellationToken)).ShouldNotBeNull();
        view.TenantId.ShouldBe(Tenant);
        view.NoteId.ShouldBe(NoteId);
        view.SchemaVersion.ShouldBe(GovernedOperationView.CurrentSchemaVersion);
        view.SourceProvenance.ShouldBe(GovernedOperationView.GovernedCommandProvenance);
        view.DerivationKernelVersion.ShouldBe(GovernedOperationView.CurrentDerivationKernelVersion);
        view.RedactionState.ShouldBe(GovernedOperationView.MetadataOnlyRedactionState);
        view.RetentionClass.ShouldBe(GovernedOperationView.GovernedOperationalRetentionClass);
        view.SourceVersion.ShouldBe(1);
        view.RecordedAt.ShouldBe(RecordedAt);
        view.LastUpdatedAt.ShouldBe(FixedClock.FixedUtcNow);
    }

    [Fact]
    public async Task DuplicateNotificationShouldBeIgnoredLeavingExactlyOneDurableEffect()
    {
        InMemoryGovernedOperationProjectionStore store = new();
        GovernedOperationProjectionHandler handler = new(store, new FixedClock());

        GovernedOperationProjectionHandler.ProjectionOutcome first = await handler.HandleAsync(Notification(1), TestContext.Current.CancellationToken);
        GovernedOperationView afterFirst = (await store.GetAsync(Tenant, NoteId, TestContext.Current.CancellationToken)).ShouldNotBeNull();
        GovernedOperationProjectionHandler.ProjectionOutcome replay = await handler.HandleAsync(Notification(1), TestContext.Current.CancellationToken);
        GovernedOperationView afterReplay = (await store.GetAsync(Tenant, NoteId, TestContext.Current.CancellationToken)).ShouldNotBeNull();

        first.ShouldBe(GovernedOperationProjectionHandler.ProjectionOutcome.Applied);
        replay.ShouldBe(GovernedOperationProjectionHandler.ProjectionOutcome.Ignored);
        afterReplay.ShouldBe(afterFirst);
    }

    [Fact]
    public async Task OutOfOrderNotificationShouldBeDroppedAndHigherVersionShouldAdvanceWhilePreservingRecordedAt()
    {
        InMemoryGovernedOperationProjectionStore store = new();
        GovernedOperationProjectionHandler handler = new(store, new FixedClock());

        _ = await handler.HandleAsync(Notification(2), TestContext.Current.CancellationToken);
        GovernedOperationView afterTwo = (await store.GetAsync(Tenant, NoteId, TestContext.Current.CancellationToken)).ShouldNotBeNull();

        // Stale (lower version) → dropped, last-writer-wins by source version.
        GovernedOperationProjectionHandler.ProjectionOutcome stale = await handler.HandleAsync(
            Notification(1, recordedAt: RecordedAt.AddMinutes(-5)),
            TestContext.Current.CancellationToken);
        GovernedOperationView afterStale = (await store.GetAsync(Tenant, NoteId, TestContext.Current.CancellationToken)).ShouldNotBeNull();

        // Newer version → advances, RecordedAt preserved from the first applied event.
        GovernedOperationProjectionHandler.ProjectionOutcome newer = await handler.HandleAsync(
            Notification(3, recordedAt: RecordedAt.AddMinutes(5)),
            TestContext.Current.CancellationToken);
        GovernedOperationView afterNewer = (await store.GetAsync(Tenant, NoteId, TestContext.Current.CancellationToken)).ShouldNotBeNull();

        stale.ShouldBe(GovernedOperationProjectionHandler.ProjectionOutcome.Ignored);
        afterStale.ShouldBe(afterTwo);
        newer.ShouldBe(GovernedOperationProjectionHandler.ProjectionOutcome.Applied);
        afterNewer.SourceVersion.ShouldBe(3);
        afterNewer.RecordedAt.ShouldBe(afterTwo.RecordedAt);
    }

    [Fact]
    public async Task ProjectionShouldBeTenantPartitionedAndNeverLeakAcrossTenants()
    {
        InMemoryGovernedOperationProjectionStore store = new();
        GovernedOperationProjectionHandler handler = new(store, new FixedClock());

        _ = await handler.HandleAsync(Notification(1), TestContext.Current.CancellationToken);

        (await store.GetAsync(Tenant, NoteId, TestContext.Current.CancellationToken)).ShouldNotBeNull();
        (await store.GetAsync(OtherTenant, NoteId, TestContext.Current.CancellationToken)).ShouldBeNull();
    }

    [Fact]
    public async Task ProjectionEndpointShouldApplyPublishedEventAndStayIdempotentOnReplay()
    {
        using WebApplicationFactory<Program> factory = new();
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage first = await client
            .PostAsJsonAsync(
                GovernedOperationProjectionEndpoints.GovernedNoteRecordedRoute,
                PublishedEvent(1),
                TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        using HttpResponseMessage replay = await client
            .PostAsJsonAsync(
                GovernedOperationProjectionEndpoints.GovernedNoteRecordedRoute,
                PublishedEvent(1),
                TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        first.StatusCode.ShouldBe(HttpStatusCode.OK);
        replay.StatusCode.ShouldBe(HttpStatusCode.OK);

        IGovernedOperationProjectionStore store = factory.Services.GetRequiredService<IGovernedOperationProjectionStore>();
        GovernedOperationView view = (await store.GetAsync(Tenant, NoteId, TestContext.Current.CancellationToken)).ShouldNotBeNull();
        view.SourceVersion.ShouldBe(1);
        view.RedactionState.ShouldBe(GovernedOperationView.MetadataOnlyRedactionState);
    }

    [Fact]
    public async Task ProjectionEndpointShouldApplyControlEventAndStayIdempotentOnReplay()
    {
        using WebApplicationFactory<Program> factory = new();
        using HttpClient client = factory.CreateClient();

        PublishedGovernedOperationEvent published = PublishedControlEvent(
            new ServiceClientDisabled(
                "change",
                Tenant,
                "service-client-001",
                "requester",
                "approver",
                "reason",
                "policy",
                ServiceClientControlState.Active,
                ServiceClientControlState.Disabled,
                RecordedAt,
                3,
                CorrelationId));

        using HttpResponseMessage first = await client
            .PostAsJsonAsync(
                GovernedOperationProjectionEndpoints.GovernedNoteRecordedRoute,
                published,
                TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        using HttpResponseMessage replay = await client
            .PostAsJsonAsync(
                GovernedOperationProjectionEndpoints.GovernedNoteRecordedRoute,
                published,
                TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        first.StatusCode.ShouldBe(HttpStatusCode.OK);
        replay.StatusCode.ShouldBe(HttpStatusCode.OK);

        IGovernedControlStateProjectionStore store = factory.Services.GetRequiredService<IGovernedControlStateProjectionStore>();
        GovernedControlStateView view = (await store.GetAsync(
            Tenant,
            GovernedControlSubjectClasses.ServiceClient,
            "service-client-001",
            TestContext.Current.CancellationToken)).ShouldNotBeNull();
        view.ControlState.ShouldBe(GovernedControlStateView.Disabled);
        view.SourceVersion.ShouldBe(3);
        view.RevocationSensitive.ShouldBeTrue();
    }

    [Fact]
    public static void TranslatorShouldDeriveTenantAndSourceVersionFromTheVerifiedEnvelopeOnly()
    {
        // M1/M2: tenant and source version come from the EventStore-stamped envelope, not a caller body. A
        // forged tenant on a separate field cannot exist — the envelope IS the verified source.
        GovernedNoteRecordedNotification notification =
            GovernedOperationProjectionTranslator.TryCreateNotification(PublishedEvent(sourceVersion: 7)).ShouldNotBeNull();

        notification.TenantId.ShouldBe(Tenant);
        notification.NoteId.ShouldBe(NoteId);
        notification.SourceVersion.ShouldBe(7);
        notification.CorrelationId.ShouldBe(CorrelationId);
    }

    [Fact]
    public static void TranslatorShouldIgnoreEventsFromAnotherDomainOrType()
    {
        // A foreign domain or an unrelated event type delivered on the topic is ignored (no projection),
        // and a missing tenant/aggregate or non-positive version is treated as malformed.
        GovernedOperationProjectionTranslator.TryCreateNotification(PublishedEvent(1) with { Domain = "folders" }).ShouldBeNull();
        GovernedOperationProjectionTranslator.TryCreateNotification(PublishedEvent(1) with { EventTypeName = "Some.Other.Event" }).ShouldBeNull();
        GovernedOperationProjectionTranslator.TryCreateNotification(PublishedEvent(1) with { TenantId = " " }).ShouldBeNull();
        GovernedOperationProjectionTranslator.TryCreateNotification(PublishedEvent(0)).ShouldBeNull();
        GovernedOperationProjectionTranslator.TryCreateNotification(null).ShouldBeNull();
    }

    [Theory]
    [MemberData(nameof(ControlEvents))]
    public static void ControlTranslatorShouldProjectEveryGovernedSubjectClass(PublishedGovernedOperationEvent published, string subjectClass, string subjectRef, string state, int? budget)
    {
        GovernedControlStateProjectionNotification notification =
            GovernedControlStateProjectionTranslator.TryCreateNotification(published).ShouldNotBeNull();

        notification.TenantId.ShouldBe(Tenant);
        notification.SubjectClass.ShouldBe(subjectClass);
        notification.SubjectRef.ShouldBe(subjectRef);
        notification.ControlState.ShouldBe(state);
        notification.RateLimitBudget.ShouldBe(budget);
        notification.SourceVersion.ShouldBe(3);
    }

    [Fact]
    public async Task ControlProjectionShouldBeVersionTolerantAndTenantPartitioned()
    {
        InMemoryGovernedControlStateProjectionStore store = new();
        GovernedControlStateProjectionHandler handler = new(store, new FixedClock());

        GovernedControlStateProjectionNotification newer = new(
            Tenant,
            GovernedControlSubjectClasses.ServiceClient,
            "service-client-001",
            GovernedControlStateView.Disabled,
            null,
            null,
            2,
            CorrelationId,
            RecordedAt,
            RevocationSensitive: true,
            GovernedControlDimension.ControlState);
        GovernedControlStateProjectionNotification stale = newer with
        {
            ControlState = GovernedControlStateView.Active,
            SourceVersion = 1,
        };

        (await handler.HandleAsync(newer, TestContext.Current.CancellationToken)).ShouldBe(GovernedControlStateProjectionHandler.ProjectionOutcome.Applied);
        (await handler.HandleAsync(stale, TestContext.Current.CancellationToken)).ShouldBe(GovernedControlStateProjectionHandler.ProjectionOutcome.Ignored);

        GovernedControlStateView view = (await store.GetAsync(Tenant, GovernedControlSubjectClasses.ServiceClient, "service-client-001", TestContext.Current.CancellationToken)).ShouldNotBeNull();
        view.ControlState.ShouldBe(GovernedControlStateView.Disabled);
        view.SourceVersion.ShouldBe(2);
        (await store.GetAsync(OtherTenant, GovernedControlSubjectClasses.ServiceClient, "service-client-001", TestContext.Current.CancellationToken)).ShouldBeNull();
    }

    [Fact]
    public async Task ControlStateAndRateLimitEventsShouldOverlayIndependentlyWithoutClobberingEachOther()
    {
        InMemoryGovernedControlStateProjectionStore store = new();
        GovernedControlStateProjectionHandler handler = new(store, new FixedClock());

        // A disabled subject that is later given a rate-limit budget must stay disabled — the rate-limit event must not
        // re-activate it — while the budget is still overlaid onto the same record.
        GovernedControlStateProjectionNotification disabled = new(
            Tenant, GovernedControlSubjectClasses.ServiceClient, "service-001", GovernedControlStateView.Disabled,
            null, null, 1, CorrelationId, RecordedAt, RevocationSensitive: true, GovernedControlDimension.ControlState);
        GovernedControlStateProjectionNotification rateLimitAfterDisable = new(
            Tenant, GovernedControlSubjectClasses.ServiceClient, "service-001", GovernedControlStateView.Active,
            5, GovernedControlStateView.RollingHour, 2, CorrelationId, RecordedAt, RevocationSensitive: false, GovernedControlDimension.RateLimit);

        (await handler.HandleAsync(disabled, TestContext.Current.CancellationToken)).ShouldBe(GovernedControlStateProjectionHandler.ProjectionOutcome.Applied);
        (await handler.HandleAsync(rateLimitAfterDisable, TestContext.Current.CancellationToken)).ShouldBe(GovernedControlStateProjectionHandler.ProjectionOutcome.Applied);

        GovernedControlStateView disabledView = (await store.GetAsync(Tenant, GovernedControlSubjectClasses.ServiceClient, "service-001", TestContext.Current.CancellationToken)).ShouldNotBeNull();
        disabledView.ControlState.ShouldBe(GovernedControlStateView.Disabled);
        disabledView.RateLimitBudget.ShouldBe(5);
        disabledView.RevocationSensitive.ShouldBeTrue();
        disabledView.SourceVersion.ShouldBe(2);

        // The reverse ordering: a rate-limited subject that is later quarantined must keep its configured budget — the
        // control-state event must not wipe it.
        GovernedControlStateProjectionNotification rateLimitFirst = new(
            Tenant, GovernedControlSubjectClasses.AiActor, "ai-001", GovernedControlStateView.Active,
            7, GovernedControlStateView.RollingHour, 1, CorrelationId, RecordedAt, RevocationSensitive: false, GovernedControlDimension.RateLimit);
        GovernedControlStateProjectionNotification quarantineAfterRateLimit = new(
            Tenant, GovernedControlSubjectClasses.AiActor, "ai-001", GovernedControlStateView.Quarantined,
            null, null, 2, CorrelationId, RecordedAt, RevocationSensitive: true, GovernedControlDimension.ControlState);

        (await handler.HandleAsync(rateLimitFirst, TestContext.Current.CancellationToken)).ShouldBe(GovernedControlStateProjectionHandler.ProjectionOutcome.Applied);
        (await handler.HandleAsync(quarantineAfterRateLimit, TestContext.Current.CancellationToken)).ShouldBe(GovernedControlStateProjectionHandler.ProjectionOutcome.Applied);

        GovernedControlStateView quarantinedView = (await store.GetAsync(Tenant, GovernedControlSubjectClasses.AiActor, "ai-001", TestContext.Current.CancellationToken)).ShouldNotBeNull();
        quarantinedView.ControlState.ShouldBe(GovernedControlStateView.Quarantined);
        quarantinedView.RateLimitBudget.ShouldBe(7);
        quarantinedView.RevocationSensitive.ShouldBeTrue();
        quarantinedView.SourceVersion.ShouldBe(2);
    }

    [Fact]
    public async Task ProjectionBackedProviderShouldReturnProjectedStateAndFailClosedWhenRevocationSensitiveStateIsStale()
    {
        InMemoryGovernedControlStateProjectionStore store = new();
        FixedClock clock = new();
        await store.SaveAsync(
            new GovernedControlStateView(
                Tenant,
                GovernedControlSubjectClasses.ServiceClient,
                "service-client-001",
                GovernedControlStateView.Quarantined,
                null,
                null,
                1,
                CorrelationId,
                RecordedAt,
                FixedClock.FixedUtcNow,
                RevocationSensitive: true),
            TestContext.Current.CancellationToken);
        ProjectionBackedServiceClientControlStateProvider provider = new(store, clock);

        (await provider.GetControlStateAsync(Tenant, "service-client-001", TestContext.Current.CancellationToken)).ShouldBe(ServiceClientControlState.Quarantined);
        (await provider.GetControlStateAsync(OtherTenant, "service-client-001", TestContext.Current.CancellationToken)).ShouldBe(ServiceClientControlState.Active);

        FixedClock staleClock = new(FixedClock.FixedUtcNow.AddSeconds(61));
        ProjectionBackedServiceClientControlStateProvider staleProvider = new(store, staleClock);
        (await staleProvider.GetControlStateAsync(Tenant, "service-client-001", TestContext.Current.CancellationToken)).ShouldBe(ServiceClientControlState.Disabled);
    }

    [Fact]
    public async Task ProjectionBackedControlProvidersShouldMapEveryRuntimeSubjectClassAndApplyFreshnessBounds()
    {
        InMemoryGovernedControlStateProjectionStore store = new();
        FixedClock clock = new();
        await SaveControlViewAsync(
            store,
            GovernedControlSubjectClasses.ServiceClient,
            "service-client-001",
            GovernedControlStateView.Disabled,
            FixedClock.FixedUtcNow,
            revocationSensitive: true);
        await SaveControlViewAsync(
            store,
            GovernedControlSubjectClasses.AiActor,
            "ai-actor-001",
            GovernedControlStateView.Quarantined,
            FixedClock.FixedUtcNow,
            revocationSensitive: true);
        await SaveControlViewAsync(
            store,
            GovernedControlSubjectClasses.CommandCapability,
            nameof(Hexalith.ChatBot.Contracts.Commands.RecordGovernedNote),
            GovernedControlStateView.Disabled,
            FixedClock.FixedUtcNow,
            revocationSensitive: false);
        await SaveControlViewAsync(
            store,
            GovernedControlSubjectClasses.OutboundChannel,
            "adapter:mailbox-outbound",
            GovernedControlStateView.Quarantined,
            FixedClock.FixedUtcNow,
            revocationSensitive: false);

        (await new ProjectionBackedServiceClientControlStateProvider(store, clock)
            .GetControlStateAsync(Tenant, "service-client-001", TestContext.Current.CancellationToken))
            .ShouldBe(ServiceClientControlState.Disabled);
        (await new ProjectionBackedAiActorControlStateProvider(store, clock)
            .GetControlStateAsync(Tenant, "ai-actor-001", TestContext.Current.CancellationToken))
            .ShouldBe(AiActorControlState.Quarantined);
        (await new ProjectionBackedCommandCapabilityControlStateProvider(store, clock)
            .GetControlStateAsync(Tenant, nameof(Hexalith.ChatBot.Contracts.Commands.RecordGovernedNote), TestContext.Current.CancellationToken))
            .ShouldBe(CommandCapabilityControlState.Disabled);
        (await new ProjectionBackedOutboundChannelControlStateProvider(store, clock)
            .GetControlStateAsync(Tenant, "adapter:mailbox-outbound", TestContext.Current.CancellationToken))
            .ShouldBe(OutboundChannelControlState.Quarantined);

        FixedClock revocationFreshClock = new(FixedClock.FixedUtcNow.AddSeconds(60));
        FixedClock revocationStaleClock = new(FixedClock.FixedUtcNow.AddSeconds(61));
        (await new ProjectionBackedAiActorControlStateProvider(store, revocationFreshClock)
            .GetControlStateAsync(Tenant, "ai-actor-001", TestContext.Current.CancellationToken))
            .ShouldBe(AiActorControlState.Quarantined);
        (await new ProjectionBackedAiActorControlStateProvider(store, revocationStaleClock)
            .GetControlStateAsync(Tenant, "ai-actor-001", TestContext.Current.CancellationToken))
            .ShouldBe(AiActorControlState.Disabled);

        FixedClock ordinaryFreshClock = new(FixedClock.FixedUtcNow.AddMinutes(5));
        FixedClock ordinaryStaleClock = new(FixedClock.FixedUtcNow.AddMinutes(5).AddSeconds(1));
        (await new ProjectionBackedOutboundChannelControlStateProvider(store, ordinaryFreshClock)
            .GetControlStateAsync(Tenant, "adapter:mailbox-outbound", TestContext.Current.CancellationToken))
            .ShouldBe(OutboundChannelControlState.Quarantined);
        (await new ProjectionBackedOutboundChannelControlStateProvider(store, ordinaryStaleClock)
            .GetControlStateAsync(Tenant, "adapter:mailbox-outbound", TestContext.Current.CancellationToken))
            .ShouldBe(OutboundChannelControlState.Disabled);
    }

    [Fact]
    public async Task ProjectionBackedRateLimitProvidersShouldMapEveryRuntimeSubjectClassAndNeverRaiseOutOfBoundsBudgets()
    {
        InMemoryGovernedControlStateProjectionStore store = new();
        FixedClock clock = new();
        await SaveControlViewAsync(
            store,
            GovernedControlSubjectClasses.ServiceClient,
            "shared-ref",
            GovernedControlStateView.Active,
            FixedClock.FixedUtcNow,
            rateLimitBudget: ServiceClientRateLimitBounds.Maximum + 1);
        await SaveControlViewAsync(
            store,
            GovernedControlSubjectClasses.AiActor,
            "shared-ref",
            GovernedControlStateView.Active,
            FixedClock.FixedUtcNow,
            rateLimitBudget: AiActorRateLimitBounds.Maximum + 1);
        await SaveControlViewAsync(
            store,
            GovernedControlSubjectClasses.CommandCapability,
            "shared-ref",
            GovernedControlStateView.Active,
            FixedClock.FixedUtcNow,
            rateLimitBudget: CommandCapabilityRateLimitBounds.Maximum + 1);
        await SaveControlViewAsync(
            store,
            GovernedControlSubjectClasses.OutboundChannel,
            "shared-ref",
            GovernedControlStateView.Active,
            FixedClock.FixedUtcNow,
            rateLimitBudget: OutboundChannelRateLimitBounds.Maximum + 1);

        ServiceClientRateLimitState serviceClient = (await new ProjectionBackedServiceClientRateLimitProvider(store, clock)
            .GetRateLimitAsync(Tenant, "shared-ref", TestContext.Current.CancellationToken)).ShouldNotBeNull();
        AiActorRateLimitState aiActor = (await new ProjectionBackedAiActorRateLimitProvider(store, clock)
            .GetRateLimitAsync(Tenant, "shared-ref", TestContext.Current.CancellationToken)).ShouldNotBeNull();
        CommandCapabilityRateLimitState commandCapability = (await new ProjectionBackedCommandCapabilityRateLimitProvider(store, clock)
            .GetRateLimitAsync(Tenant, "shared-ref", TestContext.Current.CancellationToken)).ShouldNotBeNull();
        OutboundChannelRateLimitState outboundChannel = (await new ProjectionBackedOutboundChannelRateLimitProvider(store, clock)
            .GetRateLimitAsync(Tenant, "shared-ref", TestContext.Current.CancellationToken)).ShouldNotBeNull();

        serviceClient.EffectiveBudget.ShouldBe(ServiceClientRateLimitBounds.SafeDefaults.HourlyCommandBudget);
        aiActor.EffectiveBudget.ShouldBe(AiActorRateLimitBounds.SafeDefaults.HourlyProposalBudget);
        commandCapability.EffectiveBudget.ShouldBe(CommandCapabilityRateLimitBounds.SafeDefaults.HourlyCommandBudget);
        outboundChannel.EffectiveBudget.ShouldBe(OutboundChannelRateLimitBounds.SafeDefaults.HourlySendBudget);

        FixedClock staleClock = new(FixedClock.FixedUtcNow.AddMinutes(5).AddSeconds(1));
        ServiceClientRateLimitState staleServiceClient = (await new ProjectionBackedServiceClientRateLimitProvider(store, staleClock)
            .GetRateLimitAsync(Tenant, "shared-ref", TestContext.Current.CancellationToken)).ShouldNotBeNull();
        staleServiceClient.EffectiveBudget.ShouldBe(1);
    }

    [Fact]
    public async Task ProjectionBackedRateLimitHistoryShouldAdvanceOnlyTheMatchingTenantSubject()
    {
        InMemoryGovernedControlStateProjectionStore store = new();
        await store.SaveAsync(
            new GovernedControlStateView(
                Tenant,
                GovernedControlSubjectClasses.CommandCapability,
                nameof(Hexalith.ChatBot.Contracts.Commands.RecordGovernedNote),
                GovernedControlStateView.Active,
                2,
                GovernedControlStateView.RollingHour,
                1,
                CorrelationId,
                RecordedAt,
                FixedClock.FixedUtcNow,
                RevocationSensitive: false),
            TestContext.Current.CancellationToken);
        ProjectionBackedCommandCapabilityCommandHistory history = new(store, new FixedClock());

        await history.RecordAdmittedAsync(
            Tenant,
            nameof(Hexalith.ChatBot.Contracts.Commands.RecordGovernedNote),
            FixedClock.FixedUtcNow,
            TestContext.Current.CancellationToken);

        (await history.GetRecentAdmittedAsync(Tenant, nameof(Hexalith.ChatBot.Contracts.Commands.RecordGovernedNote), TestContext.Current.CancellationToken)).Count.ShouldBe(1);
        (await history.GetRecentAdmittedAsync(OtherTenant, nameof(Hexalith.ChatBot.Contracts.Commands.RecordGovernedNote), TestContext.Current.CancellationToken)).ShouldBeEmpty();
    }

    [Fact]
    public async Task ProjectionBackedHistoriesShouldStayIndependentAcrossSubjectClassesWithTheSameSubjectRef()
    {
        InMemoryGovernedControlStateProjectionStore store = new();
        FixedClock clock = new();
        await SaveControlViewAsync(store, GovernedControlSubjectClasses.ServiceClient, "shared-ref", GovernedControlStateView.Active, FixedClock.FixedUtcNow, rateLimitBudget: 2);
        await SaveControlViewAsync(store, GovernedControlSubjectClasses.AiActor, "shared-ref", GovernedControlStateView.Active, FixedClock.FixedUtcNow, rateLimitBudget: 2);
        await SaveControlViewAsync(store, GovernedControlSubjectClasses.OutboundChannel, "shared-ref", GovernedControlStateView.Active, FixedClock.FixedUtcNow, rateLimitBudget: 2);

        ProjectionBackedServiceClientCommandHistory serviceHistory = new(store, clock);
        ProjectionBackedAiActorProposalHistory aiHistory = new(store, clock);
        ProjectionBackedOutboundChannelSendHistory outboundHistory = new(store, clock);

        await serviceHistory.RecordAdmittedAsync(Tenant, "shared-ref", FixedClock.FixedUtcNow, TestContext.Current.CancellationToken);
        await outboundHistory.RecordSendAsync(Tenant, "shared-ref", FixedClock.FixedUtcNow.AddMinutes(1), TestContext.Current.CancellationToken);

        (await serviceHistory.GetRecentAdmittedAsync(Tenant, "shared-ref", TestContext.Current.CancellationToken)).ShouldBe([FixedClock.FixedUtcNow], ignoreOrder: false);
        (await aiHistory.GetRecentAdmittedAsync(Tenant, "shared-ref", TestContext.Current.CancellationToken)).ShouldBeEmpty();
        (await outboundHistory.GetRecentSendsAsync(Tenant, "shared-ref", TestContext.Current.CancellationToken)).ShouldBe([FixedClock.FixedUtcNow.AddMinutes(1)], ignoreOrder: false);
    }

    private static GovernedNoteRecordedNotification Notification(long sourceVersion, DateTimeOffset? recordedAt = null)
        => new(Tenant, NoteId, MessageId, sourceVersion, recordedAt ?? RecordedAt, CorrelationId);

    private static PublishedGovernedOperationEvent PublishedEvent(long sourceVersion)
        => new(
            Tenant,
            "chatbot",
            NoteId,
            GovernedOperationProjectionTranslator.GovernedNoteRecordedEventType,
            sourceVersion,
            CorrelationId,
            MessageId,
            RecordedAt);

    public static IEnumerable<object?[]> ControlEvents()
    {
        yield return Row(new MailboxSourceDisabled("change", Tenant, "mailbox-001", "requester", "approver", "reason", "policy", MailboxSourceControlState.Active, MailboxSourceControlState.Disabled, RecordedAt, 3, CorrelationId), GovernedControlSubjectClasses.MailboxSource, "mailbox-001", GovernedControlStateView.Disabled, null);
        yield return Row(new MailboxSourceQuarantined("change", Tenant, "mailbox-001", "requester", "approver", "reason", "policy", MailboxSourceControlState.Active, MailboxSourceControlState.Quarantined, RecordedAt, 3, CorrelationId), GovernedControlSubjectClasses.MailboxSource, "mailbox-001", GovernedControlStateView.Quarantined, null);
        yield return Row(new MailboxSourceRateLimitConfigured("change", Tenant, "mailbox-001", "actor", "requester", "reason", "policy", 1, 2, MailboxRateLimitWindow.RollingHour, RecordedAt, 3, CorrelationId), GovernedControlSubjectClasses.MailboxSource, "mailbox-001", GovernedControlStateView.Active, 2);
        yield return Row(new ServiceClientDisabled("change", Tenant, "service-001", "requester", "approver", "reason", "policy", ServiceClientControlState.Active, ServiceClientControlState.Disabled, RecordedAt, 3, CorrelationId), GovernedControlSubjectClasses.ServiceClient, "service-001", GovernedControlStateView.Disabled, null);
        yield return Row(new ServiceClientQuarantined("change", Tenant, "service-001", "requester", "approver", "reason", "policy", ServiceClientControlState.Active, ServiceClientControlState.Quarantined, RecordedAt, 3, CorrelationId), GovernedControlSubjectClasses.ServiceClient, "service-001", GovernedControlStateView.Quarantined, null);
        yield return Row(new ServiceClientRateLimitConfigured("change", Tenant, "service-001", "actor", "requester", "reason", "policy", 1, 2, ServiceClientRateLimitWindow.RollingHour, RecordedAt, 3, CorrelationId), GovernedControlSubjectClasses.ServiceClient, "service-001", GovernedControlStateView.Active, 2);
        yield return Row(new AiActorDisabled("change", Tenant, "ai-001", "requester", "approver", "reason", "policy", AiActorControlState.Active, AiActorControlState.Disabled, RecordedAt, 3, CorrelationId), GovernedControlSubjectClasses.AiActor, "ai-001", GovernedControlStateView.Disabled, null);
        yield return Row(new AiActorQuarantined("change", Tenant, "ai-001", "requester", "approver", "reason", "policy", AiActorControlState.Active, AiActorControlState.Quarantined, RecordedAt, 3, CorrelationId), GovernedControlSubjectClasses.AiActor, "ai-001", GovernedControlStateView.Quarantined, null);
        yield return Row(new AiActorRateLimitConfigured("change", Tenant, "ai-001", "actor", "requester", "reason", "policy", 1, 2, AiActorRateLimitWindow.RollingHour, RecordedAt, 3, CorrelationId), GovernedControlSubjectClasses.AiActor, "ai-001", GovernedControlStateView.Active, 2);
        yield return Row(new CommandCapabilityDisabled("change", Tenant, "RecordGovernedNote", "requester", "approver", "reason", "policy", CommandCapabilityControlState.Active, CommandCapabilityControlState.Disabled, RecordedAt, 3, CorrelationId), GovernedControlSubjectClasses.CommandCapability, "RecordGovernedNote", GovernedControlStateView.Disabled, null);
        yield return Row(new CommandCapabilityQuarantined("change", Tenant, "RecordGovernedNote", "requester", "approver", "reason", "policy", CommandCapabilityControlState.Active, CommandCapabilityControlState.Quarantined, RecordedAt, 3, CorrelationId), GovernedControlSubjectClasses.CommandCapability, "RecordGovernedNote", GovernedControlStateView.Quarantined, null);
        yield return Row(new CommandCapabilityRateLimitConfigured("change", Tenant, "RecordGovernedNote", "actor", "requester", "reason", "policy", 1, 2, CommandCapabilityRateLimitWindow.RollingHour, RecordedAt, 3, CorrelationId), GovernedControlSubjectClasses.CommandCapability, "RecordGovernedNote", GovernedControlStateView.Active, 2);
        yield return Row(new OutboundChannelDisabled("change", Tenant, "adapter:mailbox-outbound", "requester", "approver", "reason", "policy", OutboundChannelControlState.Active, OutboundChannelControlState.Disabled, RecordedAt, 3, CorrelationId), GovernedControlSubjectClasses.OutboundChannel, "adapter:mailbox-outbound", GovernedControlStateView.Disabled, null);
        yield return Row(new OutboundChannelQuarantined("change", Tenant, "adapter:mailbox-outbound", "requester", "approver", "reason", "policy", OutboundChannelControlState.Active, OutboundChannelControlState.Quarantined, RecordedAt, 3, CorrelationId), GovernedControlSubjectClasses.OutboundChannel, "adapter:mailbox-outbound", GovernedControlStateView.Quarantined, null);
        yield return Row(new OutboundChannelRateLimitConfigured("change", Tenant, "adapter:mailbox-outbound", "actor", "requester", "reason", "policy", 1, 2, OutboundChannelRateLimitWindow.RollingHour, RecordedAt, 3, CorrelationId), GovernedControlSubjectClasses.OutboundChannel, "adapter:mailbox-outbound", GovernedControlStateView.Active, 2);
    }

    private static object?[] Row(object payload, string subjectClass, string subjectRef, string state, int? budget)
        => [PublishedControlEvent(payload), subjectClass, subjectRef, state, budget];

    private static PublishedGovernedOperationEvent PublishedControlEvent(object payload)
        => new(
            Tenant,
            "chatbot",
            NoteId,
            payload.GetType().FullName,
            3,
            CorrelationId,
            MessageId,
            RecordedAt,
            JsonSerializer.SerializeToUtf8Bytes(payload));

    private static Task SaveControlViewAsync(
        IGovernedControlStateProjectionStore store,
        string subjectClass,
        string subjectRef,
        string controlState,
        DateTimeOffset lastUpdatedAtUtc,
        bool revocationSensitive = false,
        int? rateLimitBudget = null)
        => store.SaveAsync(
            new GovernedControlStateView(
                Tenant,
                subjectClass,
                subjectRef,
                controlState,
                rateLimitBudget,
                rateLimitBudget is null ? null : GovernedControlStateView.RollingHour,
                1,
                CorrelationId,
                RecordedAt,
                lastUpdatedAtUtc,
                revocationSensitive),
            TestContext.Current.CancellationToken);

    private sealed class FixedClock(DateTimeOffset? now = null) : ISystemClock
    {
        public static DateTimeOffset FixedUtcNow { get; } = new(2026, 5, 31, 9, 0, 0, TimeSpan.Zero);

        public DateTimeOffset UtcNow => now ?? FixedUtcNow;
    }
}
