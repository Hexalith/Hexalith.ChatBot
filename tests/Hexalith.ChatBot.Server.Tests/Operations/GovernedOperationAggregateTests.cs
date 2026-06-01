using System.Text;
using System.Text.Json;

using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Contracts.Queries;
using Hexalith.ChatBot.Server.Association.Intake;
using Hexalith.ChatBot.Server.Governance.AiMediation;
using Hexalith.ChatBot.Server.Operations;
using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Events;
using Hexalith.EventStore.Contracts.Results;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Operations;

public static class GovernedOperationAggregateTests
{
    private const string NoteId = "01ARZ3NDEKTSV4RRFFQ69G5FAY";
    private const string IntakeId = "01ARZ3NDEKTSV4RRFFQ69G5FAZ";

    [Fact]
    public static void HandleLowRiskAiExecutionShouldEmitStartedAndTerminalOutcomeEvents()
    {
        ExecuteLowRiskAIAssistance command = LowRiskExecutionCommand("success");

        DomainResult result = GovernedOperationAggregate.Handle(command, null, Envelope(command));

        result.IsSuccess.ShouldBeTrue();
        result.Events.Count.ShouldBe(2);
        LowRiskAiAssistanceExecutionStarted started = result.Events[0].ShouldBeOfType<LowRiskAiAssistanceExecutionStarted>();
        started.ExecutionId.ShouldBe(command.ExecutionId);
        started.PolicyReasonCode.ShouldBe("low-risk-execute-allowed");
        LowRiskAiAssistanceExecutionSucceeded succeeded = result.Events[1].ShouldBeOfType<LowRiskAiAssistanceExecutionSucceeded>();
        succeeded.Record.SafeNextAction.ShouldBe("none");
    }

    [Fact]
    public static void HandleLowRiskAiExecutionRoutedToApprovalShouldNotEmitExecutionStarted()
    {
        ExecuteLowRiskAIAssistance command = LowRiskExecutionCommand("pending-approval", "low_risk_policy_false");

        DomainResult result = GovernedOperationAggregate.Handle(command, null, Envelope(command));

        result.IsSuccess.ShouldBeTrue();
        result.Events.Count.ShouldBe(2);
        LowRiskAiAssistanceRoutedToApproval routed = result.Events[0].ShouldBeOfType<LowRiskAiAssistanceRoutedToApproval>();
        routed.Record.SafeNextAction.ShouldBe("review-ai-action");
        routed.Record.PolicyReasonCode.ShouldBe("low_risk_policy_false");
        AiActionApprovalRequested approval = result.Events[1].ShouldBeOfType<AiActionApprovalRequested>();
        approval.ProposalId.ShouldBe(command.ProposalId);
        approval.SourceMessageId.ShouldBe(command.SourceMessageId);
    }

    [Fact]
    public static void HandleLowRiskAiExecutionShouldRejectSuccessWithApprovalNextAction()
    {
        ExecuteLowRiskAIAssistance command = LowRiskExecutionCommand("success") with
        {
            ExecutionRecord = LowRiskExecutionCommand("success").ExecutionRecord! with
            {
                SafeNextAction = "review-ai-action",
            },
        };

        DomainResult result = GovernedOperationAggregate.Handle(command, null, Envelope(command));

        result.IsRejection.ShouldBeTrue();
    }

    [Fact]
    public static void HandleLowRiskAiExecutionShouldBeIdempotentForExistingExecutionId()
    {
        ExecuteLowRiskAIAssistance command = LowRiskExecutionCommand("failed");
        GovernedOperationState state = new();
        state.Apply(new LowRiskAiAssistanceExecutionStarted(
            command.ExecutionId,
            command.ProposalId,
            command.ProjectId,
            command.TaskIntentId,
            command.SourceMessageId,
            command.RequesterId,
            "summarize-visible-context",
            command.ContextPackageId,
            command.ContextPackageVersion,
            "policy-snap-001",
            "low-risk-execute-allowed",
            command.ExpectedProposalSourceVersion,
            command.CorrelationId,
            DateTimeOffset.UtcNow));

        DomainResult result = GovernedOperationAggregate.Handle(command, state, Envelope(command));

        result.IsNoOp.ShouldBeTrue();
        result.Events.ShouldBeEmpty();
    }

