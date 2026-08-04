using Hexalith.ChatBot.Server.Audit;

namespace Hexalith.ChatBot.IntegrationTests.Recovery;

/// <summary>Preserves metadata-only live rebuild failures that the fail-safe coordinator deliberately reduces.</summary>
internal sealed class CapturingProjectionRebuildDriver(IProjectionRebuildDriver inner) : IProjectionRebuildDriver
{
    private readonly List<Exception> _failures = [];

    public IReadOnlyList<Exception> Failures => _failures;

    /// <inheritdoc />
    public async ValueTask<ProjectionRebuildMeasurement> RebuildAsync(
        string testTenantRef,
        string datasetRef,
        string correlationId,
        CancellationToken cancellationToken)
    {
        try
        {
            return await inner
                .RebuildAsync(testTenantRef, datasetRef, correlationId, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            _failures.Add(new InvalidOperationException($"Live projection rebuild '{datasetRef}' failed.", exception));
            throw;
        }
    }
}
