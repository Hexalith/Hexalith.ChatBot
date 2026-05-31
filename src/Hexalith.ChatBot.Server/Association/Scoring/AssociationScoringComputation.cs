using Hexalith.ChatBot.Contracts.Commands;

namespace Hexalith.ChatBot.Server.Association.Scoring;

internal sealed record AssociationScoringComputation(
    AssociationScoringResult Result,
    IReadOnlyList<AssociationCandidate> Candidates,
    IReadOnlyList<AssociationExclusion> Exclusions);
