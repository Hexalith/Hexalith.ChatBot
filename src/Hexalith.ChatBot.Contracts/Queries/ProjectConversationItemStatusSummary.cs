using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Queries;

public sealed record ProjectConversationItemStatusSummary(
    IReadOnlyList<ProjectConversationItemStatusFacet> Facets);

public sealed record ProjectConversationItemStatusFacet(
    string Domain,
    ChatBotHealthStatus Health,
    string SourceState,
    string MessageCode,
    string SafeNextAction,
    IReadOnlyDictionary<string, string>? SafeMetadataIds = null,
    bool NotApplicable = false,
    string? OperationId = null,
    string? CompletionStatus = null,
    string? ProjectionStatus = null,
    string? AuditStatus = null,
    string? CorrelationId = null,
    int? RetryCount = null,
    string? TerminalReasonCode = null,
    string? ResponsibleOwnerRole = null,
    string? DuplicateSafetyState = null);
