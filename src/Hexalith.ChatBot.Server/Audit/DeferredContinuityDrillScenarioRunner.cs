namespace Hexalith.ChatBot.Server.Audit;

/// <summary>
/// Fail-safe product default. Live fault authority is available only when the opted-in Tier-3 recovery harness
/// constructs its separate runner; ordinary product composition remains non-destructive.
/// </summary>
internal sealed class DeferredContinuityDrillScenarioRunner : IContinuityDrillScenarioRunner
{
    /// <inheritdoc />
    public ValueTask<ContinuityDrillMeasurement> RunAsync(
        string scenario,
        string testTenantRef,
        string correlationId,
        CancellationToken cancellationToken)
        => throw new NotSupportedException(
            "continuity-drill live fault injection requires the opted-in Tier-3 recovery harness");
}
