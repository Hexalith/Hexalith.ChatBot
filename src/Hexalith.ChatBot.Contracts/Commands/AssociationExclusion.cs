using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Commands;

/// <summary>
/// Safe exclusion row for candidates or deterministic evidence that cannot be surfaced as authorized.
/// </summary>
public sealed record AssociationExclusion(
    string ProjectId,
    AssociationExclusionState State,
    AssociationReasonCode ReasonCode,
    string EvidenceReference,
    string EvidenceFingerprint);
