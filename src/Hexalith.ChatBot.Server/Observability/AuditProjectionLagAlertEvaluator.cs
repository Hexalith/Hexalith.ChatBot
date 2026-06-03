using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Projections;

namespace Hexalith.ChatBot.Server.Observability;

/// <summary>
/// Pure, deterministic evaluator for the NFR43 <c>audit-projection-lag</c> alert threshold (Story 8.4, AC1). Given a
/// coarse <see cref="AuditProjectionLagStatus"/> already computed by <see cref="AuditProjectionLagEvaluator.Evaluate"/>,
/// it fires a metadata-only <see cref="OperationalAlertPayload"/> when the health is <see cref="ChatBotHealthStatus.Degraded"/>
/// or <see cref="ChatBotHealthStatus.Failed"/>, and suppresses (returns <see langword="null"/>) for
/// <see cref="ChatBotHealthStatus.Healthy"/> or <see cref="ChatBotHealthStatus.Unknown"/> — honouring the fail-safe
/// doctrine that no-data never fabricates an alert. No IO, no clock beyond the passed-in timestamp; mirrors the pure
/// static shape of <see cref="ErrorBudgetBurnEvaluator"/>.
/// </summary>
internal static class AuditProjectionLagAlertEvaluator
{
    public const string ReasonCode = "audit_projection_lag_breached";
    public const string OwnerRole = AdminRoles.OperationsAdmin;
    public const string NextSafeAction = "review-audit-projection-lag";

    public static OperationalAlertPayload? Evaluate(
        AuditProjectionLagStatus status,
        string tenantRef,
        string correlationId,
        DateTimeOffset firedAtUtc)
    {
        ArgumentNullException.ThrowIfNull(status);
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantRef);
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);

        if (status.Health is not (ChatBotHealthStatus.Degraded or ChatBotHealthStatus.Failed))
        {
            return null;
        }

        return new OperationalAlertPayload(
            OperatorAlertKind.AuditProjectionLagBreached,
            $"tenant:{tenantRef}",
            OwnerRole,
            NextSafeAction,
            ReasonCode,
            tenantRef,
            correlationId,
            firedAtUtc.ToUniversalTime());
    }
}
