using System.Text;
using System.Text.Json;

using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Contracts.Identities;
using Hexalith.ChatBot.Contracts.Messages;
using Hexalith.ChatBot.Contracts.Queries;
using Hexalith.ChatBot.Server.Association.Intake;
using Hexalith.ChatBot.Server.Governance.AiMediation;
using Hexalith.ChatBot.Server.Governance.Outbound;
using Hexalith.ChatBot.Server.Governance.Policy;
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
    public static void HandleTenantPolicySensitiveChangeShouldCreatePendingApproval()
    {
        SubmitTenantPolicyChange command = TenantPolicyChange(TenantPolicyKnobIds.AssociationTHigh);

        DomainResult result = GovernedOperationAggregate.Handle(command, null, Envelope(command));

        result.IsSuccess.ShouldBeTrue();
        TenantPolicyChangePendingApproval pending = result.Events.ShouldHaveSingleItem().ShouldBeOfType<TenantPolicyChangePendingApproval>();
        pending.PolicyChangeId.ShouldBe(command.PolicyChangeId);
        pending.SourcePolicySnapshotId.ShouldBe(command.SourcePolicySnapshotId);
        pending.ProposedPolicySnapshotId.ShouldBe(command.ProposedPolicySnapshotId);
        pending.ChangedKnobIds.ShouldBe(command.ChangedKnobIds, ignoreOrder: false);
        pending.RequesterActorId.ShouldBe("actor-alpha");
        pending.SourceVersion.ShouldBe(command.SourceVersion + 1);
    }

    [Fact]
    public static void HandleTenantPolicyStandardChangeShouldActivateSnapshotDirectly()
    {
        SubmitTenantPolicyChange command = TenantPolicyChange(
            TenantPolicyKnobIds.MailboxRoutingRules,
            new TenantPolicyChangeSet([new(TenantPolicyKnobIds.MailboxRoutingRules, StringListValue: ["routing-rule-001"])]));

        DomainResult result = GovernedOperationAggregate.Handle(command, null, Envelope(command));

        result.IsSuccess.ShouldBeTrue();
        TenantPolicySnapshotActivated activated = result.Events.ShouldHaveSingleItem().ShouldBeOfType<TenantPolicySnapshotActivated>();
        activated.ApprovalStatus.ShouldBe(TenantPolicyApprovalStatus.NotRequired);
        activated.ActivatedPolicySnapshotId.ShouldBe(command.ProposedPolicySnapshotId);
    }

    [Fact]
    public static void HandleTenantPolicyChangeShouldRejectUnknownSchemaVersion()
    {
        SubmitTenantPolicyChange command = TenantPolicyChange(TenantPolicyKnobIds.AssociationTHigh) with
        {
            SchemaVersion = "tenant-policy-schema.custom.v1",
        };

        DomainResult result = GovernedOperationAggregate.Handle(command, null, Envelope(command));

        result.IsRejection.ShouldBeTrue();
        result.Events.ShouldHaveSingleItem().ShouldBeOfType<TenantPolicyChangeRejected>().ReasonCode
            .ShouldBe("invalid_tenant_policy_change");
    }

    [Fact]
    public static void HandleTenantPolicyApprovalShouldRequirePendingChangeAndSecondActor()
    {
        SubmitTenantPolicyChange change = TenantPolicyChange(TenantPolicyKnobIds.AssociationTHigh);
        TenantPolicyChangePendingApproval pending = GovernedOperationAggregate
            .Handle(change, null, Envelope(change))
            .Events
            .ShouldHaveSingleItem()
            .ShouldBeOfType<TenantPolicyChangePendingApproval>();
        GovernedOperationState state = new();
        state.Apply(pending);
        ApproveTenantPolicyChange approval = new(
            change.PolicyChangeId,
            change.ProposedPolicySnapshotId,
            "policy-snapshot-active",
            pending.SourceVersion,
            change.ChangedKnobIds,
            "second-admin-approval",
            change.RequesterRef,
            "admin-approver",
            TenantPolicySchemaVersions.M0,
            change.CorrelationId);

        DomainResult selfApproval = GovernedOperationAggregate.Handle(approval, state, Envelope(approval));
        DomainResult secondActorApproval = GovernedOperationAggregate.Handle(approval, state, Envelope(approval, "actor-beta"));

        selfApproval.IsRejection.ShouldBeTrue();
        TenantPolicySnapshotActivated activated = secondActorApproval.Events.ShouldHaveSingleItem().ShouldBeOfType<TenantPolicySnapshotActivated>();
        activated.ApprovalStatus.ShouldBe(TenantPolicyApprovalStatus.Approved);
        activated.ApproverRef.ShouldBe("admin-approver");
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
    public static void HandleApprovedAiActionExecutionShouldRequireApprovedAllowlistedCommand()
    {
        ExecuteApprovedAIAction command = ApprovedExecutionCommand();
        GovernedOperationState state = ApprovedExecutionState();

        DomainResult result = GovernedOperationAggregate.Handle(command, state, Envelope(command));

        result.IsSuccess.ShouldBeTrue();
        result.Events.Count.ShouldBe(2);
        ApprovedAiActionExecutionStarted started = result.Events[0].ShouldBeOfType<ApprovedAiActionExecutionStarted>();
        started.CommandName.ShouldBe(AiActionCommandMetadataProvider.AppendConversationMessageCommandName);
        started.CommandAllowlistVersion.ShouldBe(AiActionCommandMetadataProvider.M0AllowlistVersion);
        ApprovedAiActionExecutionSucceeded succeeded = result.Events[1].ShouldBeOfType<ApprovedAiActionExecutionSucceeded>();
        succeeded.Record.SafeNextAction.ShouldBe("none");
    }

    [Fact]
    public static void HandleApprovedAiActionExecutionShouldRejectNonAllowlistedCommand()
    {
        ExecuteApprovedAIAction command = ApprovedExecutionCommand() with
        {
            CommandName = "Project.SendEmail",
            ExecutionRecord = ApprovedExecutionRecord(commandName: "Project.SendEmail"),
        };

        DomainResult result = GovernedOperationAggregate.Handle(command, ApprovedExecutionState(), Envelope(command));

        result.IsRejection.ShouldBeTrue();
        ApprovedAiActionExecutionRejected rejection = result.Events.ShouldHaveSingleItem().ShouldBeOfType<ApprovedAiActionExecutionRejected>();
        rejection.ReasonCode.ShouldBe(ChatBotRefusalReasonCodes.CommandNotAllowlisted);
        rejection.ProjectId.ShouldBe("project-001");
        rejection.RequesterId.ShouldBe("party-001");
        rejection.SourceMessageId.ShouldBe("graph-message-001");
    }

    [Fact]
    public static void HandleApprovedAiActionExecutionShouldRejectNonApproveDecision()
    {
        ExecuteApprovedAIAction command = ApprovedExecutionCommand();
        GovernedOperationState state = ApprovedExecutionState(ApprovalDecisionKind.Reject);

        DomainResult result = GovernedOperationAggregate.Handle(command, state, Envelope(command));

        result.IsRejection.ShouldBeTrue();
        result.Events.ShouldHaveSingleItem().ShouldBeOfType<ApprovedAiActionExecutionRejected>().ReasonCode
            .ShouldBe(ChatBotRefusalReasonCodes.ApprovalStateInvalid);
    }

    [Fact]
    public static void HandleApprovedAiActionExecutionShouldRejectStaleApprovalEvidence()
    {
        ExecuteApprovedAIAction command = ApprovedExecutionCommand();
        GovernedOperationState state = ApprovedExecutionState(
            ApprovalDecisionKind.Approve,
            [ApprovalEvidenceFreshness.Stale]);

        DomainResult result = GovernedOperationAggregate.Handle(command, state, Envelope(command));

        result.IsRejection.ShouldBeTrue();
        result.Events.ShouldHaveSingleItem().ShouldBeOfType<ApprovedAiActionExecutionRejected>().ReasonCode
            .ShouldBe(ChatBotRefusalReasonCodes.EvidenceExpired);
    }

    [Fact]
    public static void HandleApprovedAiActionExecutionShouldTreatEquivalentReplayAsNoOpAndConflictAsRejection()
    {
        ExecuteApprovedAIAction command = ApprovedExecutionCommand();
        GovernedOperationState state = ApprovedExecutionState();
        state.Apply(new ApprovedAiActionExecutionStarted(
            command.ExecutionId,
            command.ProposalId,
            command.ApprovalId,
            command.ProjectId,
            command.TaskIntentId,
            command.SourceMessageId,
            command.SourceConversationItemId,
            command.RequesterId,
            command.CommandName,
            command.CommandAllowlistVersion,
            command.ExpectedApprovalSourceVersion,
            command.ExpectedProposalSourceVersion,
            command.PolicySnapshotId!,
            command.CorrelationId,
            DateTimeOffset.UtcNow));

        DomainResult replay = GovernedOperationAggregate.Handle(command, state, Envelope(command));
        DomainResult conflict = GovernedOperationAggregate.Handle(command with { ExpectedProposalSourceVersion = 8 }, state, Envelope(command));

        replay.IsNoOp.ShouldBeTrue();
        conflict.IsRejection.ShouldBeTrue();
        conflict.Events.ShouldHaveSingleItem().ShouldBeOfType<ApprovedAiActionExecutionRejected>().ReasonCode
            .ShouldBe(ChatBotRefusalReasonCodes.ApprovalStateInvalid);
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
    public static void HandleCreateOutboundDraftShouldCreateLocalDraftWithoutExternalOutcome()
    {
        CreateOutboundDraft command = OutboundDraftCommand();

        DomainResult result = GovernedOperationAggregate.Handle(command, state: null, Envelope(command));

        result.IsSuccess.ShouldBeTrue();
        OutboundDraftCreated created = result.Events.ShouldHaveSingleItem().ShouldBeOfType<OutboundDraftCreated>();
        created.DraftId.ShouldBe(command.DraftId);
        created.ProjectId.ShouldBe(command.ProjectId);
        created.SenderAuthorityClass.ShouldBe(SenderAuthorityClass.DraftOnly);
        created.RecipientRefs.ShouldBe(command.RecipientRefs);
        created.GovernedContent.ContentText.ShouldBe("Governed draft content.");
    }

    [Fact]
    public static void HandleCreateOutboundDraftShouldReplayEquivalentAndRejectConflictingDuplicate()
    {
        CreateOutboundDraft command = OutboundDraftCommand();
        GovernedOperationState state = new();
        state.Apply(new OutboundDraftCreated(
            command.DraftId,
            command.ProjectId,
            command.RequesterId,
            command.SourceActorId,
            command.SourceConversationId,
            command.SourceMessageId,
            command.SourceConversationItemId,
            command.RecipientRefs,
            command.ContextRefs,
            command.PolicySnapshotId,
            command.CorrelationId,
            SenderAuthorityClass.DraftOnly,
            command.GovernedContent,
            DateTimeOffset.UtcNow,
            command.RedactionState,
            command.RetentionClass));

        DomainResult replay = GovernedOperationAggregate.Handle(command, state, Envelope(command));
        DomainResult conflict = GovernedOperationAggregate.Handle(
            command with { GovernedContent = command.GovernedContent with { ContentText = "Changed content." } },
            state,
            Envelope(command));

        replay.IsNoOp.ShouldBeTrue();
        conflict.IsRejection.ShouldBeTrue();
        conflict.Events.ShouldHaveSingleItem().ShouldBeOfType<OutboundDraftCreationRejected>().ReasonCode
            .ShouldBe("idempotency_conflict_outbound_draft_creation");
    }

    [Fact]
    public static void HandleCreateOutboundDraftShouldRejectNonDraftAuthorityAndSendPosture()
    {
        CreateOutboundDraft command = OutboundDraftCommand() with
        {
            SenderAuthorityClass = SenderAuthorityClass.AuthenticatedUserSend,
            HasM365SendPosture = true,
        };

        DomainResult result = GovernedOperationAggregate.Handle(command, state: null, Envelope(command));

        result.IsRejection.ShouldBeTrue();
        result.Events.ShouldHaveSingleItem().ShouldBeOfType<OutboundDraftCreationRejected>().ReasonCode
            .ShouldBe(ChatBotDisabledActionReasons.PolicyBlocked);
    }

    [Fact]
    public static void HandleOutboundApprovalRequestShouldPreserveDraftContentAndProjectApprovalMetadata()
    {
        GovernedOperationState state = OutboundApprovalState(includeRequest: false, includeDecision: false);
        RequestOutboundSendApproval command = OutboundApprovalRequest();

        DomainResult result = GovernedOperationAggregate.Handle(command, state, Envelope(command));

        result.IsSuccess.ShouldBeTrue();
        OutboundApprovalRequested requested = result.Events.ShouldHaveSingleItem().ShouldBeOfType<OutboundApprovalRequested>();
        requested.ApprovalId.ShouldBe(command.ApprovalId);
        requested.DraftId.ShouldBe(command.DraftId);
        requested.RecipientRefs.ShouldBe(command.RecipientRefs);
        requested.SenderAuthorityClass.ShouldBe(SenderAuthorityClass.AuthenticatedUserSend);
        requested.EvidenceFreshness.ShouldBe(ApprovalEvidenceFreshness.Fresh);
        requested.ContentSnapshot.ProposedContent.ContentText.ShouldBe("Governed draft content.");
        requested.ContentSnapshot.PublicRedactionState.ShouldBe("metadata_only");

        state.Apply(requested);
        GovernedOperationAggregate.Handle(command, state, Envelope(command)).IsNoOp.ShouldBeTrue();
    }

    [Theory]
    [InlineData(ApprovalDecisionKind.Approve, "send-approved-outbound-draft")]
    [InlineData(ApprovalDecisionKind.Reject, "none")]
    [InlineData(ApprovalDecisionKind.RequestRevision, "revise-outbound-draft")]
    [InlineData(ApprovalDecisionKind.Cancel, "none")]
    public static void HandleOutboundApprovalDecisionShouldRecordAllDecisionKindsAppendOnly(
        ApprovalDecisionKind decision,
        string expectedNextAction)
    {
        GovernedOperationState state = OutboundApprovalState(includeRequest: true, includeDecision: false);
        DecideOutboundApproval command = OutboundApprovalDecision(decision);

        DomainResult result = GovernedOperationAggregate.Handle(command, state, Envelope(command));

        result.IsSuccess.ShouldBeTrue();
        OutboundApprovalDecisionRecorded recorded = result.Events.ShouldHaveSingleItem().ShouldBeOfType<OutboundApprovalDecisionRecorded>();
        recorded.DecisionKind.ShouldBe(decision);
        recorded.SafeNextAction.ShouldBe(expectedNextAction);
        recorded.ContentSnapshot.ApprovedContent.ShouldBe(decision is ApprovalDecisionKind.Approve ? command.ApprovedContent : null);

        state.Apply(recorded);
        GovernedOperationAggregate.Handle(command, state, Envelope(command)).IsNoOp.ShouldBeTrue();
        ApprovalDecisionKind conflictingDecision = decision is ApprovalDecisionKind.Cancel
            ? ApprovalDecisionKind.Approve
            : ApprovalDecisionKind.Cancel;
        GovernedOperationAggregate
            .Handle(command with { Decision = conflictingDecision, DecisionId = "decision-002" }, state, Envelope(command))
            .IsRejection
            .ShouldBeTrue();
    }

    [Fact]
    public static void HandleOutboundApprovalDecisionShouldRejectApproveWhenEvidenceExpiredButAllowReject()
    {
        GovernedOperationState state = OutboundApprovalState(
            includeRequest: true,
            includeDecision: false,
            freshness: ApprovalEvidenceFreshness.Expired);

        DomainResult approve = GovernedOperationAggregate.Handle(
            OutboundApprovalDecision(ApprovalDecisionKind.Approve),
            state,
            Envelope(OutboundApprovalDecision(ApprovalDecisionKind.Approve)));
        DomainResult reject = GovernedOperationAggregate.Handle(
            OutboundApprovalDecision(ApprovalDecisionKind.Reject),
            state,
            Envelope(OutboundApprovalDecision(ApprovalDecisionKind.Reject)));

        approve.IsRejection.ShouldBeTrue();
        approve.Events.ShouldHaveSingleItem().ShouldBeOfType<OutboundApprovalDecisionRejected>().ReasonCode.ShouldBe("evidence-expired");
        reject.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public static void HandleOutboundSendShouldRequireApprovedDecisionAndRecordSingleShotOutcome()
    {
        GovernedOperationState state = OutboundApprovalState(includeRequest: true, includeDecision: true);
        ExecuteApprovedOutboundDraft command = OutboundSendCommand();

        DomainResult result = GovernedOperationAggregate.Handle(command, state, Envelope(command));

        result.IsSuccess.ShouldBeTrue();
        result.Events.Count.ShouldBe(3);
        OutboundSendStarted started = result.Events[0].ShouldBeOfType<OutboundSendStarted>();
        started.SendKey.ShouldBe("tenant-alpha:draft-001:actor-alpha");
        started.AuthorityResult.DenialReason.ShouldBeNull();
        result.Events[1].ShouldBeOfType<OutboundSendSucceeded>().AdapterRef.ShouldBe("adapter:mailbox-outbound");
        result.Events[2].ShouldBeOfType<OutboundApprovalOutcomeRecorded>().CommandOutcomeStatus.ShouldBe("sent");

        state.Apply(started);
        DomainResult duplicate = GovernedOperationAggregate.Handle(command with { SendId = "send-002" }, state, Envelope(command));

        duplicate.IsRejection.ShouldBeTrue();
        duplicate.Events.ShouldHaveSingleItem().ShouldBeOfType<OutboundSendRejected>().ReasonCode
            .ShouldBe("idempotency_conflict_outbound_send");
    }

    [Fact]
    public static void HandleOutboundSendShouldRejectApprovalScopeMismatch()
    {
        GovernedOperationState state = OutboundApprovalState(includeRequest: true, includeDecision: true);
        ExecuteApprovedOutboundDraft command = OutboundSendCommand() with
        {
            PolicySnapshotId = "policy-snapshot-other",
        };

        DomainResult result = GovernedOperationAggregate.Handle(command, state, Envelope(command));

        result.IsRejection.ShouldBeTrue();
        result.Events.ShouldHaveSingleItem().ShouldBeOfType<OutboundSendRejected>().ReasonCode
            .ShouldBe(ChatBotRefusalReasonCodes.ApprovalStateInvalid);
    }

    [Theory]
    [InlineData(ApprovalEvidenceFreshness.Stale)]
    [InlineData(ApprovalEvidenceFreshness.Expired)]
    public static void HandleOutboundSendShouldRejectNonFreshEvidenceAtSendTime(ApprovalEvidenceFreshness freshness)
    {
        GovernedOperationState state = OutboundApprovalState(includeRequest: true, includeDecision: true);
        ExecuteApprovedOutboundDraft command = OutboundSendCommand() with { EvidenceFreshness = freshness };

        DomainResult result = GovernedOperationAggregate.Handle(command, state, Envelope(command));

        result.IsRejection.ShouldBeTrue();
        result.Events.ShouldHaveSingleItem().ShouldBeOfType<OutboundSendRejected>().ReasonCode
            .ShouldBe(ChatBotRefusalReasonCodes.ApprovalStateInvalid);
    }

    [Theory]
    [InlineData(ApprovalDecisionKind.Reject, ChatBotRefusalReasonCodes.ApprovalStateInvalid)]
    [InlineData(ApprovalDecisionKind.RequestRevision, ChatBotRefusalReasonCodes.ApprovalStateInvalid)]
    [InlineData(ApprovalDecisionKind.Cancel, ChatBotRefusalReasonCodes.ApprovalStateInvalid)]
    public static void HandleOutboundSendShouldNeverSendForNonApproveDecisions(
        ApprovalDecisionKind decision,
        string expectedReason)
    {
        GovernedOperationState state = OutboundApprovalState(includeRequest: true, includeDecision: true, decision: decision);
        ExecuteApprovedOutboundDraft command = OutboundSendCommand();

        DomainResult result = GovernedOperationAggregate.Handle(command, state, Envelope(command));

        result.IsRejection.ShouldBeTrue();
        result.Events.ShouldHaveSingleItem().ShouldBeOfType<OutboundSendRejected>().ReasonCode.ShouldBe(expectedReason);
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
    public static void HandleMailboxIntakeShouldPersistAuthenticityMetadataWithoutBlockingMalformedVerdicts()
    {
        CaptureMailboxMessageIntake command = MailboxCommand() with
        {
            Authenticity = MailboxAuthenticity(),
        };

        DomainResult result = GovernedOperationAggregate.Handle(command, state: null);

        result.IsSuccess.ShouldBeTrue();
        MailboxAuthenticityMetadata authenticity = result.Events[0]
            .ShouldBeOfType<MailboxMessageIntakeCaptured>()
            .Authenticity
            .ShouldNotBeNull();
        authenticity.AuthenticationResults.Spf.ShouldBe(MailboxAuthenticationVerdictKind.Malformed);
        authenticity.AuthenticationResults.Dkim.ShouldBe(MailboxAuthenticationVerdictKind.NotSupplied);
        authenticity.HeaderInspection.From.ShouldBe(MailboxHeaderValueState.Malformed);
        authenticity.HeaderInspection.Discrepancies.ShouldContain(MailboxHeaderDiscrepancyKind.MalformedFrom);
    }

    [Fact]
    public static void HandleMailboxIntakeShouldRejectUnboundedAuthenticityDiscrepancyShape()
    {
        CaptureMailboxMessageIntake command = MailboxCommand() with
        {
            Authenticity = MailboxAuthenticity() with
            {
                HeaderInspection = MailboxAuthenticity().HeaderInspection with
                {
                    Discrepancies = Enumerable.Repeat(MailboxHeaderDiscrepancyKind.MalformedFrom, 33).ToArray(),
                },
            },
        };

        DomainResult result = GovernedOperationAggregate.Handle(command, state: null);

        result.IsSuccess.ShouldBeFalse();
        result.Events.ShouldHaveSingleItem().ShouldBeOfType<MailboxMessageIntakeInvalidRejection>();
    }

    [Fact]
    public static void HandleMailboxIntakeShouldRejectDuplicateAuthenticityDiscrepancyCodes()
    {
        CaptureMailboxMessageIntake command = MailboxCommand() with
        {
            Authenticity = MailboxAuthenticity() with
            {
                HeaderInspection = MailboxAuthenticity().HeaderInspection with
                {
                    Discrepancies =
                    [
                        MailboxHeaderDiscrepancyKind.MalformedFrom,
                        MailboxHeaderDiscrepancyKind.MalformedFrom,
                    ],
                },
            },
        };

        DomainResult result = GovernedOperationAggregate.Handle(command, state: null);

        result.IsSuccess.ShouldBeFalse();
        result.Events.ShouldHaveSingleItem().ShouldBeOfType<MailboxMessageIntakeInvalidRejection>();
    }

    [Fact]
    public static void HandleMailboxIntakeShouldRejectInconsistentDelegatedSenderPosture()
    {
        CaptureMailboxMessageIntake missingPrincipal = MailboxCommand() with
        {
            Source = MailboxCommand().Source with
            {
                Sender = new MailboxParticipantIdentity("delegate@example.test", "Delegate"),
                DelegatedSender = new MailboxDelegatedSenderSnapshot(
                    MailboxDelegatedSenderState.Delegated,
                    new MailboxParticipantIdentity("delegate@example.test", "Delegate"),
                    PrincipalFor: null,
                    ["provider:sender", "provider:from"],
                    []),
            },
        };
        CaptureMailboxMessageIntake notDelegatedWithPrincipal = MailboxCommand() with
        {
            Source = MailboxCommand().Source with
            {
                DelegatedSender = new MailboxDelegatedSenderSnapshot(
                    MailboxDelegatedSenderState.NotDelegated,
                    Delegate: null,
                    new MailboxParticipantIdentity("principal@example.test", "Principal"),
                    ["provider:from"],
                    []),
            },
        };

        DomainResult missingPrincipalResult = GovernedOperationAggregate.Handle(missingPrincipal, state: null);
        DomainResult notDelegatedResult = GovernedOperationAggregate.Handle(notDelegatedWithPrincipal, state: null);

        missingPrincipalResult.IsSuccess.ShouldBeFalse();
        missingPrincipalResult.Events.ShouldHaveSingleItem().ShouldBeOfType<MailboxMessageIntakeInvalidRejection>();
        notDelegatedResult.IsSuccess.ShouldBeFalse();
        notDelegatedResult.Events.ShouldHaveSingleItem().ShouldBeOfType<MailboxMessageIntakeInvalidRejection>();
    }

    [Fact]
    public static void HandleMailboxIntakeShouldRejectContradictoryExternalSenderPosture()
    {
        CaptureMailboxMessageIntake internalMarkedExternal = MailboxCommand() with
        {
            Source = MailboxCommand().Source with
            {
                ExternalSender = new MailboxExternalSenderPosture(
                    ExternalSender: true,
                    MailboxPartyResolutionState.ResolvedInternal,
                    "party:internal-001",
                    ["external-sender:true", "party-resolution:resolved-internal"]),
            },
        };
        CaptureMailboxMessageIntake internalWithoutPartyRef = MailboxCommand() with
        {
            Source = MailboxCommand().Source with
            {
                ExternalSender = new MailboxExternalSenderPosture(
                    ExternalSender: false,
                    MailboxPartyResolutionState.ResolvedInternal,
                    ResolvedPartyRef: null,
                    ["external-sender:false", "party-resolution:resolved-internal"]),
            },
        };

        DomainResult internalMarkedExternalResult = GovernedOperationAggregate.Handle(internalMarkedExternal, state: null);
        DomainResult internalWithoutPartyRefResult = GovernedOperationAggregate.Handle(internalWithoutPartyRef, state: null);

        internalMarkedExternalResult.IsSuccess.ShouldBeFalse();
        internalMarkedExternalResult.Events.ShouldHaveSingleItem().ShouldBeOfType<MailboxMessageIntakeInvalidRejection>();
        internalWithoutPartyRefResult.IsSuccess.ShouldBeFalse();
        internalWithoutPartyRefResult.Events.ShouldHaveSingleItem().ShouldBeOfType<MailboxMessageIntakeInvalidRejection>();
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
    public static void HandleProposalInvalidationShouldRecordCorrectionLineageAndRejectConflictingReplay()
    {
        GovernedOperationState state = ProposalApprovalState();
        MarkAiActionProposalInvalidatedByCorrection command = ProposalInvalidationCommand();

        DomainResult result = GovernedOperationAggregate.Handle(command, state, Envelope(command));

        result.IsSuccess.ShouldBeTrue();
        AiActionProposalInvalidatedByCorrection invalidated = result.Events.ShouldHaveSingleItem().ShouldBeOfType<AiActionProposalInvalidatedByCorrection>();
        invalidated.ProposalId.ShouldBe(command.ProposalId);
        invalidated.ApprovalId.ShouldBe(command.ApprovalId);
        invalidated.AssociationId.ShouldBe(command.AssociationId);
        invalidated.CorrectionId.ShouldBe(command.CorrectionId);
        invalidated.CorrectedEvidenceState.ShouldBe("corrected");
        invalidated.EvidenceSnapshotSourceVersion.ShouldBe(11);

        state.Apply(invalidated);
        GovernedOperationAggregate.Handle(command, state, Envelope(command)).IsNoOp.ShouldBeTrue();

        DomainResult conflict = GovernedOperationAggregate.Handle(
            command with { CorrectedEvidenceState = "conflicting" },
            state,
            Envelope(command));

        conflict.IsRejection.ShouldBeTrue();
        conflict.Events.ShouldHaveSingleItem().ShouldBeOfType<AiActionProposalInvalidationRejected>().ReasonCode
            .ShouldBe(ChatBotRefusalReasonCodes.CorrectedContextInvalidated);
    }

    [Fact]
    public static void HandleProposalInvalidationShouldRejectAssociationOrSourceVersionMismatch()
    {
        GovernedOperationState state = ProposalApprovalState();
        MarkAiActionProposalInvalidatedByCorrection command = ProposalInvalidationCommand();

        DomainResult wrongAssociation = GovernedOperationAggregate.Handle(
            command with { AssociationId = "01ARZ3NDEKTSV4RRFFQ69G5FAA" },
            state,
            Envelope(command));
        DomainResult staleCorrection = GovernedOperationAggregate.Handle(
            command with { EvidenceSnapshotSourceVersion = 10 },
            state,
            Envelope(command));

        wrongAssociation.IsRejection.ShouldBeTrue();
        wrongAssociation.Events.ShouldHaveSingleItem().ShouldBeOfType<AiActionProposalInvalidationRejected>().ReasonCode
            .ShouldBe("proposal_unavailable");
        staleCorrection.IsRejection.ShouldBeTrue();
        staleCorrection.Events.ShouldHaveSingleItem().ShouldBeOfType<AiActionProposalInvalidationRejected>().ReasonCode
            .ShouldBe("proposal_unavailable");
    }

    [Fact]
    public static void HandleApproveAiActionShouldRejectInvalidatedProposal()
    {
        AiActionApprovalRequested requested = ApprovalRequest([ApprovalEvidenceFreshness.Fresh]);
        GovernedOperationState state = ProposalApprovalState();
        state.Apply(ProposalInvalidated());
        DecideAiActionApproval command = ApprovalDecision(ApprovalDecisionKind.Approve, requested);

        DomainResult result = GovernedOperationAggregate.Handle(command, state, Envelope(command));

        result.IsRejection.ShouldBeTrue();
        result.Events.ShouldHaveSingleItem().ShouldBeOfType<AiActionApprovalDecisionRejected>().ReasonCode
            .ShouldBe(ChatBotRefusalReasonCodes.CorrectedContextInvalidated);
    }

    [Fact]
    public static void HandleApprovedAiActionExecutionShouldRejectInvalidatedProposal()
    {
        ExecuteApprovedAIAction command = ApprovedExecutionCommand();
        GovernedOperationState state = ProposalApprovalState(withApprovedDecision: true);
        state.Apply(ProposalInvalidated());

        DomainResult result = GovernedOperationAggregate.Handle(command, state, Envelope(command));

        result.IsRejection.ShouldBeTrue();
        ApprovedAiActionExecutionRejected rejected = result.Events.ShouldHaveSingleItem().ShouldBeOfType<ApprovedAiActionExecutionRejected>();
        rejected.ReasonCode.ShouldBe(ChatBotRefusalReasonCodes.CorrectedContextInvalidated);
        rejected.ProjectId.ShouldBe(command.ProjectId);
        rejected.SourceMessageId.ShouldBe(command.SourceMessageId);
    }

    [Fact]
    public static void HandleLowRiskAiExecutionShouldRejectInvalidatedProposal()
    {
        ExecuteLowRiskAIAssistance command = LowRiskExecutionCommand("success");
        GovernedOperationState state = ProposalApprovalState();
        state.Apply(ProposalInvalidated());

        DomainResult result = GovernedOperationAggregate.Handle(command, state, Envelope(command));

        result.IsRejection.ShouldBeTrue();
        result.Events.ShouldHaveSingleItem().ShouldBeOfType<TaskIntentTransitionRejected>().ReasonCode
            .ShouldBe(ChatBotRefusalReasonCodes.CorrectedContextInvalidated);
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
        => Envelope(command, "actor-alpha");

    private static CommandEnvelope Envelope(IChatBotCommand command, string userId)
        => new(
            MessageId: "01ARZ3NDEKTSV4RRFFQ69G5FAL",
            TenantId: "tenant-alpha",
            Domain: "chatbot",
            AggregateId: "graph-message-001",
            CommandType: command.GetType().Name,
            Payload: JsonSerializer.SerializeToUtf8Bytes(command),
            CorrelationId: "correlation-001",
            CausationId: null,
            UserId: userId,
            Extensions: null);

    private static SubmitTenantPolicyChange TenantPolicyChange(string knobId, TenantPolicyChangeSet? changeSet = null)
        => new(
            "policy-change-001",
            "policy-snapshot-current",
            "policy-snapshot-proposed",
            4,
            [knobId],
            changeSet ?? new TenantPolicyChangeSet([new(knobId, NumberValue: 0.92)]),
            "security-owner-request",
            "admin-requester",
            TenantPolicySchemaVersions.M0,
            "01ARZ3NDEKTSV4RRFFQ69G5FAW",
            "old-fingerprint-001",
            "new-fingerprint-001");

    private static CreateOutboundDraft OutboundDraftCommand()
        => new(
            "draft-001",
            "project-001",
            "requester-001",
            "actor-001",
            "conv-001",
            "msg-001",
            "item-001",
            ["recipient:party-001"],
            ["conversation:conv-001", "source-message:msg-001", "file:file-001"],
            "policy-snap-001",
            "correlation-001",
            new OutboundDraftContent("Status update", "Governed draft content.", "text/plain"));

    private static RequestOutboundSendApproval OutboundApprovalRequest(
        ApprovalEvidenceFreshness freshness = ApprovalEvidenceFreshness.Fresh)
        => new(
            "approval-001",
            "draft-001",
            "project-001",
            "requester-001",
            "conv-001",
            "msg-001",
            "item-001",
            ["recipient:party-001"],
            ["conversation:conv-001", "source-message:msg-001", "file:file-001"],
            "policy-snap-001",
            "authorized",
            nameof(ExecuteApprovedOutboundDraft),
            "chatbot-spine.v1",
            "metadata_only",
            new OutboundApprovalContentSnapshot(
                new OutboundDraftContent("Status update", "Governed draft content.", "text/plain"),
                null,
                "governed_content",
                null),
            SenderAuthorityClass.AuthenticatedUserSend,
            freshness,
            1,
            "correlation-001");

    private static DecideOutboundApproval OutboundApprovalDecision(ApprovalDecisionKind decision)
        => new(
            "approval-001",
            "draft-001",
            "project-001",
            decision,
            "decision-001",
            2,
            "correlation-001",
            decision is ApprovalDecisionKind.Approve
                ? new OutboundDraftContent("Approved status update", "Approved governed content.", "text/plain")
                : null);

    private static ExecuteApprovedOutboundDraft OutboundSendCommand()
        => new(
            "send-001",
            "approval-001",
            "draft-001",
            "project-001",
            "requester-001",
            "actor-alpha",
            "conv-001",
            "msg-001",
            "item-001",
            ["recipient:party-001"],
            ["conversation:conv-001", "source-message:msg-001", "file:file-001"],
            "policy-snap-001",
            nameof(ExecuteApprovedOutboundDraft),
            "chatbot-spine.v1",
            SenderAuthorityClass.AuthenticatedUserSend,
            ApprovalEvidenceFreshness.Fresh,
            3,
            1,
            "correlation-001",
            AuthorityResult: OutboundAuthorityResult());

    private static GovernedOperationState OutboundApprovalState(
        bool includeRequest,
        bool includeDecision,
        ApprovalEvidenceFreshness freshness = ApprovalEvidenceFreshness.Fresh,
        ApprovalDecisionKind decision = ApprovalDecisionKind.Approve)
    {
        GovernedOperationState state = new();
        CreateOutboundDraft draft = OutboundDraftCommand();
        state.Apply(new OutboundDraftCreated(
            draft.DraftId,
            draft.ProjectId,
            draft.RequesterId,
            draft.SourceActorId,
            draft.SourceConversationId,
            draft.SourceMessageId,
            draft.SourceConversationItemId,
            draft.RecipientRefs,
            draft.ContextRefs,
            draft.PolicySnapshotId,
            draft.CorrelationId,
            SenderAuthorityClass.DraftOnly,
            draft.GovernedContent,
            DateTimeOffset.UtcNow,
            draft.RedactionState,
            draft.RetentionClass));

        if (!includeRequest)
        {
            return state;
        }

        RequestOutboundSendApproval requestCommand = OutboundApprovalRequest(freshness);
        OutboundApprovalRequested request = new(
            requestCommand.ApprovalId,
            requestCommand.DraftId,
            requestCommand.ProjectId,
            requestCommand.RequesterId,
            "human",
            requestCommand.SourceConversationId,
            requestCommand.SourceMessageId,
            requestCommand.SourceConversationItemId,
            requestCommand.RecipientRefs,
            requestCommand.ContextRefs,
            requestCommand.PolicySnapshotId,
            requestCommand.PolicySnapshotVisibility,
            requestCommand.CommandName,
            requestCommand.CommandAllowlistVersion,
            requestCommand.ContentSnapshot,
            requestCommand.SenderAuthorityClass,
            requestCommand.EvidenceFreshness,
            requestCommand.ExpectedPostStateRedactionState,
            requestCommand.ExpectedDraftSourceVersion,
            2,
            DateTimeOffset.UtcNow,
            requestCommand.CorrelationId,
            requestCommand.RedactionState,
            requestCommand.RetentionClass);
        state.Apply(request);

        if (includeDecision)
        {
            DecideOutboundApproval decisionCommand = OutboundApprovalDecision(decision);
            state.Apply(new OutboundApprovalDecisionRecorded(
                decisionCommand.ApprovalId,
                decisionCommand.DraftId,
                decisionCommand.ProjectId,
                decision,
                "approver-001",
                "human",
                DateTimeOffset.UtcNow,
                request.SourceVersion,
                "authorized",
                null,
                "metadata_only",
                "audit:decision-001",
                "available",
                request.PolicySnapshotId,
                decision is ApprovalDecisionKind.Approve ? "send-approved-outbound-draft" : "none",
                decision is ApprovalDecisionKind.Approve
                    ? request.ContentSnapshot with
                    {
                        ApprovedContent = decisionCommand.ApprovedContent,
                        ApprovedContentRedactionState = "governed_content",
                    }
                    : request.ContentSnapshot,
                3,
                decisionCommand.CorrelationId));
        }

        return state;
    }

    private static SenderAuthorityClassificationResult OutboundAuthorityResult()
        => new(
            SenderAuthorityClass.AuthenticatedUserSend,
            "requester:requester-001",
            "mailbox:mailbox-001",
            null,
            null,
            "approval:approval-001",
            "policy-snapshot:policy-snap-001",
            "fresh",
            [
                "sender-authority:authenticated-user-send",
                "requester:requester-001",
                "mailbox:mailbox-001",
                "approval:approval-001",
                "policy-snapshot:policy-snap-001",
            ],
            null);

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

    private static ExecuteApprovedAIAction ApprovedExecutionCommand()
        => new(
            "project-001",
            "ai-proposal-001",
            "approval:ai-proposal-001",
            "task-intent-001",
            "graph-message-001",
            "party-001",
            AiActionCommandMetadataProvider.AppendConversationMessageCommandName,
            AiActionCommandMetadataProvider.M0AllowlistVersion,
            10,
            9,
            "correlation-001",
            "ai-approved-execution-001",
            "approved-execution-transition-001",
            ["evidence-001"],
            ["project:project-001"],
            ["party-001"],
            "graph-message-001",
            "policy-snap-001",
            ExecutionRecord: ApprovedExecutionRecord());

    private static ApprovedAiActionExecutionRecord ApprovedExecutionRecord(string commandName = AiActionCommandMetadataProvider.AppendConversationMessageCommandName)
        => new(
            "ai-approved-execution-001",
            "ai-proposal-001",
            "approval:ai-proposal-001",
            commandName,
            AiActionCommandMetadataProvider.M0AllowlistVersion,
            "success",
            new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero),
            "audit:ai-approved-execution-001",
            "available",
            "correlation-001",
            "metadata_only",
            "none");

    private static GovernedOperationState ProposalApprovalState(bool withApprovedDecision = false)
    {
        GovernedOperationState state = new();
        TaskIntentRecord taskIntent = new(
            "task-intent-001",
            "tenant-alpha",
            "project-001",
            "graph-message-001",
            "party-001",
            "authorized conversation item requests action",
            ProjectConversationDetectedActionKind.RequestAction,
            [new TaskIntentSourceEvidenceOffset("evidence-001", 0, 10, "safe-token")],
            DeterministicTaskIntentKernel.CurrentKernelVersion,
            0.82,
            new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero),
            TaskIntentState.Converted,
            DeterministicTaskIntentKernel.CurrentSchemaVersion,
            TaskIntentReasonCodes.Converted,
            "authorized-project-conversation",
            "metadata_only",
            "collaboration_input",
            8,
            "correlation-001",
            "policy-snap-001",
            "correction-lineage-001",
            ConvertedProposalId: "ai-proposal-001",
            ReviewerActorId: "actor-alpha",
            DecidedAtUtc: new DateTimeOffset(2026, 6, 1, 0, 1, 0, TimeSpan.Zero),
            AuditOperationId: "audit:transition-001",
            TransitionId: "transition-001");
        AiActionProposalRecord proposal = new(
            "ai-proposal-001",
            taskIntent.TaskIntentId,
            taskIntent.SourceMessageId,
            "graph-message-001",
            taskIntent.RequesterPartyId,
            "actor-alpha",
            ["evidence-001"],
            AiActionCommandMetadataProvider.AppendConversationMessageCommandName,
            "append-conversation-message",
            ["project:project-001"],
            ["party-001"],
            "policy-snap-001",
            9,
            "correlation-001",
            "metadata_only",
            "collaboration_input",
            "chatbot.ai-action-proposal.v1",
            "review-ai-action",
            new Dictionary<string, string>
            {
                ["associationId"] = "01ARZ3NDEKTSV4RRFFQ69G5FAV",
                ["evidenceSnapshotSourceVersion"] = "11",
                ["contextPackageId"] = "context-package-001",
                ["contextPackageVersion"] = "v1",
            },
            AiActionRiskClass.ApprovalRequired,
            [AiActionRiskActionClass.ModifiesState],
            "chatbot.ai-action-risk-classifier.m0.v1",
            null,
            "approval-required",
            AiActionCommandMetadataProvider.M0AllowlistVersion,
            AiActionRiskClass.ApprovalRequired,
            "project-contributor",
            new DateTimeOffset(2026, 6, 1, 0, 1, 0, TimeSpan.Zero),
            null,
            taskIntent.CorrectionLineageId,
            "01ARZ3NDEKTSV4RRFFQ69G5FAV",
            11,
            "context-package-001",
            "v1");
        state.Apply(new TaskIntentConvertedToAiActionProposal(
            taskIntent,
            proposal,
            "actor-alpha",
            new DateTimeOffset(2026, 6, 1, 0, 1, 0, TimeSpan.Zero),
            "audit:transition-001"));

        AiActionApprovalRequested request = ApprovalRequest([ApprovalEvidenceFreshness.Fresh]);
        state.Apply(request);
        if (withApprovedDecision)
        {
            state.Apply(new AiActionApprovalDecisionRecorded(
                request.ApprovalId,
                request.ProjectId,
                request.ProposalId,
                request.SourceMessageId,
                ApprovalDecisionKind.Approve,
                "approver-001",
                "human",
                new DateTimeOffset(2026, 6, 1, 0, 2, 0, TimeSpan.Zero),
                request.SourceVersion,
                "authorized",
                null,
                "metadata_only",
                "audit:approval-decision-001",
                "available",
                request.PolicySnapshotId,
                "execute-approved-ai-action",
                request.SourceVersion + 1,
                request.CorrelationId));
        }

        return state;
    }

    private static MarkAiActionProposalInvalidatedByCorrection ProposalInvalidationCommand()
        => new(
            "project-001",
            "ai-proposal-001",
            "approval:ai-proposal-001",
            "task-intent-001",
            "graph-message-001",
            "graph-message-001",
            "party-001",
            "01ARZ3NDEKTSV4RRFFQ69G5FAV",
            "01ARZ3NDEKTSV4RRFFQ69G5FAV:correction:11",
            "corrected",
            11,
            "correlation-001");

    private static AiActionProposalInvalidatedByCorrection ProposalInvalidated()
        => new(
            "ai-proposal-001",
            "approval:ai-proposal-001",
            "task-intent-001",
            "graph-message-001",
            "graph-message-001",
            "party-001",
            "project-001",
            "01ARZ3NDEKTSV4RRFFQ69G5FAV",
            "01ARZ3NDEKTSV4RRFFQ69G5FAV:correction:11",
            "corrected",
            11,
            "correlation-001",
            "metadata_only",
            "collaboration_input");

    private static GovernedOperationState ApprovedExecutionState(
        ApprovalDecisionKind decision = ApprovalDecisionKind.Approve,
        IReadOnlyList<ApprovalEvidenceFreshness>? freshness = null)
    {
        GovernedOperationState state = new();
        AiActionApprovalRequested request = ApprovalRequest(freshness ?? [ApprovalEvidenceFreshness.Fresh]);
        state.Apply(request);
        state.Apply(new AiActionApprovalDecisionRecorded(
            request.ApprovalId,
            request.ProjectId,
            request.ProposalId,
            request.SourceMessageId,
            decision,
            "approver-001",
            "human",
            new DateTimeOffset(2026, 6, 1, 0, 1, 0, TimeSpan.Zero),
            request.SourceVersion,
            "authorized",
            null,
            "metadata_only",
            "audit:approval-decision-001",
            "available",
            request.PolicySnapshotId,
            decision is ApprovalDecisionKind.Approve ? "execute-approved-ai-action" : "none",
            request.SourceVersion + 1,
            request.CorrelationId));
        return state;
    }

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

    private static MailboxAuthenticityMetadata MailboxAuthenticity()
        => new(
            new MailboxAuthenticationResultSnapshot(
                MailboxAuthenticationVerdictKind.Malformed,
                MailboxAuthenticationVerdictKind.NotSupplied,
                MailboxAuthenticationVerdictKind.NotSupplied,
                MailboxAuthenticationVerdictKind.NotSupplied,
                null,
                [new MailboxSelectedHeaderSnapshot("Authentication-Results", 0, MailboxHeaderValueState.Malformed)]),
            new MailboxHeaderInspectionSnapshot(
                [],
                [new MailboxSelectedHeaderSnapshot("Authentication-Results", 0, MailboxHeaderValueState.Malformed)],
                MailboxHeaderValueState.Malformed,
                MailboxHeaderValueState.NotSupplied,
                MailboxHeaderValueState.NotSupplied,
                MailboxHeaderValueState.NotSupplied,
                [MailboxHeaderDiscrepancyKind.MalformedFrom]));

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
