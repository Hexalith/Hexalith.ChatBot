namespace Hexalith.ChatBot.Contracts.Enums;

/// <summary>
/// Stable wire tokens and helpers for <see cref="DashboardObservabilityView"/>. The <see cref="All"/> order is the
/// canonical render order for the operational dashboard (the six FR67 views followed by audit projection lag).
/// </summary>
public static class DashboardObservabilityViews
{
    public const string MailboxProcessing = "mailbox-processing";
    public const string FailedAssociations = "failed-associations";
    public const string ApprovalQueues = "approval-queues";
    public const string DuplicateHandling = "duplicate-handling";
    public const string AiActionOutcomes = "ai-action-outcomes";
    public const string AuditProjectionLag = "audit-projection-lag";

    public static IReadOnlyList<DashboardObservabilityView> All { get; } =
    [
        DashboardObservabilityView.MailboxProcessing,
        DashboardObservabilityView.FailedAssociations,
        DashboardObservabilityView.ApprovalQueues,
        DashboardObservabilityView.DuplicateHandling,
        DashboardObservabilityView.AiActionOutcomes,
        DashboardObservabilityView.AuditProjectionLag,
    ];

    public static bool TryFromWireValue(string? value, out DashboardObservabilityView view)
    {
        view = DashboardObservabilityView.MailboxProcessing;
        switch (value?.Trim().ToLowerInvariant())
        {
            case MailboxProcessing:
                view = DashboardObservabilityView.MailboxProcessing;
                return true;
            case FailedAssociations:
                view = DashboardObservabilityView.FailedAssociations;
                return true;
            case ApprovalQueues:
                view = DashboardObservabilityView.ApprovalQueues;
                return true;
            case DuplicateHandling:
                view = DashboardObservabilityView.DuplicateHandling;
                return true;
            case AiActionOutcomes:
                view = DashboardObservabilityView.AiActionOutcomes;
                return true;
            case AuditProjectionLag:
                view = DashboardObservabilityView.AuditProjectionLag;
                return true;
            default:
                return false;
        }
    }

    public static string ToWireValue(DashboardObservabilityView view)
        => view switch
        {
            DashboardObservabilityView.MailboxProcessing => MailboxProcessing,
            DashboardObservabilityView.FailedAssociations => FailedAssociations,
            DashboardObservabilityView.ApprovalQueues => ApprovalQueues,
            DashboardObservabilityView.DuplicateHandling => DuplicateHandling,
            DashboardObservabilityView.AiActionOutcomes => AiActionOutcomes,
            DashboardObservabilityView.AuditProjectionLag => AuditProjectionLag,
            _ => throw new ArgumentOutOfRangeException(nameof(view), view, "Unsupported dashboard observability view."),
        };
}
