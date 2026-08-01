using Hexalith.ChatBot.Server.Gateway.Stages;

namespace Hexalith.ChatBot.Server.Audit;

/// <summary>
/// Injectable scoped-outage degradation validation coordinator (Story 9.13, AC1–AC4, NFR58/NFR59/NFR41/NFR17/NFR13)
/// following the <see cref="ProjectionRebuildValidationCoordinator"/> / <see cref="ContinuityDrillCoordinator"/>
/// discipline <b>exactly</b>: a pure evaluator (<see cref="ScopedOutageDegradationEvaluator"/>) over the measured
/// assertions from the <see cref="IScopedOutageInjectionDriver"/> seam, followed by fail-closed audit-then-deliver on a
/// breach. For each of the six <see cref="ScopedOutageDependencies"/> outages it drives an outage exercise against a
/// <b>test tenant</b> and produces a metadata-only <see cref="ScopedOutageDegradationReport"/> — the NFR58/NFR59
/// validation evidence.
/// <para>
/// <b>Test-tenant by construction (NFR9a/NFR59).</b> A validation must never target a production tenant and must exercise
/// a known dependency: the coordinator fails closed to an <c>unmeasurable</c> report when
/// <see cref="ReplayTenantPolicy.IsTestTenant"/> is false <b>or</b> the dependency is unknown — <b>without invoking the
/// driver</b> — so the outage lands only in the test tenant's partition and no production-tenant durable state is
/// mutated. Never a fabricated <c>contained</c>.
/// </para>
/// <para>
/// <b>Fail-safe over fabrication (Epic 8/9).</b> A driver that throws, an outage exercise that never completes, or
/// unavailable assertion results yield <see cref="ScopedOutageDegradationReport.Unmeasurable"/> — a breach signal — never
/// a silent pass. On any breach (a <c>breached</c> validation, a late scope recording, <b>or</b> an unmeasurable
/// validation) it writes the <see cref="AuditEnvelopeFactory.ScopedOutageDegradationBreach"/> pre-commit envelope
/// <b>before</b> emitting exactly one <see cref="OperatorAlertKind.ScopedOutageDegradationBreach"/> alert (audit-down ⇒
/// no alert, but the report is still returned).
/// </para>
/// <para>
/// <b>Three distinct breach dimensions.</b> A <c>breached</c> validation is the serious NFR58/NFR59 isolation/scope/
/// recovery breach (stop-ship-style, like a 9.4/9.5 isolation breach); a late scope recording
/// (<see cref="ScopedOutageDegradationReport.ScopeRecordedWithinTarget"/> false) is a monitoring-latency miss /
/// recalibration signal (a contained-but-slow degradation stays <c>contained</c>); an <c>unmeasurable</c> validation is
/// the fail-safe breach. The structured <see cref="ScopedOutageDegradationOutcome"/> keeps Contained/Breached/
/// ScopeRecordingExceeded/Unmeasurable distinct so a CI/release gate asserts the dimension it cares about. No always-on
/// <c>BackgroundService</c> is introduced — a periodic scheduler AND a release gate need only call
/// <see cref="RunAllScenariosAsync"/> on its cadence. Story 12.15 supplies that serialized Tier-3 workflow and a
/// separate live <see cref="IScopedOutageInjectionDriver"/> while product DI remains inert.
/// </para>
/// </summary>
internal sealed class ScopedOutageDegradationValidationCoordinator(
    IScopedOutageInjectionDriver injectionDriver,
    IAuditWriter auditWriter,
    IOperatorAlertSink operatorAlertSink,
    ISystemClock clock,
    IRecoveryValidationEvidenceSink evidenceSink)
{
    /// <summary>Creates a coordinator with the inert product evidence sink for existing non-live callers.</summary>
    internal ScopedOutageDegradationValidationCoordinator(
        IScopedOutageInjectionDriver injectionDriver,
        IAuditWriter auditWriter,
        IOperatorAlertSink operatorAlertSink,
        ISystemClock clock)
        : this(injectionDriver, auditWriter, operatorAlertSink, clock, DiscardingRecoveryValidationEvidenceSink.Instance)
    {
    }

    /// <summary>
    /// Runs one dependency-outage scenario against the test tenant and, on breach, audits-then-alerts. Returns the
    /// metadata-only report (the NFR58/NFR59 validation evidence).
    /// </summary>
    /// <param name="dependency">A <see cref="ScopedOutageDependencies"/> token.</param>
    /// <param name="testTenantRef">The test tenant the outage runs against (must satisfy <see cref="ReplayTenantPolicy.IsTestTenant"/>).</param>
    /// <param name="correlationId">The run correlation id.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The validation report.</returns>
    public async ValueTask<ScopedOutageDegradationReport> RunScenarioAndRecordAsync(
        string dependency,
        string testTenantRef,
        string correlationId,
        CancellationToken cancellationToken)
    {
        (ScopedOutageDegradationReport report, _) = await RunScenarioInternalAsync(
            dependency,
            testTenantRef,
            correlationId,
            cancellationToken)
            .ConfigureAwait(false);
        return report;
    }

    /// <summary>
    /// Runs <b>every</b> <see cref="ScopedOutageDependencies"/> scenario against the configured test tenant (the method a
    /// periodic scheduler AND a release gate call) and tallies the structured outcome.
    /// <c>Breached == 0 &amp;&amp; Unmeasurable == 0</c> ⇒ every dependency outage degraded only its scope and produced
    /// evidence.
    /// </summary>
    /// <param name="testTenantRef">The validation test tenant (must satisfy <see cref="ReplayTenantPolicy.IsTestTenant"/>).</param>
    /// <param name="runCorrelationId">The run correlation id applied to every scenario.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The structured validation outcome.</returns>
    public async ValueTask<ScopedOutageDegradationOutcome> RunAllScenariosAsync(
        string testTenantRef,
        string runCorrelationId,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(testTenantRef);
        ArgumentException.ThrowIfNullOrWhiteSpace(runCorrelationId);

        int scenariosValidated = 0;
        int contained = 0;
        int breached = 0;
        int scopeRecordingExceeded = 0;
        int unmeasurable = 0;
        int alerted = 0;
        // SweepOrder, not All: the destructive sweep's ordering (identity last) is a safety contract and must not
        // depend on HashSet<T> enumeration order.
        foreach (string dependency in ScopedOutageDependencies.SweepOrder)
        {
            scenariosValidated++;
            (ScopedOutageDegradationReport report, bool didAlert) = await RunScenarioInternalAsync(
                dependency,
                testTenantRef,
                runCorrelationId,
                cancellationToken)
                .ConfigureAwait(false);

            if (string.Equals(report.Verdict, ScopedOutageDegradationVerdicts.Contained, StringComparison.Ordinal))
            {
                contained++;
            }
            else if (report.IsScopeBreach)
            {
                breached++;
            }
            else
            {
                unmeasurable++;
            }

            // A late scope recording is counted independently of the verdict: a contained-but-slow degradation is
            // contained AND ScopeRecordingExceeded — the monitoring-latency miss is a distinct dimension a gate may assert.
            if (!report.ScopeRecordedWithinTarget && string.Equals(report.Verdict, ScopedOutageDegradationVerdicts.Contained, StringComparison.Ordinal))
            {
                scopeRecordingExceeded++;
            }

            if (didAlert)
            {
                alerted++;
            }
        }

        return new ScopedOutageDegradationOutcome(scenariosValidated, contained, breached, scopeRecordingExceeded, unmeasurable, alerted);
    }

    private async ValueTask<(ScopedOutageDegradationReport Report, bool Alerted)> RunScenarioInternalAsync(
        string dependency,
        string testTenantRef,
        string correlationId,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dependency);
        ArgumentException.ThrowIfNullOrWhiteSpace(testTenantRef);
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);

        DateTimeOffset now = clock.UtcNow;

        // Fail-closed: a validation must never target a production tenant and must exercise a known dependency. Either
        // condition yields an unmeasurable report (a breach signal), never a fabricated contained, and the driver is
        // never invoked.
        if (!ReplayTenantPolicy.IsTestTenant(testTenantRef) || !ScopedOutageDependencies.Contains(dependency))
        {
            ScopedOutageDegradationReport unmeasurable = ScopedOutageDegradationReport.Unmeasurable(testTenantRef, dependency, correlationId, now, now);
            unmeasurable = await RetainAsync(unmeasurable, cancellationToken).ConfigureAwait(false);
            bool didAlertGuard = await AuditThenAlertAsync(unmeasurable, correlationId, cancellationToken).ConfigureAwait(false);
            return (unmeasurable, didAlertGuard);
        }

        ScopedOutageDegradationReport report;
        try
        {
            ScopedOutageDegradationMeasurement measurement = await injectionDriver
                .InjectAndMeasureAsync(dependency, testTenantRef, correlationId, cancellationToken)
                .ConfigureAwait(false);

            bool scopeRecordedWithinTarget = measurement.ScopeRecordingLatency <= RecoveryTargets.MaxScopeRecordingLatency;
            string verdict = ScopedOutageDegradationEvaluator.Evaluate(measurement);
            IReadOnlyList<string> deviations = ScopedOutageDegradationEvaluator.Deviations(measurement, scopeRecordedWithinTarget);
            string? firstBreachLocator = ScopedOutageDegradationEvaluator.FirstBreachLocator(measurement);

            report = new ScopedOutageDegradationReport(
                testTenantRef,
                dependency,
                measurement.ExpectedScope,
                measurement.ObservedScope,
                measurement.StartedAtUtc,
                measurement.EndedAtUtc,
                measurement.ScopeRecordingLatency,
                scopeRecordedWithinTarget,
                verdict,
                deviations,
                firstBreachLocator,
                correlationId,
                ScopedOutageDegradationReport.ValidationCompletedReasonCode,
                measurement.ExecutionAssertions);
        }
        catch (Exception) when (cancellationToken is { IsCancellationRequested: false })
        {
            // Fail-safe: a validation that cannot complete is itself a breach signal, never a fabricated contained.
            report = ScopedOutageDegradationReport.Unmeasurable(testTenantRef, dependency, correlationId, now, clock.UtcNow);
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
    private async ValueTask<ScopedOutageDegradationReport> RetainAsync(
        ScopedOutageDegradationReport report,
        CancellationToken cancellationToken)
    {
        try
        {
            await evidenceSink.RecordAsync(report, cancellationToken).ConfigureAwait(false);
            return report;
        }
        catch (Exception) when (cancellationToken is { IsCancellationRequested: false })
        {
            ScopedOutageDegradationReport unmeasurable = ScopedOutageDegradationReport.Unmeasurable(
                report.TenantRef,
                report.Dependency,
                report.CorrelationId,
                report.StartedAtUtc,
                clock.UtcNow);

            // Keep WHY it is unmeasurable in the artifact. The verdict model is unchanged — a retention failure is
            // still a stop-ship — but the deviation distinguishes "the sink was unavailable" from "the outage exercise
            // failed".
            unmeasurable = unmeasurable with
            {
                Deviations = [.. unmeasurable.Deviations, ScopedOutageDegradationReport.EvidenceRetentionFailedDeviation],
            };
            try
            {
                await evidenceSink.RecordAsync(unmeasurable, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception) when (cancellationToken is { IsCancellationRequested: false })
            {
                // The caller still receives an unmeasurable stop-ship report when the retaining sink is unavailable.
            }

            return unmeasurable;
        }
    }

    private async ValueTask<bool> AuditThenAlertAsync(
        ScopedOutageDegradationReport report,
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
        AuditEnvelope envelope = AuditEnvelopeFactory.ScopedOutageDegradationBreach(report, correlationId, now);
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
                    OperatorAlertKind.ScopedOutageDegradationBreach,
                    report.ReasonCode,
                    report.TenantRef,
                    "ScopedOutageDegradationBreach",
                    correlationId,
                    now,
                    report.FirstBreachLocator ?? (report.Deviations.Count > 0 ? report.Deviations[0] : null)),
                cancellationToken)
            .ConfigureAwait(false);

        return true;
    }
}
