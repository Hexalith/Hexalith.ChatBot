using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Commands;

/// <summary>
/// Authorized association candidate ranked by the deterministic scorer.
/// </summary>
public sealed record AssociationCandidate(
    string ProjectId,
    string? DisplayName,
    double ConfidenceScore,
    int Rank,
    IReadOnlyList<AssociationReasonCode> ReasonCodes,
    IReadOnlyList<AssociationEvidenceReference> EvidenceRefs,
    IReadOnlyList<AssociationConfidenceInput> ConfidenceInputs,
    bool RequiredEvidenceComplete);