    [Fact]
    public static void HandleOnNewAggregateShouldRecordTheNote()
    {
        DomainResult result = GovernedOperationAggregate.Handle(new RecordGovernedNote(NoteId), state: null);

        result.IsSuccess.ShouldBeTrue();
        result.Events.Count.ShouldBe(1);
        GovernedNoteRecorded recorded = result.Events[0].ShouldBeOfType<GovernedNoteRecorded>();
        recorded.NoteId.ShouldBe(NoteId);
    }

    [Fact]
    public static void HandleOnAlreadyRecordedAggregateShouldRejectWithoutThrowing()
    {
        GovernedOperationState state = new();
        state.Apply(new GovernedNoteRecorded(NoteId));

        DomainResult result = GovernedOperationAggregate.Handle(new RecordGovernedNote(NoteId), state);

        result.IsRejection.ShouldBeTrue();
        result.Events.Count.ShouldBe(1);
        GovernedNoteAlreadyRecordedRejection rejection = result.Events[0].ShouldBeOfType<GovernedNoteAlreadyRecordedRejection>();
        rejection.NoteId.ShouldBe(NoteId);
        result.Events[0].ShouldBeAssignableTo<IRejectionEvent>();
    }

    [Fact]
    public static void ApplyShouldBeIdempotentOnReplay()
    {
        GovernedOperationState state = new();
        state.IsRecorded.ShouldBeFalse();

        state.Apply(new GovernedNoteRecorded(NoteId));
        state.IsRecorded.ShouldBeTrue();
        state.NoteId.ShouldBe(NoteId);

        // A duplicate event during replay must leave state unchanged (order-tolerant, idempotent).
        state.Apply(new GovernedNoteRecorded(NoteId));
        state.IsRecorded.ShouldBeTrue();
        state.NoteId.ShouldBe(NoteId);
    }

    [Fact]
    public static async Task ProcessAsyncShouldDiscoverHandleByReflectionAndProduceTheEvent()
    {
        GovernedOperationAggregate aggregate = new();
        CommandEnvelope command = Envelope(new RecordGovernedNote(NoteId));

        DomainResult result = await aggregate.ProcessAsync(command, currentState: null);

        result.IsSuccess.ShouldBeTrue();
        result.Events[0].ShouldBeOfType<GovernedNoteRecorded>().NoteId.ShouldBe(NoteId);
    }

    [Fact]
    public static void HandleMailboxIntakeShouldCaptureSourceIdentityAndNormalizeTimestampsToUtc()
    {
        CaptureMailboxMessageIntake command = MailboxCommand();

        DomainResult result = GovernedOperationAggregate.Handle(command, state: null);

        result.IsSuccess.ShouldBeTrue();
        MailboxMessageIntakeCaptured captured = result.Events[0].ShouldBeOfType<MailboxMessageIntakeCaptured>();
        captured.IntakeId.ShouldBe(IntakeId);
        captured.ProviderMessageId.ShouldBe("graph-message-001");
        captured.InternetMessageId.ShouldBe("<message-001@example.test>");
        captured.ConversationId.ShouldBe("graph-conversation-001");
        captured.MailboxId.ShouldBe("controlled-mailbox-001");
        captured.Sender.Address.ShouldBe("sender@example.test");
        captured.Recipients.Single().Address.ShouldBe("project@example.test");
        captured.AttachmentReferences.Single().ProviderAttachmentId.ShouldBe("attachment-001");
        captured.ReceivedAtUtc.Offset.ShouldBe(TimeSpan.Zero);
        captured.ReceivedAtUtc.ShouldBe(new DateTimeOffset(2026, 5, 30, 8, 15, 0, TimeSpan.Zero));
        captured.SourceTimezone.ShouldBe("W. Europe Standard Time");
        captured.SourceProvenance.ShouldBe("m365-mailbox-intake");
        captured.RedactionState.ShouldBe("metadata_only");
        captured.RetentionClass.ShouldBe("collaboration_input");
    }

