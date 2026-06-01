namespace Hexalith.ChatBot.UI.State.ProjectConversation;

public sealed record ProjectConversationModel(
    string ProjectId,
    string ProjectDisplayName,
    string? TenantContext,
    string Status,
    string ConversationState,
    IReadOnlyList<ProjectConversationItemModel> Items,
    string? NextCursor,
    bool HasMore,
    int PageSize,
    string SourceProvenance,
    string RedactionState,
    string RetentionClass,
    string SchemaVersion,
    string CorrelationId,
    string SafeNextAction)
{
    public bool IsEmpty => Items.Count == 0;

    public bool IsBlockedOrStale => Status is "Blocked" or "Stale" or "Degraded";
}

public sealed record ProjectConversationItemModel(
    string ItemId,
    string Kind,
    string ActorKind,
    string ActorLabel,
    DateTimeOffset OccurredAt,
    string LifecycleState,
    string ThresholdBand,
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
    string? ProjectId,
    string? ProjectDisplayName,
    string? DecisionLabel,
    string? SafeNextAction,
    string? ParticipantResolutionId,
    string? SourceParticipantId,
    string? PartyId,
    string? ParticipantStatus,
    string? ParticipantBlockedReason,
    string? ParticipantDisplayKind,
    string? ParticipantEvidenceReference,
    string? ParticipantEvidenceFingerprint,
    IReadOnlyList<string> ParticipantAllowedReviewActions,
    string? ParticipantRedactionState)
{
    public bool IsParticipant => string.Equals(Kind, "Participant", StringComparison.Ordinal);
}
