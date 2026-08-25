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
        // An internal deadline (a per-scenario CTS, a restoration budget, or an HttpClient timeout, which surfaces
        // as TaskCanceledException) is a genuine scenario failure, not a caller cancellation — but the previous
        // filter excluded every OperationCanceledException, so such a failure was dropped here and reduced by the
        // fail-safe coordinator to an unmeasurable report with no retained cause. Only a cancellation that the
        // CALLER actually requested is passed through silently.
        catch (Exception exception) when (exception is not OperationCanceledException
            || !cancellationToken.IsCancellationRequested)
        {
            _failures.Add(new InvalidOperationException($"Live projection rebuild '{datasetRef}' failed.", exception));
            throw;
        }
    }
}
