using System.Collections.Concurrent;

using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Contracts.Queries;
using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Notifications;
using Hexalith.ChatBot.Server.Observability;
using Hexalith.ChatBot.Server.Projections;

using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Hexalith.ChatBot.Server.Operations.PeriodicEnforcement;

internal sealed class PeriodicEnforcementCoordinator(
    IPeriodicEnforcementInputSource inputSource,
    IGovernedControlStateProjectionStore controlStateStore,
    EscalationEvaluationCoordinator escalationCoordinator,
    NotificationThrottleCoordinator throttleCoordinator,
    ReviewerBacklogAlertCoordinator backlogCoordinator,
    ApprovalRubberStampRateCoordinator rubberStampCoordinator,
    OperationalAlertWiringCoordinator alertCoordinator,
    AuditChainVerificationCoordinator auditChainVerificationCoordinator,
    ReplayIsolationProbeCoordinator replayIsolationProbeCoordinator,
    DerivedStoreIsolationProbeCoordinator derivedStoreIsolationProbeCoordinator,
    AuditCompletenessMeasurer completenessMeasurer,
    AuditCompletenessAlertCoordinator completenessAlertCoordinator,
    SweepBackedAuditCompletenessSource completenessSource,
    IAuditProjectionCheckpointSource checkpointSource,
    CheckpointBackedAuditProjectionLagSource lagSource,
    IOperatorAlertSink operatorAlertSink,
    IPeriodicEnforcementStatusStore statusStore,
    ISystemClock clock,
    IOptions<PeriodicEnforcementOptions> options)
{
    // Committed only after a sweep completes — see RunM2SweepAsync.
    private readonly ConcurrentDictionary<string, string> _lastM2SweepPartitionByJob = new(StringComparer.Ordinal);

    // The failed-attempt backoff, scoped to the partition it was recorded in. Keying it by time alone let the backoff
    // outlive its own period: a sweep that failed late in partition N still suppressed the first attempt of N+1.
    private readonly ConcurrentDictionary<string, (string Partition, DateTimeOffset AttemptedAtUtc)> _lastM2SweepAttemptByJob = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, DateTimeOffset> _lastSchedulerAlertByReason = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, string> _lastRunbookWeekByTenant = new(StringComparer.Ordinal);
    private readonly DateTimeOffset _m2SweepMonitoringStartedAtUtc = clock.UtcNow;
    private int _running;

    public PeriodicEnforcementRunStatus Status => statusStore.Read();

    public bool M2SweepsEnabled => options.Value.RunM2AuditRecoverySweeps;

    public PeriodicEnforcementM2ReleaseGateResponse M2ReleaseGateStatus
        => PeriodicEnforcementM2ReleaseGateResponse.From(
            Status,
            M2SweepsEnabled,
            clock.UtcNow,
            options.Value.M2SweepCadence + options.Value.MissedCadenceAlertAfter);

    public async ValueTask<PeriodicEnforcementRunOutcome> RunOnceAsync(
        string correlationId,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);

        DateTimeOffset started = clock.UtcNow;
        if (Interlocked.Exchange(ref _running, 1) == 1)
        {
            statusStore.RecordOverlap(started, correlationId);
            await EmitSchedulerAlertAsync("periodic_enforcement_overlap_skipped", correlationId, cancellationToken).ConfigureAwait(false);
            return new PeriodicEnforcementRunOutcome(
                correlationId,
                started,
                started,
                TenantsEvaluated: 0,
                EvaluatorsFailed: 1,
                ControlStateHeartbeats: 0,
                new RunbookDiagnosticCompletenessReport(0, 0, []),
                AuditChainVerification: null,
                ReplayIsolationProbe: null,
                DerivedStoreIsolationProbe: null);
        }

        statusStore.RecordStarted(started, correlationId);
        int failures = 0;
        int tenantsEvaluated = 0;
        int heartbeats = 0;
        int runbookTenantsSampled = 0;
        RunbookDiagnosticCompletenessReport runbookAggregate = new(0, 0, []);
        AuditChainVerificationOutcome? auditChainVerification = null;
        ReplayIsolationProbeOutcome? replayIsolationProbe = null;
        DerivedStoreIsolationProbeOutcome? derivedStoreIsolationProbe = null;
        M2SweepExecution auditChainVerificationExecution = M2SweepExecution.Skipped;
        M2SweepExecution replayIsolationProbeExecution = M2SweepExecution.Skipped;
        M2SweepExecution derivedStoreIsolationProbeExecution = M2SweepExecution.Skipped;

        try
        {
            // Tenant discovery/input/control-state work belongs to the ordinary enforcement phase. The M2
            // coordinators below self-enumerate their own stores, so an unavailable projection/input source must not
            // suppress those process-global audit and isolation sweeps. Keep the required ordering, but contain an
            // unrelated tenant-phase failure at the same evaluator seam used by the rest of the pass.
            int tenantPhaseFailure = await RunEvaluatorAsync("tenant-enforcement", async () =>
            {
                IReadOnlyList<string> tenants = await EnumerateTenantsAsync(cancellationToken).ConfigureAwait(false);
                foreach (string tenant in tenants)
                {
                    tenantsEvaluated++;
                    PeriodicEnforcementTenantInputs inputs = await inputSource.GetTenantInputsAsync(tenant, cancellationToken).ConfigureAwait(false);
                    heartbeats += await RefreshControlStateAsync(tenant, cancellationToken).ConfigureAwait(false);

                    failures += await RunEvaluatorAsync("escalation", () => escalationCoordinator.EvaluateAndDeliverAsync(
                        inputs.QueueItems.Select(static item => new EscalationQueueItem(item, item.GroupProjectRef)).ToArray(),
                        inputs.EscalationPolicy,
                        inputs.RecipientCandidates,
                        tenant,
                        correlationId,
                        cancellationToken).AsTask()).ConfigureAwait(false);

                    failures += await RunEvaluatorAsync("notification-throttle", () => throttleCoordinator.EvaluateAndDeliverAsync(
                        inputs.NotificationDeliveries,
                        inputs.ThrottleCeilings,
                        tenant,
                        cancellationToken).AsTask()).ConfigureAwait(false);

                    failures += await RunEvaluatorAsync("reviewer-backlog", () => backlogCoordinator.EvaluateAndDeliverAsync(
                        inputs.QueueItems,
                        inputs.RecipientCandidates,
                        tenant,
                        correlationId,
                        inputs.ReviewerBacklogThreshold,
                        cancellationToken).AsTask()).ConfigureAwait(false);

                    failures += await RunEvaluatorAsync("approval-rubber-stamp", () => rubberStampCoordinator.EvaluateAndRecordAsync(
                        inputs.ApprovalDecisionSamples,
                        tenant,
                        correlationId,
                        cancellationToken).AsTask()).ConfigureAwait(false);

                    failures += await RunEvaluatorAsync("operational-alerts", () => alertCoordinator.EvaluateAndDeliverAsync(
                        inputs.QueueItems,
                        inputs.RecipientCandidates,
                        tenant,
                        correlationId,
                        cancellationToken).AsTask()).ConfigureAwait(false);

                    (RunbookDiagnosticCompletenessReport report, bool runbookExecuted) = await RunRunbookSamplerAsync(tenant, inputs.RunbookDiagnostics, correlationId, cancellationToken)
                        .ConfigureAwait(false);
                    if (runbookExecuted)
                    {
                        runbookTenantsSampled++;
                        runbookAggregate = Merge(runbookAggregate, report);
                    }
                }

                // AC5/NFR44: record metadata-only positive evidence of the weekly sweep (sampled/complete/defect
                // counts, swept-at, correlation) when at least one tenant's sample actually ran this pass.
                if (runbookTenantsSampled > 0)
                {
                    statusStore.RecordRunbookSweep(new PeriodicEnforcementRunbookEvidence(
                        runbookAggregate.Sampled,
                        runbookAggregate.Complete,
                        runbookAggregate.DefectWorkflowItemRefs.Count,
                        clock.UtcNow,
                        correlationId));
                }
            }).ConfigureAwait(false);
            failures += tenantPhaseFailure;

            if (options.Value.RunM2AuditRecoverySweeps)
            {
                // One clock read drives all three cadence gates. Reading clock.UtcNow per gate lets a sweep that
                // straddles the partition boundary push the later sweeps into the NEXT partition early — they then
                // skip that whole partition, opening a two-period coverage hole and firing false missed-cadence
                // alerts for the rest of it.
                DateTimeOffset m2Now = clock.UtcNow;

                (auditChainVerification, M2SweepExecution wormExecution) = await RunM2SweepAsync(
                    M2SweepJobs.WormAuditChain,
                    m2Now,
                    correlationId,
                    ct => auditChainVerificationCoordinator.VerifyAllTenantsAsync(correlationId, ct),
                    // Population == coverage for the single-tenant sweeps: each enumerates tenants directly, so there
                    // is no structural floor below which "nothing probed" is expected.
                    static outcome => (outcome.Breaches, outcome.Alerted, outcome.TenantsChecked, outcome.TenantsChecked),
                    cancellationToken).ConfigureAwait(false);

                (replayIsolationProbe, M2SweepExecution replayExecution) = await RunM2SweepAsync(
                    M2SweepJobs.ReplayIsolationProbe,
                    m2Now,
                    correlationId,
                    ct => replayIsolationProbeCoordinator.SweepAllProductionTenantsAsync(correlationId, ct),
                    static outcome => (outcome.Breaches, outcome.Alerted, outcome.TenantsSwept, outcome.TenantsSwept),
                    cancellationToken).ConfigureAwait(false);

                (derivedStoreIsolationProbe, M2SweepExecution derivedStoreExecution) = await RunM2SweepAsync(
                    M2SweepJobs.DerivedStoreIsolationProbe,
                    m2Now,
                    correlationId,
                    ct => derivedStoreIsolationProbeCoordinator.SweepAllTenantPairsAsync(correlationId, ct),
                    // Coverage is pairs probed; population is the tenant set those pairs are drawn from. They are not
                    // the same number (n tenants ⇒ n·(n−1) pairs), and below two tenants there are no pairs at all —
                    // which is why the gate needs the population to tell "nothing to check" from "checked nothing".
                    static outcome => (outcome.Breaches, outcome.Alerted, outcome.PartitionsProbed, outcome.TenantsEnumerated),
                    cancellationToken).ConfigureAwait(false);

                auditChainVerificationExecution = wormExecution;
                replayIsolationProbeExecution = replayExecution;
                derivedStoreIsolationProbeExecution = derivedStoreExecution;
                failures += (wormExecution is M2SweepExecution.Failed ? 1 : 0) +
                    (replayExecution is M2SweepExecution.Failed ? 1 : 0) +
                    (derivedStoreExecution is M2SweepExecution.Failed ? 1 : 0);
            }

            failures += await RunEvaluatorAsync("audit-completeness", async () =>
            {
                IReadOnlyList<AuditCompletenessMeasurement> measurements = await completenessMeasurer
                    .MeasureAllTenantsAsync(cancellationToken)
                    .ConfigureAwait(false);
                completenessSource.Publish(measurements);
                _ = await completenessAlertCoordinator
                    .MeasureAllTenantsAndAlertAsync(correlationId, cancellationToken)
                    .ConfigureAwait(false);
            }).ConfigureAwait(false);

            failures += await RunEvaluatorAsync("audit-projection-lag", async () =>
            {
                IReadOnlyList<AuditProjectionCheckpoint> checkpoints = await checkpointSource
                    .ReadCheckpointsAsync(cancellationToken)
                    .ConfigureAwait(false);
                lagSource.Publish(checkpoints);
            }).ConfigureAwait(false);

            DateTimeOffset completed = clock.UtcNow;
            TimeSpan duration = completed - started;
            if (failures == 0)
            {
                statusStore.RecordSucceeded(completed, duration);
            }
            else
            {
                statusStore.RecordFailed(completed, duration);
            }

            return new PeriodicEnforcementRunOutcome(
                correlationId,
                started,
                completed,
                tenantsEvaluated,
                failures,
                heartbeats,
                runbookAggregate,
                auditChainVerification,
                replayIsolationProbe,
                derivedStoreIsolationProbe,
                auditChainVerificationExecution,
                replayIsolationProbeExecution,
                derivedStoreIsolationProbeExecution);
        }
        catch
        {
            DateTimeOffset failedAt = clock.UtcNow;
            statusStore.RecordFailed(failedAt, failedAt - started);
            throw;
        }
        finally
        {
            _ = Interlocked.Exchange(ref _running, 0);
        }
    }

    public async ValueTask CheckHealthAsync(string correlationId, CancellationToken cancellationToken)
    {
        PeriodicEnforcementRunStatus status = statusStore.Read();
        DateTimeOffset now = clock.UtcNow;
        TimeSpan staleAfter = options.Value.MissedCadenceAlertAfter;
        DateTimeOffset? lastObserved = status.LastSucceededAtUtc ?? status.LastStartedAtUtc;
        if (lastObserved is null || now - lastObserved.Value > staleAfter)
        {
            await EmitSchedulerAlertAsync("periodic_enforcement_missed_cadence", correlationId, cancellationToken).ConfigureAwait(false);
        }

        if (status.IsRunning && status.LastStartedAtUtc is { } started && now - started > staleAfter)
        {
            await EmitSchedulerAlertAsync("periodic_enforcement_stalled", correlationId, cancellationToken).ConfigureAwait(false);
        }

        // A pass that throws is recorded by RunOnceAsync and then swallowed by the background loop, so that it cannot
        // stop the host. Nothing else reported it: RecordStarted stamps LastStartedAtUtc *before* the throw, so the
        // missed-cadence check above sees this tick's own start and computes a near-zero age; IsRunning is already
        // false so the stall check cannot fire either; and LastFailedAtUtc was written but read nowhere. A runtime
        // whose every pass threw — an unavailable projection store at boot, say — therefore reported healthy forever,
        // where before the exception escaped and StopHost made it loud. This is what makes the failure observable.
        if (status.LastFailedAtUtc is { } failedAt &&
            (status.LastSucceededAtUtc is not { } succeededAt || failedAt > succeededAt))
        {
            await EmitSchedulerAlertThrottledAsync("periodic_enforcement_pass_failed", now, correlationId, cancellationToken)
                .ConfigureAwait(false);
        }

        if (options.Value.RunM2AuditRecoverySweeps)
        {
            TimeSpan m2CadenceBudget = options.Value.M2SweepCadence + staleAfter;
            await CheckM2SweepHealthAsync(
                M2SweepJobs.WormAuditChain,
                "m2_worm_verify_missed_cadence",
                status,
                now,
                m2CadenceBudget,
                correlationId,
                cancellationToken).ConfigureAwait(false);
            await CheckM2SweepHealthAsync(
                M2SweepJobs.ReplayIsolationProbe,
                "m2_replay_isolation_missed_cadence",
                status,
                now,
                m2CadenceBudget,
                correlationId,
                cancellationToken).ConfigureAwait(false);
            await CheckM2SweepHealthAsync(
                M2SweepJobs.DerivedStoreIsolationProbe,
                "m2_derived_store_isolation_missed_cadence",
                status,
                now,
                m2CadenceBudget,
                correlationId,
                cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Emits the per-sweep missed-cadence alert, de-duplicated.
    /// </summary>
    /// <remarks>
    /// Unlike <c>periodic_enforcement_missed_cadence</c> — which self-clears on the next tick because the enforcement
    /// pass runs every tick — an overdue M2 sweep stays overdue until its next partition. Emitting on every tick would
    /// append roughly 1,400 identical alerts per job per day to the operator-alert sink, which is an unbounded
    /// in-memory list. The suppression window keeps the signal without the flood.
    /// <para>
    /// The baseline prefers the last success, then the last <em>attempt</em>, and only then process start. Falling
    /// straight back to process start made "never succeeded" and "attempted and failed every time" indistinguishable,
    /// so a pod restarting more often than the budget could never alert. Note the residual: the status store is still
    /// in-memory, so a restart loop that never attempts at all continues to reset the window — durable scheduler
    /// status is tracked as deferred work.
    /// </para>
    /// </remarks>
    private async ValueTask CheckM2SweepHealthAsync(
        string jobName,
        string missedCadenceReason,
        PeriodicEnforcementRunStatus status,
        DateTimeOffset now,
        TimeSpan cadenceBudget,
        string correlationId,
        CancellationToken cancellationToken)
    {
        _ = status.M2SweepStatuses.TryGetValue(jobName, out PeriodicEnforcementM2SweepStatus? sweepStatus);

        // The baseline is the last *success*, or else when monitoring began. It deliberately does NOT fall back to
        // LastRanAtUtc: that field is refreshed on every attempt including failures, so a sweep failing on its
        // M2SweepRetryAfter loop kept the baseline permanently fresh and this check could never fire — the exact
        // "attempted and failed every time" case it exists for. That case is covered loudly by the per-sweep
        // m2_*_sweep_failed alert instead. The restart-loop residual (in-memory status resets the window on every
        // boot) is unchanged and tracked as deferred work.
        DateTimeOffset baseline = sweepStatus?.LastSucceededAtUtc ?? _m2SweepMonitoringStartedAtUtc;
        TimeSpan sinceBaseline = now - baseline;
        if (sinceBaseline < TimeSpan.Zero)
        {
            // The clock moved backwards past a recorded success. Report it rather than early-returning: the same
            // regression stalls the cadence gate, so silence here means a silent stall.
            await EmitSchedulerAlertThrottledAsync(
                "periodic_enforcement_clock_regression",
                now,
                correlationId,
                cancellationToken).ConfigureAwait(false);
            return;
        }

        if (sinceBaseline <= cadenceBudget)
        {
            return;
        }

        await EmitSchedulerAlertThrottledAsync(missedCadenceReason, now, correlationId, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Emits a scheduler alert at most once per <see cref="PeriodicEnforcementOptions.M2MissedCadenceAlertResuppressAfter"/>
    /// per reason code.
    /// </summary>
    /// <remarks>
    /// For conditions that cannot self-clear on the next tick — an overdue sweep stays overdue until its next
    /// partition, a failing sweep keeps failing, a failing pass keeps failing — emitting per tick appends roughly
    /// 1,400 identical alerts per reason per day to an unbounded, never-drained in-memory sink. The suppression stamp
    /// is written only *after* a successful emit: recording it first meant a transient sink failure at the moment a
    /// condition first tripped silenced it for the whole window with nothing ever delivered.
    /// </remarks>
    private async ValueTask EmitSchedulerAlertThrottledAsync(
        string reasonCode,
        DateTimeOffset now,
        string correlationId,
        CancellationToken cancellationToken)
    {
        if (_lastSchedulerAlertByReason.TryGetValue(reasonCode, out DateTimeOffset lastAlert))
        {
            TimeSpan sinceAlert = now - lastAlert;
            if (sinceAlert >= TimeSpan.Zero && sinceAlert < options.Value.M2MissedCadenceAlertResuppressAfter)
            {
                return;
            }
        }

        if (await TryEmitSchedulerAlertAsync(reasonCode, correlationId, cancellationToken).ConfigureAwait(false))
        {
            _lastSchedulerAlertByReason[reasonCode] = now;
        }
    }

    private async ValueTask<IReadOnlyList<string>> EnumerateTenantsAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<string> inputTenants = await inputSource.GetTenantRefsAsync(cancellationToken).ConfigureAwait(false);
        IReadOnlyList<string> controlTenants = await controlStateStore.EnumerateTenantIdsAsync(cancellationToken).ConfigureAwait(false);
        return inputTenants
            .Concat(controlTenants)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();
    }

    private async ValueTask<int> RefreshControlStateAsync(string tenant, CancellationToken cancellationToken)
    {
        DateTimeOffset now = clock.UtcNow;
        int refreshed = 0;
        IReadOnlyList<GovernedControlStateView> candidates = await controlStateStore
            .ReadRefreshCandidatesAsync(tenant, cancellationToken)
            .ConfigureAwait(false);
        foreach (GovernedControlStateView candidate in candidates)
        {
            if (candidate.RevocationSensitive ||
                !string.Equals(candidate.ControlState, GovernedControlStateView.Active, StringComparison.Ordinal) ||
                now - candidate.LastUpdatedAtUtc < options.Value.ControlStateHeartbeatBeforeStale)
            {
                continue;
            }

            if (await controlStateStore.TryRefreshFreshnessAsync(candidate, now, cancellationToken).ConfigureAwait(false))
            {
                refreshed++;
            }
        }

        return refreshed;
    }

    private async ValueTask<(RunbookDiagnosticCompletenessReport Report, bool Executed)> RunRunbookSamplerAsync(
        string tenant,
        IReadOnlyList<OperationalQueueDiagnostics> diagnostics,
        string correlationId,
        CancellationToken cancellationToken)
    {
        DateTimeOffset now = clock.UtcNow;
        string week = WeeklyPartitionKey(tenant, now);

        // NFR44 is a *weekly* sample: although the enforcement pass runs on the (sub-weekly) trigger cadence, the
        // runbook sweep — and its defect alerts — must fire at most once per ISO week per tenant. Re-running on every
        // cadence tick would re-emit the same defect alert each minute instead of once per week. The guard is in-memory
        // and tenant-partitioned; a process restart simply lets that week's sample run once more, which is harmless.
        if (_lastRunbookWeekByTenant.TryGetValue(tenant, out string? lastWeek) &&
            string.Equals(lastWeek, week, StringComparison.Ordinal))
        {
            return (new RunbookDiagnosticCompletenessReport(0, 0, []), false);
        }

        OperationalQueueDiagnostics[] sample = SelectRunbookSample(week, diagnostics, options.Value.RunbookSampleSize);
        RunbookDiagnosticCompletenessReport report = RunbookDiagnosticCompletenessValidator.EvaluateSample(sample);
        _lastRunbookWeekByTenant[tenant] = week;
        if (report.DefectWorkflowItemRefs.Count > 0)
        {
            await operatorAlertSink
                .EmitAsync(
                    new OperatorAlert(
                        OperatorAlertKind.DependencyDegraded,
                        "runbook_diagnostic_defect_detected",
                        tenant,
                        "PeriodicRunbookSampler",
                        correlationId,
                        now,
                        $"sampled:{report.Sampled}|complete:{report.Complete}|defects:{report.DefectWorkflowItemRefs.Count}|first:{report.DefectWorkflowItemRefs[0]}"),
                    cancellationToken)
                .ConfigureAwait(false);
        }

        return (report, true);
    }

    private async ValueTask<int> RunEvaluatorAsync(string evaluatorName, Func<Task> action)
    {
        try
        {
            await action().ConfigureAwait(false);
            return 0;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            statusStore.RecordEvaluatorFailure(evaluatorName);
            return 1;
        }
    }

    /// <summary>
    /// Runs one M2 sweep under its cadence gate and records its status.
    /// </summary>
    /// <remarks>
    /// The cadence partition is committed only after the sweep <em>completes</em>. Claiming it up front (as the first
    /// implementation did) meant a single transient store failure consumed the whole period: the sweep threw, the
    /// partition was already recorded as entered, and every subsequent tick in that period skipped it — a two-second
    /// blip cost a full day of unperformed WORM/isolation verification. A failed sweep now retries after
    /// <see cref="PeriodicEnforcementOptions.M2SweepRetryAfter"/> instead, which bounds retry cost without burning the
    /// period.
    /// <para>
    /// The sweep's own breach alerting is fail-closed but conditional: each coordinator writes a pre-commit audit
    /// envelope and emits its operator alert only if that write succeeded, deliberately surfacing an un-alerted breach
    /// to the caller instead. This method is that caller, so it reconciles <c>Breaches</c> against <c>Alerted</c> and
    /// raises its own alert for the difference — otherwise a real breach detected while the audit writer is degraded
    /// would produce no operator signal at all.
    /// </para>
    /// </remarks>
    private async ValueTask<(TOutcome? Outcome, M2SweepExecution Execution)> RunM2SweepAsync<TOutcome>(
        string jobName,
        DateTimeOffset now,
        string correlationId,
        Func<CancellationToken, ValueTask<TOutcome>> sweep,
        Func<TOutcome, (int Breaches, int Alerted, int Coverage, int Population)> project,
        CancellationToken cancellationToken)
        where TOutcome : class
    {
        if (!ShouldRunM2Sweep(jobName, now, out string partition))
        {
            return (null, M2SweepExecution.Skipped);
        }

        _lastM2SweepAttemptByJob[jobName] = (partition, now);
        statusStore.RecordM2SweepRan(jobName, now, correlationId);

        TOutcome? outcome = null;
        int failed = await RunEvaluatorAsync(jobName, async () =>
        {
            // The sweep gets its own deadline. Without one, a sweep that hangs rather than throws blocks the pass
            // forever — and because CheckHealthAsync runs only after the pass returns, it also blocks the detector
            // that exists to report the stall. A timeout converts an invisible hang into an ordinary failure, which
            // leaves the partition uncommitted, alerts, and retries.
            using CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(options.Value.M2SweepTimeout);
            try
            {
                outcome = await sweep(timeout.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (timeout.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
            {
                throw new TimeoutException(
                    $"The '{jobName}' M2 sweep exceeded its {options.Value.M2SweepTimeout} deadline.");
            }
        }).ConfigureAwait(false);

        if (failed != 0 || outcome is null)
        {
            // The partition stays uncommitted so the sweep is retried after the backoff. A failed sweep is alerted
            // immediately rather than waiting out the missed-cadence budget: on a process that restarts more often
            // than that budget, the cadence check alone would never fire. Throttled on the reason code — a
            // persistently failing sweep retries every M2SweepRetryAfter, which would otherwise append ~96 identical
            // alerts per job per day to an unbounded in-memory sink.
            await EmitSchedulerAlertThrottledAsync(M2SweepFailedReasonCode(jobName), now, correlationId, cancellationToken)
                .ConfigureAwait(false);
            return (null, M2SweepExecution.Failed);
        }

        (int breaches, int alerted, int coverage, int population) = project(outcome);
        DateTimeOffset succeededAt = clock.UtcNow;
        _lastM2SweepPartitionByJob[jobName] = partition;

        // A sweep that straddled a partition boundary also covers the partition it finished in; without this the
        // gate re-runs the whole O(tenants²) probe minutes later for the period it just crossed into.
        string completionPartition = M2SweepPartitionKey(
            jobName,
            succeededAt,
            options.Value.M2SweepCadence,
            options.Value.M2SweepDayAnchorUtc);
        if (!string.Equals(completionPartition, partition, StringComparison.Ordinal))
        {
            _lastM2SweepPartitionByJob[jobName] = completionPartition;
        }

        statusStore.RecordM2SweepSucceeded(jobName, succeededAt, correlationId, breaches, coverage, population);

        if (breaches > alerted)
        {
            await EmitSchedulerAlertThrottledAsync(
                M2SweepUnalertedBreachReasonCode(jobName),
                now,
                correlationId,
                cancellationToken).ConfigureAwait(false);
        }

        return (outcome, M2SweepExecution.Completed);
    }

    /// <summary>
    /// The cadence gate. Returns <see langword="true"/> when the sweep should run now: its partition has not been
    /// committed yet, and any previous failed attempt is outside the retry backoff.
    /// </summary>
    private bool ShouldRunM2Sweep(string jobName, DateTimeOffset now, out string partition)
    {
        partition = M2SweepPartitionKey(
            jobName,
            now,
            options.Value.M2SweepCadence,
            options.Value.M2SweepDayAnchorUtc);

        if (_lastM2SweepPartitionByJob.TryGetValue(jobName, out string? committedPartition) &&
            string.Equals(committedPartition, partition, StringComparison.Ordinal))
        {
            return false;
        }

        if (!_lastM2SweepAttemptByJob.TryGetValue(jobName, out (string Partition, DateTimeOffset AttemptedAtUtc) lastAttempt))
        {
            return true;
        }

        // The backoff bounds retries *within* a partition, as the option's contract says. Applying it across the
        // boundary meant a sweep that failed late in one period also delayed the first attempt of the next.
        if (!string.Equals(lastAttempt.Partition, partition, StringComparison.Ordinal))
        {
            return true;
        }

        // A backwards clock step (NTP correction, VM snapshot restore) makes this delta negative, which would
        // otherwise hold every sweep off until wall-clock caught up — silently, because the same regression also
        // short-circuits the missed-cadence check. Treat it as backoff-expired and let the sweep run.
        TimeSpan sinceAttempt = now - lastAttempt.AttemptedAtUtc;
        return sinceAttempt < TimeSpan.Zero || sinceAttempt >= options.Value.M2SweepRetryAfter;
    }

    private static string M2SweepFailedReasonCode(string jobName) => jobName switch
    {
        M2SweepJobs.WormAuditChain => "m2_worm_verify_sweep_failed",
        M2SweepJobs.ReplayIsolationProbe => "m2_replay_isolation_sweep_failed",
        _ => "m2_derived_store_isolation_sweep_failed",
    };

    private static string M2SweepUnalertedBreachReasonCode(string jobName) => jobName switch
    {
        M2SweepJobs.WormAuditChain => "m2_worm_verify_breach_unalerted",
        M2SweepJobs.ReplayIsolationProbe => "m2_replay_isolation_breach_unalerted",
        _ => "m2_derived_store_isolation_breach_unalerted",
    };

    /// <summary>
    /// Emits one scheduler alert. A failing alert transport is recorded as an evaluator failure rather than
    /// propagated.
    /// </summary>
    /// <remarks>
    /// The M2 sweep's failure and unalerted-breach alerts are emitted outside <see cref="RunEvaluatorAsync"/>'s
    /// delegate, so a throwing <see cref="IOperatorAlertSink"/> used to unwind all the way to <c>RunOnceAsync</c>'s
    /// rethrowing catch — meaning a broken alert transport silently skipped every later evaluator in the pass,
    /// including the audit-completeness measurement and the projection-lag checkpoint publication. Containing the
    /// throw here restores the fail-isolation guarantee AC1 requires without moving every call site inside the
    /// wrapper. Cancellation still propagates: shutdown is not a sink failure.
    /// </remarks>
    private async ValueTask EmitSchedulerAlertAsync(string reasonCode, string correlationId, CancellationToken cancellationToken)
        => _ = await TryEmitSchedulerAlertAsync(reasonCode, correlationId, cancellationToken).ConfigureAwait(false);

    /// <summary>Returns <see langword="true"/> when the sink accepted the alert.</summary>
    private async ValueTask<bool> TryEmitSchedulerAlertAsync(string reasonCode, string correlationId, CancellationToken cancellationToken)
    {
        try
        {
            await operatorAlertSink
                .EmitAsync(
                    new OperatorAlert(
                        OperatorAlertKind.DependencyDegraded,
                        reasonCode,
                        "system",
                        "PeriodicEnforcementRuntime",
                        correlationId,
                        clock.UtcNow,
                        "owner:operations-admin"),
                    cancellationToken)
                .ConfigureAwait(false);
            return true;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            statusStore.RecordEvaluatorFailure("operator-alert-sink");
            return false;
        }
    }

    private static OperationalQueueDiagnostics[] SelectRunbookSample(
        string partition,
        IReadOnlyList<OperationalQueueDiagnostics> diagnostics,
        int sampleSize)
        => diagnostics
            .OrderBy(item => DeterministicKey(partition, item.WorkflowItemRef), StringComparer.Ordinal)
            .Take(Math.Max(0, sampleSize))
            .ToArray();

    // The deterministic per-tenant ISO-week partition: it both seeds the weekly sample selection (so the chosen items
    // rotate by tenant/week) and keys the once-per-week execution guard.
    private static string WeeklyPartitionKey(string tenant, DateTimeOffset now)
    {
        int year = System.Globalization.ISOWeek.GetYear(now.UtcDateTime);
        int week = System.Globalization.ISOWeek.GetWeekOfYear(now.UtcDateTime);
        return $"{tenant}:{year:D4}:W{week:D2}";
    }

    private static string M2SweepPartitionKey(
        string jobName,
        DateTimeOffset now,
        TimeSpan cadence,
        TimeSpan dayAnchorUtc)
    {
        if (cadence <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(cadence), cadence, "The M2 sweep cadence must be positive.");
        }

        if (dayAnchorUtc < TimeSpan.Zero || dayAnchorUtc >= cadence)
        {
            throw new ArgumentOutOfRangeException(
                nameof(dayAnchorUtc),
                dayAnchorUtc,
                "The M2 sweep UTC day anchor must be non-negative and less than the cadence.");
        }

        DateTimeOffset anchored = now.ToUniversalTime() - dayAnchorUtc;

        // InvariantCulture: the key is compared with StringComparison.Ordinal against a previously stored key, so a
        // non-Gregorian ambient calendar would format the same UTC day differently and silently re-open a period that
        // was already swept — duplicating sentinel seeding and breach alerts.
        if (cadence == PeriodicEnforcementOptions.DefaultM2SweepCadence)
        {
            return string.Create(
                System.Globalization.CultureInfo.InvariantCulture,
                $"{jobName}:{anchored.UtcDateTime:yyyyMMdd}");
        }

        long cadencePartition = (anchored.UtcTicks - DateTimeOffset.UnixEpoch.UtcTicks) / cadence.Ticks;
        return string.Create(
            System.Globalization.CultureInfo.InvariantCulture,
            $"{jobName}:{cadencePartition}");
    }

    private static string DeterministicKey(string partition, string itemRef)
    {
        byte[] hash = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(partition + ":" + itemRef));
        return Convert.ToHexString(hash);
    }

    private static RunbookDiagnosticCompletenessReport Merge(
        RunbookDiagnosticCompletenessReport left,
        RunbookDiagnosticCompletenessReport right)
        => new(
            left.Sampled + right.Sampled,
            left.Complete + right.Complete,
            left.DefectWorkflowItemRefs.Concat(right.DefectWorkflowItemRefs).ToArray());
}
