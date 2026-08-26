using Hexalith.ChatBot.Client;
using Hexalith.ChatBot.Client.Generated;

using IChatBotCommand = Hexalith.ChatBot.Contracts.Commands.IChatBotCommand;
using ChatBotSurfaceOrigin = Hexalith.ChatBot.Contracts.Enums.ChatBotSurfaceOrigin;

namespace Hexalith.ChatBot.UI.E2E.Tests;

/// <summary>
/// Story 13.9 UI-boundary seam: a deterministic, never-throwing <see cref="IChatBotClient"/> fake that returns
/// metadata-only safe-token DTOs for the four data surfaces (governed operations, project conversation,
/// association review, compliance audit) so the real <c>Hexalith.ChatBot.UI</c> app renders all six routable
/// surfaces without reaching a live backend (Server, gateway, EventStore, Dapr). The shapes mirror the valid
/// fixtures already proven by the <c>Hexalith.ChatBot.UI.Tests</c> service tests; operational dashboards reach
/// no client method (its service assembles a fail-safe Unknown overview at the UI boundary).
/// </summary>
internal sealed class FakeChatBotClient : IChatBotClient
{
    private static readonly DateTimeOffset At = new(2026, 5, 31, 9, 0, 0, TimeSpan.Zero);
    private const string CorrelationId = "01ARZ3NDEKTSV4RRFFQ69G5FAW";
    private bool _messageSubmitted;
    private bool _proposalSubmitted;
    private bool _responseStopped;

    public Task<CommandSubmissionResponse> SubmitAsync(
        IChatBotCommand command,
        string? correlationId = null,
        string? taskId = null,
        ChatBotSurfaceOrigin origin = ChatBotSurfaceOrigin.Api,
        CancellationToken cancellationToken = default)
    {
        switch (command)
        {
            case Hexalith.ChatBot.Contracts.Commands.RecordProjectConversationMessage:
                _messageSubmitted = true;
                break;
            case Hexalith.ChatBot.Contracts.Commands.ProposeAIAction:
                _proposalSubmitted = true;
                break;
            case Hexalith.ChatBot.Contracts.Commands.CancelAiResponseGeneration:
                _responseStopped = true;
                break;
        }

        return Task.FromResult(new CommandSubmissionResponse
        {
            CommandId = "01ARZ3NDEKTSV4RRFFQ69G5FCM",
            CorrelationId = correlationId ?? CorrelationId,
            TaskId = taskId,
            LifecycleState = LifecycleState.Proposed,
            AcceptedAt = At,
        });
    }

    public Task<OperationStatus> GetOperationStatusAsync(
        string operationId,
        string? correlationId = null,
        string? taskId = null,
        CancellationToken cancellationToken = default)
        => Task.FromResult(new OperationStatus
        {
            OperationId = operationId,
            CommandId = "01ARZ3NDEKTSV4RRFFQ69G5FCM",
            CorrelationId = CorrelationId,
            LifecycleState = LifecycleState.Proposed,
            RetryCount = 0,
            CompletionStatus = OperationCompletionStatus.AcceptedProjectionPending,
            AuditStatus = OperationAuditStatus.Committed,
            PartialOutputs = new OperationStatusPartialOutputs
            {
                AcceptedAt = At,
                CompletionStatus = OperationCompletionStatus.AcceptedProjectionPending,
                AuditStatus = OperationAuditStatus.Committed,
            },
            SafeNextActions = [ChatBotMessageNextAction.None],
            OperationClass = "message-intake",
            MaxAttempts = 5,
            DuplicateSafetyNote = "duplicate-safe",
            OwnerRole = "mailbox-operator",
            AcceptedAt = At,
            LastUpdatedAt = At,
        });

    public Task<OperationAuditHistory> GetOperationAuditHistoryAsync(
        string operationId,
        string? correlationId = null,
        string? taskId = null,
        CancellationToken cancellationToken = default)
        => Task.FromResult(new OperationAuditHistory
        {
            OperationId = operationId,
            AuditStatus = OperationAuditStatus.Committed,
            Entries =
            [
                new AuditHistoryEntry
                {
                    Phase = AuditHistoryPhase.PostCommit,
                    Decision = "allow",
                    ReasonCode = "eventstore_dispatch_accepted",
                    Outcome = "proposed",
                    StateTransition = "Received->Proposed",
                    RedactionDecision = AuditHistoryEntryRedactionDecision.Metadata_only,
                    SurfaceOrigin = SurfaceOrigin.Ui,
                    ResourceId = "01ARZ3NDEKTSV4RRFFQ69G5FAZ",
                    CorrelationId = CorrelationId,
                    RecordedAt = At,
                },
            ],
        });

