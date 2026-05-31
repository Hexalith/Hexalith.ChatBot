using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Server.Projections;

internal sealed record ParticipantResolutionNotification(
    string TenantId,
    string ResolutionId,
    string IntakeId,
    string SourceMailboxId,
    string SourceParticipantId,
    string? PartyId,
    ParticipantResolutionStatus Status,
    ParticipantResolutionBlockedReason? Reason,
    string EvidenceReference,
    string EvidenceFingerprint,
    long SourceVersion,
    DateTimeOffset RecordedAt,
    string CorrelationId);
