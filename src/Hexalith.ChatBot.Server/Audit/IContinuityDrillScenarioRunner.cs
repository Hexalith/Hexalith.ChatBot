namespace Hexalith.ChatBot.Server.Audit;

/// <summary>
/// The seam the <see cref="ContinuityDrillCoordinator"/> consumes to actually run a recovery scenario and return its
/// measured RPO/RTO + data-loss check (Story 9.11, AC1). Story 12.15 supplies the live Tier-3 implementation that
/// stops the Aspire EventStore resource and faults the topology-composed subscription boundary. Product DI retains a
/// separate inert default so ordinary deployments never acquire fault authority.
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
