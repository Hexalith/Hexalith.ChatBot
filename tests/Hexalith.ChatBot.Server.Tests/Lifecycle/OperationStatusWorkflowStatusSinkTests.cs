using System.Text.Json;

using Hexalith.ChatBot.Client.Generated;
using Hexalith.ChatBot.Contracts.Messages;
using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Gateway.Status;
using Hexalith.ChatBot.Server.Lifecycle.Workflows;
using Hexalith.ChatBot.Server.Tests.Gateway.Status;

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
    [InlineData("external-private-failure", "generic_denial")]
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
        if (expected == ChatBotMessageCodes.GenericDenial)
        {
            wire.TryGetProperty("workflowLastFailureCode", out _).ShouldBeFalse();
        }
        else
        {
            wire.GetProperty("workflowLastFailureCode").GetString().ShouldBe(expected);
        }
        wire.GetRawText().ShouldNotContain("external-private-failure", Case.Sensitive);
    }

    [Fact]
    public void UnknownStoredActionShouldResolveToTerminalDismissWithoutRawToken()
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
            ReasonCode = "private_unknown_code",
            SafeNextActions = ["retry-later", "private-unsafe-action"],
            DuplicateSafetyNote = "restricted private owner detail",
            NextRetryAt = now.AddSeconds(-1),
            MaxAttempts = 5,
        };

        JsonElement wire = OperationStatusHttpResults.ToJsonElement(record, now);

        wire.GetProperty("reasonCode").GetString().ShouldBe(ChatBotMessageCodes.GenericDenial);
        wire.GetProperty("completionStatus").GetString().ShouldBe(OperationStatusRecord.Failed);
        wire.GetProperty("lifecycleState").GetString().ShouldBe("Failed");
        wire.GetProperty("maxAttempts").GetInt32().ShouldBe(1);
        wire.GetProperty("retryCount").GetInt32().ShouldBe(0);
        wire.GetProperty("workflowRetryCount").GetInt32().ShouldBe(0);
        wire.GetProperty("retryEligible").GetBoolean().ShouldBeFalse();
        wire.GetProperty("safeNextActions").EnumerateArray().Single().GetString().ShouldBe("dismiss");
        wire.GetRawText().ShouldNotContain("private-unsafe-action");
        wire.GetRawText().ShouldNotContain("restricted private owner detail");
    }

    [Fact]
    public void UnknownSecondaryReasonShouldTurnSuccessfulStoredStatusIntoTerminalDenial()
    {
        DateTimeOffset now = new(2026, 8, 9, 10, 0, 0, TimeSpan.Zero);
        CommandSubmissionResponse accepted = new()
        {
            CommandId = "01ARZ3NDEKTSV4RRFFQ69G5FAY",
            CorrelationId = "01ARZ3NDEKTSV4RRFFQ69G5FAW",
            OperationId = "01ARZ3NDEKTSV4RRFFQ69G5FAX",
            LifecycleState = LifecycleState.Corrected,
            AcceptedAt = now,
        };
        OperationStatusRecord record = OperationStatusRecord.Accepted("tenant-alpha", accepted, false, now) with
        {
            ReasonCode = ChatBotMessageCodes.CommandAccepted,
            FailureReasonCode = "private-unknown-reason",
            CompletionStatus = OperationStatusRecord.Completed,
            SafeNextActions = ["retry-later"],
            NextRetryAt = now.AddSeconds(-1),
            MaxAttempts = 5,
        };

        JsonElement wire = OperationStatusHttpResults.ToJsonElement(record, now);

        wire.GetProperty("reasonCode").GetString().ShouldBe(ChatBotMessageCodes.GenericDenial);
        wire.GetProperty("failureReasonCode").GetString().ShouldBe(ChatBotMessageCodes.GenericDenial);
        wire.GetProperty("completionStatus").GetString().ShouldBe(OperationStatusRecord.Failed);
        wire.GetProperty("lifecycleState").GetString().ShouldBe("Failed");
        wire.GetProperty("retryEligible").GetBoolean().ShouldBeFalse();
        wire.GetProperty("safeNextActions").EnumerateArray().Single().GetString().ShouldBe("dismiss");
        wire.GetRawText().ShouldNotContain("private-unknown-reason");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ExplicitGenericDenialShouldSuppressSuccessfulStateAndOperationCounts(bool secondary)
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        OperationStatusRecord record = OperationStatusRecord.Accepted("tenant-alpha", new CommandSubmissionResponse
        {
            CommandId = "01ARZ3NDEKTSV4RRFFQ69G5FAY",
            CorrelationId = "01ARZ3NDEKTSV4RRFFQ69G5FAW",
            OperationId = "01ARZ3NDEKTSV4RRFFQ69G5FAX",
            LifecycleState = LifecycleState.Corrected,
            AcceptedAt = now,
        }, false, now) with
        {
            ReasonCode = secondary ? ChatBotMessageCodes.CommandAccepted : ChatBotMessageCodes.GenericDenial,
            FailureReasonCode = secondary ? ChatBotMessageCodes.GenericDenial : null,
            CompletionStatus = OperationStatusRecord.Completed,
            RetryCount = 2,
            MaxAttempts = 5,
            WorkflowRetryCount = 3,
            DuplicateAttemptCount = 4,
            SafeNextActions = ["retry-later"],
            NextRetryAt = now.AddSeconds(-1),
        };
        JsonElement wire = OperationStatusHttpResults.ToJsonElement(record, now);
        wire.GetProperty("completionStatus").GetString().ShouldBe("failed");
        wire.GetProperty("lifecycleState").GetString().ShouldBe("Failed");
        wire.GetProperty("safeNextActions")[0].GetString().ShouldBe("dismiss");
        wire.GetProperty("retryEligible").GetBoolean().ShouldBeFalse();
        wire.GetProperty("maxAttempts").GetInt32().ShouldBe(1);
        foreach (string field in new[] { "retryCount", "workflowRetryCount", "duplicateAttemptCount" })
        {
            wire.GetProperty(field).GetInt32().ShouldBe(0);
        }
    }

    [Theory]
    [InlineData(ChatBotMessageCodes.CommandAccepted)]
    [InlineData(ChatBotMessageCodes.AssociationCorrectionPropagationPending)]
    [InlineData(ChatBotMessageCodes.RetryQueued)]
    public void WaitingOutcomeShouldNotInventRetryPermission(string code)
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        OperationStatusRecord record = OperationStatusRecord.Accepted("tenant", new CommandSubmissionResponse
        {
            CommandId = "command", OperationId = "operation", CorrelationId = "correlation", AcceptedAt = now,
        }, false, now) with
        {
            ReasonCode = code,
            SafeNextActions = [ChatBotMessageNextActions.None],
            NextRetryAt = now.AddSeconds(-1),
            MaxAttempts = 5,
        };
        JsonElement wire = OperationStatusHttpResults.ToJsonElement(record, now);
        wire.GetProperty("retryEligible").GetBoolean().ShouldBeFalse();
        wire.GetProperty("safeNextActions").EnumerateArray().ShouldNotContain(action => action.GetString() == "retry-later");
    }

    /// <summary>Each optional reason guard independently denies malformed stored metadata.</summary>
    /// <param name="field">The optional reason field to invalidate.</param>
    [Theory]
    [InlineData("terminal")]
    [InlineData("failure")]
    [InlineData("terminal-code")]
    [InlineData("workflow")]
    public void EachUnknownOptionalReasonShouldIndependentlyDenyOutcome(string field)
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        OperationStatusRecord record = RetryableRecord(now);
        const string unknown = "private-optional-reason";
        record = field switch
        {
            "terminal" => record with { TerminalReason = unknown },
            "failure" => record with { FailureReasonCode = unknown },
            "terminal-code" => record with { TerminalReasonCode = unknown },
            _ => record with { WorkflowLastFailureCode = unknown },
        };
        JsonElement wire = OperationStatusHttpResults.ToJsonElement(record, now);
        wire.GetProperty("reasonCode").GetString().ShouldBe(ChatBotMessageCodes.GenericDenial);
        wire.GetProperty("completionStatus").GetString().ShouldBe("failed");
        wire.GetProperty("lifecycleState").GetString().ShouldBe("Failed");
        wire.GetProperty("retryEligible").GetBoolean().ShouldBeFalse();
        wire.GetProperty("safeNextActions").EnumerateArray().ShouldHaveSingleItem().GetString().ShouldBe("dismiss");
        wire.GetRawText().ShouldNotContain(unknown);
    }

    /// <summary>The primary lifecycle guard denies an otherwise valid retryable stored status.</summary>
    [Fact]
    public void UndefinedPrimaryLifecycleShouldIndependentlyDenyOutcome()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        OperationStatusRecord record = RetryableRecord(now);
        JsonElement valid = OperationStatusHttpResults.ToJsonElement(record, now);
        valid.GetProperty("reasonCode").GetString().ShouldBe(ChatBotMessageCodes.AuditUnavailable);
        valid.GetProperty("retryEligible").GetBoolean().ShouldBeTrue();

        JsonElement wire = OperationStatusHttpResults.ToJsonElement(record with { LifecycleState = (LifecycleState)999999 }, now);
        wire.GetProperty("reasonCode").GetString().ShouldBe(ChatBotMessageCodes.GenericDenial);
        wire.GetProperty("completionStatus").GetString().ShouldBe("failed");
        wire.GetProperty("lifecycleState").GetString().ShouldBe("Failed");
        wire.GetProperty("retryEligible").GetBoolean().ShouldBeFalse();
        wire.GetProperty("safeNextActions").EnumerateArray().ShouldHaveSingleItem().GetString().ShouldBe("dismiss");
        wire.GetRawText().ShouldNotContain("999999");
    }

    /// <summary>Malformed nested reasons and lifecycle values deny the whole status.</summary>
    /// <param name="field">The nested field to invalidate.</param>
    [Theory]
    [InlineData("reason")]
    [InlineData("lifecycle")]
    [InlineData("generic")]
    public void NestedOnlyMalformedOutcomeShouldDenyWholeStatus(string field)
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        CommandSubmissionResponse previous = new()
        {
            CommandId = "previous-command", OperationId = "previous-operation", CorrelationId = "previous-correlation",
            ReasonCode = field == "reason" ? (ChatBotMessageCode)999999 : field == "generic" ? ChatBotMessageCode.Generic_denial : ChatBotMessageCode.Command_accepted,
            LifecycleState = field == "lifecycle" ? (LifecycleState)999999 : LifecycleState.Proposed,
            RetryEligible = true,
            AcceptedAt = now,
        };
        JsonElement wire = OperationStatusHttpResults.ToJsonElement(RetryableRecord(now) with { PriorOutcome = previous }, now);
        wire.GetProperty("reasonCode").GetString().ShouldBe(ChatBotMessageCodes.GenericDenial);
        wire.GetProperty("completionStatus").GetString().ShouldBe("failed");
        wire.GetProperty("lifecycleState").GetString().ShouldBe("Failed");
        wire.GetProperty("retryEligible").GetBoolean().ShouldBeFalse();
        wire.GetProperty("safeNextActions").EnumerateArray().ShouldHaveSingleItem().GetString().ShouldBe("dismiss");
        wire.TryGetProperty("priorOutcome", out _).ShouldBeFalse();
        wire.GetRawText().ShouldNotContain("999999");
    }

    /// <summary>Nested retry permission cannot exceed the catalog action permission.</summary>
    /// <param name="reason">The nested catalog reason.</param>
    /// <param name="permitted">Whether that reason permits retry.</param>
    [Theory]
    [InlineData(ChatBotMessageCode.Command_accepted, false)]
    [InlineData(ChatBotMessageCode.Authentication_denied, false)]
    [InlineData(ChatBotMessageCode.Audit_unavailable, true)]
    public void NestedRetryEligibilityShouldIntersectCatalogPermission(ChatBotMessageCode reason, bool permitted)
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        CommandSubmissionResponse previous = new()
        {
            CommandId = "previous-command", OperationId = "previous-operation", CorrelationId = "previous-correlation",
            ReasonCode = reason, LifecycleState = LifecycleState.Proposed, RetryEligible = true, AcceptedAt = now,
        };
        JsonElement wire = OperationStatusHttpResults.ToJsonElement(RetryableRecord(now) with { PriorOutcome = previous }, now);
        wire.GetProperty("priorOutcome").GetProperty("retryEligible").GetBoolean().ShouldBe(permitted);
        wire.GetProperty("reasonCode").GetString().ShouldBe(ChatBotMessageCodes.AuditUnavailable);
    }

    private static OperationStatusRecord RetryableRecord(DateTimeOffset now)
        => OperationStatusRecord.Accepted("tenant", new CommandSubmissionResponse
        {
            CommandId = "command", OperationId = "operation", CorrelationId = "correlation", AcceptedAt = now,
            LifecycleState = LifecycleState.Proposed,
        }, false, now) with
        {
            ReasonCode = ChatBotMessageCodes.AuditUnavailable,
            SafeNextActions = [ChatBotMessageNextActions.RetryLater],
            NextRetryAt = now.AddSeconds(-1),
            MaxAttempts = 5,
        };

    [Theory]
    [InlineData(new string[] { "none" }, false)]
    [InlineData(new string[] { "retry-later", "private-unsafe-action", "escalate" }, true)]
    public void KnownCodeShouldOnlyExposeStoredActionsAllowedByCatalog(string[] storedActions, bool retryAllowed)
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
            ReasonCode = ChatBotMessageCodes.AuditUnavailable,
            SafeNextActions = storedActions,
            NextRetryAt = now.AddSeconds(-1),
            MaxAttempts = 5,
        };

        JsonElement wire = OperationStatusHttpResults.ToJsonElement(record, now);

        wire.GetProperty("reasonCode").GetString().ShouldBe(ChatBotMessageCodes.AuditUnavailable);
        wire.GetProperty("retryEligible").GetBoolean().ShouldBe(retryAllowed);
        string[] actions = wire.GetProperty("safeNextActions").EnumerateArray()
            .Select(action => action.GetString()!).ToArray();
        actions.ShouldBe(retryAllowed ? ["retry-later"] : []);
        wire.GetRawText().ShouldNotContain("private-unsafe-action");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RetryActivityShouldPublishDueTimeAndEligibilityUntilAttemptLimit(bool sdk)
    {
        DateTimeOffset now = new(2026, 8, 9, 10, 0, 0, TimeSpan.Zero);
        FixedClock clock = new(now);
        IOperationStatusStore store = sdk ? new ReadModelOperationStatusStore(new AtomicOperationStatusReadModelStore()) : new InMemoryOperationStatusStore();
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

    [Theory]
    [InlineData(false, CorrectionPropagationWorkflowStatuses.Started)]
    [InlineData(false, CorrectionPropagationWorkflowStatuses.Retrying)]
    [InlineData(true, CorrectionPropagationWorkflowStatuses.Started)]
    [InlineData(true, CorrectionPropagationWorkflowStatuses.Retrying)]
    public async Task OlderWorkflowReportLosingAtomicWriteShouldPreserveCompletedProgressAndDisableRetry(bool sdk, string olderStatus)
    {
        using RacingOperationStatusStore store = new(sdk) { SecondWriterWins = true };
        DateTimeOffset now = new(2026, 8, 9, 10, 0, 0, TimeSpan.Zero);
        OperationStatusWorkflowStatusSink olderSink = new(store, new FixedClock(now));
        OperationStatusWorkflowStatusSink completionSink = new(store, new FixedClock(now.AddSeconds(1)));
        CommandSubmissionResponse accepted = new()
        {
            CommandId = "01ARZ3NDEKTSV4RRFFQ69G5FAY", CorrelationId = "01ARZ3NDEKTSV4RRFFQ69G5FAW",
            OperationId = "01ARZ3NDEKTSV4RRFFQ69G5FAX", LifecycleState = LifecycleState.Correcting, AcceptedAt = now,
        };
        await store.UpsertAsync(OperationStatusRecord.Accepted("tenant-alpha", accepted, false, now), TestContext.Current.CancellationToken);
        CorrectionPropagationRequest request = new("tenant-alpha", "actor", "association", "intake", "correction", "workflow",
            "prior-project", "corrected-project", 1, accepted.CorrelationId, now, now.AddMinutes(1), OperationId: accepted.OperationId);
        await olderSink.ReportAsync(request, CorrectionPropagationWorkflowStatuses.Retrying, 1,
            CorrectionPropagationWorkflowFailureCodes.CaseResolutionUnavailable, TestContext.Current.CancellationToken, retryDueAt: now);
        OperationStatusRecord pending = (await store.TryGetAsync(request.TenantId, request.OperationId, TestContext.Current.CancellationToken))!;
        OperationStatusHttpResults.ToJsonElement(pending, now).GetProperty("retryEligible").GetBoolean().ShouldBeTrue();
        store.Arm();
        Task older = Task.Run(async () => await olderSink.ReportAsync(request, olderStatus, 3,
            CorrectionPropagationWorkflowFailureCodes.CaseResolutionUnavailable, TestContext.Current.CancellationToken,
            retryDueAt: now.AddSeconds(30)).ConfigureAwait(false), TestContext.Current.CancellationToken);
        await store.FirstSnapshotRead.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
        await completionSink.ReportAsync(request, CorrectionPropagationWorkflowStatuses.Completed, 0,
            CorrectionPropagationWorkflowFailureCodes.None, TestContext.Current.CancellationToken);
        await older.ConfigureAwait(true);
        OperationStatusRecord completed = (await store.TryGetAsync(request.TenantId, request.OperationId, TestContext.Current.CancellationToken))!;
        completed.WorkflowInstanceId.ShouldBe(request.WorkflowInstanceId);
        completed.WorkflowStatus.ShouldBe(CorrectionPropagationWorkflowStatuses.Completed);
        completed.WorkflowRetryCount.ShouldBe(0);
        completed.CompletionStatus.ShouldBe(OperationStatusRecord.Completed);
        completed.ReasonCode.ShouldBe(ChatBotMessageCodes.AssociationCorrectionPropagationComplete);
        completed.SafeNextActions.ShouldBe([ChatBotMessageNextActions.None]);
        completed.NextRetryAt.ShouldBeNull();
        completed.FailureReasonCode.ShouldBeNull();
        completed.WorkflowLastFailureCode.ShouldBeNull();
        completed.LastUpdatedAt.ShouldBe(now.AddSeconds(1));
        JsonElement wire = OperationStatusHttpResults.ToJsonElement(completed, now.AddMinutes(1));
        wire.GetProperty("workflowStatus").GetString().ShouldBe(CorrectionPropagationWorkflowStatuses.Completed);
        wire.GetProperty("reasonCode").GetString().ShouldBe(ChatBotMessageCodes.AssociationCorrectionPropagationComplete);
        wire.GetProperty("safeNextActions").EnumerateArray().Single().GetString().ShouldBe(ChatBotMessageNextActions.None);
        wire.GetProperty("retryEligible").GetBoolean().ShouldBeFalse();
        store.CallbackCount.ShouldBe(3);
        if (sdk) { store.ConditionalConflicts.ShouldBe(1); }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task WorkflowReportForAbsentOperationShouldRemainAbsent(bool sdk)
    {
        IOperationStatusStore store = sdk ? new ReadModelOperationStatusStore(new AtomicOperationStatusReadModelStore()) : new InMemoryOperationStatusStore();
        FixedClock clock = new(new DateTimeOffset(2026, 8, 9, 10, 0, 0, TimeSpan.Zero));
        OperationStatusWorkflowStatusSink sink = new(store, clock);
        CorrectionPropagationRequest request = new("tenant-alpha", "actor", "association", "intake", "correction", "workflow",
            "prior-project", "corrected-project", 1, "01ARZ3NDEKTSV4RRFFQ69G5FAW", clock.UtcNow, clock.UtcNow.AddMinutes(1),
            OperationId: "01ARZ3NDEKTSV4RRFFQ69G5FAX");
        await sink.ReportAsync(request, CorrectionPropagationWorkflowStatuses.Completed, 0, null, TestContext.Current.CancellationToken);
        (await store.TryGetAsync(request.TenantId, request.OperationId, TestContext.Current.CancellationToken)).ShouldBeNull();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ReportAsyncShouldWriteWorkflowFieldsOntoExistingOperationStatus(bool sdk)
    {
        IOperationStatusStore store = sdk ? new ReadModelOperationStatusStore(new AtomicOperationStatusReadModelStore()) : new InMemoryOperationStatusStore();
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