    public Task<AssociationRoutingStatus> GetAssociationRoutingStatusAsync(
        string associationId,
        string? correlationId = null,
        string? taskId = null,
        CancellationToken cancellationToken = default)
    {
        AssociationEvidenceReference evidence = new()
        {
            EvidenceReference = "evidence-ref-1",
            EvidenceFingerprint = "fingerprint-1",
            EvidenceKind = "subject-signal",
        };

        return Task.FromResult(new AssociationRoutingStatus
        {
            AssociationId = associationId,
            IntakeId = "01ARZ3NDEKTSV4RRFFQ69G5FBA",
            SourceMailboxId = "mailbox-metadata",
            SourceConversationId = "conversation-metadata",
            LifecycleState = LifecycleState.NeedsReview,
            Outcome = AssociationScoringOutcome.CandidatesGenerated,
            ThresholdBand = AssociationThresholdBand.Ambiguous,
            ConfidenceScore = 0.64,
            ReasonCodes = [AssociationReasonCode.MultipleAuthorizedCandidates],
            Candidates =
            [
                new AssociationCandidate
                {
                    ProjectId = "01ARZ3NDEKTSV4RRFFQ69G5FBB",
                    DisplayName = "Authorized candidate",
                    ConfidenceScore = 0.64,
                    Rank = 1,
                    ReasonCodes = [AssociationReasonCode.ExplicitProjectIdentifierMatched],
                    EvidenceRefs = [evidence],
                    ConfidenceInputs = [],
                    RequiredEvidenceComplete = true,
                },
            ],
            Exclusions = [],
            ThresholdPolicyVersion = "association-thresholds.m0.default.v1",
            EvidenceRefs = [evidence],
            KernelVersion = "association-deterministic.kernel.m0.v1",
            DetectedAt = At,
            SourceProvenance = AssociationRoutingStatusSourceProvenance.M365MailboxIntake,
            RedactionState = AssociationRoutingStatusRedactionState.Metadata_only,
            RetentionClass = AssociationRoutingStatusRetentionClass.Collaboration_input,
            SchemaVersion = "chatbot.association-routing-status.v1",
            SourceVersion = 1,
            CorrelationId = CorrelationId,
            DisabledActionReasonCodes = [],
            NextActionReasonCodes = [ChatBotMessageCode.Association_ambiguous_routed],
        });
    }

    public Task<ProjectConversationResponse> GetProjectConversationAsync(
        string projectId,
        string? cursor = null,
        int pageSize = 25,
        string? correlationId = null,
        string? taskId = null,
        CancellationToken cancellationToken = default)
        => Task.FromResult(new ProjectConversationResponse
        {
            ProjectId = projectId,
            ProjectDisplayName = "Authorized Project",
            Status = ProjectConversationReadStatus.Current,
            ConversationState = LifecycleState.Associated,

            // One pending-approval item so the live route actually mounts ChatBotApprovalConversationItem and
            // ChatBotAiActionPreviewSections. With an empty list the real-render suite asserted "no <dl> in
            // main" against a surface that was never instantiated, so every approval assertion passed vacuously.
            Items = cursor is null ? CurrentItems() : [OlderApprovalItem],
            Page = new ProjectConversationCursorPage
            {
                NextCursor = cursor is null ? "opaque-history-cursor" : null,
                HasMore = cursor is null,
                PageSize = pageSize,
            },
            SourceProvenance = ProjectConversationResponseSourceProvenance.M365MailboxIntake,
            RedactionState = ProjectConversationResponseRedactionState.Metadata_only,
            RetentionClass = ProjectConversationResponseRetentionClass.Collaboration_input,
            SchemaVersion = ProjectConversationResponseSchemaVersion.Chatbot_projectConversationResponse_v1,
            CorrelationId = CorrelationId,
            SafeNextAction = "none",
        });

    private ICollection<ProjectConversationItem> CurrentItems()
    {
        List<ProjectConversationItem> items = [PendingApprovalItem, ActiveResponseItem(_responseStopped)];
        if (_messageSubmitted)
        {
            items.Insert(0, SubmittedItem("item:message-submitted", "message:submitted", 4));
        }

        if (_proposalSubmitted)
        {
            items.Insert(0, SubmittedItem("item:proposal-submitted", "proposal:submitted", 5));
        }

        return items;
    }

    private static ProjectConversationItem SubmittedItem(string itemId, string proposalId, long sourceVersion)
    {
        ProjectConversationItem item = PendingApprovalItem;
        item.ItemId = itemId;
        item.ApprovalId = $"approval:{proposalId}";
        item.ApprovalProposalId = proposalId;
        item.ApprovalSourceMessageId = $"message:{proposalId}";
        item.SourceVersion = sourceVersion;
        item.OccurredAt = At.AddMinutes(sourceVersion);
        return item;
    }

