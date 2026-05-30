namespace Hexalith.ChatBot.Contracts.Messages;

public static class ChatBotMessageCodes
{
    public const string AuthenticationDenied = "authentication_denied";
    public const string AuthorizationDenied = "authorization_denied";
    public const string AuditUnavailable = "audit_unavailable";
    public const string IdempotencyConflictCommandExecution = "idempotency_conflict_command_execution";
    public const string InvalidLifecycleTransition = "invalid_lifecycle_transition";
    public const string RefusalBlockedAction = "refusal_blocked_action";
    public const string DependencyDegraded = "dependency_degraded";
    public const string FailedAttachment = "failed_attachment";
    public const string FailedCommand = "failed_command";
    public const string DegradedMailbox = "degraded_mailbox";
}
