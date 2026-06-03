using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Gateway;

namespace Hexalith.ChatBot.Server.Observability;

/// <summary>
/// Pure, deterministic evaluator for the NFR43 <c>authorization-failure spike</c> alert threshold (Story 8.4, AC5).
/// Given the per-tenant rolling-window authorization-failure counts produced by <see cref="IAuthorizationFailureCounter"/>,
/// it fires one metadata-only <see cref="OperationalAlertPayload"/> per tenant whose aggregate failure count strictly
/// exceeds the baseline (default 10 in the rolling 10-minute window). The payload carries only <c>tenant:{ref}</c> as
/// the affected scope — never an actor, command, or project detail (NFR2) — and the count is the aggregate integer
/// failure count, never a percentile. Returns an empty list when no tenant exceeds the baseline.
/// </summary>
internal static class AuthorizationFailureSpikeEvaluator
{
    /// <summary>The rolling window over which authorization failures are counted (10 minutes).</summary>
    public const int DefaultAuthFailureWindowSeconds = 600;

    /// <summary>The baseline an in-window count must strictly exceed to fire (fires when &gt; 10).</summary>
    public const int DefaultAuthFailureBaselineCount = 10;

    public const string ReasonCode = "authorization_failure_spike_detected";
    public const string OwnerRole = AdminRoles.TenantAdmin;
    public const string NextSafeAction = "investigate-authorization-failures";

    public static IReadOnlyList<OperationalAlertPayload> Evaluate(
        IReadOnlyList<AuthorizationFailureReading> readings,
        string correlationId,
        DateTimeOffset firedAtUtc,
        int baselineCount = DefaultAuthFailureBaselineCount)
    {
        ArgumentNullException.ThrowIfNull(readings);
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);

        DateTimeOffset firedUtc = firedAtUtc.ToUniversalTime();

        // Deterministic order by tenant ref so the alert set is stable given the same readings.
        return readings
            .Where(reading => reading.FailureCount > baselineCount && !string.IsNullOrWhiteSpace(reading.TenantId))
            .OrderBy(static reading => reading.TenantId, StringComparer.Ordinal)
            .Select(reading => new OperationalAlertPayload(
                OperatorAlertKind.AuthorizationFailureSpike,
                $"tenant:{reading.TenantId}",
                OwnerRole,
                NextSafeAction,
                ReasonCode,
                reading.TenantId,
                correlationId,
                firedUtc))
            .ToList();
    }
}
