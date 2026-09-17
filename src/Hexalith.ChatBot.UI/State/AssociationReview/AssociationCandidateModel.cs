using Hexalith.ChatBot.UI.Design;

namespace Hexalith.ChatBot.UI.State.AssociationReview;

public sealed record AssociationCandidateModel(
    string ProjectId,
    string DisplayLabel,
    double ConfidenceScore,
    int Rank,
    IReadOnlyList<string> ReasonCodes,
    IReadOnlyList<AssociationEvidenceModel> Evidence,
    bool RequiredEvidenceComplete);
