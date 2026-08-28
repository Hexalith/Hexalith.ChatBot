namespace Hexalith.ChatBot.Server.Adapters.Projects;

/// <summary>Resolves a Project to its single authorization-filtered Memories case reference.</summary>
internal interface IMemoriesCaseResolver
{
    ValueTask<string> ResolveCaseIdAsync(
        string tenantId,
        string projectId,
        string correlationId,
        CancellationToken cancellationToken = default);
}
