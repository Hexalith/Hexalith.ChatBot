namespace Hexalith.ChatBot.Server.Adapters.Conversations;

internal sealed record ConversationAppendResult(
    string Outcome,
    string AuditStatus,
    string GeneratedContentVisibility,
    string SafeNextAction,
    string? FailureCode = null,
    string? Retryability = null);
