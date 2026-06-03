using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Projections;

namespace Hexalith.ChatBot.Server.Observability;

/// <summary>
/// Pure, deterministic evaluator for the NFR43 <c>mailbox-subscription-expiry</c> alert threshold (Story 8.4, AC4).
/// Given the tenant-bound queue snapshot, it scans <see cref="OperationalQueueFamily.FailedIngestion"/> items whose
/// degradation reason (the item's <see cref="AdminQueueSummaryProjectionItem.FailureState"/>, normalized to lowercase
/// with hyphens removed) reports a Graph subscription expiry, and fires one metadata-only
/// <see cref="OperationalAlertPayload"/> per distinct affected mailbox. The <see cref="OperationalAlertPayload.AffectedScope"/>
/// is always a safe metadata token (<c>tenant:{ref} mailbox:{ref}</c>) — never a project ref. Returns an empty list
/// when no item reports a subscription expiry.
/// </summary>
internal static class SubscriptionExpiryAlertEvaluator
{
    public const string ReasonCode = "subscription_expiry_threshold_exceeded";
    public const string OwnerRole = AdminRoles.MailboxAdmin;
    public const string NextSafeAction = "renew-graph-subscription";

    /// <summary>The normalized degradation-reason tokens that indicate a Graph subscription expiry.</summary>
    private static readonly IReadOnlySet<string> SubscriptionExpiredTokens = new HashSet<string>(StringComparer.Ordinal)
    {
        "graphsubscriptionexpired",
        "subscriptionexpired",
    };

    public static IReadOnlyList<OperationalAlertPayload> Evaluate(
        IReadOnlyList<AdminQueueSummaryProjectionItem> items,
        string tenantRef,
        string correlationId,
        DateTimeOffset firedAtUtc)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantRef);
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);

        DateTimeOffset firedUtc = firedAtUtc.ToUniversalTime();

        // One alert per distinct affected mailbox ref, in deterministic order, regardless of how many items report
        // the same mailbox's expiry.
        IEnumerable<string> affectedMailboxes = items
            .Where(IsSubscriptionExpired)
            .Select(static item => string.IsNullOrWhiteSpace(item.MailboxRef) ? "unknown" : item.MailboxRef!)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(static mailbox => mailbox, StringComparer.Ordinal);

        List<OperationalAlertPayload> alerts = [];
        foreach (string mailboxRef in affectedMailboxes)
        {
            alerts.Add(new OperationalAlertPayload(
                OperatorAlertKind.SubscriptionExpiryImminent,
                $"tenant:{tenantRef} mailbox:{mailboxRef}",
                OwnerRole,
                NextSafeAction,
                ReasonCode,
                tenantRef,
                correlationId,
                firedUtc));
        }

        return alerts;
    }

    private static bool IsSubscriptionExpired(AdminQueueSummaryProjectionItem item)
    {
        if (item.QueueFamily != OperationalQueueFamily.FailedIngestion ||
            string.IsNullOrWhiteSpace(item.FailureState))
        {
            return false;
        }

        string normalized = item.FailureState.ToLowerInvariant().Replace("-", string.Empty, StringComparison.Ordinal);
        return SubscriptionExpiredTokens.Any(token => normalized.Contains(token, StringComparison.Ordinal));
    }
}
