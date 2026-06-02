namespace Hexalith.ChatBot.Contracts.Enums;

public static class AdminQueueOperations
{
    public const string Retry = "retry";
    public const string Requeue = "requeue";
    public const string Quarantine = "quarantine";
    public const string Dismiss = "dismiss";
    public const string Claim = "claim";
    public const string Assign = "assign";
    public const string Prioritize = "prioritize";

    public static IReadOnlyList<AdminQueueOperation> All { get; } =
    [
        AdminQueueOperation.Retry,
        AdminQueueOperation.Requeue,
        AdminQueueOperation.Quarantine,
        AdminQueueOperation.Dismiss,
        AdminQueueOperation.Claim,
        AdminQueueOperation.Assign,
        AdminQueueOperation.Prioritize,
    ];

    public static bool TryFromWireValue(string? value, out AdminQueueOperation operation)
    {
        operation = AdminQueueOperation.Retry;
        switch (value?.Trim().ToLowerInvariant())
        {
            case Retry:
                operation = AdminQueueOperation.Retry;
                return true;
            case Requeue:
                operation = AdminQueueOperation.Requeue;
                return true;
            case Quarantine:
                operation = AdminQueueOperation.Quarantine;
                return true;
            case Dismiss:
                operation = AdminQueueOperation.Dismiss;
                return true;
            case Claim:
                operation = AdminQueueOperation.Claim;
                return true;
            case Assign:
                operation = AdminQueueOperation.Assign;
                return true;
            case Prioritize:
                operation = AdminQueueOperation.Prioritize;
                return true;
            default:
                return false;
        }
    }

    public static string ToWireValue(AdminQueueOperation operation)
        => operation switch
        {
            AdminQueueOperation.Retry => Retry,
            AdminQueueOperation.Requeue => Requeue,
            AdminQueueOperation.Quarantine => Quarantine,
            AdminQueueOperation.Dismiss => Dismiss,
            AdminQueueOperation.Claim => Claim,
            AdminQueueOperation.Assign => Assign,
            AdminQueueOperation.Prioritize => Prioritize,
            _ => throw new ArgumentOutOfRangeException(nameof(operation), operation, "Unsupported admin queue operation."),
        };
}
