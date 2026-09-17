using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Queries;

public sealed record SearchOperationalQueueItems(
    OperationalQueueFamily QueueFamily,
    int? PageSize,
    string? PageToken,
    OperationalQueueSortKey SortKey,
    bool SortDescending,
    OperationalQueueFilter Filter);