    [Fact]
    public static void HandleMailboxIntakeOnCapturedAggregateShouldReturnStructuredRejection()
    {
        GovernedOperationState state = new();
        state.Apply(new MailboxMessageIntakeCaptured(
            IntakeId,
            "graph-message-001",
            "<message-001@example.test>",
            "graph-conversation-001",
            null,
            "controlled-mailbox-001",
            new MailboxParticipantIdentity("sender@example.test", null),
            [new MailboxRecipientIdentity("project@example.test", null, "to")],
            DateTimeOffset.UtcNow,
            null,
            null,
            [],
            null,
            "graph-message-v1",
            "m365-mailbox-intake",
            "mailbox-intake.kernel.v1",
            "metadata_only",
            "collaboration_input",
            1));

        DomainResult result = GovernedOperationAggregate.Handle(MailboxCommand(), state);

        result.IsRejection.ShouldBeTrue();
        result.Events[0].ShouldBeOfType<MailboxMessageIntakeAlreadyCapturedRejection>().IntakeId.ShouldBe(IntakeId);
        result.Events[0].ShouldBeAssignableTo<IRejectionEvent>();
    }

    [Fact]
    public static async Task ProcessAsyncShouldHandleWorkflowRetryThroughAggregateReflection()
    {
        GovernedOperationAggregate aggregate = new();
        RequestFailedWorkflowRetry command = RetryCommand();
        CommandEnvelope envelope = new(
            MessageId: command.RetryId,
            TenantId: "tenant-alpha",
            Domain: "chatbot",
            AggregateId: command.RetryId,
            CommandType: nameof(RequestFailedWorkflowRetry),
            Payload: JsonSerializer.SerializeToUtf8Bytes(command),
            CorrelationId: "01ARZ3NDEKTSV4RRFFQ69G5FAW",
            CausationId: null,
            UserId: "actor-alpha",
            Extensions: null);

        DomainResult result = await aggregate.ProcessAsync(envelope, currentState: null);

        result.IsSuccess.ShouldBeTrue();
        WorkflowRetryRequested retry = result.Events.ShouldHaveSingleItem().ShouldBeOfType<WorkflowRetryRequested>();
        retry.RetryId.ShouldBe(command.RetryId);
        retry.FailedEventId.ShouldBe(command.FailedEventId);
        retry.FailedOperationClass.ShouldBe("message-intake");
        retry.FailureReasonCode.ShouldBe("graph_throttled");
    }

    [Fact]
    public static void HandleWorkflowRetryShouldRejectInvalidPayloadWithoutThrowing()
    {
        RequestFailedWorkflowRetry command = RetryCommand() with { FailedEventId = "raw-provider-message-id" };

        DomainResult result = GovernedOperationAggregate.Handle(command, state: null);

        result.IsRejection.ShouldBeTrue();
        WorkflowRetryInvalidRejection rejection = result.Events.ShouldHaveSingleItem().ShouldBeOfType<WorkflowRetryInvalidRejection>();
        rejection.RetryId.ShouldBe(command.RetryId);
        rejection.ReasonCode.ShouldBe("invalid_workflow_retry_payload");
        result.Events[0].ShouldBeAssignableTo<IRejectionEvent>();
    }

    [Fact]
    public static void HandleCaptureTaskIntentShouldCaptureAndTreatReplayAsNoOp()
    {
        CaptureTaskIntent command = TaskIntentCommand();
        CommandEnvelope envelope = TaskIntentEnvelope(command);

        DomainResult result = GovernedOperationAggregate.Handle(command, state: null, envelope);

        result.IsSuccess.ShouldBeTrue();
        TaskIntentCaptured captured = result.Events.ShouldHaveSingleItem().ShouldBeOfType<TaskIntentCaptured>();
        captured.Record.TenantId.ShouldBe("tenant-alpha");
        captured.Record.TaskIntentId.ShouldBe(TaskIntentIdempotency.ComposeKey(
            "tenant-alpha",
            command.ProjectId,
            command.SourceMessageId,
            command.RequesterPartyId,
            command.KernelVersion,
            command.DetectedActionKind,
            command.SourceEvidenceOffsets));

        GovernedOperationState state = new();
        state.Apply(captured);

        DomainResult replay = GovernedOperationAggregate.Handle(command, state, envelope);

        replay.IsNoOp.ShouldBeTrue();
    }

