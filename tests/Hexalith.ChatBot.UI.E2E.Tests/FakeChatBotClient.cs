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

    public Task<CommandSubmissionResponse> SubmitAsync(
        IChatBotCommand command,
        string? correlationId = null,
        string? taskId = null,
        ChatBotSurfaceOrigin origin = ChatBotSurfaceOrigin.Api,
        CancellationToken cancellationToken = default)
        => Task.FromResult(new CommandSubmissionResponse
        {
            CommandId = "01ARZ3NDEKTSV4RRFFQ69G5FCM",
            CorrelationId = correlationId ?? CorrelationId,
            TaskId = taskId,
            LifecycleState = LifecycleState.Proposed,
            AcceptedAt = At,
        });

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
            Items = [],
            Page = new ProjectConversationCursorPage
            {
                NextCursor = null,
                HasMore = false,
                PageSize = pageSize,
            },
            SourceProvenance = ProjectConversationResponseSourceProvenance.M365MailboxIntake,
            RedactionState = ProjectConversationResponseRedactionState.Metadata_only,
            RetentionClass = ProjectConversationResponseRetentionClass.Collaboration_input,
            SchemaVersion = ProjectConversationResponseSchemaVersion.Chatbot_projectConversationResponse_v1,
            CorrelationId = CorrelationId,
            SafeNextAction = "none",
        });

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
