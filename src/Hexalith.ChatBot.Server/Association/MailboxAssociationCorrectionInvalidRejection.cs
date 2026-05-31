using Hexalith.EventStore.Contracts.Events;

namespace Hexalith.ChatBot.Server.Association;

public sealed record MailboxAssociationCorrectionInvalidRejection(string? AssociationId, string ReasonCode) : IRejectionEvent;
