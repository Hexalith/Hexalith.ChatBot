using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Queries;

public sealed record GetOperationalQueueItemDetail(
    OperationalQueueFamily QueueFamily,
    string QueueRef,
    string ItemRef,
    long SourceVersion);
