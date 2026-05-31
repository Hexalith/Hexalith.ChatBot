using Hexalith.EventStore.Contracts.Events;

namespace Hexalith.ChatBot.Server.Association;

public sealed record MailboxAssociationInvalidRejection(string? AssociationId, string ReasonCode) : IRejectionEvent;
