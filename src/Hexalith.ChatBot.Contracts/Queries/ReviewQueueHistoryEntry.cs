namespace Hexalith.ChatBot.Contracts.Queries;

/// <summary>Attributable metadata from a committed queue transition.</summary>
public sealed record ReviewQueueHistoryEntry(string State, DateTimeOffset OccurredAtUtc, bool Terminal, string? Reason,
    string OperationId, string CorrelationId, long SourceVersion, int RetryCount, string? SourceStreamId = null, string? Actor = null);
