namespace Hexalith.ChatBot.UI.State.ProjectConversation;

public sealed record ProjectConversationDetectedIntentModel(
    string Summary,
    string ActionKind,
    IReadOnlyList<string> SourceEvidenceIds,
    string SafeNextAction,
    string MessageCode,
    string RedactionState);
