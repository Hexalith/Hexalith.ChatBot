using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Server.Adapters.Projects;

internal sealed class UnavailableProjectDirectory : IProjectDirectory
{
    public ValueTask<ProjectDirectoryAssociationResult> FindAuthorizedCandidatesAsync(
        ProjectDirectoryAssociationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        AssociationExclusion[] exclusions = request.Signals
            .Where(static signal => !string.IsNullOrWhiteSpace(signal.ProjectId))
            .Select(static signal => new AssociationExclusion(
                signal.ProjectId,
                AssociationExclusionState.Unavailable,
                AssociationReasonCode.AuthorizationEvidenceUnavailable,
                signal.EvidenceReference,
                signal.EvidenceFingerprint))
            .ToArray();

        return ValueTask.FromResult(ProjectDirectoryAssociationResult.Unavailable(exclusions));
    }
}
