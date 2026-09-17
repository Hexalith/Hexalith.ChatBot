using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Queries;

public sealed record OperationalQueueSearchResult(
    IReadOnlyList<OperationalQueueRow> Rows,
    string? NextPageToken,
    int PageSize,
    int TotalCount,
    string StableFilterFingerprint,
    string SchemaVersion,
    string CorrelationId);
