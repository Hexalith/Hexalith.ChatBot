using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Server.Association.Scoring;

namespace Hexalith.ChatBot.Server.Adapters.Projects;

internal interface IProjectDirectory
{
    ValueTask<ProjectDirectoryAssociationResult> FindAuthorizedCandidatesAsync(
        ProjectDirectoryAssociationRequest request,
        CancellationToken cancellationToken = default);
}
