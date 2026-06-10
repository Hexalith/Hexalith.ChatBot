namespace Hexalith.ChatBot.Server.Lifecycle.Retry;

internal static class RetryFailurePolicy
{
    public const int DefaultMaxAttempts = 5;

    private static readonly HashSet<string> RetryableReasons = new(StringComparer.Ordinal)
    {
        "graph_throttled",
        "graph_subscription_expired",
        "graph_token_expired",
        "graph_partial_access",
        "chatbot_submission_recoverable",
        "audit_unavailable",
        "dispatch_unavailable",
        "projection_retryable",
    };

    private static readonly Dictionary<string, string> OwnerRoles = new(StringComparer.Ordinal)
    {
        ["graph_subscription_expired"] = "mailbox-admin",
        ["graph_token_expired"] = "mailbox-admin",
        ["graph_permission_revoked"] = "tenant-admin",
        ["graph_scope_mismatch"] = "tenant-admin",
        ["mailbox_scope_mismatch"] = "mailbox-admin",
        ["mailbox_message_scope_mismatch"] = "mailbox-admin",
    };

    public static RetryPolicyDecision Classify(string reasonCode, int retryCount, DateTimeOffset observedAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reasonCode);

        int normalizedRetryCount = Math.Max(0, retryCount);
        bool reasonRetryable = RetryableReasons.Contains(reasonCode);
        bool exhausted = reasonRetryable && normalizedRetryCount >= DefaultMaxAttempts;
        bool retryable = reasonRetryable && !exhausted;
        DateTimeOffset? nextRetryAt = retryable
            ? observedAt.Add(BackoffDelay(reasonCode, normalizedRetryCount))
            : null;

        return new RetryPolicyDecision(
            reasonCode,
            retryable,
            exhausted,
            normalizedRetryCount,
            DefaultMaxAttempts,
            nextRetryAt,
            OwnerRoles.GetValueOrDefault(reasonCode, "mailbox-operator"),
            exhausted ? "retry_exhausted" : TerminalReason(reasonCode, reasonRetryable),
            retryable ? "retry-later" : "escalate",
            retryable ? "wait-for-next-retry" : "escalate-to-operations");
    }

    private static TimeSpan BackoffDelay(string reasonCode, int retryCount)
    {
        int exponent = Math.Clamp(retryCount, 0, 5);
        int baseSeconds = 30 * (1 << exponent);

        // Bounded jitter in [0, 16]. Compute in long and take a non-negative modulo directly rather than
        // Math.Abs(int): Math.Abs(int.MinValue) throws OverflowException, and the hash + retryCount sum can
        // reach int.MinValue (string hash codes are full-range), which would crash the failure-handling path.
        long hash = StringComparer.Ordinal.GetHashCode(reasonCode);
        int jitterSeconds = (int)(((hash + retryCount) % 17 + 17) % 17);
        return TimeSpan.FromSeconds(Math.Min(baseSeconds + jitterSeconds, 900));
    }

    private static string TerminalReason(string reasonCode, bool reasonRetryable)
        => reasonRetryable ? string.Empty : reasonCode;
}
