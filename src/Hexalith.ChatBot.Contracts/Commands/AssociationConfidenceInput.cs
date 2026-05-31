using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Commands;

/// <summary>
/// Machine-readable confidence contribution used by the deterministic association scorer.
/// </summary>
public sealed record AssociationConfidenceInput(
    AssociationSignalClass SignalClass,
    AssociationReasonCode ReasonCode,
    double Weight,
    string EvidenceReference,
    string EvidenceFingerprint);
