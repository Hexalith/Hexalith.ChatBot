using Hexalith.ChatBot.Server.Gateway.Stages;

namespace Hexalith.ChatBot.Server.Audit;

/// <summary>The result of a chain-verification pass: how many tenant chains were checked, breached, and alerted.</summary>
internal sealed record AuditChainVerificationOutcome(int TenantsChecked, int Breaches, int Alerted);

/// <summary>
/// Injectable nightly WORM-chain verification coordinator (Story 9.1, AC2/NFR49a) following the
/// <see cref="Notifications.ReviewerBacklogAlertCoordinator"/> / <see cref="Notifications.OperationalAlertWiringCoordinator"/>
/// discipline: a pure evaluator (<see cref="WormAuditChainVerifier"/>) followed by a fail-closed audit-then-deliver
/// step. For each tenant it enumerates the chain, verifies it, and on any breach writes the metadata-only
/// <see cref="AuditEnvelopeFactory.AuditChainBroken"/> pre-commit envelope before emitting exactly one
/// <see cref="OperatorAlertKind.AuditChainBroken"/> operator alert through the existing <see cref="IOperatorAlertSink"/>.
/// <para>
/// Fail-closed (Epic 8 no-fabrication doctrine): an enumeration that throws is treated as
/// <see cref="WormChainVerificationStatus.Unknown"/> — a breach signal — never a silent <c>Verified</c>. The
/// detection→emit path is synchronous within a pass, so the AC2 five-minute budget
/// (<see cref="WormAuditChainVerifier.DetectionToAlertBudget"/>) holds by construction. No always-on
/// <c>BackgroundService</c> is introduced; the periodic runtime trigger (Dapr timer / PeriodicTimer) is deferred,
/// consistent with the Epic 7/8 inert-control-floor pattern — the verifier, coordinator, and alert path are fully built
/// and tested, and a scheduler need only call <see cref="VerifyAllTenantsAsync"/> per tenant on its cadence.
/// </para>
/// </summary>
internal sealed class AuditChainVerificationCoordinator(
    IWormAuditStore wormStore,
    IAuditWriter auditWriter,
    IOperatorAlertSink operatorAlertSink,
    ISystemClock clock)
{
    /// <summary>Verifies a single tenant's chain and, on breach, audits-then-alerts. Returns the breach result (or the verified pass).</summary>
    public async ValueTask<WormAuditChainVerificationResult> VerifyTenantAndAlertAsync(
        string tenantRef,
        string correlationId,
        CancellationToken cancellationToken)
    {
        (WormAuditChainVerificationResult result, _) = await VerifyTenantInternalAsync(tenantRef, correlationId, cancellationToken)
            .ConfigureAwait(false);
        return result;
    }

    private async ValueTask<(WormAuditChainVerificationResult Result, bool Alerted)> VerifyTenantInternalAsync(
        string tenantRef,
        string correlationId,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantRef);
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);

        WormAuditChainVerificationResult result;
        try
        {
            IReadOnlyList<WormAuditChainRecord> chain = wormStore.EnumerateChain(tenantRef);
            result = WormAuditChainVerifier.Verify(tenantRef, chain);
        }
        catch (Exception) when (cancellationToken is { IsCancellationRequested: false })
        {
            // Fail-closed: a verification that cannot complete is itself a breach signal, never silent success.
            result = new WormAuditChainVerificationResult(
                tenantRef,
                WormChainVerificationStatus.Unknown,
                WormAuditChainVerificationResult.VerificationIncompleteReasonCode,
                FirstBreakLocator: null);
        }

        if (!result.IsBreach)
        {
            return (result, false);
        }

        DateTimeOffset now = clock.UtcNow;

        // Fail-closed audit-then-deliver (NFR15a): write the breach's pre-commit audit envelope before alerting. If the
        // audit is unavailable, no operator alert is emitted — no durable/observable side effect — but the breach is
        // still surfaced to the caller (which keeps its own non-silent record of the unverifiable chain).
        AuditEnvelope envelope = AuditEnvelopeFactory.AuditChainBroken(result, correlationId, now);
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
                    OperatorAlertKind.AuditChainBroken,
                    result.ReasonCode,
                    tenantRef,
                    "AuditChainBroken",
                    correlationId,
                    now,
                    result.FirstBreakLocator),
                cancellationToken)
            .ConfigureAwait(false);

        return (result, true);
    }

    /// <summary>
    /// Sweeps every tenant chain currently in the store (the method a periodic scheduler would call on its cadence).
    /// Each tenant's correlation id is derived deterministically from the supplied run correlation id.
    /// </summary>
    public async ValueTask<AuditChainVerificationOutcome> VerifyAllTenantsAsync(
        string runCorrelationId,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(runCorrelationId);

        int breaches = 0;
        int alerted = 0;
        IReadOnlyList<string> tenants = wormStore.EnumerateTenants();
        foreach (string tenantRef in tenants)
        {
            (WormAuditChainVerificationResult result, bool didAlert) = await VerifyTenantInternalAsync(
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

        return new AuditChainVerificationOutcome(tenants.Count, breaches, alerted);
    }
}
