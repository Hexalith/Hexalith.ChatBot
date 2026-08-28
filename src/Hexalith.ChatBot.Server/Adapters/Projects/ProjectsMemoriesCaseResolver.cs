using Generated = Hexalith.Projects.Client.Generated;

namespace Hexalith.ChatBot.Server.Adapters.Projects;

/// <summary>Projects-client implementation of the authoritative Project-to-Memories-case lookup.</summary>
internal sealed class ProjectsMemoriesCaseResolver(Generated.IClient projects) : IMemoriesCaseResolver
{
    public const string ContextUnavailableReasonCode = "project_context_unavailable";
    public const string ContextStaleReasonCode = "project_context_stale";
    public const string ContextMismatchReasonCode = "project_context_mismatch";
    public const string MemoryReferenceAmbiguousReasonCode = "project_memory_reference_ambiguous";

    public async ValueTask<string> ResolveCaseIdAsync(
        string tenantId,
        string projectId,
        string correlationId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(projectId);
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);

        Generated.ProjectContext context;
        try
        {
            context = await projects
                .GetProjectContextAsync(projectId, correlationId, null, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex) when (ex is Generated.HexalithProjectsApiException
            or HttpRequestException
            or InvalidOperationException)
        {
            throw new InvalidOperationException(ContextUnavailableReasonCode, ex);
        }

        if (!string.Equals(context.ProjectId, projectId, StringComparison.Ordinal)
            || context.AssemblyOutcome is not Generated.ProjectContextAssemblyOutcome.Assembled
            || context.Lifecycle is not Generated.ProjectContextLifecycle.Active)
        {
            throw new InvalidOperationException(ContextMismatchReasonCode);
        }

        if (context.Freshness is not Generated.ProjectContextFreshness.Fresh)
        {
            throw new InvalidOperationException(ContextStaleReasonCode);
        }

        Generated.ProjectContextReference[] memories =
        [.. context.MemoryReferences.Where(static reference =>
            reference.ReferenceKind is Generated.ProjectContextReferenceReferenceKind.Memory
            && reference.ReferenceState is Generated.ProjectContextReferenceReferenceState.Included
            && !string.IsNullOrWhiteSpace(reference.ReferenceId))];
        if (memories.Length != 1)
        {
            throw new InvalidOperationException(MemoryReferenceAmbiguousReasonCode);
        }

        return memories[0].ReferenceId;
    }
}
