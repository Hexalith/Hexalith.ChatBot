namespace Hexalith.ChatBot.Server.Projections;

/// <summary>A committed request awaiting its exact failed event and version.</summary>
internal sealed record ReviewQueueRetryRequest(string RetryId, string EventId, long FailedVersion, string OperationClass,
    string Stream, long Version, DateTimeOffset At, string Correlation, string? Actor);
