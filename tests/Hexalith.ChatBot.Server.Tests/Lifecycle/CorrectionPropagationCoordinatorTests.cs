using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Association;
using Hexalith.ChatBot.Server.Gateway;
using Hexalith.ChatBot.Server.Gateway.Stages;
using Hexalith.ChatBot.Server.Lifecycle.Workflows;
using Hexalith.ChatBot.Server.Projections.DerivedStores;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Lifecycle;

public sealed class CorrectionPropagationCoordinatorTests
{
    private static readonly DateTimeOffset StartedAt = new(2026, 5, 31, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task CoordinatorShouldFanOutM0StoresAndCompleteWithDeterministicWorkflowId()
    {
        RecordingWriter writer = new();
        RecordingAlertSink alerts = new();
        DaprCorrectionPropagationCoordinator coordinator = new(
            writer,
            CorrectionPropagationStoreKeys.RequiredM0.Select(static key => new SucceedingActivity(key, StartedAt.AddSeconds(5))),
            alerts,
            new RecordingAuditWriter(),
            new FixedClock());
        CorrectionPropagationRequest request = Request();

        await coordinator.StartAsync(request, TestContext.Current.CancellationToken);

        coordinator.IsReady.ShouldBeTrue();
        request.WorkflowInstanceId.ShouldBe(DaprCorrectionPropagationCoordinator.WorkflowInstanceIdFor(
            request.TenantId,
            request.AssociationId,
            request.CorrectionId,
            request.SourceVersion));
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
    public async Task CoordinatorShouldMarkDelayedAndAlertWhenARequiredStoreFails()
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
        DaprCorrectionPropagationCoordinator coordinator = new(writer, activities, alerts, new RecordingAuditWriter(), new FixedClock());

        await coordinator.StartAsync(Request(), TestContext.Current.CancellationToken);

        writer.CommandTypes.Last().ShouldBe(nameof(DelayMailboxAssociationCorrectionPropagation));
        alerts.Alerts.ShouldHaveSingleItem().Kind.ShouldBe(OperatorAlertKind.CorrectionDelayed);
    }

    // ----- Story 9.6 (AC1/AC2): M2 scope + fail-closed P2 audit-then-deliver delay -----

    [Fact]
    public async Task M2ScopeRunsTheVectorReindexActivityAndCompletes()
    {
        RecordingWriter writer = new();
        RecordingAlertSink alerts = new();
        DaprCorrectionPropagationCoordinator coordinator = new(
            writer,
            CorrectionPropagationStoreKeys.RequiredM2.Select(static key => new SucceedingActivity(key, StartedAt.AddSeconds(5))),
            alerts,
            new RecordingAuditWriter(),
            new FixedClock());

        await coordinator.StartAsync(Request(), TestContext.Current.CancellationToken);

        coordinator.IsReady.ShouldBeTrue();
        // Five stores fan out (the four M0 + vector-reindex) ⇒ five acknowledgements + a completion, no delay/alert.
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
        DaprCorrectionPropagationCoordinator coordinator = new(writer, activities, alerts, audit, new FixedClock());

        await coordinator.StartAsync(Request(), TestContext.Current.CancellationToken);

        writer.CommandTypes.Last().ShouldBe(nameof(DelayMailboxAssociationCorrectionPropagation));
        OperatorAlert alert = alerts.Alerts.ShouldHaveSingleItem();
        alert.Kind.ShouldBe(OperatorAlertKind.CorrectionDelayed);
        alert.ReasonCode.ShouldBe(VectorReindexCorrectionPropagationStoreActivity.SloExceededReasonCode);
        alert.FirstBreakLocator.ShouldNotBeNull();

        // The P2 audit envelope carries the owner role + next safe action and is written BEFORE the alert.
        AuditEnvelope envelope = audit.PreCommitEnvelopes.ShouldHaveSingleItem();
        envelope.SourceEvidenceRefs.ShouldContain("correction-propagation-severity:p2");
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
        DaprCorrectionPropagationCoordinator coordinator = new(writer, activities, alerts, audit, new FixedClock());

        await coordinator.StartAsync(Request(), TestContext.Current.CancellationToken);

        // A hard reindex failure (not just an SLO breach) drives the same fail-closed delay path, carrying its own
        // reason code onto both the delay alert and the P2 audit envelope.
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
        DaprCorrectionPropagationCoordinator coordinator = new(writer, activities, alerts, audit, new FixedClock());

        await coordinator.StartAsync(Request(), TestContext.Current.CancellationToken);

        // Fail-closed: a failed audit write means NO operator alert is emitted (the delay command still records state).
        writer.CommandTypes.Last().ShouldBe(nameof(DelayMailboxAssociationCorrectionPropagation));
        audit.PreCommitEnvelopes.ShouldHaveSingleItem();
        alerts.Alerts.ShouldBeEmpty();
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
