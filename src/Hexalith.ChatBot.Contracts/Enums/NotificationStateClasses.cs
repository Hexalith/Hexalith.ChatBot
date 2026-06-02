namespace Hexalith.ChatBot.Contracts.Enums;

/// <summary>
/// Wire-token companion for <see cref="NotificationStateClass"/>. The six declared classes are the only ones
/// the routing engine accepts after the trust boundary.
/// </summary>
public static class NotificationStateClasses
{
    public const string ReviewNeeded = "review-needed";
    public const string ApprovalPending = "approval-pending";
    public const string Failure = "failure";
    public const string Degraded = "degraded";
    public const string Quarantine = "quarantine";
    public const string Retry = "retry";

    public static IReadOnlyList<NotificationStateClass> All { get; } =
    [
        NotificationStateClass.ReviewNeeded,
        NotificationStateClass.ApprovalPending,
        NotificationStateClass.Failure,
        NotificationStateClass.Degraded,
        NotificationStateClass.Quarantine,
        NotificationStateClass.Retry,
    ];

    public static bool TryFromWireValue(string? value, out NotificationStateClass stateClass)
    {
        stateClass = NotificationStateClass.ReviewNeeded;
        switch (value?.Trim().ToLowerInvariant())
        {
            case ReviewNeeded:
                stateClass = NotificationStateClass.ReviewNeeded;
                return true;
            case ApprovalPending:
                stateClass = NotificationStateClass.ApprovalPending;
                return true;
            case Failure:
                stateClass = NotificationStateClass.Failure;
                return true;
            case Degraded:
                stateClass = NotificationStateClass.Degraded;
                return true;
            case Quarantine:
                stateClass = NotificationStateClass.Quarantine;
                return true;
            case Retry:
                stateClass = NotificationStateClass.Retry;
                return true;
            default:
                return false;
        }
    }

    public static string ToWireValue(NotificationStateClass stateClass)
        => stateClass switch
        {
            NotificationStateClass.ReviewNeeded => ReviewNeeded,
            NotificationStateClass.ApprovalPending => ApprovalPending,
            NotificationStateClass.Failure => Failure,
            NotificationStateClass.Degraded => Degraded,
            NotificationStateClass.Quarantine => Quarantine,
            NotificationStateClass.Retry => Retry,
            _ => throw new ArgumentOutOfRangeException(nameof(stateClass), stateClass, "Unsupported notification state class."),
        };
}
