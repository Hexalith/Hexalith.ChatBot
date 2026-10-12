using Hexalith.ChatBot.Contracts.Messages;
using Hexalith.ChatBot.UI.Design;

namespace Hexalith.ChatBot.UI.State.AssociationReview;

/// <summary>
/// Classifies the failure codes this surface is willing to show. Server-supplied problem codes are untrusted
/// display input: an unrecognized code is replaced with the generic code rather than rendered, so arbitrary
/// server text can never reach the page body or a live-region announcement.
/// </summary>
public static class AssociationReviewFailureCatalog
{
    public const string GenericFailureCode = ChatBotMessageCodes.GenericDenial;

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
        "dispatch-unavailable",
        "timeout",
        "conflict",
        "rate-limited",
    ];

    /// <summary>Gets a value indicating whether the code is one this surface knows how to present.</summary>
    public static bool IsKnown(string? code)
        => !string.IsNullOrWhiteSpace(code)
            && (ChatBotMessageCatalog.IsKnown(code)
                || Contains(Unauthorized, code)
                || Contains(TerminalPolicy, code)
                || Contains(DependencyDegraded, code)
                || Contains(Retryable, code));

    /// <summary>Returns the code when it is known, otherwise the generic code.</summary>
    public static string SafeCode(string? code)
        => IsKnown(code) ? code! : ChatBotMessageCatalog.Resolve(code).Code;

    /// <summary>
    /// Maps a failure code to its UX-DR35 state family. An authorization denial must not land on
    /// <see cref="ChatBotFeedbackStateFamily.RetryableFailure"/>: the retryable copy invites the reviewer to
    /// try again, which is exactly the wrong instruction for a decision the server refused on authority.
    /// </summary>
    public static ChatBotFeedbackStateFamily StateFamily(string? code)
        => CatalogCode(code) == ChatBotMessageCodes.GenericDenial ? ChatBotFeedbackStateFamily.TerminalPolicyFailure
        : Contains(Unauthorized, code) ? ChatBotFeedbackStateFamily.BlockedAction
        : Contains(TerminalPolicy, code) ? ChatBotFeedbackStateFamily.TerminalPolicyFailure
        : Contains(DependencyDegraded, code) ? ChatBotFeedbackStateFamily.DependencyDegraded
        : ChatBotMessageCatalog.Resolve(CatalogCode(code)).IsTerminal ? ChatBotFeedbackStateFamily.TerminalPolicyFailure
        : ChatBotFeedbackStateFamily.RetryableFailure;

    /// <summary>Maps a failure code to the blocked reason used when the surface renders a blocked state.</summary>
    public static ChatBotBlockedReason BlockedReason(string? code)
        => Contains(Unauthorized, code) ? ChatBotBlockedReason.Denial
        : Contains(TerminalPolicy, code) ? ChatBotBlockedReason.Quarantine
        : Contains(DependencyDegraded, code) ? ChatBotBlockedReason.FailedDependency
        : ChatBotBlockedReason.UnresolvedAssociation;

    /// <summary>Gets a value indicating whether retrying the operation is a safe instruction for this code.</summary>
    public static bool IsRetryable(string? code)
        => ChatBotMessageCatalog.Resolve(CatalogCode(code)).NextAction == ChatBotMessageNextActions.RetryLater;

    /// <summary>Maps established surface tokens to the authoritative safe-message catalog.</summary>
    public static string CatalogCode(string? code)
        => code?.ToLowerInvariant() switch
        {
            "authorization_denied" or "authorization-denied" or "not-authorized" or "target-unauthorized" or "tenant-mismatch" or "forbidden" => ChatBotMessageCodes.AuthorizationDenied,
            "policy-blocked" or "quarantined" or "quarantine-terminal" => ChatBotMessageCodes.RefusalBlockedAction,
            "already-decided" => ChatBotMessageCodes.AssociationAlreadyDecided,
            "already-corrected" => ChatBotMessageCodes.AssociationAlreadyCorrected,
            "terminal-state" or "conflict" => ChatBotMessageCodes.InvalidLifecycleTransition,
            "projection-pending" or "projection-invalidation-unavailable" => ChatBotMessageCodes.AssociationCorrectionProjectionUnavailable,
            "audit-unavailable" => ChatBotMessageCodes.AssociationCorrectionAuditUnavailable,
            "scorer-unavailable" => ChatBotMessageCodes.AssociationScorerUnavailable,
            "correction-delayed" => ChatBotMessageCodes.AssociationCorrectionPropagationDelayed,
            "dependency-degraded" or "dispatch-unavailable" or "timeout" or "rate-limited" => ChatBotMessageCodes.DependencyDegraded,
            _ => ChatBotMessageCatalog.Resolve(code).Code,
        };

    private static bool Contains(string[] codes, string? code)
        => code is not null && Array.Exists(codes, known => string.Equals(known, code, StringComparison.OrdinalIgnoreCase));
}
