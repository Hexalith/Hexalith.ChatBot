namespace Hexalith.ChatBot.Server.Adapters.Projects;

/// <summary>Fail-closed default used when the supported Projects client is not configured.</summary>
internal sealed class UnavailableMemoriesCaseResolver : IMemoriesCaseResolver
{
    public ValueTask<string> ResolveCaseIdAsync(
        string tenantId,
        string projectId,
        string correlationId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        throw new InvalidOperationException(ProjectsMemoriesCaseResolver.ContextUnavailableReasonCode);
    }
}