    [Fact]
    public static void HandleProposeAiActionShouldConvertCapturedTaskIntentAndRejectSecondConversion()
    {
        CaptureTaskIntent capture = TaskIntentCommand();
        CommandEnvelope envelope = TaskIntentEnvelope(capture);
        TaskIntentCaptured captured = GovernedOperationAggregate
            .Handle(capture, state: null, envelope)
            .Events
            .ShouldHaveSingleItem()
            .ShouldBeOfType<TaskIntentCaptured>();
        GovernedOperationState state = new();
        state.Apply(captured);

        ProposeAIAction command = new(
            capture.ProjectId,
            captured.Record.TaskIntentId,
            capture.SourceMessageId,
            capture.RequesterPartyId,
            "CreateProjectTask",
            "project-task",
            capture.SourceVersion,
            ["message:offset:001"],
            ["project:project-001"],
            ["party-001"],
            capture.PolicySnapshotId,
            capture.CorrelationId,
            "transition-001",
            SourceConversationItemId: capture.SourceMessageId,
            RiskClassification: Classification("CreateProjectTask", capture.CorrelationId, capture.PolicySnapshotId));

        DomainResult result = GovernedOperationAggregate.Handle(command, state, envelope);

        result.IsSuccess.ShouldBeTrue();
        result.Events.Count.ShouldBe(2);
        TaskIntentConvertedToAiActionProposal converted = result.Events.OfType<TaskIntentConvertedToAiActionProposal>().ShouldHaveSingleItem();
        converted.TaskIntent.State.ShouldBe(TaskIntentState.Converted);
        converted.TaskIntent.ConvertedProposalId.ShouldBe(converted.Proposal.ProposalId);
        converted.Proposal.SafeNextAction.ShouldBe("review-ai-action");
        AiActionApprovalRequested requested = result.Events.OfType<AiActionApprovalRequested>().ShouldHaveSingleItem();
        requested.ApprovalId.ShouldBe($"approval:{converted.Proposal.ProposalId}");
        requested.AiRiskClass.ShouldBe(AiActionRiskClass.ApprovalRequired);
        requested.AiRiskActionClasses.ShouldBe(["creates-tasks"], ignoreOrder: false);
        requested.EvidenceFreshnessStates.ShouldBe([ApprovalEvidenceFreshness.Expired], ignoreOrder: false);

        state.Apply(converted);
        state.Apply(requested);
        DomainResult replay = GovernedOperationAggregate.Handle(command, state, envelope);
        replay.IsNoOp.ShouldBeTrue();

        ProposeAIAction conflicting = command with { TransitionId = "transition-002" };
        DomainResult rejected = GovernedOperationAggregate.Handle(conflicting, state, envelope);
        rejected.IsRejection.ShouldBeTrue();
        rejected.Events.ShouldHaveSingleItem().ShouldBeOfType<TaskIntentTransitionRejected>().ReasonCode.ShouldBe("task_intent_already_converted");
    }

    [Fact]
    public static void HandleApproveAiActionShouldRecordDecisionAndPermissionForLaterExecution()
    {
        AiActionApprovalRequested requested = ApprovalRequest([ApprovalEvidenceFreshness.Fresh]);
        GovernedOperationState state = new();
        state.Apply(requested);
        DecideAiActionApproval command = ApprovalDecision(ApprovalDecisionKind.Approve, requested);

        DomainResult result = GovernedOperationAggregate.Handle(command, state, Envelope(command));

        result.IsSuccess.ShouldBeTrue();
        AiActionApprovalDecisionRecorded recorded = result.Events.ShouldHaveSingleItem().ShouldBeOfType<AiActionApprovalDecisionRecorded>();
        recorded.DecisionKind.ShouldBe(ApprovalDecisionKind.Approve);
        recorded.SafeNextAction.ShouldBe("execute-approved-ai-action");
        recorded.AuditOperationId.ShouldBe($"audit:{command.DecisionId}");

        state.Apply(recorded);
        GovernedOperationAggregate.Handle(command, state, Envelope(command)).IsNoOp.ShouldBeTrue();
        GovernedOperationAggregate
            .Handle(command with { Decision = ApprovalDecisionKind.Reject, DecisionId = "approval-decision-002" }, state, Envelope(command))
            .IsRejection
            .ShouldBeTrue();
    }

