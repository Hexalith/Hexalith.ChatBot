using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Contracts.Messages;
using Hexalith.ChatBot.Contracts.Queries;

namespace Hexalith.ChatBot.Server.Projections;

internal sealed record ProjectConversationItemView(
    string TenantId,
    string ProjectId,
    string? ProjectDisplayName,
    string ItemId,
    string IntakeId,
    ProjectConversationItemKind Kind,
    ProjectConversationActorKind ActorKind,
    string ActorLabel,
    DateTimeOffset OccurredAt,
    LifecycleState LifecycleState,
    AssociationThresholdBand ThresholdBand,
    double ConfidenceScore,
    string AssociationId,
    string SourceMailboxId,
    string? SourceProviderMessageId,
    string? InternetMessageId,
    string SourceConversationId,
    string? SourceThreadId,
    DateTimeOffset? SourceReceivedAtUtc,
    DateTimeOffset? SourceSentAtUtc,
    DateTimeOffset? SourceCreatedAtUtc,
    string? SourceTimezone,
    string? SourceProvenanceDisplayToken,
    string SourceProvenance,
    string RedactionState,
    string RetentionClass,
    string SchemaVersion,
    long SourceVersion,
    string CorrelationId,
    string? DecisionLabel = null,
    string? SafeNextAction = null,
    string? ParticipantResolutionId = null,
    string? SourceParticipantId = null,
    string? PartyId = null,
    ParticipantResolutionStatus? ParticipantStatus = null,
    ParticipantResolutionBlockedReason? ParticipantBlockedReason = null,
    ProjectConversationParticipantDisplayKind? ParticipantDisplayKind = null,
    string? ParticipantEvidenceReference = null,
    string? ParticipantEvidenceFingerprint = null,
    IReadOnlyList<ParticipantReviewAction>? ParticipantAllowedReviewActions = null,
    string? ParticipantRedactionState = null,
    string? SourceProviderAttachmentId = null,
    string? AttachmentDisplayName = null,
    string? AttachmentContentType = null,
    long? AttachmentSizeInBytes = null,
    ProjectConversationAttachmentStatus? AttachmentCaptureStatus = null,
    ProjectConversationAttachmentStatus? AttachmentStorageStatus = null,
    ProjectConversationAttachmentStatus? AttachmentScanStatus = null,
    string? AttachmentFolderId = null,
    string? AttachmentFileId = null,
    string? AttachmentDuplicateState = null,
    string? AttachmentRetryState = null,
    string? AttachmentAiContextEligibility = null,
    IReadOnlyList<string>? AttachmentAllowedActions = null,
    string? AttachmentRedactionState = null,
    AssociationDecisionKind? DecisionKind = null,
    string? DecisionActorId = null,
    string? DecisionActorType = null,
    DateTimeOffset? DecidedAtUtc = null,
    string? DecisionNoteRedactionState = null,
    string? SurfaceOrigin = null,
    string? PolicySnapshotVersion = null,
    IReadOnlyList<string>? EvidenceReferenceSummary = null,
    AssociationCorrectionKind? CorrectionKind = null,
    string? PriorProjectId = null,
    string? CorrectedProjectId = null,
    string? PredecessorAssociationId = null,
    string? SupersedesAssociationId = null,
    string? SupersededByAssociationId = null,
    string? CorrectionRationaleRedactionState = null,
    string? CorrectionActorId = null,
    string? CorrectionActorType = null,
    DateTimeOffset? CorrectedAtUtc = null,
    string? DownstreamImpactStatus = null,
    string? CorrectionId = null,
    string? WorkflowInstanceId = null,
    IReadOnlyList<string>? RequiredStoreKeys = null,
    IReadOnlyList<string>? CompletedStoreKeys = null,
    IReadOnlyList<string>? FailedStoreKeys = null,
    int? PropagationProgressNumerator = null,
    int? PropagationProgressDenominator = null,
    DateTimeOffset? PropagationStartedAtUtc = null,
    DateTimeOffset? PropagationCompletedAtUtc = null,
    DateTimeOffset? PropagationEstimatedCompletionAtUtc = null,
    string? PropagationStatus = null,
    bool? IsCorrectedContextStale = null,
    string? ResponsibleOwnerRole = null,
    string? ApprovalId = null,
    ApprovalEventKind? ApprovalEventKind = null,
    ApprovalStatus? ApprovalStatus = null,
    ApprovalDecisionKind? ApprovalDecisionKind = null,
    string? ApprovalRequesterId = null,
    string? ApprovalRequesterActorType = null,
    DateTimeOffset? ApprovalRequestedAtUtc = null,
    string? ApprovalDecisionActorId = null,
    string? ApprovalDecisionActorType = null,
    DateTimeOffset? ApprovalDecidedAtUtc = null,
    DateTimeOffset? ApprovalOutcomeAtUtc = null,
    string? ApprovalProposalId = null,
    string? ApprovalSourceMessageId = null,
    string? ApprovalSourceConversationItemId = null,
    string? ApprovalCommandName = null,
    string? ApprovalCommandAllowlistVersion = null,
    RiskClass? ApprovalRiskClass = null,
    IReadOnlyList<string>? ApprovalRiskActionClasses = null,
    AiActionRiskClass? ApprovalAiRiskClass = null,
    IReadOnlyList<string>? ApprovalAiRiskActionClasses = null,
    string? ApprovalAiRiskInputTuple = null,
    string? ApprovalPolicySnapshotId = null,
    string? ApprovalPolicySnapshotVisibility = null,
    IReadOnlyList<string>? ApprovalEvidenceReferences = null,
    IReadOnlyList<ApprovalEvidenceFreshness>? ApprovalEvidenceFreshnessStates = null,
    IReadOnlyList<string>? ApprovalAffectedResourceReferences = null,
    IReadOnlyList<string>? ApprovalRecipientReferences = null,
    string? ApprovalSenderAuthorityClass = null,
    string? ApprovalExpectedPostStateRedactionState = null,
    string? ApprovalActionSummaryRedactionState = null,
    string? ApprovalDecisionRationaleRedactionState = null,
    string? ApprovalAuthorityResult = null,
    string? ApprovalDisabledReason = null,
    string? ApprovalAuditOperationId = null,
    string? ApprovalAuditStatus = null,
    string? ApprovalCommandOutcomeStatus = null,
    string? ApprovalProjectedOutcomeItemId = null,
    string? ApprovalFailureCode = null,
    string? ApprovalRetryability = null,
    string? SupersedesApprovalId = null,
    string? SupersededByApprovalId = null,
    FailureStateKind? FailureStateKind = null,
    FailureStatus? FailureStatus = null,
    string? MessageCatalogCode = null,
    string? MessageCatalogVersion = null,
    string? MessageDetailVisibility = null,
    string? FailureCategory = null,
    string? FailureScope = null,
    string? FailureReasonCode = null,
    string? BlockedReason = null,
    bool? Retryable = null,
    int? RetryCount = null,
    int? MaxRetryCount = null,
    DateTimeOffset? NextRetryAtUtc = null,
    DateTimeOffset? LastRetryAtUtc = null,
    string? RetryOperationId = null,
    string? SupersedesWorkflowInstanceId = null,
    string? SupersededByWorkflowInstanceId = null,
    string? TaskId = null,
    string? OperationId = null,
    string? AuditOperationId = null,
    string? AuditStatus = null,
    string? ClientAction = null,
    string? DuplicateSafetyState = null,
    string? DuplicateSuppressionId = null,
    string? DependencyName = null,
    DateTimeOffset? DegradedUntilUtc = null,
    string? EscalationTargetRole = null,
    string? ReprocessCreatedWorkflowInstanceId = null,
    AiOutcomeKind? AiOutcomeKind = null,
    AiOutcomeStatus? AiOutcomeStatus = null,
    string? AiActorId = null,
    string? AiActorType = null,
    string? AiProposalId = null,
    string? AiRequestId = null,
    string? AiRequesterId = null,
    string? AiSourceConversationItemId = null,
    string? AiSourceMessageId = null,
    string? AiOperationId = null,
    string? AiCorrelationId = null,
    AiActionRiskClass? AiRiskClass = null,
    IReadOnlyList<string>? AiRiskActionClasses = null,
    string? AiPolicyReasonCode = null,
    string? AiClassifierVersion = null,
    string? AiRiskInputTuple = null,
    string? AiRequesterAuthorityClass = null,
    string? AiIndeterminateReason = null,
    string? AiPolicySnapshotId = null,
    string? AiPolicySnapshotVisibility = null,
    string? AiContextPackageId = null,
    string? AiContextPackageVersion = null,
    string? AiContextRedactionState = null,
    IReadOnlyList<string>? AiAuthorizedContextReferences = null,
    IReadOnlyList<string>? AiExcludedContextReasons = null,
    string? AiGeneratedSummaryRedactionState = null,
    string? AiGeneratedContentVisibility = null,
    string? AiCommandName = null,
    string? AiCommandAllowlistVersion = null,
    string? AiApprovalId = null,
    string? AiApprovalStatus = null,
    string? AiExecutionStatus = null,
    string? AiExecutionOutcomeCode = null,
    string? AiAuditOperationId = null,
    string? AiAuditStatus = null,
    string? AiFailureCode = null,
    string? AiRetryability = null,
    string? AiSafeNextAction = null,
    string? SupersedesAiOutcomeId = null,
    string? SupersededByAiOutcomeId = null,
    TaskIntentRecord? CapturedTaskIntent = null)
{
    public const string CurrentSchemaVersion = "chatbot.project-conversation-item.v1";
    public const string ClassificationKernelVersion = "chatbot.project-conversation-classification.kernel.v1";

    public static string KeyFor(string tenantId, string projectId, string itemId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(projectId);
        ArgumentException.ThrowIfNullOrWhiteSpace(itemId);
        return $"{tenantId}:project-conversation:{projectId}:{itemId}";
    }

    public static bool ShouldReplace(ProjectConversationItemView existing, ProjectConversationItemView incoming)
    {
        ArgumentNullException.ThrowIfNull(existing);
        ArgumentNullException.ThrowIfNull(incoming);
        return incoming.SourceVersion >= existing.SourceVersion;
    }

    public ProjectConversationItemView WithSourceEmail(ProjectConversationSourceEmailView source)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (!string.Equals(TenantId, source.TenantId, StringComparison.Ordinal) ||
            !string.Equals(IntakeId, source.IntakeId, StringComparison.Ordinal))
        {
            return this;
        }

        return IsSourceEmailEnrichableKind(Kind) ? this with
        {
            SourceProviderMessageId = source.SourceProviderMessageId,
            InternetMessageId = source.InternetMessageId,
            SourceReceivedAtUtc = source.SourceReceivedAtUtc,
            SourceSentAtUtc = source.SourceSentAtUtc,
            SourceCreatedAtUtc = source.SourceCreatedAtUtc,
            SourceTimezone = source.SourceTimezone,
            SourceProvenanceDisplayToken = source.SourceProvenanceDisplayToken,
            CorrelationId = string.IsNullOrWhiteSpace(CorrelationId) ? source.CorrelationId : CorrelationId,
        } : this;
    }

    public ProjectConversationItemStatusSummary BuildStatusSummary()
        => new(
        [
            BuildAssociationFacet(),
            BuildAttachmentFacet(),
            BuildTaskFacet(),
            BuildApprovalFacet(),
            BuildCommandFacet(),
            BuildFailureFacet(),
            BuildRetryFacet(),
            BuildNextActionFacet(),
        ]);

    public ProjectConversationItemClassification BuildClassification()
    {
        ProjectConversationClassificationKind kind = IsActionable() ? ProjectConversationClassificationKind.Actionable : ProjectConversationClassificationKind.Informational;
        return new ProjectConversationItemClassification(
            kind,
            ClassificationKernelVersion,
            ConfidenceScore,
            kind is ProjectConversationClassificationKind.Actionable
                ? "conversation_item_actionable"
                : "conversation_item_informational",
            SafeEvidenceIds(),
            SafeRedactionState());
    }

    public ProjectConversationDetectedIntent? BuildDetectedIntent()
    {
        if (CapturedTaskIntent is not null)
        {
            return new ProjectConversationDetectedIntent(
                CapturedTaskIntent.DetectedIntentSummary,
                CapturedTaskIntent.DetectedActionKind,
                CapturedTaskIntent.SourceEvidenceOffsets
                    .Select(static evidence => evidence.EvidenceReference)
                    .Where(static evidence => !string.IsNullOrWhiteSpace(evidence))
                    .Distinct(StringComparer.Ordinal)
                    .Order(StringComparer.Ordinal)
                    .ToArray(),
                CapturedTaskIntent.SafeNextAction ?? "review-task-intent",
                CapturedTaskIntent.ReasonCode,
                CapturedTaskIntent.RedactionState);
        }

        if (!IsActionable())
        {
            return null;
        }

        string safeNextAction = ResolvedSafeNextAction();
        ProjectConversationDetectedActionKind actionKind = DetectedActionKindFor(safeNextAction);
        return new ProjectConversationDetectedIntent(
            $"intent:{actionKind switch
            {
                ProjectConversationDetectedActionKind.RequestDecision => "review-decision",
                ProjectConversationDetectedActionKind.RequestInformation => "request-information",
                ProjectConversationDetectedActionKind.RequestAction => "perform-safe-action",
                _ => "inform-only",
            }}",
            actionKind,
            SafeEvidenceIds(),
            safeNextAction,
            $"detected_intent_{WireToken(actionKind).Replace("-", "_", StringComparison.Ordinal)}",
            SafeRedactionState());
    }

    public ProjectConversationAiSummaryProvenance? BuildAiSummaryProvenance()
    {
        if (Kind is not ProjectConversationItemKind.AiOutcome)
        {
            return null;
        }

        IReadOnlyList<string> sourceEvidenceIds = SafeEvidenceIds();
        return new ProjectConversationAiSummaryProvenance(
            "unavailable",
            OccurredAt,
            sourceEvidenceIds,
            IsUnavailableReferenceStatus(AiContextRedactionState) ? null : AiContextPackageId,
            IsUnavailableReferenceStatus(AiContextRedactionState) ? null : AiContextPackageVersion,
            FirstNonBlank(AiGeneratedSummaryRedactionState, AiGeneratedContentVisibility, AiContextRedactionState, RedactionState) ?? "unavailable");
    }

    public IReadOnlyList<ProjectConversationReviewHistoryEntry> BuildReviewHistory()
    {
        ProjectConversationReviewHistoryEntry? entry = Kind switch
        {
            ProjectConversationItemKind.SystemDecision => BuildDecisionReviewEntry(),
            ProjectConversationItemKind.ApprovalEvent => BuildApprovalReviewEntry(),
            ProjectConversationItemKind.AiOutcome => BuildAiOutcomeReviewEntry(),
            ProjectConversationItemKind.FailureState => BuildFailureReviewEntry(),
            ProjectConversationItemKind.Attachment => BuildAttachmentReviewEntry(),
            ProjectConversationItemKind.Participant => BuildParticipantReviewEntry(),
            ProjectConversationItemKind.EmailDerived => BuildEmailReviewEntry(),
            _ => null,
        };

        return entry is null ? [] : [entry];
    }

    private ProjectConversationItemStatusFacet BuildAssociationFacet()
        => new(
            "association",
            HealthFromLifecycle(LifecycleState),
            WireToken(LifecycleState),
            LifecycleState is LifecycleState.Failed ? "association_context_unavailable" : "association_decision_accepted",
            ResolvedSafeNextAction(),
            SafeMetadataIds: new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["associationId"] = AssociationId,
            });

    private ProjectConversationItemStatusFacet BuildAttachmentFacet()
    {
        string? state = AttachmentScanStatus is not null
            ? WireToken(AttachmentScanStatus.Value)
            : AttachmentStorageStatus is not null
                ? WireToken(AttachmentStorageStatus.Value)
                : AttachmentCaptureStatus is not null
                    ? WireToken(AttachmentCaptureStatus.Value)
                    : null;

        return new ProjectConversationItemStatusFacet(
            "attachment",
            state is null ? ChatBotHealthStatus.Unknown : HealthFromAttachmentState(state),
            state ?? "not-applicable",
            state is null ? "status_attachment_not_applicable" : "status_attachment_available",
            ResolvedSafeNextAction(),
            SafeMetadataIds: SafeIds(
                ("attachmentId", SourceProviderAttachmentId),
                ("folderId", AttachmentFolderId),
                ("fileId", AttachmentFileId)),
            NotApplicable: state is null);
    }

    private ProjectConversationItemStatusFacet BuildTaskFacet()
    {
        if (CapturedTaskIntent is null)
        {
            return new ProjectConversationItemStatusFacet(
            "task",
            ChatBotHealthStatus.Unknown,
            "unknown",
            "status_task_unknown",
            "none");
        }

        string state = WireToken(CapturedTaskIntent.State);
        bool terminal = CapturedTaskIntent.State is TaskIntentState.Converted or
            TaskIntentState.NotActionable or
            TaskIntentState.Duplicate or
            TaskIntentState.AlreadyHandled or
            TaskIntentState.OutOfScope;
        return new ProjectConversationItemStatusFacet(
            "task",
            terminal ? ChatBotHealthStatus.Healthy : ChatBotHealthStatus.Degraded,
            state,
            CapturedTaskIntent.ReasonCode,
            CapturedTaskIntent.SafeNextAction ?? "review-task-intent",
            SafeMetadataIds: SafeIds(
                ("taskIntentId", CapturedTaskIntent.TaskIntentId),
                ("proposalId", CapturedTaskIntent.ConvertedProposalId),
                ("predecessorTaskIntentId", CapturedTaskIntent.DuplicatePredecessorTaskIntentId)),
            OperationId: CapturedTaskIntent.AuditOperationId,
            AuditStatus: CapturedTaskIntent.AuditOperationId is null ? null : "recorded",
            CorrelationId: CapturedTaskIntent.CorrelationId,
            TerminalReasonCode: terminal ? CapturedTaskIntent.ReasonCode : null);
    }

    private ProjectConversationItemStatusFacet BuildApprovalFacet()
    {
        string? state = ApprovalStatus is null ? null : WireToken(ApprovalStatus.Value);
        return new ProjectConversationItemStatusFacet(
            "approval",
            state is null ? ChatBotHealthStatus.Unknown : HealthFromApprovalState(state),
            state ?? "not-applicable",
            state is null ? "status_approval_not_applicable" : "status_approval_available",
            ResolvedSafeNextAction(),
            SafeMetadataIds: SafeIds(
                ("approvalId", ApprovalId),
                ("proposalId", ApprovalProposalId),
                ("projectedOutcomeItemId", ApprovalProjectedOutcomeItemId)),
            NotApplicable: state is null,
            OperationId: ApprovalAuditOperationId,
            AuditStatus: ApprovalAuditStatus,
            ResponsibleOwnerRole: ResponsibleOwnerRole);
    }

    private ProjectConversationItemStatusFacet BuildCommandFacet()
    {
        string? operationId = FirstNonBlank(OperationId, AiOperationId, ApprovalAuditOperationId, AuditOperationId);
        string? completionStatus = FirstNonBlank(ApprovalCommandOutcomeStatus, AiExecutionStatus, FailureStatus is null ? null : WireToken(FailureStatus.Value));
        string sourceState = FirstNonBlank(completionStatus, LifecycleState is LifecycleState.Proposed ? "accepted-projection-pending" : null) ?? "not-applicable";

        return new ProjectConversationItemStatusFacet(
            "command",
            operationId is null ? ChatBotHealthStatus.Unknown : HealthFromCommandState(sourceState),
            sourceState,
            operationId is null ? "status_command_not_applicable" : "operation_projection_pending",
            ResolvedSafeNextAction(),
            SafeMetadataIds: SafeIds(("operationId", operationId), ("taskId", TaskId)),
            NotApplicable: operationId is null,
            OperationId: operationId,
            CompletionStatus: completionStatus,
            ProjectionStatus: sourceState,
            AuditStatus: FirstNonBlank(AuditStatus, AiAuditStatus, ApprovalAuditStatus),
            CorrelationId: FirstNonBlank(AiCorrelationId, CorrelationId),
            RetryCount: RetryCount,
            TerminalReasonCode: FirstNonBlank(FailureReasonCode, ApprovalFailureCode, AiFailureCode),
            ResponsibleOwnerRole: ResponsibleOwnerRole,
            DuplicateSafetyState: DuplicateSafetyState);
    }

    private ProjectConversationItemStatusFacet BuildFailureFacet()
    {
        string? state = FailureStateKind is null
            ? FailureStatus is null ? null : WireToken(FailureStatus.Value)
            : WireToken(FailureStateKind.Value);
        string? healthState = FailureStatus is null ? state : WireToken(FailureStatus.Value);
        return new ProjectConversationItemStatusFacet(
            "failure",
            state is null ? ChatBotHealthStatus.Healthy : HealthFromFailureState(healthState ?? state),
            state ?? "none",
            state is null ? "status_failure_none" : (MessageCatalogCode ?? "failed_command"),
            ResolvedSafeNextAction(),
            SafeMetadataIds: SafeIds(("operationId", OperationId), ("workflowInstanceId", WorkflowInstanceId)),
            NotApplicable: state is null,
            OperationId: OperationId,
            AuditStatus: AuditStatus,
            CorrelationId: CorrelationId,
            RetryCount: RetryCount,
            TerminalReasonCode: FailureReasonCode,
            DuplicateSafetyState: DuplicateSafetyState);
    }

    private ProjectConversationItemStatusFacet BuildRetryFacet()
    {
        string? failureRetryState = FailureStateKind is { } failureStateKind && IsFailureRetryState(failureStateKind)
            ? WireToken(failureStateKind)
            : null;
        bool hasRetry = Retryable is not null ||
            RetryCount is not null ||
            RetryOperationId is not null ||
            failureRetryState is not null ||
            !string.IsNullOrWhiteSpace(AttachmentRetryState);
        string state = FirstNonBlank(AttachmentRetryState, failureRetryState, RetryOperationId is not null ? "retry-accepted" : null, Retryable is true ? "retryable" : null) ?? "none";
        return new ProjectConversationItemStatusFacet(
            "retry",
            hasRetry ? HealthFromRetryState(state, Retryable) : ChatBotHealthStatus.Healthy,
            state,
            hasRetry ? "status_retry_available" : "status_retry_none",
            ResolvedSafeNextAction(),
            SafeMetadataIds: SafeIds(("retryOperationId", RetryOperationId)),
            NotApplicable: !hasRetry,
            OperationId: RetryOperationId,
            RetryCount: RetryCount,
            DuplicateSafetyState: DuplicateSafetyState);
    }

    private ProjectConversationItemStatusFacet BuildNextActionFacet()
        => new(
            "next-action",
            string.Equals(ResolvedSafeNextAction(), "none", StringComparison.OrdinalIgnoreCase) ? ChatBotHealthStatus.Healthy : HealthFromLifecycle(LifecycleState),
            ResolvedSafeNextAction(),
            $"next_action_{ResolvedSafeNextAction().Replace("-", "_", StringComparison.Ordinal)}",
            ResolvedSafeNextAction(),
            SafeMetadataIds: SafeIds(("ownerRole", ResponsibleOwnerRole), ("escalationTargetRole", EscalationTargetRole)));

    private string ResolvedSafeNextAction()
        => FirstNonBlank(AiSafeNextAction, SafeNextAction, ClientAction) ?? "none";

    private bool IsActionable()
        => !string.Equals(ResolvedSafeNextAction(), "none", StringComparison.OrdinalIgnoreCase) ||
            LifecycleState is LifecycleState.NeedsReview or LifecycleState.Failed or LifecycleState.Deferred or LifecycleState.Correcting or LifecycleState.CorrectionDelayed ||
            ApprovalStatus is Hexalith.ChatBot.Contracts.Enums.ApprovalStatus.Pending or Hexalith.ChatBot.Contracts.Enums.ApprovalStatus.RevisionRequested ||
            FailureStateKind is not null ||
            ParticipantAllowedReviewActions is { Count: > 0 } ||
            AttachmentAllowedActions is { Count: > 0 };

    private static ProjectConversationDetectedActionKind DetectedActionKindFor(string safeNextAction)
    {
        if (string.Equals(safeNextAction, "none", StringComparison.OrdinalIgnoreCase))
        {
            return ProjectConversationDetectedActionKind.InformOnly;
        }

        if (safeNextAction.Contains("review", StringComparison.OrdinalIgnoreCase) ||
            safeNextAction.Contains("approval", StringComparison.OrdinalIgnoreCase) ||
            safeNextAction.Contains("decision", StringComparison.OrdinalIgnoreCase))
        {
            return ProjectConversationDetectedActionKind.RequestDecision;
        }

        if (safeNextAction.Contains("wait", StringComparison.OrdinalIgnoreCase) ||
            safeNextAction.Contains("inspect", StringComparison.OrdinalIgnoreCase))
        {
            return ProjectConversationDetectedActionKind.RequestInformation;
        }

        return ProjectConversationDetectedActionKind.RequestAction;
    }

    private IReadOnlyList<string> SafeEvidenceIds()
    {
        IEnumerable<string?> values = (EvidenceReferenceSummary ?? [])
            .Concat(ApprovalEvidenceReferences ?? [])
            .Concat(AiAuthorizedContextReferences ?? [])
            .Concat([ParticipantEvidenceReference, SourceProvenanceDisplayToken, SourceConversationId, SourceThreadId]);

        return values
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .Select(static value => value!)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();
    }

    private string SafeRedactionState()
        => FirstNonBlank(
            DecisionNoteRedactionState,
            CorrectionRationaleRedactionState,
            ParticipantRedactionState,
            AttachmentRedactionState,
            ApprovalDecisionRationaleRedactionState,
            AiGeneratedSummaryRedactionState,
            RedactionState) ?? "unavailable";

    private ProjectConversationReviewHistoryEntry? BuildDecisionReviewEntry()
    {
        DateTimeOffset? reviewedAt = CorrectedAtUtc ?? DecidedAtUtc;
        if (reviewedAt is null)
        {
            return null;
        }

        return new ProjectConversationReviewHistoryEntry(
            "association",
            AssociationId,
            CorrectionKind is null ? "association-decision" : "association-correction",
            CorrectionKind is null ? DecisionKind is null ? null : WireToken(DecisionKind.Value) : WireToken(CorrectionKind.Value),
            CorrectionKind is null ? DecisionActorType : CorrectionActorType,
            CorrectionKind is null ? DecisionActorId : CorrectionActorId,
            reviewedAt.Value,
            SurfaceOrigin,
            CorrelationId,
            WorkflowInstanceId,
            SafeRedactionState(),
            CorrectionKind is null ? "association_decision_recorded" : "association_correction_recorded");
    }

    private ProjectConversationReviewHistoryEntry BuildApprovalReviewEntry()
        => new(
            "approval",
            ApprovalId ?? AssociationId,
            ApprovalEventKind is null ? "approval-event" : WireToken(ApprovalEventKind.Value),
            ApprovalDecisionKind is null
                ? ApprovalStatus is null ? null : WireToken(ApprovalStatus.Value)
                : WireToken(ApprovalDecisionKind.Value),
            ApprovalDecisionActorType ?? ApprovalRequesterActorType,
            ApprovalDecisionActorId ?? ApprovalRequesterId,
            ApprovalDecidedAtUtc ?? ApprovalOutcomeAtUtc ?? ApprovalRequestedAtUtc ?? OccurredAt,
            SurfaceOrigin,
            CorrelationId,
            ApprovalAuditOperationId,
            SafeRedactionState(),
            ApprovalFailureCode ?? "approval_review_recorded");

    private ProjectConversationReviewHistoryEntry BuildAiOutcomeReviewEntry()
        => new(
            "ai-outcome",
            AiProposalId ?? AiOperationId ?? AiRequestId ?? AssociationId,
            AiOutcomeKind is null ? "ai-outcome" : WireToken(AiOutcomeKind.Value),
            AiOutcomeStatus is null ? null : WireToken(AiOutcomeStatus.Value),
            AiActorType,
            AiActorId,
            OccurredAt,
            SurfaceOrigin,
            AiCorrelationId ?? CorrelationId,
            FirstNonBlank(AiOperationId, AiAuditOperationId),
            SafeRedactionState(),
            AiFailureCode ?? "ai_outcome_recorded");

    private ProjectConversationReviewHistoryEntry BuildFailureReviewEntry()
        => new(
            "failure-state",
            OperationId ?? WorkflowInstanceId ?? AssociationId,
            FailureStateKind is null ? "failure-state" : WireToken(FailureStateKind.Value),
            FailureStatus is null ? null : WireToken(FailureStatus.Value),
            WireToken(ActorKind),
            ActorLabel,
            OccurredAt,
            SurfaceOrigin,
            CorrelationId,
            AuditOperationId,
            SafeRedactionState(),
            FailureReasonCode ?? MessageCatalogCode ?? "failure_state_recorded");

    private ProjectConversationReviewHistoryEntry BuildAttachmentReviewEntry()
        => new(
            "attachment",
            SourceProviderAttachmentId ?? AttachmentFileId ?? AttachmentFolderId ?? ItemId,
            "attachment-reviewed",
            AttachmentScanStatus is null
                ? AttachmentStorageStatus is null
                    ? AttachmentCaptureStatus is null ? null : WireToken(AttachmentCaptureStatus.Value)
                    : WireToken(AttachmentStorageStatus.Value)
                : WireToken(AttachmentScanStatus.Value),
            WireToken(ActorKind),
            ActorLabel,
            OccurredAt,
            SurfaceOrigin,
            CorrelationId,
            AuditOperationId,
            SafeRedactionState(),
            "attachment_metadata_reviewed");

    private ProjectConversationReviewHistoryEntry BuildParticipantReviewEntry()
        => new(
            "participant",
            SourceParticipantId ?? ParticipantResolutionId ?? ItemId,
            "participant-reviewed",
            ParticipantStatus is null ? null : WireToken(ParticipantStatus.Value),
            WireToken(ActorKind),
            ActorLabel,
            OccurredAt,
            SurfaceOrigin,
            CorrelationId,
            AuditOperationId,
            SafeRedactionState(),
            ParticipantBlockedReason is null ? "participant_metadata_reviewed" : WireToken(ParticipantBlockedReason.Value));

    private ProjectConversationReviewHistoryEntry BuildEmailReviewEntry()
    {
        ProjectConversationItemClassification classification = BuildClassification();
        return new ProjectConversationReviewHistoryEntry(
            "email",
            AssociationId,
            "classification-projected",
            WireToken(classification.Kind),
            WireToken(ActorKind),
            ActorLabel,
            OccurredAt,
            SurfaceOrigin,
            CorrelationId,
            AuditOperationId,
            SafeRedactionState(),
            classification.MessageCode);
    }

    private static ChatBotHealthStatus HealthFromLifecycle(LifecycleState state)
        => state switch
        {
            LifecycleState.Failed => ChatBotHealthStatus.Failed,
            LifecycleState.NeedsReview or LifecycleState.Correcting or LifecycleState.CorrectionDelayed or LifecycleState.Deferred => ChatBotHealthStatus.Degraded,
            _ => ChatBotHealthStatus.Healthy,
        };

    private static ChatBotHealthStatus HealthFromAttachmentState(string state)
    {
        if (string.Equals(state, WireToken(ProjectConversationAttachmentStatus.Captured), StringComparison.Ordinal))
        {
            return ChatBotHealthStatus.Healthy;
        }

        if (string.Equals(state, WireToken(ProjectConversationAttachmentStatus.Unsafe), StringComparison.Ordinal) ||
            string.Equals(state, WireToken(ProjectConversationAttachmentStatus.Failed), StringComparison.Ordinal) ||
            string.Equals(state, WireToken(ProjectConversationAttachmentStatus.Rejected), StringComparison.Ordinal))
        {
            return ChatBotHealthStatus.Failed;
        }

        if (string.Equals(state, WireToken(ProjectConversationAttachmentStatus.Pending), StringComparison.Ordinal) ||
            string.Equals(state, WireToken(ProjectConversationAttachmentStatus.Retryable), StringComparison.Ordinal) ||
            string.Equals(state, WireToken(ProjectConversationAttachmentStatus.Unavailable), StringComparison.Ordinal))
        {
            return ChatBotHealthStatus.Degraded;
        }

        return ChatBotHealthStatus.Unknown;
    }

    private static ChatBotHealthStatus HealthFromApprovalState(string state)
    {
        if (string.Equals(state, WireToken(Hexalith.ChatBot.Contracts.Enums.ApprovalStatus.Approved), StringComparison.Ordinal) ||
            string.Equals(state, WireToken(Hexalith.ChatBot.Contracts.Enums.ApprovalStatus.Executed), StringComparison.Ordinal))
        {
            return ChatBotHealthStatus.Healthy;
        }

        if (string.Equals(state, WireToken(Hexalith.ChatBot.Contracts.Enums.ApprovalStatus.Failed), StringComparison.Ordinal) ||
            string.Equals(state, WireToken(Hexalith.ChatBot.Contracts.Enums.ApprovalStatus.Rejected), StringComparison.Ordinal) ||
            string.Equals(state, WireToken(Hexalith.ChatBot.Contracts.Enums.ApprovalStatus.Cancelled), StringComparison.Ordinal))
        {
            return ChatBotHealthStatus.Failed;
        }

        if (string.Equals(state, WireToken(Hexalith.ChatBot.Contracts.Enums.ApprovalStatus.Pending), StringComparison.Ordinal) ||
            string.Equals(state, WireToken(Hexalith.ChatBot.Contracts.Enums.ApprovalStatus.RevisionRequested), StringComparison.Ordinal))
        {
            return ChatBotHealthStatus.Degraded;
        }

        return ChatBotHealthStatus.Unknown;
    }

    private static ChatBotHealthStatus HealthFromCommandState(string state)
        => state switch
        {
            "completed" or "executed" => ChatBotHealthStatus.Healthy,
            "failed" or "terminal" => ChatBotHealthStatus.Failed,
            "accepted-projection-pending" or "retryable" => ChatBotHealthStatus.Degraded,
            _ => ChatBotHealthStatus.Unknown,
        };

    private static ChatBotHealthStatus HealthFromFailureState(string state)
        => state switch
        {
            "resolved" => ChatBotHealthStatus.Healthy,
            "terminal" or "blocked" => ChatBotHealthStatus.Failed,
            "retryable" or "degraded" => ChatBotHealthStatus.Degraded,
            _ => ChatBotHealthStatus.Unknown,
        };

    private static ChatBotHealthStatus HealthFromRetryState(string state, bool? retryable)
        => state switch
        {
            "retry-exhausted" => ChatBotHealthStatus.Failed,
            "retryable" or "retry-accepted" or "retry-queued" or "projection-retryable" or "queued" or "retryable-after-policy-window" => ChatBotHealthStatus.Degraded,
            "redacted" or "unavailable" or "unknown" => ChatBotHealthStatus.Unknown,
            _ => retryable is true ? ChatBotHealthStatus.Degraded : ChatBotHealthStatus.Healthy,
        };

    private static bool IsFailureRetryState(Hexalith.ChatBot.Contracts.Enums.FailureStateKind state)
        => state is Hexalith.ChatBot.Contracts.Enums.FailureStateKind.RetryQueued or
            Hexalith.ChatBot.Contracts.Enums.FailureStateKind.RetryAccepted or
            Hexalith.ChatBot.Contracts.Enums.FailureStateKind.RetryExhausted or
            Hexalith.ChatBot.Contracts.Enums.FailureStateKind.ProjectionRetryable;

    private static IReadOnlyDictionary<string, string>? SafeIds(params (string Key, string? Value)[] values)
    {
        Dictionary<string, string> ids = new(StringComparer.Ordinal);
        foreach ((string key, string? value) in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                ids[key] = value;
            }
        }

        return ids.Count == 0 ? null : ids;
    }

    private static string? FirstNonBlank(params string?[] values)
        => values.FirstOrDefault(static value => !string.IsNullOrWhiteSpace(value));

    private static string WireToken<TEnum>(TEnum value)
        where TEnum : struct, Enum
    {
        string? name = Enum.GetName(value);
        if (name is null)
        {
            return value.ToString();
        }

        return typeof(TEnum)
            .GetMember(name)
            .FirstOrDefault()?
            .GetCustomAttributes(typeof(System.Runtime.Serialization.EnumMemberAttribute), false)
            .OfType<System.Runtime.Serialization.EnumMemberAttribute>()
            .FirstOrDefault()?
            .Value ?? value.ToString();
    }

    public static ProjectConversationItemView? FromAssociation(
        AssociationCandidateView view,
        ProjectConversationSourceEmailView? source = null)
        => FromAssociationSourceContext(view, source);

    public static ProjectConversationItemView? FromAssociationSourceContext(
        AssociationCandidateView view,
        ProjectConversationSourceEmailView? source = null)
    {
        ArgumentNullException.ThrowIfNull(view);
        if (string.IsNullOrWhiteSpace(view.ProjectId))
        {
            return null;
        }

        return new ProjectConversationItemView(
            view.TenantId,
            view.ProjectId,
            view.ProjectDisplayName,
            view.AssociationId,
            view.IntakeId,
            ProjectConversationItemKind.EmailDerived,
            ProjectConversationActorKind.Mailbox,
            "Mailbox event",
            view.DetectedAt,
            view.LifecycleState,
            view.ThresholdBand,
            view.ConfidenceScore,
            view.AssociationId,
            view.SourceMailboxId,
            source?.SourceProviderMessageId,
            source?.InternetMessageId,
            view.SourceConversationId,
            view.SourceThreadId,
            source?.SourceReceivedAtUtc,
            source?.SourceSentAtUtc,
            source?.SourceCreatedAtUtc,
            source?.SourceTimezone,
            source?.SourceProvenanceDisplayToken,
            view.SourceProvenance,
            view.RedactionState,
            view.RetentionClass,
            CurrentSchemaVersion,
            view.SourceVersion,
            view.CorrelationId,
            SafeNextAction: view.SafeNextAction);
    }

    public static ProjectConversationItemView? FromAssociationDecision(
        AssociationCandidateView view,
        ProjectConversationSourceEmailView? source = null)
    {
        ArgumentNullException.ThrowIfNull(view);
        if (string.IsNullOrWhiteSpace(view.ProjectId) ||
            (view.DecisionKind is null && view.CorrectionKind is null))
        {
            return null;
        }

        string? decisionLabel = view.CorrectionKind?.ToString() ?? view.DecisionKind?.ToString();
        DateTimeOffset occurredAt = view.CorrectedAt ?? view.DecidedAt ?? view.DetectedAt;

        return new ProjectConversationItemView(
            view.TenantId,
            view.ProjectId,
            view.ProjectDisplayName,
            DecisionItemIdFor(view.AssociationId, view.SourceVersion),
            view.IntakeId,
            ProjectConversationItemKind.SystemDecision,
            ProjectConversationActorKind.SystemDecision,
            "System decision",
            occurredAt,
            view.LifecycleState,
            view.ThresholdBand,
            view.ConfidenceScore,
            view.AssociationId,
            view.SourceMailboxId,
            source?.SourceProviderMessageId,
            source?.InternetMessageId,
            view.SourceConversationId,
            view.SourceThreadId,
            source?.SourceReceivedAtUtc,
            source?.SourceSentAtUtc,
            source?.SourceCreatedAtUtc,
            source?.SourceTimezone,
            source?.SourceProvenanceDisplayToken,
            view.SourceProvenance,
            view.RedactionState,
            view.RetentionClass,
            CurrentSchemaVersion,
            view.SourceVersion,
            view.CorrelationId,
            decisionLabel,
            view.SafeNextAction,
            DecisionKind: view.DecisionKind,
            DecisionActorId: view.DecisionActorId,
            DecisionActorType: view.DecisionActorType,
            DecidedAtUtc: view.DecidedAt,
            DecisionNoteRedactionState: view.DecisionNoteRedactionState,
            SurfaceOrigin: view.SurfaceOrigin,
            PolicySnapshotVersion: view.PolicySnapshotVersion ?? view.ThresholdPolicyVersion,
            EvidenceReferenceSummary: EvidenceReferenceSummaryFor(view),
            CorrectionKind: view.CorrectionKind,
            PriorProjectId: view.PriorProjectId,
            CorrectedProjectId: view.CorrectedProjectId,
            PredecessorAssociationId: view.PredecessorAssociationId,
            SupersedesAssociationId: view.SupersedesAssociationId,
            SupersededByAssociationId: view.SupersededByAssociationId,
            CorrectionRationaleRedactionState: view.CorrectionRationaleRedactionState,
            CorrectionActorId: view.CorrectionActorId,
            CorrectionActorType: view.CorrectionActorType,
            CorrectedAtUtc: view.CorrectedAt,
            DownstreamImpactStatus: view.DownstreamImpactStatus,
            CorrectionId: view.CorrectionId,
            WorkflowInstanceId: view.WorkflowInstanceId,
            RequiredStoreKeys: view.RequiredStoreKeys,
            CompletedStoreKeys: view.CompletedStoreKeys,
            FailedStoreKeys: view.FailedStoreKeys,
            PropagationProgressNumerator: view.PropagationProgressNumerator,
            PropagationProgressDenominator: view.PropagationProgressDenominator,
            PropagationStartedAtUtc: view.PropagationStartedAtUtc,
            PropagationCompletedAtUtc: view.PropagationCompletedAtUtc,
            PropagationEstimatedCompletionAtUtc: view.PropagationEstimatedCompletionAtUtc,
            PropagationStatus: view.PropagationStatus,
            IsCorrectedContextStale: view.IsCorrectedContextStale,
            ResponsibleOwnerRole: view.ResponsibleOwnerRole);
    }

    public static ProjectConversationItemView FromParticipant(
        ParticipantResolutionView participant,
        ProjectConversationItemView association)
    {
        ArgumentNullException.ThrowIfNull(participant);
        ArgumentNullException.ThrowIfNull(association);

        ProjectConversationParticipantDisplayKind displayKind = participant.DisplayKind;
        ProjectConversationActorKind actorKind = displayKind switch
        {
            ProjectConversationParticipantDisplayKind.InternalParticipant => ProjectConversationActorKind.InternalParticipant,
            ProjectConversationParticipantDisplayKind.ExternalParticipant => ProjectConversationActorKind.ExternalParticipant,
            ProjectConversationParticipantDisplayKind.UnresolvedParticipant => ProjectConversationActorKind.UnresolvedParticipant,
            ProjectConversationParticipantDisplayKind.RestrictedParticipant => ProjectConversationActorKind.RestrictedParticipant,
            _ => ProjectConversationActorKind.RestrictedParticipant,
        };

        return new ProjectConversationItemView(
            association.TenantId,
            association.ProjectId,
            association.ProjectDisplayName,
            ParticipantItemIdFor(participant.ResolutionId, participant.SourceParticipantId),
            association.IntakeId,
            ProjectConversationItemKind.Participant,
            actorKind,
            participant.SafeDisplayLabel,
            participant.RecordedAt,
            association.LifecycleState,
            association.ThresholdBand,
            association.ConfidenceScore,
            association.AssociationId,
            participant.SourceMailboxId,
            null,
            null,
            association.SourceConversationId,
            association.SourceThreadId,
            null,
            null,
            null,
            null,
            null,
            participant.SourceProvenance,
            participant.RedactionState,
            participant.RetentionClass,
            CurrentSchemaVersion,
            participant.SourceVersion,
            participant.CorrelationId,
            null,
            association.SafeNextAction,
            participant.ResolutionId,
            participant.SourceParticipantId,
            participant.PartyId,
            participant.Status,
            participant.Reason,
            displayKind,
            participant.EvidenceReference,
            participant.EvidenceFingerprint,
            participant.AllowedReviewActions,
            participant.RedactionState);
    }

    public static ProjectConversationItemView FromAttachment(
        ProjectConversationAttachmentReferenceView attachment,
        ProjectConversationItemView association)
    {
        ArgumentNullException.ThrowIfNull(attachment);
        ArgumentNullException.ThrowIfNull(association);
        bool canExposeStoredReference = attachment.StorageStatus is ProjectConversationAttachmentStatus.Captured &&
            (!attachment.SafetyGateEvaluated || attachment.ScanStatus is ProjectConversationAttachmentStatus.Captured);

        return new ProjectConversationItemView(
            association.TenantId,
            association.ProjectId,
            association.ProjectDisplayName,
            attachment.StableMaterializedIdFor(association.AssociationId),
            association.IntakeId,
            ProjectConversationItemKind.Attachment,
            ProjectConversationActorKind.MailboxAttachment,
            "Mailbox attachment",
            association.OccurredAt,
            association.LifecycleState,
            association.ThresholdBand,
            association.ConfidenceScore,
            association.AssociationId,
            association.SourceMailboxId,
            null,
            null,
            association.SourceConversationId,
            association.SourceThreadId,
            null,
            null,
            null,
            null,
            null,
            association.SourceProvenance,
            attachment.RedactionState,
            attachment.RetentionClass,
            CurrentSchemaVersion,
            attachment.SourceVersion,
            attachment.CorrelationId,
            null,
            attachment.SafeNextAction,
            AttachmentDisplayName: attachment.SafeDisplayName,
            AttachmentContentType: attachment.ContentType,
            AttachmentSizeInBytes: attachment.SizeInBytes,
            AttachmentCaptureStatus: attachment.CaptureStatus,
            AttachmentStorageStatus: attachment.StorageStatus,
            AttachmentScanStatus: attachment.ScanStatus,
            AttachmentFolderId: canExposeStoredReference ? attachment.FolderId : null,
            AttachmentFileId: canExposeStoredReference ? attachment.FileId : null,
            AttachmentDuplicateState: attachment.DuplicateState,
            AttachmentRetryState: attachment.RetryState,
            AttachmentAiContextEligibility: attachment.AiContextEligibility,
            AttachmentAllowedActions: attachment.AllowedActions,
            AttachmentRedactionState: attachment.RedactionState,
            SourceProviderAttachmentId: attachment.ProviderAttachmentId);
    }

    public static ProjectConversationItemView FromApprovalEvent(ApprovalEventView approval)
    {
        ArgumentNullException.ThrowIfNull(approval);

        string sourceConversationId = approval.SourceConversationItemId
            ?? approval.SourceMessageId
            ?? approval.ProposalId
            ?? approval.ApprovalId;
        string associationId = approval.ProposalId ?? approval.ApprovalId;

        return new ProjectConversationItemView(
            approval.TenantId,
            approval.ProjectId,
            null,
            approval.StableItemId,
            approval.ApprovalId,
            ProjectConversationItemKind.ApprovalEvent,
            ProjectConversationActorKind.ApprovalSystem,
            "Approval event",
            approval.OccurredAtUtc,
            LifecycleState.NeedsReview,
            AssociationThresholdBand.Auto,
            0,
            associationId,
            "approval-event",
            null,
            null,
            sourceConversationId,
            null,
            null,
            null,
            null,
            null,
            null,
            "approval-event",
            approval.RedactionState,
            approval.RetentionClass,
            CurrentSchemaVersion,
            approval.SourceVersion,
            approval.CorrelationId,
            SafeNextAction: approval.SafeNextAction,
            ApprovalId: approval.ApprovalId,
            ApprovalEventKind: approval.EventKind,
            ApprovalStatus: approval.Status,
            ApprovalDecisionKind: approval.DecisionKind,
            ApprovalRequesterId: approval.RequesterId,
            ApprovalRequesterActorType: approval.RequesterActorType,
            ApprovalRequestedAtUtc: approval.RequestedAtUtc,
            ApprovalDecisionActorId: approval.DecisionActorId,
            ApprovalDecisionActorType: approval.DecisionActorType,
            ApprovalDecidedAtUtc: approval.DecidedAtUtc,
            ApprovalOutcomeAtUtc: approval.OutcomeAtUtc,
            ApprovalProposalId: approval.ProposalId,
            ApprovalSourceMessageId: approval.SourceMessageId,
            ApprovalSourceConversationItemId: approval.SourceConversationItemId,
            ApprovalCommandName: approval.CommandName,
            ApprovalCommandAllowlistVersion: approval.CommandAllowlistVersion,
            ApprovalRiskClass: approval.RiskClass,
            ApprovalRiskActionClasses: approval.RiskActionClasses,
            ApprovalAiRiskClass: approval.AiRiskClass,
            ApprovalAiRiskActionClasses: approval.AiRiskActionClasses,
            ApprovalAiRiskInputTuple: approval.AiRiskInputTuple,
            ApprovalPolicySnapshotId: AuthorizedPolicySnapshotId(approval.PolicySnapshotId, approval.PolicySnapshotVisibility),
            ApprovalPolicySnapshotVisibility: approval.PolicySnapshotVisibility,
            ApprovalEvidenceReferences: approval.EvidenceReferences,
            ApprovalEvidenceFreshnessStates: approval.EvidenceFreshnessStates,
            ApprovalAffectedResourceReferences: approval.AffectedResourceReferences,
            ApprovalRecipientReferences: approval.RecipientReferences,
            ApprovalSenderAuthorityClass: approval.SenderAuthorityClass,
            ApprovalExpectedPostStateRedactionState: approval.ExpectedPostStateRedactionState,
            ApprovalActionSummaryRedactionState: approval.ActionSummaryRedactionState,
            ApprovalDecisionRationaleRedactionState: approval.DecisionRationaleRedactionState,
            ApprovalAuthorityResult: approval.AuthorityResult,
            ApprovalDisabledReason: approval.DisabledReason,
            ApprovalAuditOperationId: AuthorizedAuditOperationId(approval.AuditOperationId, approval.AuditStatus),
            ApprovalAuditStatus: approval.AuditStatus,
            ApprovalCommandOutcomeStatus: approval.CommandOutcomeStatus,
            ApprovalProjectedOutcomeItemId: approval.ProjectedOutcomeItemId,
            ApprovalFailureCode: approval.FailureCode,
            ApprovalRetryability: approval.Retryability,
            SupersedesApprovalId: approval.SupersedesApprovalId,
            SupersededByApprovalId: approval.SupersededByApprovalId);
    }

    public static ProjectConversationItemView FromFailureStateEvent(FailureStateEventView failure)
    {
        ArgumentNullException.ThrowIfNull(failure);

        string sourceConversationId = failure.SourceConversationItemId
            ?? failure.SourceMessageId
            ?? failure.WorkflowInstanceId
            ?? failure.OperationId;
        string associationId = failure.AssociationId
            ?? failure.SourceConversationItemId
            ?? failure.OperationId;

        ChatBotMessageCatalogEntry catalogEntry = ChatBotMessageCatalog.Resolve(failure.MessageCatalogCode);

        return new ProjectConversationItemView(
            failure.TenantId,
            failure.ProjectId,
            null,
            failure.StableItemId,
            failure.OperationId,
            ProjectConversationItemKind.FailureState,
            ProjectConversationActorKind.SystemStatus,
            "System status",
            failure.OccurredAtUtc,
            LifecycleState.Failed,
            AssociationThresholdBand.Auto,
            0,
            associationId,
            "failure-state",
            null,
            null,
            sourceConversationId,
            null,
            null,
            null,
            null,
            null,
            null,
            "failure-state",
            failure.RedactionState,
            failure.RetentionClass,
            CurrentSchemaVersion,
            failure.SourceVersion,
            failure.CorrelationId,
            SafeNextAction: failure.SafeNextAction ?? catalogEntry.NextAction,
            WorkflowInstanceId: failure.WorkflowInstanceId,
            FailureStateKind: failure.FailureStateKind,
            FailureStatus: failure.FailureStatus,
            MessageCatalogCode: failure.MessageCatalogCode,
            MessageCatalogVersion: ChatBotMessageCatalogVersion.Current,
            MessageDetailVisibility: catalogEntry.DetailVisibility,
            FailureCategory: failure.FailureCategory,
            FailureScope: failure.FailureScope,
            FailureReasonCode: failure.FailureReasonCode,
            BlockedReason: failure.BlockedReason ?? catalogEntry.DisabledActionReason,
            Retryable: failure.Retryable,
            RetryCount: failure.RetryCount,
            MaxRetryCount: failure.MaxRetryCount,
            NextRetryAtUtc: failure.NextRetryAtUtc,
            LastRetryAtUtc: failure.LastRetryAtUtc,
            RetryOperationId: failure.RetryOperationId,
            SupersedesWorkflowInstanceId: failure.SupersedesWorkflowInstanceId,
            SupersededByWorkflowInstanceId: failure.SupersededByWorkflowInstanceId,
            TaskId: failure.TaskId,
            OperationId: failure.OperationId,
            AuditOperationId: AuthorizedAuditOperationId(failure.AuditOperationId, failure.AuditStatus),
            AuditStatus: failure.AuditStatus,
            ClientAction: failure.ClientAction ?? catalogEntry.NextAction,
            DuplicateSafetyState: failure.DuplicateSafetyState,
            DuplicateSuppressionId: failure.DuplicateSuppressionId,
            DependencyName: failure.DependencyName,
            DegradedUntilUtc: failure.DegradedUntilUtc,
            EscalationTargetRole: failure.EscalationTargetRole,
            ReprocessCreatedWorkflowInstanceId: failure.ReprocessCreatedWorkflowInstanceId);
    }

    public static ProjectConversationItemView FromAiOutcomeEvent(AiOutcomeEventView outcome)
    {
        ArgumentNullException.ThrowIfNull(outcome);

        string identity = outcome.ProposalId
            ?? outcome.OperationId
            ?? outcome.RequestId
            ?? throw new InvalidOperationException("AI outcome identity is required.");
        string sourceConversationId = outcome.SourceConversationItemId
            ?? outcome.SourceMessageId
            ?? identity;
        string associationId = identity;
        string actorLabel = outcome.ActorType switch
        {
            "service" => "AI service",
            "system" => "AI system",
            _ => "AI actor",
        };

        return new ProjectConversationItemView(
            outcome.TenantId,
            outcome.ProjectId,
            null,
            outcome.StableItemId,
            identity,
            ProjectConversationItemKind.AiOutcome,
            ProjectConversationActorKind.AiActor,
            actorLabel,
            outcome.OccurredAtUtc,
            LifecycleState.NeedsReview,
            AssociationThresholdBand.Auto,
            0,
            associationId,
            "ai-outcome",
            null,
            null,
            sourceConversationId,
            null,
            null,
            null,
            null,
            null,
            null,
            "ai-outcome",
            outcome.RedactionState,
            outcome.RetentionClass,
            CurrentSchemaVersion,
            outcome.SourceVersion,
            outcome.CorrelationId,
            SafeNextAction: outcome.SafeNextAction,
            AiOutcomeKind: outcome.OutcomeKind,
            AiOutcomeStatus: outcome.OutcomeStatus,
            AiActorId: outcome.ActorId,
            AiActorType: outcome.ActorType,
            AiProposalId: outcome.ProposalId,
            AiRequestId: outcome.RequestId,
            AiRequesterId: outcome.RequesterId,
            AiSourceConversationItemId: outcome.SourceConversationItemId,
            AiSourceMessageId: outcome.SourceMessageId,
            AiOperationId: outcome.OperationId,
            AiCorrelationId: outcome.CorrelationId,
            AiRiskClass: outcome.RiskClass,
            AiRiskActionClasses: outcome.RiskActionClasses,
            AiPolicyReasonCode: outcome.PolicyReasonCode,
            AiClassifierVersion: outcome.ClassifierVersion,
            AiRiskInputTuple: outcome.RiskInputTuple,
            AiRequesterAuthorityClass: outcome.RequesterAuthorityClass,
            AiIndeterminateReason: outcome.IndeterminateReason,
            AiPolicySnapshotId: AuthorizedPolicySnapshotId(outcome.PolicySnapshotId, outcome.PolicySnapshotVisibility),
            AiPolicySnapshotVisibility: outcome.PolicySnapshotVisibility,
            AiContextPackageId: outcome.ContextPackageId,
            AiContextPackageVersion: outcome.ContextPackageVersion,
            AiContextRedactionState: outcome.ContextRedactionState,
            AiAuthorizedContextReferences: outcome.AuthorizedContextReferences,
            AiExcludedContextReasons: outcome.ExcludedContextReasons,
            AiGeneratedSummaryRedactionState: outcome.GeneratedSummaryRedactionState,
            AiGeneratedContentVisibility: outcome.GeneratedContentVisibility,
            AiCommandName: outcome.CommandName,
            AiCommandAllowlistVersion: outcome.CommandAllowlistVersion,
            AiApprovalId: outcome.ApprovalId,
            AiApprovalStatus: outcome.ApprovalStatus,
            AiExecutionStatus: outcome.ExecutionStatus,
            AiExecutionOutcomeCode: outcome.ExecutionOutcomeCode,
            AiAuditOperationId: AuthorizedAuditOperationId(outcome.AuditOperationId, outcome.AuditStatus),
            AiAuditStatus: outcome.AuditStatus,
            AiFailureCode: outcome.FailureCode,
            AiRetryability: outcome.Retryability,
            AiSafeNextAction: outcome.SafeNextAction,
            SupersedesAiOutcomeId: outcome.SupersedesAiOutcomeId,
            SupersededByAiOutcomeId: outcome.SupersededByAiOutcomeId);
    }

    public static string ParticipantItemIdFor(string resolutionId, string sourceParticipantId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(resolutionId);
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceParticipantId);
        return $"participant:{resolutionId}:{sourceParticipantId}";
    }

    public static string DecisionItemIdFor(string associationId, long sourceVersion)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(associationId);
        return $"decision:{associationId}:{sourceVersion}";
    }

    public static string ApprovalItemIdFor(string approvalId, ApprovalEventKind eventKind, long sourceVersion)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(approvalId);
        return $"approval:{approvalId}:{ApprovalEventKindToken(eventKind)}:{sourceVersion}";
    }

    public static string FailureStateItemIdFor(string operationId, FailureStateKind stateKind, long sourceVersion)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(operationId);
        return $"failure:{operationId}:{FailureStateKindToken(stateKind)}:{sourceVersion}";
    }

    public static string AiOutcomeItemIdFor(string stableId, AiOutcomeKind outcomeKind, long sourceVersion)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(stableId);
        return $"ai:{stableId}:{AiOutcomeKindToken(outcomeKind)}:{sourceVersion}";
    }

    public static bool IsSourceEmailEnrichableKind(ProjectConversationItemKind kind)
        => kind is ProjectConversationItemKind.EmailDerived or ProjectConversationItemKind.SystemDecision;

    public static bool IsAssociationContextKind(ProjectConversationItemKind kind)
        => kind is ProjectConversationItemKind.EmailDerived;

    private static IReadOnlyList<string> EvidenceReferenceSummaryFor(AssociationCandidateView view)
        => view.Candidates
            .SelectMany(static candidate => candidate.EvidenceRefs)
            .Select(static evidence => evidence.EvidenceReference)
            .Where(static reference => !string.IsNullOrWhiteSpace(reference))
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();

    private static string ApprovalEventKindToken(ApprovalEventKind eventKind)
        => eventKind switch
        {
            Hexalith.ChatBot.Contracts.Enums.ApprovalEventKind.Request => "request",
            Hexalith.ChatBot.Contracts.Enums.ApprovalEventKind.Decision => "decision",
            Hexalith.ChatBot.Contracts.Enums.ApprovalEventKind.Outcome => "outcome",
            _ => eventKind.ToString(),
        };

    private static string FailureStateKindToken(FailureStateKind stateKind)
        => stateKind switch
        {
            Hexalith.ChatBot.Contracts.Enums.FailureStateKind.Failure => "failure",
            Hexalith.ChatBot.Contracts.Enums.FailureStateKind.RetryQueued => "retry-queued",
            Hexalith.ChatBot.Contracts.Enums.FailureStateKind.RetryAccepted => "retry-accepted",
            Hexalith.ChatBot.Contracts.Enums.FailureStateKind.RetryExhausted => "retry-exhausted",
            Hexalith.ChatBot.Contracts.Enums.FailureStateKind.Blocked => "blocked",
            Hexalith.ChatBot.Contracts.Enums.FailureStateKind.DuplicateSuppressed => "duplicate-suppressed",
            Hexalith.ChatBot.Contracts.Enums.FailureStateKind.DependencyDegraded => "dependency-degraded",
            Hexalith.ChatBot.Contracts.Enums.FailureStateKind.ProjectionRetryable => "projection-retryable",
            Hexalith.ChatBot.Contracts.Enums.FailureStateKind.TerminalFailure => "terminal-failure",
            Hexalith.ChatBot.Contracts.Enums.FailureStateKind.ReprocessCreated => "reprocess-created",
            _ => stateKind.ToString(),
        };

    private static string AiOutcomeKindToken(AiOutcomeKind outcomeKind)
        => outcomeKind switch
        {
            Hexalith.ChatBot.Contracts.Enums.AiOutcomeKind.Proposal => "proposal",
            Hexalith.ChatBot.Contracts.Enums.AiOutcomeKind.Denial => "denial",
            Hexalith.ChatBot.Contracts.Enums.AiOutcomeKind.Refusal => "refusal",
            Hexalith.ChatBot.Contracts.Enums.AiOutcomeKind.ApprovalLinked => "approval-linked",
            Hexalith.ChatBot.Contracts.Enums.AiOutcomeKind.ExecutionStarted => "execution-started",
            Hexalith.ChatBot.Contracts.Enums.AiOutcomeKind.ExecutionSucceeded => "execution-succeeded",
            Hexalith.ChatBot.Contracts.Enums.AiOutcomeKind.ExecutionFailed => "execution-failed",
            Hexalith.ChatBot.Contracts.Enums.AiOutcomeKind.OutcomeRecorded => "outcome-recorded",
            Hexalith.ChatBot.Contracts.Enums.AiOutcomeKind.CorrectedContextInvalidated => "corrected-context-invalidated",
            _ => outcomeKind.ToString(),
        };

    private static string? AuthorizedPolicySnapshotId(string? policySnapshotId, string? policySnapshotVisibility)
        => string.Equals(policySnapshotVisibility, "authorized", StringComparison.OrdinalIgnoreCase)
            ? policySnapshotId
            : null;

    private static string? AuthorizedAuditOperationId(string? auditOperationId, string? auditStatus)
        => IsUnavailableReferenceStatus(auditStatus) ? null : auditOperationId;

    private static bool IsUnavailableReferenceStatus(string? status)
        => string.Equals(status, "redacted", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(status, "unavailable", StringComparison.OrdinalIgnoreCase);
}
