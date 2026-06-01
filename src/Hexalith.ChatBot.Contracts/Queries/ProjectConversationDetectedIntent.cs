using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Queries;

public sealed record ProjectConversationDetectedIntent(
    string Summary,
    ProjectConversationDetectedActionKind ActionKind,
    IReadOnlyList<string> SourceEvidenceIds,
    string SafeNextAction,
    string MessageCode,
    string RedactionState);
