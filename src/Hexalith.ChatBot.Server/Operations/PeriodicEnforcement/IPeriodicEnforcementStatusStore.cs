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
