namespace Hexalith.ChatBot.UI.State.ProjectConversation;

public sealed record ProjectConversationItemStatusFacetModel(
    string Domain,
    string Health,
    string SourceState,
    string MessageCode,
    string SafeNextAction,
    IReadOnlyDictionary<string, string> SafeMetadataIds,
    bool NotApplicable,
    string? OperationId,
    string? CompletionStatus,
    string? ProjectionStatus,
    string? AuditStatus,
    string? CorrelationId,
    int? RetryCount,
    string? TerminalReasonCode,
    string? ResponsibleOwnerRole,
    string? DuplicateSafetyState);
