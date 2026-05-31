using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.EventStore.Contracts.Events;

namespace Hexalith.ChatBot.Server.Association.Participants;

public sealed record MailboxParticipantUnresolved(
    string ResolutionId,
    string IntakeId,
    string SourceParticipantId,
    string EvidenceReference,
    string EvidenceFingerprint,
    ParticipantResolutionBlockedReason Reason,
    IReadOnlyList<ParticipantReviewAction> AllowedReviewActions,
    string SourceMailboxId,
    string SourceProvenance,
    string DerivationKernelVersion,
    string RedactionState,
    string RetentionClass,
    long SourceVersion,
    string SchemaVersion) : IEventPayload;
