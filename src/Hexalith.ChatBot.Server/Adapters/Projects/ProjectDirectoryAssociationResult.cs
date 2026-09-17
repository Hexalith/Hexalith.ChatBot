using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Server.Association.Scoring;

namespace Hexalith.ChatBot.Server.Adapters.Projects;

internal sealed record ProjectDirectoryAssociationResult(
    bool IsAvailable,
    IReadOnlyList<ProjectAssociationCandidateEvidence> Candidates,
    IReadOnlyList<AssociationExclusion> Exclusions)
{
    public static ProjectDirectoryAssociationResult Unavailable(IReadOnlyList<AssociationExclusion>? exclusions = null)
        => new(false, [], exclusions ?? []);
}
