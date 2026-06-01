using Hexalith.ChatBot.Contracts.Enums;

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
    string? SupersededByApprovalId = null)
{
    public const string CurrentSchemaVersion = "chatbot.project-conversation-item.v1";

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
            association.SafeNextAction,
            AttachmentDisplayName: attachment.SafeDisplayName,
            AttachmentContentType: attachment.ContentType,
            AttachmentSizeInBytes: attachment.SizeInBytes,
            AttachmentCaptureStatus: attachment.CaptureStatus,
            AttachmentStorageStatus: attachment.StorageStatus,
            AttachmentScanStatus: attachment.ScanStatus,
            AttachmentFolderId: attachment.FolderId,
            AttachmentFileId: attachment.FileId,
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