    [Fact]
    public static void HandleApproveAiActionShouldRejectExpiredEvidenceButAllowRejectDecision()
    {
        AiActionApprovalRequested requested = ApprovalRequest([ApprovalEvidenceFreshness.Expired]);
        GovernedOperationState state = new();
        state.Apply(requested);
        DecideAiActionApproval approveCommand = ApprovalDecision(ApprovalDecisionKind.Approve, requested);
        DecideAiActionApproval rejectCommand = ApprovalDecision(ApprovalDecisionKind.Reject, requested);

        DomainResult approve = GovernedOperationAggregate.Handle(approveCommand, state, Envelope(approveCommand));
        DomainResult reject = GovernedOperationAggregate.Handle(rejectCommand, state, Envelope(rejectCommand));

        approve.IsRejection.ShouldBeTrue();
        approve.Events.ShouldHaveSingleItem().ShouldBeOfType<AiActionApprovalDecisionRejected>().ReasonCode.ShouldBe("evidence-expired");
        reject.IsSuccess.ShouldBeTrue();
        reject.Events.ShouldHaveSingleItem().ShouldBeOfType<AiActionApprovalDecisionRecorded>().SafeNextAction.ShouldBe("none");
    }

    [Fact]
    public static void HandleProposeAiActionShouldRejectTenantRequesterAndUnsafeMetadataMismatches()
    {
        CaptureTaskIntent capture = TaskIntentCommand();
        CommandEnvelope envelope = TaskIntentEnvelope(capture);
        TaskIntentCaptured captured = GovernedOperationAggregate
            .Handle(capture, state: null, envelope)
            .Events
            .ShouldHaveSingleItem()
            .ShouldBeOfType<TaskIntentCaptured>();
        GovernedOperationState state = new();
        state.Apply(captured);
        ProposeAIAction command = new(
            capture.ProjectId,
            captured.Record.TaskIntentId,
            capture.SourceMessageId,
            capture.RequesterPartyId,
            "CreateProjectTask",
            "project-task",
            capture.SourceVersion,
            ["message:offset:001"],
            ["project:project-001"],
            ["party-001"],
            capture.PolicySnapshotId,
            capture.CorrelationId,
            "transition-001",
            SourceConversationItemId: capture.SourceMessageId,
            RiskClassification: Classification("CreateProjectTask", capture.CorrelationId, capture.PolicySnapshotId));

        DomainResult tenantRejected = GovernedOperationAggregate.Handle(
            command,
            state,
            envelope with { TenantId = "tenant-beta" });
        DomainResult requesterRejected = GovernedOperationAggregate.Handle(
            command with { RequesterId = "party-foreign" },
            state,
            envelope);
        DomainResult metadataRejected = GovernedOperationAggregate.Handle(
            command with { AffectedResourceReferences = ["project:project-001/raw-path"] },
            state,
            envelope);

        tenantRejected.IsRejection.ShouldBeTrue();
        tenantRejected.Events.ShouldHaveSingleItem().ShouldBeOfType<TaskIntentTransitionRejected>().ReasonCode
            .ShouldBe("task_intent_unavailable");
        requesterRejected.IsRejection.ShouldBeTrue();
        requesterRejected.Events.ShouldHaveSingleItem().ShouldBeOfType<TaskIntentTransitionRejected>().ReasonCode
            .ShouldBe("task_intent_transition_metadata_invalid");
        metadataRejected.IsRejection.ShouldBeTrue();
        metadataRejected.Events.ShouldHaveSingleItem().ShouldBeOfType<TaskIntentTransitionRejected>().ReasonCode
            .ShouldBe("task_intent_transition_metadata_invalid");
    }

    [Fact]
    public static void StateReplayShouldNotLetCapturedEventOverwriteTerminalTaskIntentWithSameSourceVersion()
    {
        CaptureTaskIntent capture = TaskIntentCommand();
        CommandEnvelope envelope = TaskIntentEnvelope(capture);
        TaskIntentCaptured captured = GovernedOperationAggregate
            .Handle(capture, state: null, envelope)
            .Events
            .ShouldHaveSingleItem()
            .ShouldBeOfType<TaskIntentCaptured>();
        GovernedOperationState state = new();
        state.Apply(captured);
        ProposeAIAction command = new(
            capture.ProjectId,
            captured.Record.TaskIntentId,
            capture.SourceMessageId,
            capture.RequesterPartyId,
            "CreateProjectTask",
            "project-task",
            capture.SourceVersion,
            ["message:offset:001"],
            ["project:project-001"],
            ["party-001"],
            capture.PolicySnapshotId,
            capture.CorrelationId,
            "transition-001",
            SourceConversationItemId: capture.SourceMessageId,
            RiskClassification: Classification("CreateProjectTask", capture.CorrelationId, capture.PolicySnapshotId));
        IReadOnlyList<object> conversionEvents = GovernedOperationAggregate
            .Handle(command, state, envelope)
            .Events;
        conversionEvents.Count.ShouldBe(2);
        TaskIntentConvertedToAiActionProposal converted = conversionEvents[0].ShouldBeOfType<TaskIntentConvertedToAiActionProposal>();
        conversionEvents[1].ShouldBeOfType<AiActionApprovalRequested>();

        state.Apply(converted);
        state.Apply(captured);

        state.TaskIntents[captured.Record.TaskIntentId].State.ShouldBe(TaskIntentState.Converted);
    }

