using Hexalith.ChatBot.Server.Gateway.Stages;

namespace Hexalith.ChatBot.Server.Audit;

/// <summary>
/// Injectable continuity-drill coordinator (Story 9.11, AC1–AC4, NFR56/NFR59) following the
/// <see cref="DerivedStoreIsolationProbeCoordinator"/> discipline <b>exactly</b>: a pure evaluator
/// (<see cref="ContinuityDrillEvaluator"/>) over the measured RPO/RTO + data-loss check from the
/// <see cref="IContinuityDrillScenarioRunner"/> seam, followed by fail-closed audit-then-deliver on a breach. For each
/// of the two NFR56 scenarios (<see cref="ContinuityDrillScenarios.EventStoreOutage"/>,
/// <see cref="ContinuityDrillScenarios.M365SubscriptionFailure"/>) it runs a recovery exercise against a <b>test
/// tenant</b> and produces a metadata-only <see cref="ContinuityDrillReport"/> — the A10 recalibration evidence.
/// <para>
/// <b>Test-tenant by construction (NFR9a/NFR59).</b> A drill must never target a production tenant: the coordinator
/// fails closed to an <c>unmeasurable</c> report when <see cref="ReplayTenantPolicy.IsTestTenant"/> is false (never a
/// fabricated <c>met</c>), so recovery lands only in the test tenant's partition and no production-tenant durable state
/// is mutated.
/// </para>
/// <para>
/// <b>Fail-safe over fabrication (Epic 8/9).</b> An unknown scenario, a scenario runner that throws, or a recovery that
/// never completes yields <see cref="ContinuityDrillReport.Unmeasurable"/> — a breach signal — never a silent pass. On
/// any breach (a miss <b>or</b> an unmeasurable drill) it writes the
/// <see cref="AuditEnvelopeFactory.ContinuityDrillTargetMissed"/> pre-commit envelope <b>before</b> emitting exactly one
/// <see cref="OperatorAlertKind.ContinuityDrillTargetMissed"/> alert (audit-down ⇒ no alert, but the report is still
/// returned).
/// </para>
/// <para>
/// <b>A miss remains distinct from a structural breach.</b> A <c>missed</c> drill is honest target-deviation evidence
/// that flags recalibration (<see cref="ContinuityDrillReport.RecalibrationFlag"/>) and records a follow-up; an
/// <c>unmeasurable</c> drill means no evidence was produced. The structured <see cref="ContinuityDrillOutcome"/> keeps
/// Met/Missed/Unmeasurable distinct so the externally scheduled evidence gate applies the approved A10 policy without
/// relabeling results. No always-on <c>BackgroundService</c> is introduced; Story 12.15's serialized Tier-3 workflow
/// invokes <see cref="RunAllScenariosAsync"/>.
/// </para>
/// </summary>
internal sealed class ContinuityDrillCoordinator(
    IContinuityDrillScenarioRunner scenarioRunner,
    IAuditWriter auditWriter,
    IOperatorAlertSink operatorAlertSink,
    ISystemClock clock,
    IRecoveryValidationEvidenceSink evidenceSink)
{
    /// <summary>Creates a coordinator with the inert product evidence sink for existing non-live callers.</summary>
    internal ContinuityDrillCoordinator(
        IContinuityDrillScenarioRunner scenarioRunner,
        IAuditWriter auditWriter,
        IOperatorAlertSink operatorAlertSink,
        ISystemClock clock)
        : this(scenarioRunner, auditWriter, operatorAlertSink, clock, DiscardingRecoveryValidationEvidenceSink.Instance)
    {
    }

    /// <summary>
    /// Runs one drill scenario against the test tenant and, on breach, audits-then-alerts. Returns the metadata-only
    /// report (the A10 recalibration evidence).
    /// </summary>
    /// <param name="scenario">A <see cref="ContinuityDrillScenarios"/> token.</param>
    /// <param name="testTenantRef">The test tenant the drill runs against (must satisfy <see cref="ReplayTenantPolicy.IsTestTenant"/>).</param>
    /// <param name="correlationId">The run correlation id.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The drill report.</returns>
    public async ValueTask<ContinuityDrillReport> RunScenarioAndRecordAsync(
        string scenario,
        string testTenantRef,
        string correlationId,
        CancellationToken cancellationToken)
    {
        (ContinuityDrillReport report, _) = await RunScenarioInternalAsync(
            scenario,
            testTenantRef,
            correlationId,
            cancellationToken)
            .ConfigureAwait(false);
        return report;
    }

    /// <summary>
    /// Runs <b>both</b> NFR56 scenarios against the configured drill test tenant (the method a periodic scheduler AND a
    /// release gate call) and tallies the structured outcome. <c>Unmeasurable == 0</c> ⇒ the drills produced evidence.
    /// </summary>
    /// <param name="testTenantRef">The drill test tenant (must satisfy <see cref="ReplayTenantPolicy.IsTestTenant"/>).</param>
    /// <param name="runCorrelationId">The run correlation id applied to every scenario.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The structured drill outcome.</returns>
    public async ValueTask<ContinuityDrillOutcome> RunAllScenariosAsync(
        string testTenantRef,
        string runCorrelationId,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(testTenantRef);
        ArgumentException.ThrowIfNullOrWhiteSpace(runCorrelationId);

        int scenariosRun = 0;
        int met = 0;
        int missed = 0;
        int unmeasurable = 0;
        int alerted = 0;
        // SweepOrder, not All: this sweep stops the real EventStore resource and faults the subscription boundary, so
        // its ordering is a safety contract and must not depend on HashSet<T> enumeration order.
        foreach (string scenario in ContinuityDrillScenarios.SweepOrder)
        {
            scenariosRun++;
            (ContinuityDrillReport report, bool didAlert) = await RunScenarioInternalAsync(
                scenario,
                testTenantRef,
                runCorrelationId,
                cancellationToken)
                .ConfigureAwait(false);

            if (string.Equals(report.Verdict, ContinuityDrillVerdicts.Met, StringComparison.Ordinal))
            {
                met++;
            }
            else if (report.IsMiss)
            {
                missed++;
            }
            else
            {
                unmeasurable++;
            }

            if (didAlert)
            {
                alerted++;
            }
        }

        return new ContinuityDrillOutcome(scenariosRun, met, missed, unmeasurable, alerted);
    }

    private async ValueTask<(ContinuityDrillReport Report, bool Alerted)> RunScenarioInternalAsync(
        string scenario,
        string testTenantRef,
        string correlationId,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(scenario);
        ArgumentException.ThrowIfNullOrWhiteSpace(testTenantRef);
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);

        DateTimeOffset now = clock.UtcNow;

        // Fail-closed: a drill must never target a production tenant and must run a known scenario. Either condition
        // yields an unmeasurable report (a breach signal), never a fabricated met.
        if (!ReplayTenantPolicy.IsTestTenant(testTenantRef) || !ContinuityDrillScenarios.Contains(scenario))
        {
            ContinuityDrillReport unmeasurable = ContinuityDrillReport.Unmeasurable(testTenantRef, scenario, correlationId, now, now);
            unmeasurable = await RetainAsync(unmeasurable, cancellationToken).ConfigureAwait(false);
            bool didAlertGuard = await AuditThenAlertAsync(unmeasurable, correlationId, cancellationToken).ConfigureAwait(false);
            return (unmeasurable, didAlertGuard);
        }

        ContinuityDrillReport report;
        try
        {
            ContinuityDrillMeasurement measurement = await scenarioRunner
                .RunAsync(scenario, testTenantRef, correlationId, cancellationToken)
                .ConfigureAwait(false);

            string verdict = ContinuityDrillEvaluator.Evaluate(measurement.MeasuredRpo, measurement.MeasuredRto, measurement.DataLossDetected);
            IReadOnlyList<string> deviations = ContinuityDrillEvaluator.Deviations(measurement.MeasuredRpo, measurement.MeasuredRto, measurement.DataLossDetected);
            bool isMiss = string.Equals(verdict, ContinuityDrillVerdicts.Missed, StringComparison.Ordinal);

            report = new ContinuityDrillReport(
                testTenantRef,
                scenario,
                measurement.StartedAtUtc,
                measurement.EndedAtUtc,
                measurement.MeasuredRpo,
                measurement.MeasuredRto,
                measurement.DataLossDetected,
                verdict,
                deviations,
                RecalibrationFlag: isMiss,
                FollowUpActionRef: isMiss ? $"continuity-recalibration:{scenario}" : null,
                correlationId,
                ContinuityDrillReport.DrillCompletedReasonCode,
                measurement.ExecutionAssertions);
        }
        catch (Exception) when (cancellationToken is { IsCancellationRequested: false })
        {
            // Fail-safe: a drill that cannot complete is itself a breach signal, never a fabricated met.
            report = ContinuityDrillReport.Unmeasurable(testTenantRef, scenario, correlationId, now, clock.UtcNow);
        }

        report = await RetainAsync(report, cancellationToken).ConfigureAwait(false);
        bool didAlert = await AuditThenAlertAsync(report, correlationId, cancellationToken).ConfigureAwait(false);
        return (report, didAlert);
    }

    /// <summary>
    /// Retains the canonical report before it is reduced to aggregate counts, downgrading to <c>unmeasurable</c> when
    /// the sink is unavailable. Retention deliberately runs <b>before</b> <see cref="AuditThenAlertAsync"/>: this
    /// method can substitute the report, and the audit envelope must describe the report that was actually retained.
    /// The audit-before-alert contract is unaffected — that ordering lives inside <see cref="AuditThenAlertAsync"/>.
    /// </summary>
    private async ValueTask<ContinuityDrillReport> RetainAsync(
        ContinuityDrillReport report,
        CancellationToken cancellationToken)
    {
        try
        {
            await evidenceSink.RecordAsync(report, cancellationToken).ConfigureAwait(false);
            return report;
        }
        catch (Exception) when (cancellationToken is { IsCancellationRequested: false })
        {
            ContinuityDrillReport unmeasurable = ContinuityDrillReport.Unmeasurable(
                report.TenantRef,
                report.Scenario,
                report.CorrelationId,
                report.StartedAtUtc,
                clock.UtcNow);

            // Keep WHY it is unmeasurable in the artifact. The verdict model is unchanged — a retention failure is
            // still a stop-ship — but the deviation distinguishes "the sink was unavailable" from "recovery failed".
            unmeasurable = unmeasurable with
            {
                Deviations = [.. unmeasurable.Deviations, ContinuityDrillReport.EvidenceRetentionFailedDeviation],
            };
            try
            {
                await evidenceSink.RecordAsync(unmeasurable, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception)
            {
                // Unfiltered: the primary RecordAsync already failed (not from cancellation, per the outer filter), so
                // this fallback write must still degrade to the unmeasurable report even if the token is cancelled
                // mid-fallback. The caller still receives an unmeasurable stop-ship report when the retaining sink is
                // unavailable.
            }

            return unmeasurable;
        }
    }

    private async ValueTask<bool> AuditThenAlertAsync(
        ContinuityDrillReport report,
        string correlationId,
        CancellationToken cancellationToken)
    {
        if (!report.IsBreach)
        {
            return false;
        }

        DateTimeOffset now = clock.UtcNow;

        // Fail-closed audit-then-deliver (NFR15a): write the breach's pre-commit audit envelope before alerting. If the
        // audit is unavailable, no operator alert is emitted — no observable side effect — but the report is still
        // surfaced to the caller.
        AuditEnvelope envelope = AuditEnvelopeFactory.ContinuityDrillTargetMissed(report, correlationId, now);
        AuditWriteResult auditResult = await auditWriter
            .RecordPreCommitAsync(envelope, cancellationToken)
            .ConfigureAwait(false);
        if (!auditResult.Succeeded)
        {
            return false;
        }

        await operatorAlertSink
            .EmitAsync(
                new OperatorAlert(
                    OperatorAlertKind.ContinuityDrillTargetMissed,
                    report.ReasonCode,
                    report.TenantRef,
                    "ContinuityDrillTargetMissed",
                    correlationId,
                    now,
                    report.Deviations.Count > 0 ? report.Deviations[0] : null),
                cancellationToken)
            .ConfigureAwait(false);

        return true;
    }
}
