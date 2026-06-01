namespace Hexalith.ChatBot.Contracts.Queries;

public sealed record AiActionProposalRecord(
    string ProposalId,
    string TaskIntentId,
    string SourceMessageId,
    string? SourceConversationItemId,
    string RequesterId,
    string ReviewerId,
    IReadOnlyList<string> EvidenceReferences,
    string IntendedCommandName,
    string ActionKind,
    IReadOnlyList<string> AffectedResourceReferences,
    IReadOnlyList<string> RecipientReferences,
    string? PolicySnapshotId,
    long SourceVersion,
    string CorrelationId,
    string RedactionState,
    string RetentionClass,
    string SchemaVersion,
    string SafeNextAction,
    IReadOnlyDictionary<string, string>? ProposalInputMetadata = null);