    [Theory]
    [InlineData("not-actionable", TaskIntentState.NotActionable)]
    [InlineData("already-handled", TaskIntentState.AlreadyHandled)]
    [InlineData("out-of-scope", TaskIntentState.OutOfScope)]
    public static void HandleDispositionShouldMarkTerminalState(string disposition, TaskIntentState expectedState)
    {
        CaptureTaskIntent capture = TaskIntentCommand();
        CommandEnvelope envelope = TaskIntentEnvelope(capture);
        TaskIntentCaptured captured = GovernedOperationAggregate
            .Handle(capture, state: null, envelope)
            .Events
            .ShouldHaveSingleItem()
            .ShouldBeOfType<TaskIntentCaptured>();
        GovernedOperationState state = new();
        state.Apply(captured);

        MarkTaskIntentDisposition command = new(
            capture.ProjectId,
            captured.Record.TaskIntentId,
            capture.SourceMessageId,
            disposition,
            capture.SourceVersion,
            ["message:offset:001"],
            capture.PolicySnapshotId,
            capture.CorrelationId,
            $"transition-{disposition}");

        DomainResult result = GovernedOperationAggregate.Handle(command, state, envelope);

        result.IsSuccess.ShouldBeTrue();
        TaskIntentDispositionMarked marked = result.Events.ShouldHaveSingleItem().ShouldBeOfType<TaskIntentDispositionMarked>();
        marked.TaskIntent.State.ShouldBe(expectedState);
        marked.TaskIntent.SafeNextAction.ShouldBe("none");
        marked.TaskIntent.ReviewerActorId.ShouldBe("actor-alpha");
    }

    [Fact]
    public static void DuplicateDispositionShouldRequireSameProjectPredecessor()
    {
        CaptureTaskIntent capture = TaskIntentCommand();
        CommandEnvelope envelope = TaskIntentEnvelope(capture);
        TaskIntentCaptured captured = GovernedOperationAggregate
            .Handle(capture, state: null, envelope)
            .Events
            .ShouldHaveSingleItem()
            .ShouldBeOfType<TaskIntentCaptured>();
        GovernedOperationState state = new();
        state.Apply(captured);

        MarkTaskIntentDisposition command = new(
            capture.ProjectId,
            captured.Record.TaskIntentId,
            capture.SourceMessageId,
            "duplicate",
            capture.SourceVersion,
            ["message:offset:001"],
            capture.PolicySnapshotId,
            capture.CorrelationId,
            "transition-duplicate");

        DomainResult result = GovernedOperationAggregate.Handle(command, state, envelope);

        result.IsRejection.ShouldBeTrue();
        result.Events.ShouldHaveSingleItem().ShouldBeOfType<TaskIntentTransitionRejected>().ReasonCode
            .ShouldBe("task_intent_duplicate_predecessor_unavailable");
    }

