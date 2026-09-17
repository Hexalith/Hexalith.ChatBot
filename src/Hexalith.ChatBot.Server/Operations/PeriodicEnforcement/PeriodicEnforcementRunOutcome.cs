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
