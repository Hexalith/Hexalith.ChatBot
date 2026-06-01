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
    string? AttachmentRedactionState = null)
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
    {
        ArgumentNullException.ThrowIfNull(view);
        if (string.IsNullOrWhiteSpace(view.ProjectId))
        {
            return null;
        }

        ProjectConversationItemKind kind = view.DecisionKind is null && view.CorrectionKind is null
            ? ProjectConversationItemKind.EmailDerived
            : ProjectConversationItemKind.SystemDecision;
        ProjectConversationActorKind actor = kind == ProjectConversationItemKind.SystemDecision
            ? ProjectConversationActorKind.SystemDecision
            : ProjectConversationActorKind.Mailbox;
        string label = kind == ProjectConversationItemKind.SystemDecision
            ? "System decision"
            : "Mailbox event";
        string? decisionLabel = view.CorrectionKind?.ToString() ?? view.DecisionKind?.ToString();

        return new ProjectConversationItemView(
            view.TenantId,
            view.ProjectId,
            view.ProjectDisplayName,
            view.AssociationId,
            view.IntakeId,
            kind,
            actor,
            label,
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
            decisionLabel,
            view.SafeNextAction);
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

    public static string ParticipantItemIdFor(string resolutionId, string sourceParticipantId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(resolutionId);
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceParticipantId);
        return $"participant:{resolutionId}:{sourceParticipantId}";
    }

    public static bool IsSourceEmailEnrichableKind(ProjectConversationItemKind kind)
        => kind is ProjectConversationItemKind.EmailDerived or ProjectConversationItemKind.SystemDecision;
}
