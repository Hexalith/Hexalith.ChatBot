using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Server.Audit;

namespace Hexalith.ChatBot.Server.Observability;

/// <summary>
/// Pure, deterministic evaluator for the NFR43 <c>retry-exhaustion</c> alert threshold (Story 8.4, AC2). Given a
/// boolean signal that a workflow item reached the retry-exhausted terminal state for a tenant (set by the alert
/// wiring coordinator's hook on <see cref="IChatBotMetrics.RecordRetryExhausted"/> via
/// <see cref="IRetryExhaustionAlertSource"/>), it fires a metadata-only <see cref="OperationalAlertPayload"/> when the
/// flag is true and suppresses (returns <see langword="null"/>) otherwise. No IO, no clock beyond the passed-in
/// timestamp.
/// </summary>
internal static class RetryExhaustionAlertEvaluator
{
    public const string ReasonCode = "retry_exhaustion_threshold_exceeded";
    public const string OwnerRole = AdminRoles.OperationsAdmin;
    public const string NextSafeAction = "review-failed-queue";

    public static OperationalAlertPayload? Evaluate(
        bool exhaustionOccurred,
        string tenantRef,
        string correlationId,
        DateTimeOffset firedAtUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantRef);
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);

        if (!exhaustionOccurred)
        {
            return null;
        }

        return new OperationalAlertPayload(
            OperatorAlertKind.RetryExhausted,
            $"tenant:{tenantRef}",
            OwnerRole,
            NextSafeAction,
            ReasonCode,
            tenantRef,
            correlationId,
            firedAtUtc.ToUniversalTime());
    }
}
