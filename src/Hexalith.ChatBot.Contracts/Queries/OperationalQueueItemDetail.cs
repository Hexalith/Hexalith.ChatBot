using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Queries;

public sealed record OperationalQueueItemDetail(
    OperationalQueueRow Summary,
    string DetailAccessState,
    string SafeDetailStatus,
    IReadOnlyList<string> EscalationActions);
