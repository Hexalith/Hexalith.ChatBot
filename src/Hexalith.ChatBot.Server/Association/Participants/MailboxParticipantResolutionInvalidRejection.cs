using Hexalith.EventStore.Contracts.Events;

namespace Hexalith.ChatBot.Server.Association.Participants;

public sealed record MailboxParticipantResolutionInvalidRejection(string? ResolutionId, string ReasonCode) : IRejectionEvent;
