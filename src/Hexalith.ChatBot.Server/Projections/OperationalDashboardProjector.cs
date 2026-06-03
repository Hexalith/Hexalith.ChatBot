using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Contracts.Messages;
using Hexalith.ChatBot.Contracts.Queries;
using Hexalith.ChatBot.Server.Observability;

namespace Hexalith.ChatBot.Server.Projections;

/// <summary>
/// Aggregates the read-only M2 operational dashboard overview (S8/S10, FR67) from existing tenant-wide
/// queue/health sources. It maps the FR67 view set onto the operational queue families already produced for
/// Story 7.5, layers in the AI-action-outcome health and the audit-projection-lag status, and emits a
/// metadata-only overview. Every view's <see cref="ChatBotHealthStatus"/> is the stable health enum carried by
/// the source rows — never derived from a count. Depth is a display count only. No project/evidence/file/mailbox/
/// audit detail is read into the overview, so the surface stays summary-safe by construction; restricted per-item
/// detail stays behind the existing authorized hydration path.
/// </summary>
internal static class OperationalDashboardProjector
{
    private const int MaxAggregationLimit = OperationalDashboardContractValidator.MaxAggregationLimit;
    private const string SchemaVersion = "chatbot.operational-dashboard.v1";
    private const string DefaultOwnerRole = "operations-admin";

    public static OperationalDashboardOverview Create(
        IEnumerable<AdminQueueSummaryProjectionItem> queueItems,
        AuditProjectionLagStatus auditLag,
        OperationalDashboardAiOutcomeInput aiOutcome,
        DateTimeOffset nowUtc,
        string correlationId,
        int aggregationLimit = OperationalDashboardContractValidator.DefaultAggregationLimit)
    {
        ArgumentNullException.ThrowIfNull(queueItems);
        ArgumentNullException.ThrowIfNull(auditLag);
        ArgumentNullException.ThrowIfNull(aiOutcome);
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);

        int limit = Math.Clamp(aggregationLimit, 1, MaxAggregationLimit);
        DateTimeOffset now = nowUtc.ToUniversalTime();
        AdminQueueSummaryProjectionItem[] items = queueItems.ToArray();

        List<OperationalDashboardView> views =
        [
            BuildQueueView(DashboardObservabilityView.MailboxProcessing, items, limit, now),
            BuildQueueView(DashboardObservabilityView.FailedAssociations, items, limit, now),
            BuildQueueView(DashboardObservabilityView.ApprovalQueues, items, limit, now),
            BuildQueueView(DashboardObservabilityView.DuplicateHandling, items, limit, now),
            BuildAiOutcomeView(aiOutcome, now),
            BuildAuditLagView(auditLag, now),
        ];

        DateTimeOffset overallFreshness = views.Min(static view => view.FreshnessTimestampUtc);
        ChatBotFreshnessState overallFreshnessState = OperationalDashboardFreshnessPolicy.Classify(overallFreshness, now);

