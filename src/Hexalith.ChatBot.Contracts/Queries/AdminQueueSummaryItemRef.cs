using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Contracts.Identities;

namespace Hexalith.ChatBot.Contracts.Queries;

public sealed record AdminQueueSummaryItemRef(
    string ItemRef,
    string Status,
    string OwnerClass,
    IReadOnlyList<string> DisabledActionReasonCodes);
