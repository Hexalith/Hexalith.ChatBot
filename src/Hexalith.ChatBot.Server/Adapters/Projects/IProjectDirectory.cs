using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Server.Association.Scoring;

namespace Hexalith.ChatBot.Server.Adapters.Projects;

internal interface IProjectDirectory
{
    ValueTask<ProjectDirectoryAssociationResult> FindAuthorizedCandidatesAsync(
        ProjectDirectoryAssociationRequest request,
        CancellationToken cancellationToken = default);
}

internal sealed record ProjectDirectoryAssociationRequest(
    string TenantId,
    string SourceConversationId,
    string? SourceThreadId,
    IReadOnlyList<AssociationDeterministicSignal> Signals,
    string CorrelationId);

internal sealed record ProjectDirectoryAssociationResult(
    bool IsAvailable,
    IReadOnlyList<ProjectAssociationCandidateEvidence> Candidates,
    IReadOnlyList<AssociationExclusion> Exclusions)
{
    public static ProjectDirectoryAssociationResult Unavailable(IReadOnlyList<AssociationExclusion>? exclusions = null)
        => new(false, [], exclusions ?? []);
}
