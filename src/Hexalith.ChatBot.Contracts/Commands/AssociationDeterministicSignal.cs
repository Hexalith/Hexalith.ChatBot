using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Commands;

/// <summary>
/// M0 deterministic association signal. Values are metadata-only and tenant authority is supplied by the gateway.
/// </summary>
public sealed record AssociationDeterministicSignal(
    AssociationSignalClass SignalClass,
    string ProjectId,
    string EvidenceReference,
    string EvidenceFingerprint,
    double Weight,
    bool RequiredForAutoAssociation);
