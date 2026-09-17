using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.EventStore.Contracts.Events;

namespace Hexalith.ChatBot.Server.Governance.Policy;

public sealed record NotificationRoutingChangeRejected(
    string RoutingChangeId,
    string ReasonCode,
    long? ExpectedSourceVersion,
    string CorrelationId) : IRejectionEvent;
