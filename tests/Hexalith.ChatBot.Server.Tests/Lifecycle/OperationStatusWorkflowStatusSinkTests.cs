using System.Text.Json;

using Hexalith.ChatBot.Client.Generated;
using Hexalith.ChatBot.Contracts.Messages;
using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Gateway.Status;
using Hexalith.ChatBot.Server.Lifecycle.Workflows;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Lifecycle;

public sealed class OperationStatusWorkflowStatusSinkTests
{
    [Theory]
    [InlineData("association_correction_case_resolution_unavailable", "association_correction_case_resolution_unavailable")]
    [InlineData("association_correction_store_unavailable", "association_correction_store_unavailable")]
    [InlineData("m0_store_invalidation_failed", "m0_store_invalidation_failed")]
    [InlineData("vector_reindex_failed", "vector_reindex_failed")]
    [InlineData("memories_correction_invalid_status", "memories_correction_invalid_status")]
    [InlineData("memories_correction_timed_out", "memories_correction_timed_out")]
    [InlineData("external-private-failure", "dependency_degraded")]
    public void StatusWireShouldPreserveKnownWorkflowCausesAndBoundUnknownExternalCause(string cause, string expected)
    {
        DateTimeOffset now = new(2026, 8, 9, 10, 0, 0, TimeSpan.Zero);
        CommandSubmissionResponse accepted = new()
        {
            CommandId = "01ARZ3NDEKTSV4RRFFQ69G5FAY",
            CorrelationId = "01ARZ3NDEKTSV4RRFFQ69G5FAW",
            OperationId = "01ARZ3NDEKTSV4RRFFQ69G5FAX",
            LifecycleState = LifecycleState.Correcting,
            AcceptedAt = now,
        };
        OperationStatusRecord record = OperationStatusRecord.Accepted("tenant-alpha", accepted, false, now) with
        {
            ReasonCode = cause,
            FailureReasonCode = cause,
            WorkflowLastFailureCode = cause,
        };

        JsonElement wire = OperationStatusHttpResults.ToJsonElement(record, now);

        wire.GetProperty("reasonCode").GetString().ShouldBe(expected);
        wire.GetProperty("failureReasonCode").GetString().ShouldBe(expected);
        wire.GetProperty("workflowLastFailureCode").GetString().ShouldBe(expected);
        wire.GetRawText().ShouldNotContain("external-private-failure", Case.Sensitive);
    }

