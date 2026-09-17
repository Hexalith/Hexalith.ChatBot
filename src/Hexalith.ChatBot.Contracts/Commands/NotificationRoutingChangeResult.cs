using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Commands;

public sealed record NotificationRoutingChangeResult(
    bool Accepted,
    string RoutingChangeId,
    string ActiveSnapshotRef,
    string ReasonCode);
