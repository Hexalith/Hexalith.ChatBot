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

internal sealed class PeriodicEnforcementOptions
{
    public static readonly TimeSpan DefaultM2SweepCadence = TimeSpan.FromDays(1);

    public bool UsePeriodicEnforcementRuntime { get; set; }

    public bool RunM2AuditRecoverySweeps { get; set; }

    public TimeSpan Cadence { get; set; } = TimeSpan.FromMinutes(1);

    public TimeSpan MissedCadenceAlertAfter { get; set; } = TimeSpan.FromMinutes(5);

    public TimeSpan ControlStateHeartbeatBeforeStale { get; set; } = TimeSpan.FromMinutes(4);

    public TimeSpan M2SweepCadence { get; set; } = DefaultM2SweepCadence;

    public TimeSpan M2SweepDayAnchorUtc { get; set; }

    /// <summary>
    /// The shortest interval between two attempts of the same M2 sweep within one cadence partition. A sweep that
    /// throws does NOT consume its partition (the partition is committed only on success), so it is retried — but
    /// bounded by this interval rather than on every tick, because these sweeps are expensive (the derived-store
    /// probe is O(tenants²) store round-trips).
    /// </summary>
    public TimeSpan M2SweepRetryAfter { get; set; } = TimeSpan.FromMinutes(15);

    /// <summary>
    /// The suppression window for a repeated scheduler alert whose condition cannot self-clear on the next tick — the
    /// M2 missed-cadence alerts, the per-sweep failure alerts, and the whole-pass failure alert. Without it, one
    /// persistent fault re-alerts on every tick into an unbounded in-memory sink.
    /// </summary>
    public TimeSpan M2MissedCadenceAlertResuppressAfter { get; set; } = TimeSpan.FromHours(1);

    /// <summary>
    /// The deadline for a single M2 sweep. Without it a sweep that hangs (rather than throws) blocks the enforcement
    /// pass forever, and because <c>CheckHealthAsync</c> runs after the pass returns, it also blocks the very detector
    /// meant to report the stall. The derived-store probe is the realistic trigger: it is O(tenants²) store
    /// round-trips against a store that may be unresponsive rather than fast-failing.
    /// </summary>
    public TimeSpan M2SweepTimeout { get; set; } = TimeSpan.FromMinutes(10);

    public int RunbookSampleSize { get; set; } = 100;

    /// <summary>
    /// <see cref="System.Threading.PeriodicTimer"/> rejects a period above <c>Timer.MaxSupportedTimeout</c>
    /// (<c>uint.MaxValue - 1</c> ms, ≈49.71 days). The timer is constructed outside every guarded block in
    /// <c>ExecuteAsync</c>, so an out-of-range value there escapes the background loop and stops the whole host.
    /// </summary>
    private static readonly TimeSpan _maxTimerPeriod = TimeSpan.FromMilliseconds(uint.MaxValue - 1);

