using Hexalith.ChatBot.Server.Gateway.Stages;
using Hexalith.ChatBot.Server.Projections.DerivedStores;

namespace Hexalith.ChatBot.Server.Audit;

/// <summary>The result of a derived-store cross-tenant isolation sweep: ordered pairs probed, breached, and alerted.</summary>
internal sealed record DerivedStoreIsolationProbeOutcome(int PartitionsProbed, int Breaches, int Alerted);

/// <summary>
/// Injectable synthetic cross-tenant isolation probe coordinator (Story 9.5, AC2, FR55a/NFR9a/NFR59) following the
/// <see cref="ReplayIsolationProbeCoordinator"/> discipline <b>exactly</b>: a pure evaluator
/// (<see cref="DerivedStoreIsolationVerifier"/>) followed by a fail-closed audit-then-deliver step. For each ordered
/// tenant pair <c>(owner, intruder)</c> drawn from <see cref="IDerivedStore.EnumerateTenants"/>, it <b>actively attempts
/// a cross-tenant read</b>: it seeds a reserved <see cref="SentinelResourceIdPrefix"/> sentinel into each of the owner's
/// four derived-store partitions, then reads those exact sentinel ids back <b>through the intruder tenant's scope</b>. A
/// successful cross-tenant read (the intruder observes the owner's sentinel) is a breach; the verifier confirms it.
/// <para>
/// On any breach it does fail-closed audit-then-deliver: it writes the metadata-only
/// <see cref="AuditEnvelopeFactory.DerivedStoreIsolationBreach"/> pre-commit envelope <b>before</b> emitting exactly one
/// <see cref="OperatorAlertKind.DerivedStoreIsolationBreach"/> alert through the existing <see cref="IOperatorAlertSink"/>
/// (if audit is unavailable, no alert fires — no observable side effect — but the breach still surfaces to the caller).
/// A seed or read-back that throws maps to <see cref="DerivedStoreIsolationStatus.Unknown"/> — a breach signal — never a
/// silent pass.
/// </para>
/// <para>
/// <b>M2 release gate.</b> <see cref="SweepAllTenantPairsAsync"/> returns a structured
/// <see cref="DerivedStoreIsolationProbeOutcome"/> a CI/release gate asserts against: zero breaches ⇒ the M2 release may
/// proceed; any breach is stop-ship. Story 12.14 wires <see cref="SweepAllTenantPairsAsync"/> into the existing
/// periodic-enforcement <c>BackgroundService</c> as the nightly <c>derived-store-isolation-probe</c> sweep when M2
/// audit/recovery sweeps are enabled. No second hosted scheduler is introduced.
/// </para>
/// <para>
/// The probe seeds into the live store, so the sentinel resource ids are deliberately a reserved, unambiguous probe
/// artifact (the <see cref="SentinelResourceIdPrefix"/> prefix and a metadata-only digest), never mistakable for
/// production data.
/// </para>
/// </summary>
internal sealed class DerivedStoreIsolationProbeCoordinator(
    IDerivedStore derivedStore,
    IAuditWriter auditWriter,
    IOperatorAlertSink operatorAlertSink,
    ISystemClock clock)
{
    /// <summary>The reserved resource-id prefix marking a synthetic probe sentinel — unambiguously a probe artifact.</summary>
    public const string SentinelResourceIdPrefix = "iso-probe:";

    /// <summary>The metadata-only digest stored on a probe sentinel entry (no content, clearly a probe artifact).</summary>
    public const string SentinelContentDigest = "iso-probe-sentinel";

    /// <summary>
    /// Probes a single ordered tenant pair and, on breach, audits-then-alerts. Returns the metadata-only result.
    /// </summary>
    /// <param name="ownerTenant">The tenant whose partitions are seeded with sentinels.</param>
    /// <param name="intruderTenant">The tenant whose scope attempts the cross-tenant read.</param>
    /// <param name="correlationId">The run correlation id (also the sentinel discriminator).</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The pair-probe result.</returns>
    public async ValueTask<DerivedStoreIsolationVerificationResult> ProbePairAndAlertAsync(
        string ownerTenant,
        string intruderTenant,
        string correlationId,
        CancellationToken cancellationToken)
    {
        (DerivedStoreIsolationVerificationResult result, _) = await ProbePairInternalAsync(
            ownerTenant,
            intruderTenant,
            correlationId,
            cancellationToken)
            .ConfigureAwait(false);
        return result;
    }

    private async ValueTask<(DerivedStoreIsolationVerificationResult Result, bool Alerted)> ProbePairInternalAsync(
        string ownerTenant,
        string intruderTenant,
        string correlationId,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerTenant);
        ArgumentException.ThrowIfNullOrWhiteSpace(intruderTenant);
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);

        DerivedStoreIsolationVerificationResult result;
        try
        {
            List<string> sentinelIds = [];
            List<string> observableThroughIntruder = [];

            foreach (DerivedStoreClass cls in DerivedStorePartition.AllClasses)
            {
                string sentinelId = SentinelResourceId(cls, ownerTenant, correlationId);
                sentinelIds.Add(sentinelId);

                // Seed the sentinel into the OWNER's partition.
                await derivedStore
                    .PutAsync(cls, ownerTenant, sentinelId, DerivedStoreEntry.Create(sentinelId, SentinelContentDigest), cancellationToken)
                    .ConfigureAwait(false);

                // Attempt to read the owner's sentinel back THROUGH the intruder's scope. A non-null read means the
                // intruder physically observed the owner's data — a store-layer isolation breach.
                DerivedStoreEntry? throughIntruder = await derivedStore
                    .GetAsync(cls, intruderTenant, sentinelId, cancellationToken)
                    .ConfigureAwait(false);
                if (throughIntruder is not null)
                {
                    observableThroughIntruder.Add(sentinelId);
                }
            }

            result = DerivedStoreIsolationVerifier.Verify(ownerTenant, intruderTenant, sentinelIds, observableThroughIntruder);
        }
        catch (Exception) when (cancellationToken is { IsCancellationRequested: false })
        {
            // Fail-closed: a probe that cannot complete is itself a breach signal, never silent success.
            result = new DerivedStoreIsolationVerificationResult(
                ownerTenant,
                intruderTenant,
                DerivedStoreIsolationStatus.Unknown,
                DerivedStoreIsolationVerificationResult.ProbeIncompleteReasonCode,
                FirstOffenderLocator: null);
        }

        if (!result.IsBreach)
        {
            return (result, false);
        }

        DateTimeOffset now = clock.UtcNow;

        // Fail-closed audit-then-deliver (NFR15a): write the breach's pre-commit audit envelope before alerting. If the
        // audit is unavailable, no operator alert is emitted — no observable side effect — but the breach is still
        // surfaced to the caller.
        AuditEnvelope envelope = AuditEnvelopeFactory.DerivedStoreIsolationBreach(result, correlationId, now);
        AuditWriteResult auditResult = await auditWriter
            .RecordPreCommitAsync(envelope, cancellationToken)
            .ConfigureAwait(false);
        if (!auditResult.Succeeded)
        {
            return (result, false);
        }

        await operatorAlertSink
            .EmitAsync(
                new OperatorAlert(
                    OperatorAlertKind.DerivedStoreIsolationBreach,
                    result.ReasonCode,
                    result.OwnerTenantRef,
                    "DerivedStoreIsolationBreach",
                    correlationId,
                    now,
                    result.FirstOffenderLocator),
                cancellationToken)
            .ConfigureAwait(false);

        return (result, true);
    }

    /// <summary>
    /// Sweeps every ordered tenant pair drawn from the derived store (the method a periodic scheduler AND the M2 release
    /// gate call). For each pair it seeds the owner's partitions and attempts the cross-tenant read through the intruder.
    /// Returns the structured outcome the release gate asserts against — zero breaches ⇒ the M2 release may proceed.
    /// </summary>
    /// <param name="runCorrelationId">The run correlation id applied to every pair-probe and sentinel.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The structured probe outcome.</returns>
    public async ValueTask<DerivedStoreIsolationProbeOutcome> SweepAllTenantPairsAsync(
        string runCorrelationId,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(runCorrelationId);

        IReadOnlyList<string> tenants = derivedStore.EnumerateTenants();

        int partitionsProbed = 0;
        int breaches = 0;
        int alerted = 0;
        foreach (string ownerTenant in tenants)
        {
            foreach (string intruderTenant in tenants)
            {
                if (string.Equals(ownerTenant, intruderTenant, StringComparison.Ordinal))
                {
                    continue;
                }

                partitionsProbed++;
                (DerivedStoreIsolationVerificationResult result, bool didAlert) = await ProbePairInternalAsync(
                    ownerTenant,
                    intruderTenant,
                    runCorrelationId,
                    cancellationToken)
                    .ConfigureAwait(false);
                if (result.IsBreach)
                {
                    breaches++;
                }

                if (didAlert)
                {
                    alerted++;
                }
            }
        }

        return new DerivedStoreIsolationProbeOutcome(partitionsProbed, breaches, alerted);
    }

    private static string SentinelResourceId(DerivedStoreClass cls, string ownerTenant, string correlationId)
        => $"{SentinelResourceIdPrefix}{DerivedStorePartition.Segment(cls)}:{ownerTenant}:{correlationId}";
}
