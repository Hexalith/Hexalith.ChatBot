using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Contracts.Identities;

namespace Hexalith.ChatBot.Contracts.Queries;

public sealed record AdminQueueSummaryBucket(
    string Status,
    string OwnerClass,
    int Count,
    int OldestAgeSeconds);