    [Fact]
    public static void DuplicateDispositionShouldRejectForeignTenantPredecessorAndAcceptSameScopePredecessor()
    {
        CaptureTaskIntent capture = TaskIntentCommand();
        CommandEnvelope envelope = TaskIntentEnvelope(capture);
        TaskIntentCaptured captured = GovernedOperationAggregate
            .Handle(capture, state: null, envelope)
            .Events
            .ShouldHaveSingleItem()
            .ShouldBeOfType<TaskIntentCaptured>();
        GovernedOperationState state = new();
        state.Apply(captured);
        state.Apply(new TaskIntentCaptured(captured.Record with
        {
            TaskIntentId = "task-intent:predecessor-foreign",
            TenantId = "tenant-beta",
        }));
        state.Apply(new TaskIntentCaptured(captured.Record with
        {
            TaskIntentId = "task-intent:predecessor-alpha",
        }));

        MarkTaskIntentDisposition foreign = new(
            capture.ProjectId,
            captured.Record.TaskIntentId,
            capture.SourceMessageId,
            "duplicate",
            capture.SourceVersion,
            ["message:offset:001"],
            capture.PolicySnapshotId,
            capture.CorrelationId,
            "transition-duplicate-foreign",
            "task-intent:predecessor-foreign");
        MarkTaskIntentDisposition sameScope = foreign with
        {
            TransitionId = "transition-duplicate-alpha",
            PredecessorTaskIntentId = "task-intent:predecessor-alpha",
        };

        DomainResult foreignResult = GovernedOperationAggregate.Handle(foreign, state, envelope);
        DomainResult sameScopeResult = GovernedOperationAggregate.Handle(sameScope, state, envelope);

        foreignResult.IsRejection.ShouldBeTrue();
        foreignResult.Events.ShouldHaveSingleItem().ShouldBeOfType<TaskIntentTransitionRejected>().ReasonCode
            .ShouldBe("task_intent_duplicate_predecessor_unavailable");
        sameScopeResult.IsSuccess.ShouldBeTrue();
        sameScopeResult.Events.ShouldHaveSingleItem().ShouldBeOfType<TaskIntentDispositionMarked>().TaskIntent.State
            .ShouldBe(TaskIntentState.Duplicate);
    }

    private static CommandEnvelope Envelope(RecordGovernedNote command)
        => new(
            MessageId: NoteId,
            TenantId: "tenant-alpha",
            Domain: "chatbot",
            AggregateId: command.NoteId,
            CommandType: nameof(RecordGovernedNote),
            // The aggregate base deserializes the payload with default (PascalCase, case-sensitive)
            // JsonSerializer options, so serialize the same way here.
            Payload: JsonSerializer.SerializeToUtf8Bytes(command),
            CorrelationId: "01ARZ3NDEKTSV4RRFFQ69G5FAW",
            CausationId: null,
            UserId: "actor-alpha",
            Extensions: null);

    private static CommandEnvelope Envelope(IChatBotCommand command)
        => new(
            MessageId: "01ARZ3NDEKTSV4RRFFQ69G5FAL",
            TenantId: "tenant-alpha",
            Domain: "chatbot",
            AggregateId: "graph-message-001",
            CommandType: command.GetType().Name,
            Payload: JsonSerializer.SerializeToUtf8Bytes(command),
            CorrelationId: "correlation-001",
            CausationId: null,
            UserId: "actor-alpha",
            Extensions: null);

