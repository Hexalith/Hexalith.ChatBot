namespace Hexalith.ChatBot.Server.Lifecycle.StateModel;

internal static class LifecycleStates
{
    public const string Received = "Received";
    public const string Proposed = "Proposed";
    public const string Associated = "Associated";
    public const string Rejected = "Rejected";
    public const string Deferred = "Deferred";
    public const string NeedsReview = "NeedsReview";
    public const string Failed = "Failed";
    public const string Skipped = "Skipped";
    public const string Corrected = "Corrected";
    public const string Correcting = "Correcting";
    public const string CorrectionDelayed = "Correction-delayed";
    public const string Active = "Active";
    public const string Disabled = "Disabled";

    public static IReadOnlyList<string> All { get; } =
    [
        Received,
        Proposed,
        Associated,
        Rejected,
        Deferred,
        NeedsReview,
        Failed,
        Skipped,
        Corrected,
        Correcting,
        CorrectionDelayed,
        Active,
        Disabled,
    ];
}
