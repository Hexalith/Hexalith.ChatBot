using Hexalith.ChatBot.Server.Audit;

namespace Hexalith.ChatBot.IntegrationTests.Recovery;

/// <summary>Preserves metadata-only live-run failures that the fail-safe coordinator deliberately reduces.</summary>
internal sealed class CapturingContinuityDrillScenarioRunner(IContinuityDrillScenarioRunner inner)
    : IContinuityDrillScenarioRunner
{
    private readonly List<Exception> _failures = [];

    public IReadOnlyList<Exception> Failures => _failures;

    /// <inheritdoc />
    public async ValueTask<ContinuityDrillMeasurement> RunAsync(
        string scenario,
        string testTenantRef,
        string correlationId,
        CancellationToken cancellationToken)
    {
        try
        {
            return await inner.RunAsync(scenario, testTenantRef, correlationId, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // Stable token only — do not embed exception.Message (can leak into uploaded .trx outside metadata-only reports).
            _failures.Add(new InvalidOperationException($"Live scenario '{scenario}' failed.", exception));
            throw;
        }
    }
}