    /// <summary>
    /// Validates the operator-supplied configuration. Bound from <c>ChatBot:PeriodicEnforcement</c> and enforced at
    /// startup via <c>ValidateOnStart</c>, so a bad value fails the host deterministically at boot instead of throwing
    /// out of the background loop's first tick (which the default <see cref="BackgroundServiceExceptionBehavior"/>
    /// turns into a whole-host shutdown, potentially minutes after a successful start).
    /// </summary>
    public string? Validate()
    {
        if (Cadence <= TimeSpan.Zero)
        {
            return $"{nameof(Cadence)} must be positive (was {Cadence}).";
        }

        if (Cadence > _maxTimerPeriod)
        {
            return $"{nameof(Cadence)} must not exceed {_maxTimerPeriod} (was {Cadence}); PeriodicTimer rejects a longer period.";
        }

        if (MissedCadenceAlertAfter < TimeSpan.Zero)
        {
            return $"{nameof(MissedCadenceAlertAfter)} must be non-negative (was {MissedCadenceAlertAfter}).";
        }

        if (ControlStateHeartbeatBeforeStale < TimeSpan.Zero)
        {
            return $"{nameof(ControlStateHeartbeatBeforeStale)} must be non-negative (was {ControlStateHeartbeatBeforeStale}).";
        }

        if (M2SweepCadence <= TimeSpan.Zero)
        {
            return $"{nameof(M2SweepCadence)} must be positive (was {M2SweepCadence}).";
        }

        if (RunM2AuditRecoverySweeps && Cadence > M2SweepCadence)
        {
            return $"{nameof(Cadence)} must not exceed {nameof(M2SweepCadence)} when M2 sweeps are enabled (tick {Cadence}, M2 cadence {M2SweepCadence}); the background service cannot run a sweep more often than it ticks.";
        }

        if (M2SweepDayAnchorUtc < TimeSpan.Zero || M2SweepDayAnchorUtc >= M2SweepCadence)
        {
            return $"{nameof(M2SweepDayAnchorUtc)} must be non-negative and less than {nameof(M2SweepCadence)} (anchor {M2SweepDayAnchorUtc}, cadence {M2SweepCadence}).";
        }

        // Both windows are throttles. Accepting zero disabled the guard rather than tightening it: a zero retry
        // interval retries a permanently failing sweep on every tick (~1,440 alerts/day/job), and a zero re-suppress
        // window defeats the alert de-duplication entirely. Neither is a meaningful operator intent.
        if (M2SweepRetryAfter <= TimeSpan.Zero)
        {
            return $"{nameof(M2SweepRetryAfter)} must be positive (was {M2SweepRetryAfter}).";
        }

        if (M2SweepRetryAfter >= M2SweepCadence)
        {
            return $"{nameof(M2SweepRetryAfter)} must be shorter than {nameof(M2SweepCadence)} (retry {M2SweepRetryAfter}, cadence {M2SweepCadence}); otherwise the backoff outlives the period it retries within and a correctly-running scheduler alerts as overdue.";
        }

        if (M2MissedCadenceAlertResuppressAfter <= TimeSpan.Zero)
        {
            return $"{nameof(M2MissedCadenceAlertResuppressAfter)} must be positive (was {M2MissedCadenceAlertResuppressAfter}).";
        }

        if (M2SweepTimeout <= TimeSpan.Zero)
        {
            return $"{nameof(M2SweepTimeout)} must be positive (was {M2SweepTimeout}).";
        }

        if (M2SweepTimeout > M2SweepCadence)
        {
            return $"{nameof(M2SweepTimeout)} must not exceed {nameof(M2SweepCadence)} (timeout {M2SweepTimeout}, cadence {M2SweepCadence}).";
        }

        // The missed-cadence budget is M2SweepCadence + MissedCadenceAlertAfter; reject a pair that would overflow
        // when added rather than throwing OverflowException out of the health check every tick.
        if (M2SweepCadence.Ticks > TimeSpan.MaxValue.Ticks - MissedCadenceAlertAfter.Ticks)
        {
            return $"{nameof(M2SweepCadence)} + {nameof(MissedCadenceAlertAfter)} overflows TimeSpan (cadence {M2SweepCadence}, budget {MissedCadenceAlertAfter}).";
        }

        return RunbookSampleSize < 0
            ? $"{nameof(RunbookSampleSize)} must be non-negative (was {RunbookSampleSize})."
            : null;
    }
}

internal sealed record PeriodicEnforcementTenantInputs(
    IReadOnlyList<AdminQueueSummaryProjectionItem> QueueItems,
    IReadOnlyList<NotificationRecipientCandidate> RecipientCandidates,
    EscalationPolicyChangeSet EscalationPolicy,
    IReadOnlyList<NotificationDelivery> NotificationDeliveries,
    NotificationThrottleCeilings ThrottleCeilings,
    ReviewerBacklogThreshold ReviewerBacklogThreshold,
    IReadOnlyList<ApprovalDecisionSample> ApprovalDecisionSamples,
    IReadOnlyList<OperationalQueueDiagnostics> RunbookDiagnostics);

internal interface IPeriodicEnforcementInputSource
{
    ValueTask<IReadOnlyList<string>> GetTenantRefsAsync(CancellationToken cancellationToken);

    ValueTask<PeriodicEnforcementTenantInputs> GetTenantInputsAsync(string tenantRef, CancellationToken cancellationToken);
}

