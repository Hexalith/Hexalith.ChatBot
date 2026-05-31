using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.EventStore.Contracts.Events;

namespace Hexalith.ChatBot.Server.Association.Participants;

public sealed record MailboxParticipantRejected(
    string ResolutionId,
    string IntakeId,
    string SourceParticipantId,
    ParticipantResolutionBlockedReason Reason,
    string EvidenceReference,
    string EvidenceFingerprint,
    string SchemaVersion) : IRejectionEvent;
