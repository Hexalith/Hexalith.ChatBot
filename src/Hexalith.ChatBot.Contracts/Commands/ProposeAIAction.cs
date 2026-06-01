namespace Hexalith.ChatBot.Contracts.Commands;

public sealed record ProposeAIAction(
    string ProjectId,
    string TaskIntentId,
    string SourceMessageId,
    string RequesterId,
    string IntendedCommandName,
    string ActionKind,
    long ExpectedSourceVersion,
    IReadOnlyList<string> EvidenceReferences,
    IReadOnlyList<string> AffectedResourceReferences,
    IReadOnlyList<string> RecipientReferences,
    string? PolicySnapshotId,
    string CorrelationId,
    string TransitionId,
    string? SourceConversationItemId = null,
    IReadOnlyDictionary<string, string>? ProposalInputMetadata = null,
    string RedactionState = "metadata_only",
    string RetentionClass = "collaboration_input",
    string SchemaVersion = "chatbot.ai-action-proposal.v1") : IChatBotCommand;