    private static AiActionApprovalRequested ApprovalRequest(IReadOnlyList<ApprovalEvidenceFreshness> freshness)
        => new(
            "approval:ai-proposal-001",
            "project-001",
            "ai-proposal-001",
            "task-intent-001",
            "graph-message-001",
            "graph-message-001",
            "party-001",
            "human",
            new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero),
            AiActionCommandMetadataProvider.AppendConversationMessageCommandName,
            AiActionCommandMetadataProvider.M0AllowlistVersion,
            AiActionRiskClass.ApprovalRequired,
            ["modifies-state"],
            "tuple:Project.AppendConversationMessage:project-conversation:project-contributor:approval-required",
            "policy-snap-001",
            "authorized",
            ["evidence-001"],
            freshness,
            ["project:project-001"],
            ["party-001"],
            "project-contributor",
            "metadata_only",
            "metadata_only",
            9,
            "correlation-001");

    private static DecideAiActionApproval ApprovalDecision(ApprovalDecisionKind decision, AiActionApprovalRequested request)
        => new(
            request.ProjectId,
            request.ApprovalId,
            request.ProposalId,
            request.SourceMessageId,
            decision,
            request.SourceVersion,
            request.CorrelationId,
            "approval-decision-001",
            decision is ApprovalDecisionKind.Reject ? "redacted" : "metadata_only");

    private static ExecuteLowRiskAIAssistance LowRiskExecutionCommand(
        string outcome,
        string policyReasonCode = "low-risk-execute-allowed")
        => new(
            "project-001",
            "ai-proposal-001",
            "task-intent-001",
            "graph-message-001",
            "party-001",
            LowRiskAiAssistanceKind.SummarizeVisibleContext,
            "context-package-001",
            "v1",
            "metadata_only",
            "collaboration_input",
            "disabled",
            ["evidence-001"],
            ["evidence-001"],
            ["redacted"],
            8,
            "policy-snap-001",
            "correlation-001",
            "ai-execution-001",
            "transition-001",
            RiskClassification: AiActionRiskClassifier.Classify(new AiActionRiskInputTuple(
                AiActionCommandMetadataProvider.ExecuteLowRiskAssistanceCommandName,
                [],
                "read-only",
                "low-risk",
                "project-contributor",
                "policy-snap-001",
                AiActionCommandMetadataProvider.M0AllowlistVersion,
                AiActionRiskClass.LowRisk,
                "declared",
                "authorized",
                "correlation-001")),
            ExecutionRecord: new LowRiskAiAssistanceExecutionRecord(
                "ai-execution-001",
                "ai-proposal-001",
                "summarize-visible-context",
                outcome,
                outcome == "success" ? "deterministic-test" : "disabled",
                outcome == "success" ? "test-model-v1" : "disabled",
                new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero),
                ["evidence-001"],
                "context-package-001",
                "v1",
                "metadata_only",
                "policy-snap-001",
                policyReasonCode,
                "audit:ai-execution-001",
                "available",
                "correlation-001",
                "metadata_only",
                "metadata_only",
                outcome == "success" ? "none" : "review-ai-action",
                FailureCode: outcome == "success" ? null : policyReasonCode,
                Retryability: outcome == "failed" ? "retryable" : null));

    private static CaptureMailboxMessageIntake MailboxCommand()
        => new(
            IntakeId,
            new MailboxMessageSourceIdentity(
                "graph-message-001",
                "<message-001@example.test>",
                "graph-conversation-001",
                "graph-thread-001",
                "controlled-mailbox-001",
                new MailboxParticipantIdentity("sender@example.test", "Sender"),
                new DateTimeOffset(2026, 5, 30, 10, 15, 0, TimeSpan.FromHours(2)),
                new DateTimeOffset(2026, 5, 30, 10, 10, 0, TimeSpan.FromHours(2)),
                null,
                "W. Europe Standard Time",
                "graph-message-v1",
                1),
            [new MailboxRecipientIdentity("project@example.test", "Project", "to")],
            [new MailboxAttachmentReference("attachment-001", "evidence.pdf", "application/pdf", 1024)]);

    private static RequestFailedWorkflowRetry RetryCommand()
        => new(
            "01ARZ3NDEKTSV4RRFFQ69G5FAA",
            "01ARZ3NDEKTSV4RRFFQ69G5FAB",
            "message-intake",
            "graph_throttled",
            ExpectedFailedSourceVersion: 7,
            Rationale: "safe metadata retry");

    private static CaptureTaskIntent TaskIntentCommand()
        => new(
            "project-001",
            "graph-message-001",
            "party-001",
            "authorized conversation item requests action",
            ProjectConversationDetectedActionKind.RequestAction,
            [new TaskIntentSourceEvidenceOffset("message:offset:001", 10, 40, "safe-token")],
            DeterministicTaskIntentKernel.CurrentKernelVersion,
            0.82,
            new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero),
            "metadata_only",
            "collaboration_input",
            8,
            "correlation-001",
            "policy-001",
            CorrectedContextReady: true,
            DeterministicTaskIntentKernel.CurrentSchemaVersion);

    private static CommandEnvelope TaskIntentEnvelope(CaptureTaskIntent command)
        => new(
            MessageId: "01ARZ3NDEKTSV4RRFFQ69G5FAC",
            TenantId: "tenant-alpha",
            Domain: "chatbot",
            AggregateId: "01ARZ3NDEKTSV4RRFFQ69G5FAD",
            CommandType: nameof(CaptureTaskIntent),
            Payload: JsonSerializer.SerializeToUtf8Bytes(command),
            CorrelationId: "correlation-001",
            CausationId: null,
            UserId: "actor-alpha",
            Extensions: null);

    private static AiActionRiskClassificationRecord Classification(
        string intendedCommandName,
        string correlationId,
        string? policySnapshotId)
        => AiActionRiskClassifier.Classify(new AiActionRiskInputTuple(
            intendedCommandName,
            [AiActionRiskActionClass.CreatesTasks],
            "project-conversation",
            "approval-required",
            "project-contributor",
            policySnapshotId,
            "ai-action-command-allowlist.m0",
            AiActionRiskClass.ApprovalRequired,
            "declared",
            "authorized",
            correlationId));
}
