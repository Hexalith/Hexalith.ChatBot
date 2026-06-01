using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Queries;

/// <summary>
/// Metadata-only project conversation item derived from governed email association state.
/// </summary>
public sealed record ProjectConversationItem(
    string ItemId,
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
    string? ProjectId = null,
    string? ProjectDisplayName = null,
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
    string? ParticipantRedactionState = null);
