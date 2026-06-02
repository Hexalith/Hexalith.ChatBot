namespace Hexalith.ChatBot.Contracts.Enums;

public static class OperationalQueueFamilies
{
    public const string AmbiguousAssociation = "ambiguous-association";
    public const string UnresolvedParticipant = "unresolved-participant";
    public const string PendingApproval = "pending-approval";
    public const string FailedIngestion = "failed-ingestion";
    public const string FailedAttachment = "failed-attachment";
    public const string RetryableOperation = "retryable-operation";

    public static IReadOnlyList<OperationalQueueFamily> All { get; } =
    [
        OperationalQueueFamily.AmbiguousAssociation,
        OperationalQueueFamily.UnresolvedParticipant,
        OperationalQueueFamily.PendingApproval,
        OperationalQueueFamily.FailedIngestion,
        OperationalQueueFamily.FailedAttachment,
        OperationalQueueFamily.RetryableOperation,
    ];

    public static bool TryFromWireValue(string? value, out OperationalQueueFamily family)
    {
        family = OperationalQueueFamily.AmbiguousAssociation;
        switch (value?.Trim().ToLowerInvariant())
        {
            case AmbiguousAssociation:
                family = OperationalQueueFamily.AmbiguousAssociation;
                return true;
            case UnresolvedParticipant:
                family = OperationalQueueFamily.UnresolvedParticipant;
                return true;
            case PendingApproval:
                family = OperationalQueueFamily.PendingApproval;
                return true;
            case FailedIngestion:
                family = OperationalQueueFamily.FailedIngestion;
                return true;
            case FailedAttachment:
                family = OperationalQueueFamily.FailedAttachment;
                return true;
            case RetryableOperation:
                family = OperationalQueueFamily.RetryableOperation;
                return true;
            default:
                return false;
        }
    }

    public static string ToWireValue(OperationalQueueFamily family)
        => family switch
        {
            OperationalQueueFamily.AmbiguousAssociation => AmbiguousAssociation,
            OperationalQueueFamily.UnresolvedParticipant => UnresolvedParticipant,
            OperationalQueueFamily.PendingApproval => PendingApproval,
            OperationalQueueFamily.FailedIngestion => FailedIngestion,
            OperationalQueueFamily.FailedAttachment => FailedAttachment,
            OperationalQueueFamily.RetryableOperation => RetryableOperation,
            _ => throw new ArgumentOutOfRangeException(nameof(family), family, "Unsupported operational queue family."),
        };
}
