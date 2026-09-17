using System.Security.Cryptography;
using System.Text;

using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Contracts.Queries;
using Hexalith.ChatBot.Server.Projections;

namespace Hexalith.ChatBot.Server.Lifecycle.Attachments;

internal interface IProjectAiContextPackageAssembler
{
    ValueTask<ProjectAiContextPackage> AssembleAsync(
        ProjectAiContextPackageAssemblyRequest request,
        CancellationToken cancellationToken = default);
}