        return new OperationalDashboardOverview(
            views,
            overallFreshness,
            overallFreshnessState,
            SchemaVersion,
            correlationId,
            BuildPublishedSlos(auditLag));
    }

    // Rides the static NFR42a catalog onto the authorized overview, layering the live coarse burn over each SLO
    // whose signal is wired. Today only the audit-projection-lag SLO has a live signal (the audit-lag health);
    // every other SLO keeps the catalog's fail-safe Unknown burn (honest no-data), never a fabricated within-budget.
    private static IReadOnlyList<PublishedSlo> BuildPublishedSlos(AuditProjectionLagStatus auditLag)
    {
        ErrorBudgetBurnState auditBurn = ErrorBudgetBurnEvaluator.FromHealth(auditLag.Health);

        return
        [
            .. OperatingBaselineCatalogProvider.GetCatalog().Select(slo =>
                slo.MetricName == OperatingBaselineMetrics.AuditProjectionLag
                    ? slo with { BurnState = auditBurn }
                    : slo),
        ];
    }

    private static OperationalDashboardView BuildQueueView(
        DashboardObservabilityView view,
        IReadOnlyList<AdminQueueSummaryProjectionItem> items,
        int limit,
        DateTimeOffset now)
    {
        AdminQueueSummaryProjectionItem[] contributing = items
            .Where(item => MapFamily(item.QueueFamily) == view)
            .Take(limit)
            .ToArray();

        if (contributing.Length == 0)
        {
            // No contributing source rows: report Unknown (fail-safe) rather than fabricate a healthy view.
            return new OperationalDashboardView(
                view,
                ChatBotHealthStatus.Unknown,
                Depth: 0,
                OldestItemAgeSeconds: 0,
                OwnerRole: DefaultOwnerRole,
                FreshnessTimestampUtc: now,
                FreshnessState: OperationalDashboardFreshnessPolicy.Classify(now, now),
                DetailLinkState: OperationalDashboardContractValidator.DetailRequestAccess,
                DisabledDetailReasonCodes: [ChatBotDisabledActionReasons.InsufficientAuthority]);
        }

        DateTimeOffset freshness = contributing
            .Select(item => item.FreshnessTimestampUtc?.ToUniversalTime() ?? now)
            .Min();

        return new OperationalDashboardView(
            view,
            WorstHealth(contributing),
            Depth: contributing.Length,
            OldestItemAgeSeconds: contributing.Max(static item => Math.Max(0, item.AgeSeconds)),
            OwnerRole: SafeSummaryToken(contributing[0].OwnerRole) ?? DefaultOwnerRole,
            FreshnessTimestampUtc: freshness,
            FreshnessState: OperationalDashboardFreshnessPolicy.Classify(freshness, now),
            // Tenant-wide summary rows expose metadata only; per-item detail requires the authorized hydration
            // step, so the dashboard's detail link offers a safe request-access state with no existence leakage.
            DetailLinkState: OperationalDashboardContractValidator.DetailRequestAccess,
            DisabledDetailReasonCodes: [ChatBotDisabledActionReasons.InsufficientAuthority]);
    }

    private static OperationalDashboardView BuildAiOutcomeView(OperationalDashboardAiOutcomeInput aiOutcome, DateTimeOffset now)
    {
        DateTimeOffset freshness = aiOutcome.FreshnessTimestampUtc?.ToUniversalTime() ?? now;
        return new OperationalDashboardView(
            DashboardObservabilityView.AiActionOutcomes,
            Enum.IsDefined(aiOutcome.Health) ? aiOutcome.Health : ChatBotHealthStatus.Unknown,
            Depth: Math.Max(0, aiOutcome.Depth),
            OldestItemAgeSeconds: Math.Max(0, aiOutcome.OldestItemAgeSeconds),
            OwnerRole: SafeSummaryToken(aiOutcome.OwnerRole) ?? DefaultOwnerRole,
            FreshnessTimestampUtc: freshness,
            FreshnessState: OperationalDashboardFreshnessPolicy.Classify(freshness, now),
            DetailLinkState: OperationalDashboardContractValidator.DetailDisabled,
            DisabledDetailReasonCodes: [ChatBotDisabledActionReasons.StateNotPermitted]);
    }

    private static OperationalDashboardView BuildAuditLagView(AuditProjectionLagStatus auditLag, DateTimeOffset now)
    {
        DateTimeOffset freshness = auditLag.FreshnessTimestampUtc.ToUniversalTime();
        return new OperationalDashboardView(
            DashboardObservabilityView.AuditProjectionLag,
            auditLag.Health,
            Depth: null,
            OldestItemAgeSeconds: 0,
            OwnerRole: DefaultOwnerRole,
            FreshnessTimestampUtc: freshness,
            FreshnessState: OperationalDashboardFreshnessPolicy.Classify(freshness, now),
            DetailLinkState: OperationalDashboardContractValidator.DetailDisabled,
            DisabledDetailReasonCodes: [ChatBotDisabledActionReasons.StateNotPermitted],
            // Coarse indicator only — never the raw lag count. Status is the health enum, never count-derived.
            LagIndicator: SafeSummaryToken(auditLag.LagIndicator) ?? AuditProjectionLagEvaluator.IndicatorUnknown);
    }

    private static DashboardObservabilityView? MapFamily(OperationalQueueFamily family)
        => family switch
        {
            OperationalQueueFamily.FailedIngestion => DashboardObservabilityView.MailboxProcessing,
            OperationalQueueFamily.FailedAttachment => DashboardObservabilityView.MailboxProcessing,
            OperationalQueueFamily.AmbiguousAssociation => DashboardObservabilityView.FailedAssociations,
            OperationalQueueFamily.UnresolvedParticipant => DashboardObservabilityView.FailedAssociations,
            OperationalQueueFamily.PendingApproval => DashboardObservabilityView.ApprovalQueues,
            OperationalQueueFamily.RetryableOperation => DashboardObservabilityView.DuplicateHandling,
            _ => null,
        };

    private static ChatBotHealthStatus WorstHealth(IReadOnlyList<AdminQueueSummaryProjectionItem> items)
    {
        if (items.Count == 0)
        {
            return ChatBotHealthStatus.Unknown;
        }

        if (items.Any(static item => item.Health == ChatBotHealthStatus.Failed))
        {
            return ChatBotHealthStatus.Failed;
        }

        if (items.Any(static item => item.Health == ChatBotHealthStatus.Degraded))
        {
            return ChatBotHealthStatus.Degraded;
        }

        return items.All(static item => item.Health == ChatBotHealthStatus.Unknown)
            ? ChatBotHealthStatus.Unknown
            : ChatBotHealthStatus.Healthy;
    }

    private static string? SafeSummaryToken(string? value)
        => !string.IsNullOrWhiteSpace(value) &&
            value.Length <= 200 &&
            !ContainsSensitiveMarker(value) &&
            value.All(static character => char.IsAsciiLetterOrDigit(character) || character is '.' or '-' or '_' or ':' or '@' or '|')
                ? value
                : null;

    private static bool ContainsSensitiveMarker(string value)
    {
        string[] markers =
        [
            "secret",
            "password",
            "bearer",
            "token",
            "exception",
            ".txt",
            ".json",
            ".xml",
        ];

        return markers.Any(marker => value.Contains(marker, StringComparison.OrdinalIgnoreCase));
    }
}

/// <summary>
/// Metadata-only AI-action-outcome health input for the dashboard's AI outcomes view. At M0/M1 fidelity this is
/// supplied by the caller from existing AI-outcome projection state; it defaults to <see cref="ChatBotHealthStatus.Unknown"/>
/// (fail-safe) when no AI-outcome source is wired.
/// </summary>
internal sealed record OperationalDashboardAiOutcomeInput(
    ChatBotHealthStatus Health,
    int Depth,
    int OldestItemAgeSeconds,
    DateTimeOffset? FreshnessTimestampUtc,
    string OwnerRole = "operations-admin")
{
    public static OperationalDashboardAiOutcomeInput Unknown(DateTimeOffset snapshotUtc)
        => new(ChatBotHealthStatus.Unknown, 0, 0, snapshotUtc);
}
