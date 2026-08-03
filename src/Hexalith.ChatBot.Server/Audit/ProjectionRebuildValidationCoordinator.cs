using Hexalith.ChatBot.Server.Gateway.Stages;
using Hexalith.ChatBot.Server.Projections;

namespace Hexalith.ChatBot.Server.Audit;

/// <summary>
/// Injectable projection-rebuild validation coordinator (Story 9.12, AC1–AC4, NFR57/NFR9a/NFR59) following the
/// <see cref="ContinuityDrillCoordinator"/> / <see cref="DerivedStoreIsolationProbeCoordinator"/> discipline
/// <b>exactly</b>: a pure evaluator (<see cref="ProjectionRebuildEquivalenceEvaluator"/>) over the structural snapshots +
/// measured duration from the <see cref="IProjectionRebuildDriver"/> seam, followed by fail-closed audit-then-deliver on
/// a breach. For each baseline validation dataset it drives a rebuild of a <b>test tenant's</b> derived projections
/// <b>from immutable source records + WORM audit history only</b> (never a mailbox/Graph re-ingestion) and produces a
/// metadata-only <see cref="ProjectionRebuildReport"/> — the NFR57 validation evidence.
/// <para>
/// <b>Test-tenant by construction (NFR9a/NFR59).</b> A validation must never target a production tenant: the coordinator
/// fails closed to an <c>unmeasurable</c> report when <see cref="ReplayTenantPolicy.IsTestTenant"/> is false — without
/// invoking the driver — so the rebuild lands only in the test tenant's partition and no production-tenant durable
/// projection state is mutated. Never a fabricated <c>equivalent</c>.
/// </para>
/// <para>
/// <b>Fail-safe over fabrication (Epic 8/9).</b> A driver that throws, a rebuild that never completes, or unavailable
/// snapshots/durations yield <see cref="ProjectionRebuildReport.Unmeasurable"/> — a breach signal — never a silent pass.
/// On any breach (a <c>divergent</c> rebuild, a duration overrun, <b>or</b> an unmeasurable validation) it writes the
/// <see cref="AuditEnvelopeFactory.ProjectionRebuildValidationFailed"/> pre-commit envelope <b>before</b> emitting exactly
/// one <see cref="OperatorAlertKind.ProjectionRebuildValidationFailed"/> alert (audit-down ⇒ no alert, but the report is
/// still returned).
/// </para>
/// <para>
/// <b>Three distinct breach dimensions.</b> A <c>divergent</c> rebuild is the serious determinism breach (NFR49a evidence
/// reproducibility / invariant #11); a duration overrun (<see cref="ProjectionRebuildReport.DurationWithinTarget"/>
/// false) is a recovery-time miss / recalibration signal (a deterministic-but-slow rebuild stays <c>equivalent</c>);
/// an <c>unmeasurable</c> validation is the fail-safe breach. The structured <see cref="ProjectionRebuildOutcome"/> keeps
/// Equivalent/Divergent/DurationExceeded/Unmeasurable distinct so a CI/release gate asserts the dimension it cares about.
/// No always-on <c>BackgroundService</c> is introduced — a periodic scheduler AND a release gate need only call
/// <see cref="RunAllAsync"/> on its cadence (inert-control-floor, Story 9.1/9.2/9.4/9.5/9.11). The test tenant + dataset
/// refs are supplied by the caller (there is no tenant store to enumerate for a validation rebuild); the coordinator
/// still fails closed via <see cref="ReplayTenantPolicy.IsTestTenant"/>.
/// </para>
/// </summary>
internal sealed class ProjectionRebuildValidationCoordinator(
    IProjectionRebuildDriver rebuildDriver,
    IAuditWriter auditWriter,
    IOperatorAlertSink operatorAlertSink,
    ISystemClock clock,
    IRecoveryValidationEvidenceSink evidenceSink)
{
    /// <summary>Creates a coordinator with the inert product evidence sink for existing non-live callers.</summary>
    internal ProjectionRebuildValidationCoordinator(
        IProjectionRebuildDriver rebuildDriver,
        IAuditWriter auditWriter,
        IOperatorAlertSink operatorAlertSink,
        ISystemClock clock)
        : this(rebuildDriver, auditWriter, operatorAlertSink, clock, DiscardingRecoveryValidationEvidenceSink.Instance)
    {
    }

    /// <summary>
    /// Runs one baseline-dataset validation against the test tenant and, on breach, audits-then-alerts. Returns the
    /// metadata-only report (the NFR57 validation evidence).
    /// </summary>
    /// <param name="testTenantRef">The test tenant the rebuild runs against (must satisfy <see cref="ReplayTenantPolicy.IsTestTenant"/>).</param>
    /// <param name="datasetRef">The baseline validation dataset id.</param>
    /// <param name="correlationId">The run correlation id.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The validation report.</returns>
    public async ValueTask<ProjectionRebuildReport> RunValidationAndRecordAsync(
        string testTenantRef,
        string datasetRef,
        string correlationId,
        CancellationToken cancellationToken)
    {
        (ProjectionRebuildReport report, _) = await RunValidationInternalAsync(
            testTenantRef,
            datasetRef,
            correlationId,
            cancellationToken)
            .ConfigureAwait(false);
        return report;
    }

    /// <summary>
    /// Runs every baseline validation dataset against the configured test tenant (the method a periodic scheduler AND a
    /// release gate call) and tallies the structured outcome. <c>Divergent == 0 &amp;&amp; Unmeasurable == 0</c> ⇒ the
    /// rebuilds are deterministic and produced evidence.
    /// </summary>
    /// <param name="testTenantRef">The validation test tenant (must satisfy <see cref="ReplayTenantPolicy.IsTestTenant"/>).</param>
    /// <param name="datasetRefs">The baseline validation dataset ids to run.</param>
    /// <param name="runCorrelationId">The run correlation id applied to every dataset.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The structured validation outcome.</returns>
    public async ValueTask<ProjectionRebuildOutcome> RunAllAsync(
        string testTenantRef,
        IReadOnlyList<string> datasetRefs,
        string runCorrelationId,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(testTenantRef);
        ArgumentNullException.ThrowIfNull(datasetRefs);
        ArgumentException.ThrowIfNullOrWhiteSpace(runCorrelationId);

        int tenantsValidated = 0;
        int equivalent = 0;
        int divergent = 0;
        int durationExceeded = 0;
        int unmeasurable = 0;
        int alerted = 0;
        foreach (string datasetRef in datasetRefs)
        {
            tenantsValidated++;
            (ProjectionRebuildReport report, bool didAlert) = await RunValidationInternalAsync(
                testTenantRef,
                datasetRef,
                runCorrelationId,
                cancellationToken)
                .ConfigureAwait(false);

            if (string.Equals(report.Verdict, ProjectionRebuildVerdicts.Equivalent, StringComparison.Ordinal))
            {
                equivalent++;
            }
            else if (report.IsDivergent)
            {
                divergent++;
            }
            else
            {
                unmeasurable++;
            }

            // A duration overrun is counted independently of the equivalence verdict: a deterministic-but-slow rebuild is
            // equivalent AND DurationExceeded — the recovery-time miss is a distinct dimension a gate may assert.
            if (!report.DurationWithinTarget && !string.Equals(report.Verdict, ProjectionRebuildVerdicts.Unmeasurable, StringComparison.Ordinal))
            {
                durationExceeded++;
            }

            if (didAlert)
            {
                alerted++;
            }
        }

        return new ProjectionRebuildOutcome(tenantsValidated, equivalent, divergent, durationExceeded, unmeasurable, alerted);
    }

    private async ValueTask<(ProjectionRebuildReport Report, bool Alerted)> RunValidationInternalAsync(
        string testTenantRef,
        string datasetRef,
        string correlationId,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(testTenantRef);
        ArgumentException.ThrowIfNullOrWhiteSpace(datasetRef);
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);

        DateTimeOffset now = clock.UtcNow;

        // Fail-closed: a validation must never target a production tenant. The driver is never invoked; the report is an
        // unmeasurable breach signal, never a fabricated equivalent.
        if (!ReplayTenantPolicy.IsTestTenant(testTenantRef))
        {
            ProjectionRebuildReport unmeasurable = ProjectionRebuildReport.Unmeasurable(
                testTenantRef,
                datasetRef,
                correlationId,
                now,
                now,
                GovernedOperationView.CurrentSchemaVersion);
            unmeasurable = await RetainAsync(unmeasurable, cancellationToken).ConfigureAwait(false);
            bool didAlertGuard = await AuditThenAlertAsync(unmeasurable, correlationId, cancellationToken).ConfigureAwait(false);
            return (unmeasurable, didAlertGuard);
        }

        ProjectionRebuildReport report;
        try
        {
            ProjectionRebuildMeasurement measurement = await rebuildDriver
                .RebuildAsync(testTenantRef, datasetRef, correlationId, cancellationToken)
                .ConfigureAwait(false);

            bool durationWithinTarget = measurement.MeasuredDuration <= RecoveryTargets.MaxRto;
            string verdict = ProjectionRebuildEquivalenceEvaluator.Evaluate(
                measurement.PreRebuildSnapshot,
                measurement.RebuiltSnapshot,
                measurement.PreRebuildSchemaVersion,
                measurement.RebuiltSchemaVersion);
            IReadOnlyList<string> deviations = ProjectionRebuildEquivalenceEvaluator.Deviations(verdict, durationWithinTarget);
            string? firstDiverging = string.Equals(verdict, ProjectionRebuildVerdicts.Divergent, StringComparison.Ordinal)
                ? ProjectionRebuildEquivalenceEvaluator.FirstDivergingResourceLocator(measurement.PreRebuildSnapshot, measurement.RebuiltSnapshot)
                : null;

            report = new ProjectionRebuildReport(
                testTenantRef,
                datasetRef,
                measurement.StartedAtUtc,
                measurement.EndedAtUtc,
                measurement.MeasuredDuration,
                durationWithinTarget,
                verdict,
                ResourcesCompared: measurement.PreRebuildSnapshot.Count,
                deviations,
                firstDiverging,
                ProjectionSchemaVersion: measurement.RebuiltSchemaVersion,
                correlationId,
                ProjectionRebuildReport.ValidationCompletedReasonCode,
                measurement.ExecutionAssertions);
        }
        catch (Exception) when (cancellationToken is { IsCancellationRequested: false })
        {
            // Fail-safe: a validation that cannot complete is itself a breach signal, never a fabricated equivalent.
            report = ProjectionRebuildReport.Unmeasurable(
                testTenantRef,
                datasetRef,
                correlationId,
                now,
                clock.UtcNow,
                GovernedOperationView.CurrentSchemaVersion);
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
    private async ValueTask<ProjectionRebuildReport> RetainAsync(
        ProjectionRebuildReport report,
        CancellationToken cancellationToken)
    {
        try
        {
            await evidenceSink.RecordAsync(report, cancellationToken).ConfigureAwait(false);
            return report;
        }
        catch (Exception) when (cancellationToken is { IsCancellationRequested: false })
        {
            ProjectionRebuildReport unmeasurable = ProjectionRebuildReport.Unmeasurable(
                report.TenantRef,
                report.DatasetRef,
                report.CorrelationId,
                report.StartedAtUtc,
                clock.UtcNow,
                report.ProjectionSchemaVersion);

            // Keep WHY it is unmeasurable in the artifact. The verdict model is unchanged — a retention failure is
            // still a stop-ship — but the deviation distinguishes "the sink was unavailable" from "rebuild failed".
            unmeasurable = unmeasurable with
            {
                Deviations = [.. unmeasurable.Deviations, ProjectionRebuildReport.EvidenceRetentionFailedDeviation],
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
        ProjectionRebuildReport report,
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
        AuditEnvelope envelope = AuditEnvelopeFactory.ProjectionRebuildValidationFailed(report, correlationId, now);
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
                    OperatorAlertKind.ProjectionRebuildValidationFailed,
                    report.ReasonCode,
                    report.TenantRef,
                    "ProjectionRebuildValidationFailed",
                    correlationId,
                    now,
                    report.FirstDivergingResourceLocator ?? (report.Deviations.Count > 0 ? report.Deviations[0] : null)),
                cancellationToken)
            .ConfigureAwait(false);

        return true;
    }
}
