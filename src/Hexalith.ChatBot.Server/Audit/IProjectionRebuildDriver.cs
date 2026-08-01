namespace Hexalith.ChatBot.Server.Audit;

/// <summary>
/// The seam the <see cref="ProjectionRebuildValidationCoordinator"/> consumes to actually rebuild a tenant's derived
/// projections and return the pre-rebuild + rebuilt structural snapshots, the measured duration, and the stamped schema
/// versions (Story 9.12, AC1). The equivalence evaluation, the duration-vs-target check, the report, the alert path, and
/// the gate outcome are real and fully tested. Story 12.15 supplies the live Tier-3 implementation that rebuilds an
/// isolated partition from immutable source metadata plus WORM history. Product DI retains a separate inert default so
/// ordinary deployments never initiate validation rebuilds.
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
