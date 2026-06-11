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
    public bool UsePeriodicEnforcementRuntime { get; set; }

    public TimeSpan Cadence { get; set; } = TimeSpan.FromMinutes(1);

    public TimeSpan MissedCadenceAlertAfter { get; set; } = TimeSpan.FromMinutes(5);

    public TimeSpan ControlStateHeartbeatBeforeStale { get; set; } = TimeSpan.FromMinutes(4);

    public int RunbookSampleSize { get; set; } = 100;
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
    RunbookDiagnosticCompletenessReport RunbookReport);

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
    PeriodicEnforcementRunbookEvidence? LastRunbookSweep);

internal interface IPeriodicEnforcementStatusStore
{
    PeriodicEnforcementRunStatus Read();

    void RecordStarted(DateTimeOffset startedAtUtc, string correlationId);

    void RecordSucceeded(DateTimeOffset completedAtUtc, TimeSpan duration);

    void RecordFailed(DateTimeOffset failedAtUtc, TimeSpan duration);

    void RecordOverlap(DateTimeOffset skippedAtUtc, string correlationId);

    void RecordEvaluatorFailure(string evaluatorName);

    void RecordRunbookSweep(PeriodicEnforcementRunbookEvidence evidence);
}

internal sealed class InMemoryPeriodicEnforcementStatusStore : IPeriodicEnforcementStatusStore
{
    private readonly Lock _gate = new();
    private readonly Dictionary<string, int> _failures = new(StringComparer.Ordinal);
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
                _lastRunbookSweep);
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
}

internal sealed class PeriodicEnforcementCoordinator(
    IPeriodicEnforcementInputSource inputSource,
    IGovernedControlStateProjectionStore controlStateStore,
    EscalationEvaluationCoordinator escalationCoordinator,
    NotificationThrottleCoordinator throttleCoordinator,
    ReviewerBacklogAlertCoordinator backlogCoordinator,
    ApprovalRubberStampRateCoordinator rubberStampCoordinator,
    OperationalAlertWiringCoordinator alertCoordinator,
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
    private readonly ConcurrentDictionary<string, string> _lastRunbookWeekByTenant = new(StringComparer.Ordinal);
    private int _running;

    public PeriodicEnforcementRunStatus Status => statusStore.Read();

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
                new RunbookDiagnosticCompletenessReport(0, 0, []));
        }

        statusStore.RecordStarted(started, correlationId);
        int failures = 0;
        int tenantsEvaluated = 0;
        int heartbeats = 0;
        int runbookTenantsSampled = 0;
        RunbookDiagnosticCompletenessReport runbookAggregate = new(0, 0, []);

        try
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

            // AC5/NFR44: record metadata-only positive evidence of the weekly sweep (sampled/complete/defect counts,
            // swept-at, correlation) when at least one tenant's sample actually ran this pass, so an operator can audit
            // that the weekly runbook check executed even on a clean, defect-free week. Counts only — no tenant refs.
            if (runbookTenantsSampled > 0)
            {
                statusStore.RecordRunbookSweep(new PeriodicEnforcementRunbookEvidence(
                    runbookAggregate.Sampled,
                    runbookAggregate.Complete,
                    runbookAggregate.DefectWorkflowItemRefs.Count,
                    clock.UtcNow,
                    correlationId));
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
                runbookAggregate);
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

    private async ValueTask EmitSchedulerAlertAsync(string reasonCode, string correlationId, CancellationToken cancellationToken)
        => await operatorAlertSink
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
            string correlationId = $"periodic-enforcement:{clock.UtcNow:yyyyMMddHHmmss}";
            await coordinator.RunOnceAsync(correlationId, stoppingToken).ConfigureAwait(false);
            await coordinator.CheckHealthAsync(correlationId, stoppingToken).ConfigureAwait(false);
        }
        while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false));
    }
}

internal static class PeriodicEnforcementServiceCollectionExtensions
{
    public static IServiceCollection AddChatBotPeriodicEnforcement(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        _ = services.AddOptions<PeriodicEnforcementOptions>();
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