    [Fact]
    public async Task RetryActivityShouldPublishDueTimeAndEligibilityUntilAttemptLimit()
    {
        DateTimeOffset now = new(2026, 8, 9, 10, 0, 0, TimeSpan.Zero);
        FixedClock clock = new(now);
        InMemoryOperationStatusStore store = new();
        CommandSubmissionResponse accepted = new()
        {
            CommandId = "01ARZ3NDEKTSV4RRFFQ69G5FAY",
            CorrelationId = "01ARZ3NDEKTSV4RRFFQ69G5FAW",
            OperationId = "01ARZ3NDEKTSV4RRFFQ69G5FAX",
            LifecycleState = LifecycleState.Correcting,
            AcceptedAt = now,
            RetryEligible = false,
        };
        await store.UpsertAsync(OperationStatusRecord.Accepted("tenant-alpha", accepted, false, now),
            TestContext.Current.CancellationToken);
        CorrectionPropagationRequest request = new(
            "tenant-alpha", "actor-alpha", "01ARZ3NDEKTSV4RRFFQ69G5FAV",
            "01ARZ3NDEKTSV4RRFFQ69G5FAY", "correction-1", "wf-1", "project-001", "project-002", 3,
            "01ARZ3NDEKTSV4RRFFQ69G5FAW", now, now.AddMinutes(10),
            OperationId: accepted.OperationId);
        OperationStatusWorkflowStatusSink sink = new(store, clock);
        CorrectionPropagationRetryStatusActivity activity = new(sink);

        _ = await activity.RunAsync(null!, new CorrectionPropagationRetryStatusInput(
            request, 1, CorrectionPropagationWorkflowFailureCodes.CaseResolutionUnavailable,
            RetryDueAt: now.AddSeconds(30)));

        OperationStatusRecord retry = (await store.TryGetAsync("tenant-alpha", accepted.OperationId,
            TestContext.Current.CancellationToken))!;
        retry.NextRetryAt.ShouldBe(now.AddSeconds(30));
        retry.RetryCount.ShouldBe(1);
        retry.MaxAttempts.ShouldBe(5);
        accepted.RetryEligible.ShouldBeFalse();
        OperationStatusHttpResults.ToJsonElement(retry, now).GetProperty("retryEligible").GetBoolean().ShouldBeFalse();
        OperationStatusHttpResults.ToJsonElement(retry, now.AddSeconds(30)).GetProperty("retryEligible").GetBoolean().ShouldBeTrue();
        OperationStatusHttpResults.ToJsonElement(retry with { RetryCount = 5 }, now.AddSeconds(30))
            .GetProperty("retryEligible").GetBoolean().ShouldBeFalse();

        _ = await activity.RunAsync(null!, new CorrectionPropagationRetryStatusInput(
            request, 5, CorrectionPropagationWorkflowFailureCodes.CaseResolutionUnavailable,
            CorrectionPropagationWorkflowStatuses.Failed));
        OperationStatusRecord failed = (await store.TryGetAsync("tenant-alpha", accepted.OperationId,
            TestContext.Current.CancellationToken))!;
        failed.RetryCount.ShouldBe(5);
        failed.NextRetryAt.ShouldBeNull();
        JsonElement failedWire = OperationStatusHttpResults.ToJsonElement(failed, now.AddSeconds(30));
        failedWire.GetProperty("retryEligible").GetBoolean().ShouldBeFalse();
        failedWire.GetProperty("reasonCode").GetString().ShouldBe(ChatBotMessageCodes.AssociationCorrectionPropagationFailed);
        failedWire.GetProperty("failureReasonCode").GetString().ShouldBe(CorrectionPropagationWorkflowFailureCodes.CaseResolutionUnavailable);
        failedWire.GetProperty("workflowLastFailureCode").GetString().ShouldBe(CorrectionPropagationWorkflowFailureCodes.CaseResolutionUnavailable);
        failedWire.GetProperty("terminalReasonCode").GetString().ShouldBe(ChatBotMessageCodes.AssociationCorrectionPropagationFailed);
        failedWire.GetProperty("safeNextActions").EnumerateArray().Single().GetString().ShouldBe("escalate");

        await sink.ReportAsync(request, CorrectionPropagationWorkflowStatuses.Started, 0,
            CorrectionPropagationWorkflowFailureCodes.None, TestContext.Current.CancellationToken);
        OperationStatusRecord recovered = (await store.TryGetAsync("tenant-alpha", accepted.OperationId,
            TestContext.Current.CancellationToken))!;
        recovered.ReasonCode.ShouldBe("association_correction_propagation_pending");
        recovered.CompletionStatus.ShouldBe(OperationStatusRecord.AcceptedProjectionPending);
        recovered.WorkflowLastFailureCode.ShouldBeNull();
        recovered.FailureReasonCode.ShouldBeNull();
        recovered.TerminalReasonCode.ShouldBeNull();
        recovered.SafeNextActions.Single().ShouldBe("retry-later");

        await sink.ReportAsync(request, CorrectionPropagationWorkflowStatuses.Completed, 0,
            CorrectionPropagationWorkflowFailureCodes.None, TestContext.Current.CancellationToken);
        OperationStatusRecord completed = (await store.TryGetAsync("tenant-alpha", accepted.OperationId,
            TestContext.Current.CancellationToken))!;
        completed.ReasonCode.ShouldBe("association_correction_propagation_complete");
        completed.CompletionStatus.ShouldBe(OperationStatusRecord.Completed);
        completed.SafeNextActions.Single().ShouldBe("none");
    }

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
