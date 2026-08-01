namespace Hexalith.ChatBot.Server.Audit;

/// <summary>
/// The seam the <see cref="ScopedOutageDegradationValidationCoordinator"/> consumes to actually inject a dependency
/// outage and return its measured degradation scope + the NFR58/NFR59/NFR17/NFR13/NFR41 assertions (Story 9.13, AC1). The
/// evaluator/coordinator/report/alert path are fully built and tested. Story 12.15 supplies the live Tier-3
/// implementation for all six closed dependency boundaries. Product DI retains a separate inert default so ordinary
/// deployments never acquire outage-injection authority.
/// <para>
/// A live implementation injects an outage of <c>dependency</c> against the test tenant, observes the <b>actual
/// degradation scope</b>, runs the three NFR59 assertions (cross-tenant leakage, unauthorized mutation, silent data
/// loss), the NFR58 scope-containment check, the NFR17/NFR13 recovery checks (in-flight items resume recoverable, no
/// duplicate side effects), and measures the NFR41 detection→scope-recording latency — returning the
/// <see cref="ScopedOutageDegradationMeasurement"/>.
/// </para>
/// <para>
/// <b>Test-tenant only.</b> A live driver must run <b>only</b> against a tenant for which
/// <see cref="ReplayTenantPolicy.IsTestTenant"/> is true and must <b>never</b> touch a production tenant's durable state
/// — the outage and its recovery land only in the test tenant's partition (NFR9a).
/// </para>
/// </summary>
internal interface IScopedOutageInjectionDriver
{
    /// <summary>
    /// Injects an outage of <paramref name="dependency"/> against the test tenant and returns the measured result
    /// (expected/observed scope, the three NFR59 assertions, the recovery checks, the measured scope-recording latency,
    /// and the wall-clock bounds).
    /// </summary>
    /// <param name="dependency">A <see cref="ScopedOutageDependencies"/> token.</param>
    /// <param name="testTenantRef">The test tenant the outage runs against (must satisfy <see cref="ReplayTenantPolicy.IsTestTenant"/>).</param>
    /// <param name="correlationId">The run correlation id.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The measured scoped-outage degradation result.</returns>
    ValueTask<ScopedOutageDegradationMeasurement> InjectAndMeasureAsync(
        string dependency,
        string testTenantRef,
        string correlationId,
        CancellationToken cancellationToken);
}
