namespace Hexalith.ChatBot.Contracts.Commands;

/// <summary>
/// Metadata-only evidence pointer used by association scoring; never raw mailbox or project payload.
/// </summary>
public sealed record AssociationEvidenceReference(
    string EvidenceReference,
    string EvidenceFingerprint,
    string EvidenceKind);
