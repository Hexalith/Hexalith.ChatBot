namespace Hexalith.ChatBot.Server.Projections;

/// <summary>Recorded after all subject and index writes for a delivered event have succeeded.</summary>
internal sealed record ReviewQueueCheckpoint(long Version, string? MessageId, string Digest, Dictionary<string, Contracts.Enums.OperationalQueueFamily> Subjects,
    string EventType, DateTimeOffset Timestamp, string? Correlation, Dictionary<string, string>? FactDigests = null);
