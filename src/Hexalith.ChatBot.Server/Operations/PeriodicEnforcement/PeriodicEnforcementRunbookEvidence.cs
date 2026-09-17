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
