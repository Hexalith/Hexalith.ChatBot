namespace Hexalith.ChatBot.Server.Audit;

/// <summary>
/// The seam the <see cref="ProjectionRebuildValidationCoordinator"/> consumes to actually rebuild a tenant's derived
/// projections and return the pre-rebuild + rebuilt structural snapshots, the measured duration, and the stamped schema
/// versions (Story 9.12, AC1). The equivalence evaluation, the duration-vs-target check, the report, the alert path, and
/// the gate outcome are real and fully tested against a deterministic scripted fake; the <b>live rebuild runtime</b> that
/// replays a tenant's immutable source records + WORM history into a fresh projection store against a deployed
/// AKS/Aspire environment is <b>M2-deferred</b>, exactly as Story 9.4 deferred the replay driver and 9.11 deferred the
/// fault-injection runtime.
/// <para>
/// <b>Rebuilds from the immutable source-of-record, not from mailboxes (the defining NFR57 property).</b> A live
/// implementation rebuilds <b>only</b> from (a) the tenant's immutable source records
/// (<c>ProjectConversationSourceEmailView</c> — the retained <c>source-email-metadata</c> data class, Story 9.7) and
/// (b) the tenant's WORM audit history (<c>IWormAuditStore.EnumerateChain</c>, the D4 source of truth). It does
/// <b>not</b> call Graph, does <b>not</b> re-fetch any mailbox, and does <b>not</b> re-query <i>current</i> upstream
/// Party/Folder/sibling-context data (as-of resolution, architecture invariant #11) — that would make the rebuild
/// diverge from the original as-of state.
/// </para>
/// <para>
/// <b>Test-tenant only.</b> A live driver must run <b>only</b> against a tenant for which
/// <see cref="ReplayTenantPolicy.IsTestTenant"/> is true and must <b>never</b> mutate a production tenant's durable
/// projection state — the rebuild is isolated by construction because it lands only in the test tenant's partition
/// (NFR9a/NFR59).
/// </para>
/// </summary>
internal interface IProjectionRebuildDriver
{
    /// <summary>
    /// Rebuilds the test tenant's derived projections for the baseline validation <paramref name="datasetRef"/> from its
    /// immutable source records + WORM audit history and returns the measured result (started/ended bounds, measured
    /// rebuild duration, pre-rebuild + rebuilt structural snapshots, stamped schema versions).
    /// </summary>
    /// <param name="testTenantRef">The test tenant the rebuild runs against (must satisfy <see cref="ReplayTenantPolicy.IsTestTenant"/>).</param>
    /// <param name="datasetRef">The baseline validation dataset id.</param>
    /// <param name="correlationId">The run correlation id.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The measured rebuild result.</returns>
    ValueTask<ProjectionRebuildMeasurement> RebuildAsync(
        string testTenantRef,
        string datasetRef,
        string correlationId,
        CancellationToken cancellationToken);
}

/// <summary>
/// The inert default <see cref="IProjectionRebuildDriver"/> registered until the live rebuild runtime is built (Story
/// 9.12 inert-control-floor; mirrors Story 9.4's deferred replay-driver and Story 9.11's
/// <c>DeferredContinuityDrillScenarioRunner</c> discipline). It throws <see cref="NotSupportedException"/> so the seam is
/// wired but unmistakably not yet live — the coordinator's fail-safe catch maps the throw to an <c>unmeasurable</c>
/// report rather than a fabricated <c>equivalent</c>. Tests inject a deterministic scripted fake instead.
/// </summary>
internal sealed class DeferredProjectionRebuildDriver : IProjectionRebuildDriver
{
    /// <inheritdoc />
    public ValueTask<ProjectionRebuildMeasurement> RebuildAsync(
        string testTenantRef,
        string datasetRef,
        string correlationId,
        CancellationToken cancellationToken)
        => throw new NotSupportedException("projection-rebuild live driver is M2-deferred");
}
