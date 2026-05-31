namespace Hexalith.ChatBot.Contracts.Commands;

/// <summary>
/// Source participant metadata used as evidence for resolution. Provider address/name values are input
/// evidence only and are not authority.
/// </summary>
public sealed record MailboxParticipantSourceReference(
    string SourceParticipantId,
    string Role,
    string EvidenceReference,
    string EvidenceFingerprint,
    string AddressEvidence,
    string? DisplayNameEvidence);
