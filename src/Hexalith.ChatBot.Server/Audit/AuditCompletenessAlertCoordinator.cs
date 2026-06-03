using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Server.Gateway.Stages;
using Hexalith.ChatBot.Server.Observability;

namespace Hexalith.ChatBot.Server.Audit;

/// <summary>
/// Injectable per-tenant audit-completeness alert coordinator (Story 9.2, AC2/NFR50a) following the canonical
/// <see cref="AuditChainVerificationCoordinator"/> discipline exactly: a pure measurement
/// (<see cref="AuditCompletenessMeasurer"/>) and a pure budget map (<see cref="AuditCompletenessBudgetEvaluator"/>)
/// followed by a fail-closed audit-then-deliver step. For each tenant whose completeness budget is breached — fraction
/// below the 99.5% target (<see cref="ErrorBudgetBurnState.Exhausted"/>) <b>or</b> unmeasurable
/// (<see cref="ErrorBudgetBurnState.Unknown"/>) — it writes the metadata-only
/// <see cref="AuditEnvelopeFactory.AuditCompletenessBudgetBreached"/> pre-commit envelope before emitting exactly one
/// <see cref="OperatorAlertKind.AuditCompletenessBudgetBreached"/> <b>P1</b> operator alert through the existing
/// <see cref="IOperatorAlertSink"/>.
/// <para>
/// Fail-closed (Epic 8 no-fabrication): an unmeasurable tenant is a breach signal — it alerts, never silently passes.
/// If the pre-commit audit is unavailable, no operator alert is emitted (no durable/observable side effect), but the
/// breach is still surfaced to the caller. Read-only/out-of-band: the measurement adds no commit-path gate (D4).
/// </para>
/// <para>
/// No always-on <c>BackgroundService</c> is introduced; the periodic runtime trigger (Dapr timer / PeriodicTimer) is
/// deferred, consistent with the Epic 7/8/9.1 inert-control-floor pattern — the measurer, budget evaluator, gauge, and
/// this alert path are fully built and tested, and a scheduler need only call
/// <see cref="MeasureAllTenantsAndAlertAsync"/> on its cadence.
/// </para>
/// </summary>
internal sealed class AuditCompletenessAlertCoordinator(
    IWormAuditStore wormStore,
    AuditCompletenessMeasurer measurer,
    IAuditWriter auditWriter,
    IOperatorAlertSink operatorAlertSink,
    ISystemClock clock)
{
    /// <summary>Measures a single tenant's completeness and, on breach, audits-then-alerts at P1. Returns the measurement.</summary>
    public async ValueTask<AuditCompletenessMeasurement> MeasureTenantAndAlertAsync(
        string tenantRef,
        string correlationId,
        CancellationToken cancellationToken)
    {
        (AuditCompletenessMeasurement measurement, _) = await MeasureTenantInternalAsync(tenantRef, correlationId, cancellationToken)
            .ConfigureAwait(false);
        return measurement;
    }

    private async ValueTask<(AuditCompletenessMeasurement Measurement, bool Alerted)> MeasureTenantInternalAsync(
        string tenantRef,
        string correlationId,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantRef);
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);

        AuditCompletenessMeasurement measurement = await measurer.MeasureTenantAsync(tenantRef, cancellationToken).ConfigureAwait(false);
        ErrorBudgetBurnState budgetState = AuditCompletenessBudgetEvaluator.FromMeasurement(measurement);

        // Within budget (measurable AND ≥ 99.5%) is the only non-alerting state. Exhausted (below target) and Unknown
        // (unmeasurable) both breach and must page on-call at P1 — fail-closed.
        if (budgetState == ErrorBudgetBurnState.WithinBudget)
        {
            return (measurement, false);
        }

        DateTimeOffset now = clock.UtcNow;

        // Fail-closed audit-then-deliver (NFR15a): write the breach's pre-commit audit envelope before alerting. If the
        // audit is unavailable, no operator alert is emitted, but the breach is still surfaced to the caller.
        AuditEnvelope envelope = AuditEnvelopeFactory.AuditCompletenessBudgetBreached(measurement, budgetState, correlationId, now);
        AuditWriteResult auditResult = await auditWriter
            .RecordPreCommitAsync(envelope, cancellationToken)
            .ConfigureAwait(false);
        if (!auditResult.Succeeded)
        {
            return (measurement, false);
        }

        await operatorAlertSink
            .EmitAsync(
                new OperatorAlert(
                    OperatorAlertKind.AuditCompletenessBudgetBreached,
                    measurement.ReasonCode,
                    tenantRef,
                    "AuditCompletenessBudgetBreached",
                    correlationId,
                    now,
                    measurement.FirstDivergingOperationLocator),
                cancellationToken)
            .ConfigureAwait(false);

        return (measurement, true);
    }

    /// <summary>
    /// Sweeps every tenant chain currently in the store (the method a periodic scheduler would call on its cadence),
    /// mirroring <see cref="AuditChainVerificationCoordinator.VerifyAllTenantsAsync"/>. Each tenant's correlation id is
    /// the supplied run correlation id; an unmeasurable tenant counts as a breach, never skipped.
    /// </summary>
    public async ValueTask<AuditCompletenessSweepOutcome> MeasureAllTenantsAndAlertAsync(
        string runCorrelationId,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(runCorrelationId);

        int breaches = 0;
        int unmeasurable = 0;
        IReadOnlyList<string> tenants = wormStore.EnumerateTenants();
        foreach (string tenantRef in tenants)
        {
            (AuditCompletenessMeasurement measurement, _) = await MeasureTenantInternalAsync(
                tenantRef,
                runCorrelationId,
                cancellationToken)
                .ConfigureAwait(false);

            if (AuditCompletenessBudgetEvaluator.FromMeasurement(measurement) != ErrorBudgetBurnState.WithinBudget)
            {
                breaches++;
            }

            if (!measurement.IsMeasurable)
            {
                unmeasurable++;
            }
        }

        return new AuditCompletenessSweepOutcome(tenants.Count, breaches, unmeasurable);
    }
}