internal sealed class ProjectionBackedPeriodicEnforcementInputSource(
    IProjectConversationProjectionStore conversationStore,
    ISystemClock clock) : IPeriodicEnforcementInputSource
{
    public async ValueTask<IReadOnlyList<string>> GetTenantRefsAsync(CancellationToken cancellationToken)
        => await conversationStore.EnumerateTenantIdsAsync(cancellationToken).ConfigureAwait(false);

    public async ValueTask<PeriodicEnforcementTenantInputs> GetTenantInputsAsync(
        string tenantRef,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantRef);

        IReadOnlyList<AdminQueueSummaryProjectionItem> queueItems = await conversationStore
            .ReadOperationalQueueItemsAsync(tenantRef, clock.UtcNow, cancellationToken)
            .ConfigureAwait(false);
        IReadOnlyList<ApprovalEventView> approvals = await conversationStore
            .ReadApprovalEventsAsync(tenantRef, cancellationToken)
            .ConfigureAwait(false);

        OperationalQueueDiagnostics[] diagnostics = queueItems
            .Select(static item => AdminQueueSummaryProjector.Search(
                new SearchOperationalQueueItems(
                    item.QueueFamily,
                    PageSize: 1,
                    PageToken: null,
                    OperationalQueueSortKey.Age,
                    SortDescending: true,
                    new OperationalQueueFilter()),
                [item],
                item.CorrelationId ?? "periodic-enforcement").Rows.FirstOrDefault()?.Diagnostics)
            .OfType<OperationalQueueDiagnostics>()
            .ToArray();

        return new PeriodicEnforcementTenantInputs(
            queueItems,
            RecipientCandidates: [],
            new EscalationPolicyChangeSet([]),
            NotificationDeliveries: [],
            NotificationThrottleCeilings.SafeDefaults,
            ReviewerBacklogThreshold.SafeDefault,
            BuildApprovalDecisionSamples(approvals),
            diagnostics);
    }

    private static IReadOnlyList<ApprovalDecisionSample> BuildApprovalDecisionSamples(IReadOnlyList<ApprovalEventView> approvals)
        => approvals
            .Where(static approval => approval.EventKind is ApprovalEventKind.Decision &&
                approval.DecisionKind is not null &&
                approval.RequestedAtUtc is not null &&
                approval.DecidedAtUtc is not null &&
                approval.AiRiskClass is not null)
            .Select(static approval => new ApprovalDecisionSample(
                approval.TenantId,
                approval.DecisionActorId,
                approval.RequestedAtUtc!.Value,
                approval.DecidedAtUtc!.Value,
                approval.DecisionKind!.Value,
                approval.AiRiskClass!.Value))
            .ToArray();
}

internal sealed record PeriodicEnforcementRunOutcome(
    string CorrelationId,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset CompletedAtUtc,
    int TenantsEvaluated,
    int EvaluatorsFailed,
    int ControlStateHeartbeats,
    RunbookDiagnosticCompletenessReport RunbookReport,
    AuditChainVerificationOutcome? AuditChainVerification,
    ReplayIsolationProbeOutcome? ReplayIsolationProbe,
    DerivedStoreIsolationProbeOutcome? DerivedStoreIsolationProbe,
    M2SweepExecution AuditChainVerificationExecution = M2SweepExecution.Skipped,
    M2SweepExecution ReplayIsolationProbeExecution = M2SweepExecution.Skipped,
    M2SweepExecution DerivedStoreIsolationProbeExecution = M2SweepExecution.Skipped);

/// <summary>
/// Metadata-only NFR44 evidence for the most recent weekly runbook-diagnostic sweep (AC5). It carries only the
/// aggregate sampled/complete/defect counts and the sweep timestamp/correlation — never tenant refs, project names,
/// workflow item refs, or any diagnostic content — so it is safe to surface on the tenant-free scheduler health
/// endpoint. Per-tenant defect locators stay on the tenant-scoped operator-alert path.
/// </summary>
internal sealed record PeriodicEnforcementRunbookEvidence(
    int Sampled,
    int Complete,
    int DefectCount,
    DateTimeOffset SweptAtUtc,
    string CorrelationId);

internal sealed record PeriodicEnforcementRunStatus(
    bool IsRunning,
    DateTimeOffset? LastStartedAtUtc,
    DateTimeOffset? LastSucceededAtUtc,
    DateTimeOffset? LastFailedAtUtc,
    TimeSpan? LastDuration,
    long SkippedOverlapCount,
    IReadOnlyDictionary<string, int> EvaluatorFailureCounts,
    string? LastCorrelationId,
    PeriodicEnforcementRunbookEvidence? LastRunbookSweep,
    IReadOnlyDictionary<string, PeriodicEnforcementM2SweepStatus> M2SweepStatuses);

