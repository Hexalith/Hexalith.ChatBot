using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Server.Projections;

namespace Hexalith.ChatBot.Server.Notifications;

/// <summary>
/// Deterministic mapping from an operational queue item's family/health/status to the notify-worthy state class the
/// escalation evaluator reasons over. The mapping never inspects restricted content (project names, evidence, etc.).
/// <c>retry</c> is mapped for completeness but is not an escalatable class, so retryable items never match an
/// escalation-policy entry.
/// </summary>
internal static class EscalationStateClassMap
{
    public static NotificationStateClass Map(AdminQueueSummaryProjectionItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        // An explicit quarantine signal dominates regardless of queue family.
        if (IndicatesQuarantine(item.Status) || IndicatesQuarantine(item.FailureState))
        {
            return NotificationStateClass.Quarantine;
        }

        NotificationStateClass byFamily = item.QueueFamily switch
        {
            OperationalQueueFamily.PendingApproval => NotificationStateClass.ApprovalPending,
            OperationalQueueFamily.FailedIngestion => NotificationStateClass.Failure,
            OperationalQueueFamily.FailedAttachment => NotificationStateClass.Failure,
            OperationalQueueFamily.AmbiguousAssociation => NotificationStateClass.ReviewNeeded,
            OperationalQueueFamily.UnresolvedParticipant => NotificationStateClass.ReviewNeeded,
            OperationalQueueFamily.RetryableOperation => NotificationStateClass.Retry,
            _ => NotificationStateClass.ReviewNeeded,
        };

        // A failing/degraded health signal promotes an otherwise transient retryable item to the matching class.
        return (byFamily, item.Health) switch
        {
            (NotificationStateClass.Retry, ChatBotHealthStatus.Failed) => NotificationStateClass.Failure,
            (NotificationStateClass.Retry, ChatBotHealthStatus.Degraded) => NotificationStateClass.Degraded,
            _ => byFamily,
        };
    }

    private static bool IndicatesQuarantine(string? value)
        => !string.IsNullOrWhiteSpace(value) &&
            value.Contains("quarantine", StringComparison.OrdinalIgnoreCase);
}
