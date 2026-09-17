using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Contracts.Queries;

namespace Hexalith.ChatBot.Server.Projections;

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
