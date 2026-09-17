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
