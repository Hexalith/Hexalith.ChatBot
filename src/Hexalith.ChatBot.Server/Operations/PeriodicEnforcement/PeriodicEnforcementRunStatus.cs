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
