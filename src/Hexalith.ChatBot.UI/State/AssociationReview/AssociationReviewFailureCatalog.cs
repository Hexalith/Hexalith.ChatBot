using Hexalith.ChatBot.UI.Design;

namespace Hexalith.ChatBot.UI.State.AssociationReview;

/// <summary>
/// Classifies the failure codes this surface is willing to show. Server-supplied problem codes are untrusted
/// display input: an unrecognized code is replaced with the generic code rather than rendered, so arbitrary
/// server text can never reach the page body or a live-region announcement.
/// </summary>
public static class AssociationReviewFailureCatalog
{
    public const string GenericFailureCode = "association-review-unavailable";

    private static readonly string[] Unauthorized =
    [
        "authorization_denied",
        "authorization-denied",
        "not-authorized",
        "target-unauthorized",
        "tenant-mismatch",
        "forbidden",
    ];

    private static readonly string[] TerminalPolicy =
    [
        "policy-blocked",
        "already-decided",
        "already-corrected",
        "terminal-state",
        "quarantined",
        "quarantine-terminal",
    ];

    private static readonly string[] DependencyDegraded =
    [
        "projection-pending",
        "projection-invalidation-unavailable",
        "audit-unavailable",
        "scorer-unavailable",
        "dependency-degraded",
        "correction-delayed",
    ];

    private static readonly string[] Retryable =
    [
        GenericFailureCode,
        "association-review-unavailable",
        "dispatch-unavailable",
        "timeout",
        "conflict",
        "rate-limited",
    ];

    /// <summary>Gets a value indicating whether the code is one this surface knows how to present.</summary>
    public static bool IsKnown(string? code)
        => !string.IsNullOrWhiteSpace(code)
            && (Contains(Unauthorized, code)
                || Contains(TerminalPolicy, code)
                || Contains(DependencyDegraded, code)
                || Contains(Retryable, code));

    /// <summary>Returns the code when it is known, otherwise the generic code.</summary>
    public static string SafeCode(string? code)
        => IsKnown(code) ? code! : GenericFailureCode;

    /// <summary>
    /// Maps a failure code to its UX-DR35 state family. An authorization denial must not land on
    /// <see cref="ChatBotFeedbackStateFamily.RetryableFailure"/>: the retryable copy invites the reviewer to
    /// try again, which is exactly the wrong instruction for a decision the server refused on authority.
    /// </summary>
    public static ChatBotFeedbackStateFamily StateFamily(string? code)
        => Contains(Unauthorized, code) ? ChatBotFeedbackStateFamily.BlockedAction
        : Contains(TerminalPolicy, code) ? ChatBotFeedbackStateFamily.TerminalPolicyFailure
        : Contains(DependencyDegraded, code) ? ChatBotFeedbackStateFamily.DependencyDegraded
        : ChatBotFeedbackStateFamily.RetryableFailure;

    /// <summary>Maps a failure code to the blocked reason used when the surface renders a blocked state.</summary>
    public static ChatBotBlockedReason BlockedReason(string? code)
        => Contains(Unauthorized, code) ? ChatBotBlockedReason.Denial
        : Contains(TerminalPolicy, code) ? ChatBotBlockedReason.Quarantine
        : Contains(DependencyDegraded, code) ? ChatBotBlockedReason.FailedDependency
        : ChatBotBlockedReason.UnresolvedAssociation;

    /// <summary>Gets a value indicating whether retrying the operation is a safe instruction for this code.</summary>
    public static bool IsRetryable(string? code)
        => StateFamily(code) is ChatBotFeedbackStateFamily.RetryableFailure or ChatBotFeedbackStateFamily.DependencyDegraded;

    private static bool Contains(string[] codes, string? code)
        => code is not null && Array.Exists(codes, known => string.Equals(known, code, StringComparison.OrdinalIgnoreCase));
}
