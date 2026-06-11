using Hexalith.ChatBot.Server.Association;
using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Gateway;
using Hexalith.ChatBot.Server.Gateway.Stages;
using Hexalith.ChatBot.Server.Lifecycle.Workflows;
using Hexalith.ChatBot.Server.Observability;
using Hexalith.ChatBot.Server.Projections.DerivedStores;
using Hexalith.ChatBot.Server.Tests.Observability;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Lifecycle;

public sealed class CorrectionPropagationCoordinatorTests
{
    private static readonly DateTimeOffset StartedAt = new(2026, 5, 31, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task CoordinatorShouldScheduleHostedWorkflowWithDeterministicWorkflowId()
    {
        RecordingWorkflowRuntime runtime = new();
        CorrectionPropagationActivityCatalog catalog = new(
            CorrectionPropagationStoreKeys.RequiredM0.Select(static key => new SucceedingActivity(key, StartedAt.AddSeconds(5))));
        RecordingChatBotMetrics metrics = new();
        DaprCorrectionPropagationCoordinator coordinator = new(runtime, catalog, metrics);
        CorrectionPropagationRequest request = Request();

        await coordinator.StartAsync(request, TestContext.Current.CancellationToken);

        coordinator.IsReady.ShouldBeTrue();
        runtime.Scheduled.ShouldHaveSingleItem().WorkflowInstanceId.ShouldBe(DaprCorrectionPropagationCoordinator.WorkflowInstanceIdFor(
            request.TenantId,
            request.AssociationId,
            request.CorrectionId,
            request.SourceVersion));
        metrics.WorkflowLifecycleEvents.ShouldContain(static item =>
            item.Status == CorrectionPropagationWorkflowStatuses.Started &&
            item.Reason == CorrectionPropagationWorkflowFailureCodes.None);
    }

    [Fact]
    public async Task CoordinatorShouldFailClosedWhenWorkflowRuntimeIsUnavailable()
    {
        RecordingWorkflowRuntime runtime = new() { IsAvailable = false };
        CorrectionPropagationActivityCatalog catalog = new(
            CorrectionPropagationStoreKeys.RequiredM0.Select(static key => new SucceedingActivity(key, StartedAt.AddSeconds(5))));
        RecordingChatBotMetrics metrics = new();
        DaprCorrectionPropagationCoordinator coordinator = new(runtime, catalog, metrics);

        InvalidOperationException exception = await Should.ThrowAsync<InvalidOperationException>(
            coordinator.StartAsync(Request(), TestContext.Current.CancellationToken).AsTask());

        exception.Message.ShouldBe(CorrectionPropagationWorkflowFailureCodes.WorkflowUnavailable);
        runtime.Scheduled.ShouldBeEmpty();
        metrics.WorkflowLifecycleEvents.ShouldContain(static item =>
            item.Status == CorrectionPropagationWorkflowStatuses.RuntimeUnavailable &&
            item.Reason == CorrectionPropagationWorkflowFailureCodes.WorkflowUnavailable);
    }

    [Fact]
    public async Task WorkflowActivitiesShouldFanOutM0StoresAndComplete()
    {
        RecordingWriter writer = new();
        RecordingAlertSink alerts = new();
        CorrectionPropagationActivityCatalog catalog = new(
            CorrectionPropagationStoreKeys.RequiredM0.Select(static key => new SucceedingActivity(key, StartedAt.AddSeconds(5))));

        await ExecuteWorkflowActivitiesAsync(Request(), catalog, writer, alerts, new RecordingAuditWriter());

        writer.CommandTypes.ShouldBe(
            [
                nameof(StartMailboxAssociationCorrectionPropagation),
                nameof(AcknowledgeMailboxAssociationCorrectionStoreInvalidated),
                nameof(AcknowledgeMailboxAssociationCorrectionStoreInvalidated),
                nameof(AcknowledgeMailboxAssociationCorrectionStoreInvalidated),
                nameof(AcknowledgeMailboxAssociationCorrectionStoreInvalidated),
                nameof(CompleteMailboxAssociationCorrectionPropagation),
            ],
            ignoreOrder: false);
        alerts.Alerts.ShouldBeEmpty();
    }

    [Fact]
    public async Task WorkflowActivitiesShouldMarkDelayedAndAlertWhenARequiredStoreFails()
    {
        RecordingWriter writer = new();
        RecordingAlertSink alerts = new();
        ICorrectionPropagationStoreActivity[] activities =
        [
            new SucceedingActivity(CorrectionPropagationStoreKeys.AssociationRouting, StartedAt.AddSeconds(5)),
            new SucceedingActivity(CorrectionPropagationStoreKeys.EvidenceSnapshot, StartedAt.AddSeconds(5)),
            new SucceedingActivity(CorrectionPropagationStoreKeys.OperationStatus, StartedAt.AddSeconds(5)),
            new FailingActivity(CorrectionPropagationStoreKeys.AiContextReadiness, StartedAt.AddSeconds(5)),
        ];

        await ExecuteWorkflowActivitiesAsync(Request(), new CorrectionPropagationActivityCatalog(activities), writer, alerts, new RecordingAuditWriter());

        writer.CommandTypes.Last().ShouldBe(nameof(DelayMailboxAssociationCorrectionPropagation));
        alerts.Alerts.ShouldHaveSingleItem().Kind.ShouldBe(OperatorAlertKind.CorrectionDelayed);
    }

    [Fact]
    public async Task M2ScopeRunsTheVectorReindexActivityAndCompletes()
    {
        RecordingWriter writer = new();
        RecordingAlertSink alerts = new();
        CorrectionPropagationActivityCatalog catalog = new(
            CorrectionPropagationStoreKeys.RequiredM2.Select(static key => new SucceedingActivity(key, StartedAt.AddSeconds(5))));

        await ExecuteWorkflowActivitiesAsync(Request(), catalog, writer, alerts, new RecordingAuditWriter());

        catalog.IsReady.ShouldBeTrue();
        writer.CommandTypes.Count(static type => type == nameof(AcknowledgeMailboxAssociationCorrectionStoreInvalidated)).ShouldBe(5);
        writer.CommandTypes.Last().ShouldBe(nameof(CompleteMailboxAssociationCorrectionPropagation));
        alerts.Alerts.ShouldBeEmpty();
    }

    [Fact]
    public async Task AnSloBreachMarksCorrectionDelayedWithOwnerRoleAndNextActionAndAuditsBeforeAlerting()
    {
        RecordingWriter writer = new();
        List<string> order = [];
        RecordingAlertSink alerts = new(order);
        RecordingAuditWriter audit = new(order);
        ICorrectionPropagationStoreActivity[] activities =
        [
            new SucceedingActivity(CorrectionPropagationStoreKeys.AssociationRouting, StartedAt.AddSeconds(5)),
            new SucceedingActivity(CorrectionPropagationStoreKeys.EvidenceSnapshot, StartedAt.AddSeconds(5)),
            new SucceedingActivity(CorrectionPropagationStoreKeys.OperationStatus, StartedAt.AddSeconds(5)),
            new SucceedingActivity(CorrectionPropagationStoreKeys.AiContextReadiness, StartedAt.AddSeconds(5)),
            new FixedResultActivity(CorrectionPropagationStoreKeys.VectorReindex, "failed", VectorReindexCorrectionPropagationStoreActivity.SloExceededReasonCode, StartedAt.AddSeconds(5)),
        ];

        await ExecuteWorkflowActivitiesAsync(Request(), new CorrectionPropagationActivityCatalog(activities), writer, alerts, audit);

        writer.CommandTypes.Last().ShouldBe(nameof(DelayMailboxAssociationCorrectionPropagation));
        OperatorAlert alert = alerts.Alerts.ShouldHaveSingleItem();
        alert.Kind.ShouldBe(OperatorAlertKind.CorrectionDelayed);
        alert.ReasonCode.ShouldBe(VectorReindexCorrectionPropagationStoreActivity.SloExceededReasonCode);
        alert.FirstBreakLocator.ShouldNotBeNull();

        AuditEnvelope envelope = audit.PreCommitEnvelopes.ShouldHaveSingleItem();
        envelope.SourceEvidenceRefs.ShouldContain($"correction-propagation-owner:{DaprCorrectionPropagationCoordinator.ResponsibleOwnerRole}");
        envelope.SourceEvidenceRefs.ShouldContain($"correction-propagation-next-action:{DaprCorrectionPropagationCoordinator.DelayedNextSafeAction}");
        order.ShouldBe(["audit", "alert"]);
    }

    [Fact]
    public async Task AVectorReindexHardFailureMarksDelayedWithTheVectorReindexFailedReasonCode()
    {
        RecordingWriter writer = new();
        RecordingAlertSink alerts = new();
        RecordingAuditWriter audit = new();
        ICorrectionPropagationStoreActivity[] activities =
        [
            new SucceedingActivity(CorrectionPropagationStoreKeys.AssociationRouting, StartedAt.AddSeconds(5)),
            new SucceedingActivity(CorrectionPropagationStoreKeys.EvidenceSnapshot, StartedAt.AddSeconds(5)),
            new SucceedingActivity(CorrectionPropagationStoreKeys.OperationStatus, StartedAt.AddSeconds(5)),
            new SucceedingActivity(CorrectionPropagationStoreKeys.AiContextReadiness, StartedAt.AddSeconds(5)),
            new FixedResultActivity(CorrectionPropagationStoreKeys.VectorReindex, "failed", InMemoryVectorReindexer.VectorReindexFailedReasonCode, StartedAt.AddSeconds(5)),
        ];

        await ExecuteWorkflowActivitiesAsync(Request(), new CorrectionPropagationActivityCatalog(activities), writer, alerts, audit);

        writer.CommandTypes.Last().ShouldBe(nameof(DelayMailboxAssociationCorrectionPropagation));
        OperatorAlert alert = alerts.Alerts.ShouldHaveSingleItem();
        alert.Kind.ShouldBe(OperatorAlertKind.CorrectionDelayed);
        alert.ReasonCode.ShouldBe(InMemoryVectorReindexer.VectorReindexFailedReasonCode);
        audit.PreCommitEnvelopes.ShouldHaveSingleItem()
            .SourceEvidenceRefs.ShouldContain($"correction-propagation-reason:{InMemoryVectorReindexer.VectorReindexFailedReasonCode}");
    }

    [Fact]
    public async Task AFailedAuditWriteSuppressesTheDelayAlert()
    {
        RecordingWriter writer = new();
        RecordingAlertSink alerts = new();
        RecordingAuditWriter audit = new() { Succeed = false };
        ICorrectionPropagationStoreActivity[] activities =
        [
            new SucceedingActivity(CorrectionPropagationStoreKeys.AssociationRouting, StartedAt.AddSeconds(5)),
            new SucceedingActivity(CorrectionPropagationStoreKeys.EvidenceSnapshot, StartedAt.AddSeconds(5)),
            new SucceedingActivity(CorrectionPropagationStoreKeys.OperationStatus, StartedAt.AddSeconds(5)),
            new FailingActivity(CorrectionPropagationStoreKeys.AiContextReadiness, StartedAt.AddSeconds(5)),
        ];

        await ExecuteWorkflowActivitiesAsync(Request(), new CorrectionPropagationActivityCatalog(activities), writer, alerts, audit);

        writer.CommandTypes.Last().ShouldBe(nameof(DelayMailboxAssociationCorrectionPropagation));
        audit.PreCommitEnvelopes.ShouldHaveSingleItem();
        alerts.Alerts.ShouldBeEmpty();
    }

    private static async Task ExecuteWorkflowActivitiesAsync(
        CorrectionPropagationRequest request,
        CorrectionPropagationActivityCatalog catalog,
        RecordingWriter writer,
        RecordingAlertSink alerts,
        RecordingAuditWriter audit)
    {
        IReadOnlyList<string> scope = await new CorrectionPropagationScopeActivity(catalog)
            .RunAsync(null!, request)
            .ConfigureAwait(false);
        _ = await new CorrectionPropagationStartActivity(writer)
            .RunAsync(null!, new CorrectionPropagationStartInput(request, scope))
            .ConfigureAwait(false);
        List<CorrectionPropagationActivityResult> results = [];
        foreach (string storeKey in scope)
        {
            results.Add(await new CorrectionPropagationRunStoreActivity(catalog, writer)
                .RunAsync(
                    null!,
                    new CorrectionPropagationStoreActivityInput(request, storeKey, StartedAt.AddSeconds(1)))
                .ConfigureAwait(false));
        }

        if (results.All(static result => result.IsSuccessful))
        {
            _ = await new CorrectionPropagationCompleteActivity(writer, new FixedClock())
                .RunAsync(null!, request)
                .ConfigureAwait(false);
            return;
        }

        string delayReason = results.First(static result => !result.IsSuccessful).FailureReasonCode
            ?? DaprCorrectionPropagationCoordinator.DefaultDelayReasonCode;
        _ = await new CorrectionPropagationDelayActivity(writer, alerts, audit, new FixedClock())
            .RunAsync(
                null!,
                new CorrectionPropagationDelayInput(request, delayReason))
            .ConfigureAwait(false);
    }

    private static CorrectionPropagationRequest Request()
    {
        string associationId = "01ARZ3NDEKTSV4RRFFQ69G5FAV";
        long sourceVersion = 3;
        string correctionId = DaprCorrectionPropagationCoordinator.CorrectionIdFor(associationId, sourceVersion);
        return new CorrectionPropagationRequest(
            "tenant-alpha",
            "actor-alpha",
            associationId,
            "01ARZ3NDEKTSV4RRFFQ69G5FAY",
            correctionId,
            DaprCorrectionPropagationCoordinator.WorkflowInstanceIdFor("tenant-alpha", associationId, correctionId, sourceVersion),
            "project-001",
            "project-002",
            sourceVersion,
            "01ARZ3NDEKTSV4RRFFQ69G5FAW",
            StartedAt,
            StartedAt.AddMinutes(10));
    }

    private sealed class RecordingWorkflowRuntime : ICorrectionPropagationWorkflowRuntime
    {
        public List<CorrectionPropagationRequest> Scheduled { get; } = [];

        public bool IsAvailable { get; init; } = true;

        public ValueTask ScheduleAsync(CorrectionPropagationRequest request, CancellationToken cancellationToken)
        {
            Scheduled.Add(request);
            return ValueTask.CompletedTask;
        }

        public ValueTask<CorrectionPropagationWorkflowRuntimeStatus> CheckAsync(CancellationToken cancellationToken)
            => ValueTask.FromResult(new CorrectionPropagationWorkflowRuntimeStatus(
                IsAvailable,
                IsAvailable ? "available" : CorrectionPropagationWorkflowStatuses.RuntimeUnavailable,
                IsAvailable ? CorrectionPropagationWorkflowFailureCodes.None : CorrectionPropagationWorkflowFailureCodes.WorkflowUnavailable,
                StartedAt));
    }

    private sealed class RecordingWriter : ICorrectionPropagationCommandWriter
    {
        public List<string> CommandTypes { get; } = [];

        public ValueTask SubmitAsync<TCommand>(
            CorrectionPropagationRequest request,
            string commandType,
            TCommand command,
            CancellationToken cancellationToken)
        {
            CommandTypes.Add(commandType);
            return ValueTask.CompletedTask;
        }
    }

    private sealed class SucceedingActivity(string storeKey, DateTimeOffset completedAt) : ICorrectionPropagationStoreActivity
    {
        public string StoreKey { get; } = storeKey;

        public ValueTask<CorrectionPropagationActivityResult> InvalidateAndRebuildAsync(
            CorrectionPropagationActivityRequest request,
            CancellationToken cancellationToken)
            => ValueTask.FromResult(new CorrectionPropagationActivityResult(StoreKey, "success", null, completedAt));
    }

    private sealed class FailingActivity(string storeKey, DateTimeOffset completedAt) : ICorrectionPropagationStoreActivity
    {
        public string StoreKey { get; } = storeKey;

        public ValueTask<CorrectionPropagationActivityResult> InvalidateAndRebuildAsync(
            CorrectionPropagationActivityRequest request,
            CancellationToken cancellationToken)
            => ValueTask.FromResult(new CorrectionPropagationActivityResult(StoreKey, "failed", "store_unavailable", completedAt));
    }

    private sealed class FixedResultActivity(string storeKey, string outcome, string? reasonCode, DateTimeOffset completedAt)
        : ICorrectionPropagationStoreActivity
    {
        public string StoreKey { get; } = storeKey;

        public ValueTask<CorrectionPropagationActivityResult> InvalidateAndRebuildAsync(
            CorrectionPropagationActivityRequest request,
            CancellationToken cancellationToken)
            => ValueTask.FromResult(new CorrectionPropagationActivityResult(StoreKey, outcome, reasonCode, completedAt));
    }

    private sealed class RecordingAlertSink(List<string>? order = null) : IOperatorAlertSink
    {
        public List<OperatorAlert> Alerts { get; } = [];

        public ValueTask EmitAsync(OperatorAlert alert, CancellationToken cancellationToken)
        {
            order?.Add("alert");
            Alerts.Add(alert);
            return ValueTask.CompletedTask;
        }
    }

    private sealed class RecordingAuditWriter(List<string>? order = null) : IAuditWriter
    {
        public List<AuditEnvelope> PreCommitEnvelopes { get; } = [];

        public bool Succeed { get; init; } = true;

        public ValueTask RecordAuthorizationFailureAsync(ChatBotAuthorizationFailureAuditFact fact, CancellationToken cancellationToken)
            => ValueTask.CompletedTask;

        public ValueTask<AuditWriteResult> RecordPreCommitAsync(AuditEnvelope envelope, CancellationToken cancellationToken)
        {
            order?.Add("audit");
            PreCommitEnvelopes.Add(envelope);
            return ValueTask.FromResult(Succeed ? AuditWriteResult.Success : AuditWriteResult.Unavailable());
        }

        public ValueTask<AuditWriteResult> RecordPostCommitAsync(AuditEnvelope envelope, CancellationToken cancellationToken)
            => ValueTask.FromResult(AuditWriteResult.Success);
    }

    private sealed class FixedClock : ISystemClock
    {
        public DateTimeOffset UtcNow => StartedAt.AddMinutes(1);
    }
}
