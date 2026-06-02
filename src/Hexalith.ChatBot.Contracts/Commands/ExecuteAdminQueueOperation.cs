using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Commands;

/// <summary>
/// Queue-level tenant-admin operation. The gateway supplies tenant/admin authority; payload refs stay metadata-only.
/// </summary>
public sealed record ExecuteAdminQueueOperation(
    string OperationId,
    AdminQueueOperation Operation,
    AdminScope ScopeUsed,
    string QueueRef,
    IReadOnlyList<string> ItemRefs,
    int ItemCount,
    string ReasonCode,
    string PolicySnapshotId,
    long SourceVersion,
    string RedactionState,
    OperationalQueueFamily? QueueFamily = null,
    string? AssigneeRef = null,
    string? ReviewerRef = null,
    string? PreviousAssigneeRef = null,
    DateTimeOffset? CommandTimestampUtc = null,
    string? OperationState = null) : IChatBotCommand;