internal interface IPeriodicEnforcementStatusStore
{
    PeriodicEnforcementRunStatus Read();

    void RecordStarted(DateTimeOffset startedAtUtc, string correlationId);

    void RecordSucceeded(DateTimeOffset completedAtUtc, TimeSpan duration);

    void RecordFailed(DateTimeOffset failedAtUtc, TimeSpan duration);

    void RecordOverlap(DateTimeOffset skippedAtUtc, string correlationId);

    void RecordEvaluatorFailure(string evaluatorName);

    void RecordRunbookSweep(PeriodicEnforcementRunbookEvidence evidence);

    void RecordM2SweepRan(string jobName, DateTimeOffset ranAtUtc, string correlationId);

    void RecordM2SweepSucceeded(string jobName, DateTimeOffset succeededAtUtc, string correlationId, int breaches, int coverage, int population);
}

internal sealed class InMemoryPeriodicEnforcementStatusStore : IPeriodicEnforcementStatusStore
{
    private readonly Lock _gate = new();
    private readonly Dictionary<string, int> _failures = new(StringComparer.Ordinal);
    private readonly Dictionary<string, PeriodicEnforcementM2SweepStatus> _m2SweepStatuses = new(StringComparer.Ordinal);
    private bool _isRunning;
    private DateTimeOffset? _lastStartedAtUtc;
    private DateTimeOffset? _lastSucceededAtUtc;
    private DateTimeOffset? _lastFailedAtUtc;
    private TimeSpan? _lastDuration;
    private long _skippedOverlapCount;
    private string? _lastCorrelationId;
    private PeriodicEnforcementRunbookEvidence? _lastRunbookSweep;

    public PeriodicEnforcementRunStatus Read()
    {
        lock (_gate)
        {
            return new PeriodicEnforcementRunStatus(
                _isRunning,
                _lastStartedAtUtc,
                _lastSucceededAtUtc,
                _lastFailedAtUtc,
                _lastDuration,
                _skippedOverlapCount,
                new Dictionary<string, int>(_failures, StringComparer.Ordinal),
                _lastCorrelationId,
                _lastRunbookSweep,
                new Dictionary<string, PeriodicEnforcementM2SweepStatus>(_m2SweepStatuses, StringComparer.Ordinal));
        }
    }

    public void RecordStarted(DateTimeOffset startedAtUtc, string correlationId)
    {
        lock (_gate)
        {
            _isRunning = true;
            _lastStartedAtUtc = startedAtUtc;
            _lastCorrelationId = correlationId;
        }
    }

    public void RecordSucceeded(DateTimeOffset completedAtUtc, TimeSpan duration)
    {
        lock (_gate)
        {
            _isRunning = false;
            _lastSucceededAtUtc = completedAtUtc;
            _lastDuration = duration;
        }
    }

    public void RecordFailed(DateTimeOffset failedAtUtc, TimeSpan duration)
    {
        lock (_gate)
        {
            _isRunning = false;
            _lastFailedAtUtc = failedAtUtc;
            _lastDuration = duration;
        }
    }

    public void RecordOverlap(DateTimeOffset skippedAtUtc, string correlationId)
    {
        lock (_gate)
        {
            _skippedOverlapCount++;
            _lastFailedAtUtc = skippedAtUtc;
            _lastCorrelationId = correlationId;
        }
    }

    public void RecordEvaluatorFailure(string evaluatorName)
    {
        lock (_gate)
        {
            _failures[evaluatorName] = _failures.TryGetValue(evaluatorName, out int count) ? count + 1 : 1;
        }
    }

    public void RecordRunbookSweep(PeriodicEnforcementRunbookEvidence evidence)
    {
        ArgumentNullException.ThrowIfNull(evidence);
        lock (_gate)
        {
            _lastRunbookSweep = evidence;
        }
    }

