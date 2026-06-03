using Hexalith.ChatBot.Server.Projections;

namespace Hexalith.ChatBot.Server.Audit;

/// <summary>
/// The scheduled production assertion behind NFR50a (Story 9.2, AC2): per tenant, over a rolling 7-day window, it
/// <b>rebuilds operation state from the WORM audit log and diffs it against the live projection</b>, returning the
/// fraction of state-mutating operations that are reconstructable end-to-end. This is the architecture's literal
/// definition of the completeness pillar — reconstructability, not field presence.
/// <para>
/// <b>Read-only, out-of-band (D4 two-phase audit).</b> The measurer only reads — <see cref="IWormAuditStore.EnumerateChain"/>
/// / <see cref="IWormAuditStore.EnumerateTenants"/> and the governed-operation projection. It performs no durable write,
/// adds no fail-closed gate, and never touches the commit path. Re-introducing a commit-time dependency would recreate
/// the NFR15a × NFR49a tension the two-phase model resolves.
/// </para>
/// <para>
/// <b>Replay exclusion (FR95a).</b> Replay/simulation envelopes (<see cref="AuditReplayExclusion.IsReplayEnvelope"/>)
/// are removed before grouping, so they count toward neither the numerator nor the denominator. Today there are zero in
/// production (replay execution is Story 9.4), so the exclusion holds by construction — but it is real and testable now.
/// </para>
/// <para>
/// <b>Fail-safe (Epic 8 no-fabrication).</b> If the chain or projection cannot be read, or enumeration/diff throws, the
/// tenant's result is <see cref="AuditCompletenessMeasurement.Unmeasurable"/> (a breach), never a fabricated 1.0 —
/// exactly as <see cref="AuditChainVerificationCoordinator"/> treats an incomplete verification.
/// </para>
/// <para>
/// <b>Tenant isolation by construction (NFR9a).</b> Each tenant is measured over its own chain and its own projection;
/// the projection read passes the tenant ref so no measurement can observe or link another tenant's records.
/// </para>
/// <para>
/// No always-on <c>BackgroundService</c> is introduced — consistent with the Epic 7/8/9.1 inert-control-floor pattern.
/// The reconstructor, this measurer, the budget evaluator, the gauge, and the alert coordinator are fully built and
/// tested; a periodic runtime (Dapr timer / PeriodicTimer) need only call <see cref="MeasureAllTenantsAsync"/> on its
/// cadence. This deferral is explicit, not a silent skip.
/// </para>
/// </summary>
internal sealed class AuditCompletenessMeasurer(
    IWormAuditStore wormStore,
    IGovernedOperationProjectionStore projectionStore,
    ISystemClock clock)
{
    /// <summary>
    /// Measures a single tenant's completeness over the rolling 7-day window ending now. Pure of side effects (read
    /// only). Returns the fraction, or a fail-safe <see cref="AuditCompletenessMeasurement.Unmeasurable"/> breach.
    /// </summary>
    public async ValueTask<AuditCompletenessMeasurement> MeasureTenantAsync(string tenantRef, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantRef);

        DateTimeOffset windowEndUtc = clock.UtcNow;
        DateTimeOffset windowStartUtc = windowEndUtc - AuditCompletenessMeasurement.RollingWindow;

        try
        {
            IReadOnlyList<WormAuditChainRecord> chain = wormStore.EnumerateChain(tenantRef);

            // FR95a: drop replay events, then keep only records inside the rolling window. Both numerator and
            // denominator are computed from this same in-scope set, so replay exclusion applies to both by construction.
            List<AuditEnvelope> inScope = [];
            foreach (WormAuditChainRecord record in chain)
            {
                AuditEnvelope envelope = record.Envelope;
                if (AuditReplayExclusion.IsReplayEnvelope(envelope))
                {
                    continue;
                }

                if (envelope.Timestamp >= windowStartUtc && envelope.Timestamp <= windowEndUtc)
                {
                    inScope.Add(envelope);
                }
            }

            // Group into operations by (resource ref + correlation), preserving first-seen chain order so the
            // first-diverging locator is deterministic.
            List<(string Key, List<AuditEnvelope> Envelopes)> operations = GroupIntoOperations(inScope);

            int total = operations.Count;
            int reconstructable = 0;
            string? firstDivergingLocator = null;

            foreach ((string _, List<AuditEnvelope> envelopes) in operations)
            {
                bool ok = await IsOperationReconstructableAsync(tenantRef, envelopes, cancellationToken).ConfigureAwait(false);
                if (ok)
                {
                    reconstructable++;
                }
                else
                {
                    firstDivergingLocator ??= OperationLocator(envelopes);
                }
            }

            // A completed window with zero in-scope operations is vacuously complete (1.0) — distinct from "cannot
            // complete" (which is the fail-safe Unmeasurable path below). It must not page anyone.
            double fraction = total == 0 ? 1.0 : (double)reconstructable / total;

            return new AuditCompletenessMeasurement(
                tenantRef,
                IsMeasurable: true,
                reconstructable,
                total,
                fraction,
                windowStartUtc,
                windowEndUtc,
                firstDivergingLocator,
                AuditCompletenessMeasurement.MeasuredReasonCode);
        }
        catch (Exception) when (cancellationToken is { IsCancellationRequested: false })
        {
            // Fail-safe: a measurement that cannot complete is a breach signal, never a fabricated 1.0.
            return AuditCompletenessMeasurement.Unmeasurable(tenantRef, windowStartUtc, windowEndUtc);
        }
    }

    /// <summary>
    /// Sweeps every tenant chain currently in the store (the seam a periodic scheduler calls on its cadence), mirroring
    /// <see cref="AuditChainVerificationCoordinator.VerifyAllTenantsAsync"/>. Aggregates per-tenant measurements into a
    /// coarse outcome; per-tenant results are obtained via <see cref="MeasureTenantAsync"/> so an unmeasurable tenant is
    /// counted as a breach, never skipped.
    /// </summary>
    public async ValueTask<IReadOnlyList<AuditCompletenessMeasurement>> MeasureAllTenantsAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<string> tenants = wormStore.EnumerateTenants();
        List<AuditCompletenessMeasurement> results = new(tenants.Count);
        foreach (string tenantRef in tenants)
        {
            results.Add(await MeasureTenantAsync(tenantRef, cancellationToken).ConfigureAwait(false));
        }

        return results;
    }

    private async ValueTask<bool> IsOperationReconstructableAsync(
        string tenantRef,
        IReadOnlyList<AuditEnvelope> envelopes,
        CancellationToken cancellationToken)
    {
        // Step 1 — rebuild from the chain alone (field discipline + path mapping + end-state assembly, AC1).
        AuditOperationReconstructionResult reconstruction = AuditOperationReconstructor.Reconstruct(envelopes);
        if (!reconstruction.IsReconstructable || reconstruction.State is not { } state)
        {
            return false;
        }

        // Step 2 — diff the rebuilt end-state against the LIVE projection (AC2). A missing projection, a cross-tenant
        // record, or a structural-token mismatch is divergence ⇒ not reconstructable. Read-only, tenant-scoped.
        GovernedOperationView? projection = await projectionStore
            .GetAsync(tenantRef, state.ResourceId, cancellationToken)
            .ConfigureAwait(false);

        if (projection is null)
        {
            return false;
        }

        if (!string.Equals(projection.TenantId, tenantRef, StringComparison.Ordinal))
        {
            return false;
        }

        return string.Equals(projection.RedactionState, state.ProjectionRedactionState, StringComparison.Ordinal);
    }

    // Group by (resource ref + correlation), preserving the order each operation key was first seen so the
    // first-diverging-operation locator is deterministic across runs over the same chain snapshot.
    private static List<(string Key, List<AuditEnvelope> Envelopes)> GroupIntoOperations(IReadOnlyList<AuditEnvelope> envelopes)
    {
        List<(string Key, List<AuditEnvelope> Envelopes)> operations = [];
        Dictionary<string, int> indexByKey = new(StringComparer.Ordinal);

        foreach (AuditEnvelope envelope in envelopes)
        {
            string key = $"{envelope.ResourceId}{envelope.CorrelationId}";
            if (!indexByKey.TryGetValue(key, out int index))
            {
                index = operations.Count;
                indexByKey[key] = index;
                operations.Add((key, []));
            }

            operations[index].Envelopes.Add(envelope);
        }

        return operations;
    }

    // A safe, bounded locator for the first diverging operation: the safe resource ref when available, else a stable
    // fallback. Never raw content (NFR2/NFR42).
    private static string OperationLocator(IReadOnlyList<AuditEnvelope> envelopes)
    {
        AuditEnvelope first = envelopes[0];
        return AuditMetadata.SafeOptionalToken(first.ResourceId) is { } safeResource
            ? $"op:{safeResource}"
            : "op:unresolved";
    }
}
