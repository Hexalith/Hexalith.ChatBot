using Hexalith.ChatBot.Client.Generated;
using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Gateway.Status;
using Hexalith.ChatBot.Server.Lifecycle.Workflows;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Lifecycle;

public sealed class OperationStatusWorkflowStatusSinkTests
{
    [Fact]
    public async Task ReportAsyncShouldWriteWorkflowFieldsOntoExistingOperationStatus()
    {
        InMemoryOperationStatusStore store = new();
        FixedClock clock = new(new DateTimeOffset(2026, 8, 9, 10, 0, 0, TimeSpan.Zero));
        OperationStatusWorkflowStatusSink sink = new(store, clock);
        CommandSubmissionResponse accepted = new()
        {
            CommandId = "01ARZ3NDEKTSV4RRFFQ69G5FAY",
            CorrelationId = "01ARZ3NDEKTSV4RRFFQ69G5FAW",
            TaskId = "01ARZ3NDEKTSV4RRFFQ69G5FAX",
            LifecycleState = LifecycleState.Correcting,
            AcceptedAt = clock.UtcNow,
        };
        await store.UpsertAsync(
            OperationStatusRecord.Accepted("tenant-alpha", accepted, auditReconciliationRequired: false, clock.UtcNow),
            TestContext.Current.CancellationToken);

        CorrectionPropagationRequest request = new(
            "tenant-alpha",
            "actor-alpha",
            "01ARZ3NDEKTSV4RRFFQ69G5FAV",
            "01ARZ3NDEKTSV4RRFFQ69G5FAY",
            "correction-1",
            "wf-1",
            "project-001",
            "project-002",
            3,
            "01ARZ3NDEKTSV4RRFFQ69G5FAW",
            clock.UtcNow,
            clock.UtcNow.AddMinutes(10),
            OperationId: "01ARZ3NDEKTSV4RRFFQ69G5FAX");

        await sink.ReportAsync(
            request,
            CorrectionPropagationWorkflowStatuses.Started,
            workflowRetryCount: 0,
            CorrectionPropagationWorkflowFailureCodes.None,
            TestContext.Current.CancellationToken);

        OperationStatusRecord? updated = await store.TryGetAsync(
            "tenant-alpha",
            "01ARZ3NDEKTSV4RRFFQ69G5FAX",
            TestContext.Current.CancellationToken);
        updated.ShouldNotBeNull();
        updated.WorkflowInstanceId.ShouldBe("wf-1");
        updated.WorkflowStatus.ShouldBe(CorrectionPropagationWorkflowStatuses.Started);
        updated.WorkflowRetryCount.ShouldBe(0);
        updated.WorkflowLastFailureCode.ShouldBeNull();
    }

    private sealed class FixedClock(DateTimeOffset utcNow) : ISystemClock
    {
        public DateTimeOffset UtcNow { get; } = utcNow;
    }
}
