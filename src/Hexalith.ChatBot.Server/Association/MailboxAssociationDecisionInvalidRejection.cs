using Hexalith.EventStore.Contracts.Events;

namespace Hexalith.ChatBot.Server.Association;

public sealed record MailboxAssociationDecisionInvalidRejection(string? AssociationId, string ReasonCode) : IRejectionEvent;
