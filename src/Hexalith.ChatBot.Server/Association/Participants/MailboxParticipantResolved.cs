using Hexalith.EventStore.Contracts.Events;

namespace Hexalith.ChatBot.Server.Association.Participants;

public sealed record MailboxParticipantResolved(
    string ResolutionId,
    string IntakeId,
    string SourceParticipantId,
    string PartyId,
    string PartyTenantId,
    string EvidenceReference,
    string EvidenceFingerprint,
    string SourceMailboxId,
    string SourceProvenance,
    string DerivationKernelVersion,
    string RedactionState,
    string RetentionClass,
    long SourceVersion,
    string SchemaVersion) : IEventPayload;
