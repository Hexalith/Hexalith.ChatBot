using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Commands;

/// <summary>
/// Durable participant resolution result containing stable PartyId authority and metadata-only evidence.
/// </summary>
public sealed record ResolvedMailboxParticipantReference(
    string SourceParticipantId,
    string PartyId,
    string PartyTenantId,
    string EvidenceReference,
    string EvidenceFingerprint,
    ParticipantResolutionStatus Status);
