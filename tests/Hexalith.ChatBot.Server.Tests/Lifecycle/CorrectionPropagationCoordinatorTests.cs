using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Association;
using Hexalith.ChatBot.Server.Lifecycle.Workflows;

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
        DaprCorrectionPropagationCoordinator coordinator = new(writer, activities, alerts, new FixedClock());

        await coordinator.StartAsync(Request(), TestContext.Current.CancellationToken);

        writer.CommandTypes.Last().ShouldBe(nameof(DelayMailboxAssociationCorrectionPropagation));
        alerts.Alerts.ShouldHaveSingleItem().Kind.ShouldBe(OperatorAlertKind.CorrectionDelayed);
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

    private sealed class RecordingAlertSink : IOperatorAlertSink
    {
        public List<OperatorAlert> Alerts { get; } = [];

        public ValueTask EmitAsync(OperatorAlert alert, CancellationToken cancellationToken)
        {
            Alerts.Add(alert);
            return ValueTask.CompletedTask;
        }
    }

    private sealed class FixedClock : ISystemClock
    {
        public DateTimeOffset UtcNow => StartedAt.AddMinutes(1);
    }
}