    private static ProjectConversationItem ActiveResponseItem(bool stopped)
    {
        ProjectConversationItem item = PendingApprovalItem;
        item.ItemId = "item:active-response";
        item.Kind = ProjectConversationItemKind.AiOutcome;
        item.ActorKind = ProjectConversationActorKind.AiActor;
        item.ActorLabel = "AI actor";
        item.AssociationId = "proposal:active-response";
        item.SourceConversationId = "conversation:active-response";
        item.SourceVersion = stopped ? 7 : 6;
        item.AiOutcomeKind = AiOutcomeKind.ExecutionStarted;
        item.AiOutcomeStatus = stopped ? AiOutcomeStatus.Failed : AiOutcomeStatus.Executing;
        item.AiProposalId = "response:active-response";
        item.AiRiskClass = AiActionRiskClass.LowRisk;
        item.AiRiskActionClasses = ["read-only"];
        item.AiPolicySnapshotId = "policy:story-13-2-stateful";
        item.AiPolicySnapshotVisibility = "authorized";
        item.AiAuthorizedContextReferences = ["evidence:visible-context"];
        item.AiSafeNextAction = stopped ? "none" : "wait-for-projection";
        item.AiResponseProgress = new AiResponseProgress
        {
            ProjectId = "project-alpha",
            ConversationId = "conversation:active-response",
            ResponseId = "response:active-response",
            GenerationId = "generation:active-response",
            CorrelationId = CorrelationId,
            SourceVersion = item.SourceVersion,
            Sequence = stopped ? 2 : 1,
            State = stopped ? AiResponseProgressState.Stopped : AiResponseProgressState.Rendering,
            TerminalReason = stopped ? AiResponseTerminalReason.UserStopped : AiResponseTerminalReason.None,
            SafeNextAction = stopped ? "none" : "wait-for-projection",
            RedactionState = AiResponseProgressRedactionState.Metadata_only,
            VisibilityState = AiResponseProgressVisibilityState.Metadata_only,
            IsTerminal = stopped,
        };
        return item;
    }

    private static ProjectConversationItem PendingApprovalItem => new()
    {
        ItemId = "item:approval-001",
        Kind = ProjectConversationItemKind.ApprovalEvent,
        ActorKind = ProjectConversationActorKind.SystemDecision,
        ActorLabel = "ai-mediation-worker",
        OccurredAt = new DateTimeOffset(2026, 6, 1, 9, 0, 0, TimeSpan.Zero),
        LifecycleState = LifecycleState.Associated,
        ThresholdBand = AssociationThresholdBand.Auto,
        ConfidenceScore = 0.91,
        AssociationId = "association:approval-001",
        SourceMailboxId = "mailbox:shared-1",
        SourceConversationId = "conversation:approval-001",
        ApprovalId = "approval:approval-001",
        ApprovalProposalId = "proposal:approval-001",
        ApprovalSourceMessageId = "message:approval-001",
        ApprovalStatus = Hexalith.ChatBot.Client.Generated.ApprovalStatus.Pending,
        ApprovalEventKind = Hexalith.ChatBot.Client.Generated.ApprovalEventKind.Request,
        ApprovalRequesterId = "requester:requester-1",
        ApprovalCommandName = "appendconversationmessage",
        ApprovalRiskClass = RiskClass.High,
        ApprovalRiskActionClasses = ["project-mutating"],
        ApprovalEvidenceReferences = ["evidence:approval-001"],
        ApprovalEvidenceFreshnessStates = [ApprovalEvidenceFreshness.Fresh],
        ApprovalAuditStatus = "committed",
        ApprovalPolicySnapshotVisibility = ProjectConversationItemApprovalPolicySnapshotVisibility.Authorized,
        ApprovalPolicySnapshotId = "policy:snapshot-1",
        SourceVersion = 1,
        CorrelationId = CorrelationId,
        SafeNextAction = "decide-approval",
    };

    private static ProjectConversationItem OlderApprovalItem
    {
        get
        {
            ProjectConversationItem item = PendingApprovalItem;
            item.ItemId = "item:approval-older-001";
            item.ApprovalId = "approval:older-001";
            item.ApprovalProposalId = "proposal:older-001";
            item.ApprovalSourceMessageId = "message:older-001";
            item.AssociationId = "association:older-001";
            item.SourceConversationId = "conversation:older-001";
            item.OccurredAt = new DateTimeOffset(2026, 5, 31, 8, 0, 0, TimeSpan.Zero);
            item.SourceVersion = 0;
            return item;
        }
    }

    public Task<ComplianceAuditSearchView> SearchComplianceAuditRecordsAsync(
        ComplianceAuditQuery query,
        string? correlationId = null,
        CancellationToken cancellationToken = default)
        => Task.FromResult(new ComplianceAuditSearchView(
            "audit-query-s9",
            [
                new ComplianceAuditRowView(
                    "audit-record-001",
                    "admin-alpha",
                    "human",
                    "SubmitRetentionConfigurationChange",
                    "audit-record-001",
                    "allow",
                    "pre_commit_gate",
                    CorrelationId,
                    new DateTimeOffset(2026, 6, 2, 4, 0, 0, TimeSpan.Zero),
                    "policy-snapshot-admin-v1",
                    "restricted",
                    "not-requested",
                    "request-access"),
            ],
            "sha256:1",
            new DateTimeOffset(2026, 6, 2, 5, 0, 0, TimeSpan.Zero),
            CorrelationId));

    public Task<ComplianceAuditDetailView> GetComplianceAuditDetailAsync(
        string auditRecordRef,
        string? correlationId = null,
        CancellationToken cancellationToken = default)
        => Task.FromResult(ComplianceAuditDetailView.Restricted);
}