    public void RecordM2SweepRan(string jobName, DateTimeOffset ranAtUtc, string correlationId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(jobName);
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);
        lock (_gate)
        {
            _m2SweepStatuses.TryGetValue(jobName, out PeriodicEnforcementM2SweepStatus? previous);
            _m2SweepStatuses[jobName] = new PeriodicEnforcementM2SweepStatus(
                ranAtUtc,
                correlationId,
                previous?.LastSucceededAtUtc,
                previous?.LastSuccessCorrelationId,
                previous?.LastBreaches,
                previous?.LastCoverage,
                previous?.LastPopulation,
                LastAttemptCompletedSuccessfully: false);
        }
    }

    public void RecordM2SweepSucceeded(string jobName, DateTimeOffset succeededAtUtc, string correlationId, int breaches, int coverage, int population)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(jobName);
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);
        lock (_gate)
        {
            _m2SweepStatuses.TryGetValue(jobName, out PeriodicEnforcementM2SweepStatus? previous);
            _m2SweepStatuses[jobName] = new PeriodicEnforcementM2SweepStatus(
                previous?.LastRanAtUtc,
                previous?.LastRunCorrelationId,
                succeededAtUtc,
                correlationId,
                breaches,
                coverage,
                population,
                LastAttemptCompletedSuccessfully: true);
        }
    }
}

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

internal sealed class PeriodicEnforcementBackgroundService(
    PeriodicEnforcementCoordinator coordinator,
    IOptions<PeriodicEnforcementOptions> options,
    ISystemClock clock) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.UsePeriodicEnforcementRuntime)
        {
            return;
        }

        using PeriodicTimer timer = new(options.Value.Cadence);
        do
        {
            string correlationId = string.Create(
                System.Globalization.CultureInfo.InvariantCulture,
                $"periodic-enforcement:{clock.UtcNow.UtcDateTime:yyyyMMddHHmmss}");

            // The health check must run even when the pass throws — it is the detector for exactly that failure.
            // Letting the throw escape ExecuteAsync also stopped the whole host (the default
            // BackgroundServiceExceptionBehavior is StopHost), so an unwrapped phase such as tenant enumeration could
            // take the server down instead of being reported. RunOnceAsync already recorded the failure before
            // rethrowing, and the stale LastSucceededAtUtc it leaves behind is what CheckHealthAsync alerts on.
            try
            {
                _ = await coordinator.RunOnceAsync(correlationId, stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch
            {
                // Recorded by RunOnceAsync; surfaced below via the missed-cadence/stalled alerts.
            }

            try
            {
                await coordinator.CheckHealthAsync(correlationId, stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch
            {
                // A failing monitor must not stop the scheduler it monitors.
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false));
    }
}

internal static class PeriodicEnforcementServiceCollectionExtensions
{
    public static IServiceCollection AddChatBotPeriodicEnforcement(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // ValidateOnStart: a bad cadence/anchor used to throw ArgumentOutOfRangeException from the partition-key
        // helper on the first tick — outside the per-evaluator fail-isolation wrapper — which escaped the background
        // loop and stopped the whole host minutes after an apparently successful boot. Fail at startup instead.
        _ = services
            .AddOptions<PeriodicEnforcementOptions>()
            .Validate(
                static options => options.Validate() is null,
                "Invalid ChatBot:PeriodicEnforcement configuration.")
            .ValidateOnStart();
        services.TryAddSingleton<IPeriodicEnforcementInputSource, ProjectionBackedPeriodicEnforcementInputSource>();
        services.TryAddSingleton<IPeriodicEnforcementStatusStore, InMemoryPeriodicEnforcementStatusStore>();
        services.TryAddSingleton<IAuditProjectionCheckpointSource, UnavailableAuditProjectionCheckpointSource>();
        services.TryAddSingleton<SweepBackedAuditCompletenessSource>();
        services.TryAddSingleton<CheckpointBackedAuditProjectionLagSource>();
        services.TryAddSingleton<PeriodicEnforcementCoordinator>();
        return services;
    }

    public static IServiceCollection AddChatBotPeriodicEnforcementHostedService(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.RemoveAll<IAuditProjectionLagSource>();
        services.RemoveAll<IAuditCompletenessSource>();
        services.AddSingleton<IAuditProjectionLagSource>(static provider => provider.GetRequiredService<CheckpointBackedAuditProjectionLagSource>());
        services.AddSingleton<IAuditCompletenessSource>(static provider => provider.GetRequiredService<SweepBackedAuditCompletenessSource>());
        services.AddHostedService<PeriodicEnforcementBackgroundService>();
        return services;
    }
}
