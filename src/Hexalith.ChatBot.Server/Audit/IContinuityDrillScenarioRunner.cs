namespace Hexalith.ChatBot.Server.Audit;

/// <summary>
/// The seam the <see cref="ContinuityDrillCoordinator"/> consumes to actually run a recovery scenario and return its
/// measured RPO/RTO + data-loss check (Story 9.11, AC1). The recovery measurement is real and the coordinator/evaluator/
/// report are fully built and tested against a deterministic scripted fake; the <b>live fault-injection runtime</b> that
/// downs a real EventStore / lapses a real M365 Graph subscription against a deployed AKS/Aspire environment is
/// <b>M2-deferred</b>, exactly as Story 9.4 deferred the replay driver.
/// <para>
/// <b>Test-tenant only.</b> A live runner must run <b>only</b> against a tenant for which
/// <see cref="ReplayTenantPolicy.IsTestTenant"/> is true and must <b>never</b> touch a production tenant's durable
/// state — recovery is isolated by construction because the drill lands only in the test tenant's partition (NFR9a).
/// </para>
/// </summary>
internal interface IContinuityDrillScenarioRunner
{
    /// <summary>
    /// Runs the recovery exercise for <paramref name="scenario"/> against the test tenant and returns the measured
    /// result (started/ended bounds, measured RPO/RTO, data-loss check).
    /// </summary>
    /// <param name="scenario">A <see cref="ContinuityDrillScenarios"/> token.</param>
    /// <param name="testTenantRef">The test tenant the drill runs against (must satisfy <see cref="ReplayTenantPolicy.IsTestTenant"/>).</param>
    /// <param name="correlationId">The run correlation id.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The measured drill result.</returns>
    ValueTask<ContinuityDrillMeasurement> RunAsync(
        string scenario,
        string testTenantRef,
        string correlationId,
        CancellationToken cancellationToken);
}

/// <summary>
/// The inert default <see cref="IContinuityDrillScenarioRunner"/> registered until the live fault-injection runtime is
/// built (Story 9.11 inert-control-floor; mirrors Story 9.4's deferred replay-driver discipline). It throws
/// <see cref="NotSupportedException"/> so the seam is wired but unmistakably not yet live — the coordinator's fail-safe
/// catch maps the throw to an <c>unmeasurable</c> report rather than a fabricated <c>met</c>. Tests inject a
/// deterministic scripted fake instead.
/// </summary>
internal sealed class DeferredContinuityDrillScenarioRunner : IContinuityDrillScenarioRunner
{
    /// <inheritdoc />
    public ValueTask<ContinuityDrillMeasurement> RunAsync(
        string scenario,
        string testTenantRef,
        string correlationId,
        CancellationToken cancellationToken)
        => throw new NotSupportedException("continuity-drill live fault-injection runtime is M2-deferred");
}
