using Hexalith.EventStore.Contracts.Events;

namespace Hexalith.ChatBot.Server.Association;

public sealed record AssociationThresholdPolicyInvalidRejection(string? PolicyId, string ReasonCode) : IRejectionEvent;
