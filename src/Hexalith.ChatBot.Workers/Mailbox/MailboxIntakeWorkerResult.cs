namespace Hexalith.ChatBot.Workers.Mailbox;

public sealed record MailboxIntakeWorkerResult(
    MailboxIntakeWorkerResultKind Kind,
    string ReasonCode,
    string? IntakeId,
    string OperationClass,
    int RetryCount,
    int MaxAttempts,
    DateTimeOffset? NextRetryAt,
    string OwnerRole,
    string SafeNextAction,
    MailboxRateLimitObservation? RateLimit = null)
{
    public static MailboxIntakeWorkerResult Submitted(string intakeId)
        => new(
            MailboxIntakeWorkerResultKind.Submitted,
            "submitted",
            intakeId,
            "message-intake",
            RetryCount: 0,
            MaxAttempts: 1,
            NextRetryAt: null,
            OwnerRole: "mailbox-operator",
            SafeNextAction: "none");

    public static MailboxIntakeWorkerResult Recoverable(string reasonCode)
    {
        const int retryCount = 1;
        const int maxAttempts = 5;
        bool retryable = reasonCode is "graph_throttled"
            or "graph_subscription_expired"
            or "graph_token_expired"
            or "graph_partial_access"
            or "chatbot_submission_recoverable"
            or "audit_unavailable"
            // Story 7.14: a rate-limited source defers intake on the retryable/defer path (NextRetryAt set,
            // retry-later, owner mailbox-operator) — queued for automatic retry, never dropped or escalated-to-admin.
            or "mailbox_source_rate_limited";

        return new MailboxIntakeWorkerResult(
            MailboxIntakeWorkerResultKind.Recoverable,
            reasonCode,
            null,
            "message-intake",
            retryCount,
            maxAttempts,
            retryable ? DateTimeOffset.UtcNow.AddSeconds(60) : null,
            ResolveOwnerRole(reasonCode),
            retryable ? "retry-later" : "escalate");
    }

    private static string ResolveOwnerRole(string reasonCode)
        => reasonCode switch
        {
            "graph_subscription_expired" or "graph_token_expired" or "mailbox_scope_mismatch" or "mailbox_message_scope_mismatch" or "mailbox_source_disabled" or "mailbox_source_quarantined" => "mailbox-admin",
            "graph_permission_revoked" or "graph_scope_mismatch" => "tenant-admin",
            _ => "mailbox-operator",
        };
}
