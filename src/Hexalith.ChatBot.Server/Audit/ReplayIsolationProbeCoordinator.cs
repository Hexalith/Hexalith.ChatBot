using Hexalith.ChatBot.Server.Adapters.Mailbox;
using Hexalith.ChatBot.Server.Gateway.Stages;

namespace Hexalith.ChatBot.Server.Audit;

/// <summary>The result of a replay-isolation sweep: how many production tenants were swept, breached, and alerted.</summary>
internal sealed record ReplayIsolationProbeOutcome(int TenantsSwept, int Breaches, int Alerted);

/// <summary>
/// Injectable nightly replay-isolation probe coordinator (Story 9.4, AC3, FR95a, addendum §Replay Isolation) following
/// the <see cref="AuditChainVerificationCoordinator"/> discipline <b>exactly</b>: a pure evaluator
/// (<see cref="ReplayIsolationVerifier"/>) followed by a fail-closed audit-then-deliver step. For each <b>production</b>
/// tenant (<c>!<see cref="ReplayTenantPolicy.IsTestTenant"/></c>) it enumerates the outbound-trace store <b>and</b> the
/// WORM chain, verifies the two complementary isolation invariants, and on any breach writes the metadata-only
/// <see cref="AuditEnvelopeFactory.ReplayIsolationBreach"/> pre-commit envelope before emitting exactly one
/// <see cref="OperatorAlertKind.ReplayIsolationBreach"/> operator alert through the existing
/// <see cref="IOperatorAlertSink"/>.
/// <para>
/// Fail-closed (Epic 8/9 no-fabrication doctrine): an enumeration that throws is treated as
/// <see cref="ReplayIsolationStatus.Unknown"/> — a breach signal — never a silent <c>Clean</c>. Test tenants are
/// <b>skipped</b> by construction (a test-tenant trace record is expected, not a breach); production-tenant identity is
/// the single predicate <see cref="ReplayTenantPolicy.IsTestTenant"/>.
/// </para>
/// <para>
/// <b>M2 release gate.</b> <see cref="SweepAllProductionTenantsAsync"/> returns a structured
/// <see cref="ReplayIsolationProbeOutcome"/> a CI/release gate can assert against: zero breaches ⇒ the M2 release may
/// proceed; any breach is stop-ship. No always-on <c>BackgroundService</c> is introduced — the periodic runtime trigger
/// (Dapr timer / <c>PeriodicTimer</c>) is deferred, consistent with the Story 9.1/9.2 inert-control-floor pattern; the
/// verifier, coordinator, alert path, and release-gate contract are fully built and tested, and a scheduler need only
/// call the sweep on its cadence.
/// </para>
/// </summary>
internal sealed class ReplayIsolationProbeCoordinator(
    IOutboundTraceStore traceStore,
    IWormAuditStore wormStore,
    IAuditWriter auditWriter,
    IOperatorAlertSink operatorAlertSink,
    ISystemClock clock)
{
    /// <summary>Verifies a single production tenant's isolation and, on breach, audits-then-alerts. Returns the result.</summary>
    public async ValueTask<ReplayIsolationVerificationResult> VerifyTenantAndAlertAsync(
        string tenantRef,
        string correlationId,
        CancellationToken cancellationToken)
    {
        (ReplayIsolationVerificationResult result, _) = await VerifyTenantInternalAsync(tenantRef, correlationId, cancellationToken)
            .ConfigureAwait(false);
        return result;
    }

    private async ValueTask<(ReplayIsolationVerificationResult Result, bool Alerted)> VerifyTenantInternalAsync(
        string tenantRef,
        string correlationId,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantRef);
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);

        ReplayIsolationVerificationResult result;
        try
        {
            IReadOnlyList<OutboundTraceRecord> traceRecords = traceStore.EnumerateForTenant(tenantRef);
            IReadOnlyList<AuditEnvelope> chainEnvelopes = [.. wormStore.EnumerateChain(tenantRef).Select(static record => record.Envelope)];
            result = ReplayIsolationVerifier.Verify(tenantRef, traceRecords, chainEnvelopes);
        }
        catch (Exception) when (cancellationToken is { IsCancellationRequested: false })
        {
            // Fail-closed: a sweep that cannot complete is itself a breach signal, never silent success.
            result = new ReplayIsolationVerificationResult(
                tenantRef,
                ReplayIsolationStatus.Unknown,
                ReplayIsolationVerificationResult.SweepIncompleteReasonCode,
                FirstOffenderLocator: null);
        }

        if (!result.IsBreach)
        {
            return (result, false);
        }

        DateTimeOffset now = clock.UtcNow;

        // Fail-closed audit-then-deliver (NFR15a): write the breach's pre-commit audit envelope before alerting. If the
        // audit is unavailable, no operator alert is emitted — no observable side effect — but the breach is still
        // surfaced to the caller (which keeps its own non-silent record).
        AuditEnvelope envelope = AuditEnvelopeFactory.ReplayIsolationBreach(result, correlationId, now);
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
                    OperatorAlertKind.ReplayIsolationBreach,
                    result.ReasonCode,
                    tenantRef,
                    "ReplayIsolationBreach",
                    correlationId,
                    now,
                    result.FirstOffenderLocator),
                cancellationToken)
            .ConfigureAwait(false);

        return (result, true);
    }

    /// <summary>
    /// Sweeps every PRODUCTION tenant that holds an outbound-trace partition or a WORM chain (the method a periodic
    /// scheduler and the M2 release gate call). Test tenants are skipped by construction. Each tenant's correlation id is
    /// the supplied run correlation id. Returns the structured outcome the release gate asserts against — zero breaches
    /// ⇒ the M2 release may proceed.
    /// </summary>
    public async ValueTask<ReplayIsolationProbeOutcome> SweepAllProductionTenantsAsync(
        string runCorrelationId,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(runCorrelationId);

        // The union of both stores' tenant partitions, so a tenant that has a chain but no trace record (or vice-versa)
        // is still swept. A test tenant is never a production tenant for this sweep (ReplayTenantPolicy is the one rule).
        HashSet<string> productionTenants = new(StringComparer.Ordinal);
        foreach (string tenantRef in traceStore.EnumerateTenants().Concat(wormStore.EnumerateTenants()))
        {
            if (!ReplayTenantPolicy.IsTestTenant(tenantRef))
            {
                _ = productionTenants.Add(tenantRef);
            }
        }

        int breaches = 0;
        int alerted = 0;
        foreach (string tenantRef in productionTenants)
        {
            (ReplayIsolationVerificationResult result, bool didAlert) = await VerifyTenantInternalAsync(
                tenantRef,
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

        return new ReplayIsolationProbeOutcome(productionTenants.Count, breaches, alerted);
    }
}
