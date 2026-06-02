using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Identities;

/// <summary>
/// Metadata-only reference set for tenant-admin reads and queue-level operations.
/// </summary>
public sealed record AdminOperationReference(
    string AdminId,
    string ActorType,
    AdminScope ScopeUsed,
    string QueueRef,
    IReadOnlyList<string> ItemRefs,
    int ItemCount,
    string ReasonCode,
    string PolicySnapshotId,
    DateTimeOffset Timestamp,
    long SourceVersion,
    string RedactionState);
