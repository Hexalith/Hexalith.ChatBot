using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Contracts.Queries;

namespace Hexalith.ChatBot.Server.Projections;

/// <summary>
/// Metadata-only audit-projection-lag status read model (FR67, M0/M1 fidelity). There is no existing
/// audit-projection-lag read source; this derives a coarse status from the Audit seam's last projected/reconciled
/// checkpoint position versus the latest committed event position. It surfaces a health enum and a coarse lag
/// indicator only — never audit envelope contents, hash-chain detail, redaction keys, or audit reasons. When the
/// checkpoint source is unavailable or the snapshot has expired, it prefers <see cref="ChatBotHealthStatus.Unknown"/>
/// (fail-safe) over a fabricated <see cref="ChatBotHealthStatus.Healthy"/>.
/// </summary>
internal sealed record AuditProjectionLagStatus(
    ChatBotHealthStatus Health,
    string LagIndicator,
    long? LagEvents,
    DateTimeOffset FreshnessTimestampUtc);

internal static class AuditProjectionLagEvaluator
{
    public const int DefaultDegradedLagThreshold = 100;
    public const int DefaultFailedLagThreshold = 1000;

    public const string IndicatorUnknown = "unknown";
    public const string IndicatorCurrent = "current";
    public const string IndicatorLagging = "lagging";
    public const string IndicatorCriticalLag = "critical-lag";

    public static AuditProjectionLagStatus Evaluate(
        long? lastProjectedPosition,
        long? latestCommittedPosition,
        DateTimeOffset snapshotUtc,
        DateTimeOffset nowUtc,
        int degradedLagThreshold = DefaultDegradedLagThreshold,
        int failedLagThreshold = DefaultFailedLagThreshold)
    {
        DateTimeOffset freshness = snapshotUtc.ToUniversalTime();

        // Fail-safe: without trustworthy checkpoint positions, report Unknown rather than fabricate Healthy.
        if (lastProjectedPosition is not { } projected ||
            latestCommittedPosition is not { } committed ||
            projected < 0 ||
            committed < 0)
        {
            return new AuditProjectionLagStatus(ChatBotHealthStatus.Unknown, IndicatorUnknown, null, freshness);
        }

        // An expired snapshot can no longer assert health honestly; degrade to Unknown.
        if (OperationalDashboardFreshnessPolicy.Classify(freshness, nowUtc) == ChatBotFreshnessState.Expired)
        {
            return new AuditProjectionLagStatus(ChatBotHealthStatus.Unknown, IndicatorUnknown, null, freshness);
        }

        if (committed <= projected)
        {
            return new AuditProjectionLagStatus(ChatBotHealthStatus.Healthy, IndicatorCurrent, 0, freshness);
        }

        long lag = committed - projected;
        if (lag > failedLagThreshold)
        {
            return new AuditProjectionLagStatus(ChatBotHealthStatus.Failed, IndicatorCriticalLag, lag, freshness);
        }

        return lag > degradedLagThreshold
            ? new AuditProjectionLagStatus(ChatBotHealthStatus.Degraded, IndicatorLagging, lag, freshness)
            : new AuditProjectionLagStatus(ChatBotHealthStatus.Healthy, IndicatorCurrent, lag, freshness);
    }
}
