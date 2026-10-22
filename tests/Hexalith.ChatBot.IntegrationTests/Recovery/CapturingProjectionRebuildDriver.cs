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
        // Suppress only cancellation of this exact caller token. An unrelated internal deadline remains a
        // diagnostic failure even when the caller happens to cancel at the same time.
        catch (Exception exception) when (exception is not OperationCanceledException canceled
            || !cancellationToken.IsCancellationRequested
            || canceled.CancellationToken != cancellationToken)
        {
            _failures.Add(new InvalidOperationException($"Live projection rebuild '{datasetRef}' failed.", exception));
            throw;
        }
    }
}
