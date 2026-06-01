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
    string SourceConversationId,
    string? SourceThreadId,
    string SourceProvenance,
    string RedactionState,
    string RetentionClass,
    string SchemaVersion,
    long SourceVersion,
    string CorrelationId,
    string? ProjectId = null,
    string? ProjectDisplayName = null,
    string? DecisionLabel = null,
    string? SafeNextAction = null);
