using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Queries;

public sealed record OperationalQueueRow(
    OperationalQueueFamily QueueFamily,
    string QueueRef,
    string ItemRef,
    string State,
    int AgeSeconds,
    string Risk,
    decimal Confidence,
    string? AssigneeRef,
    string NextAction,
    int RetryCount,
    bool IsTerminal,
    ChatBotHealthStatus Health,
    DateTimeOffset FreshnessTimestampUtc,
    string OwnerRole,
    IReadOnlyList<string> DisabledActionReasonCodes,
    OperationalQueueDiagnostics Diagnostics,
    string RedactionState,
    long SourceVersion,
    decimal PriorityScore,
    string PriorityExplanation,
    string? GroupKey = null,
    string? GroupRequesterRef = null,
    string? GroupCommandRef = null,
    string? GroupProjectRef = null);
