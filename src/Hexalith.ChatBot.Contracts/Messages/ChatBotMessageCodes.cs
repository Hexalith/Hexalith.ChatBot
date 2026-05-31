namespace Hexalith.ChatBot.Contracts.Messages;

public static class ChatBotMessageCodes
{
    public const string AuthenticationDenied = "authentication_denied";
    public const string AuthorizationDenied = "authorization_denied";
    public const string AuditUnavailable = "audit_unavailable";
    public const string IdempotencyConflictCommandExecution = "idempotency_conflict_command_execution";
    public const string IdempotencyConflictMessageIntake = "idempotency_conflict_message_intake";
    public const string InvalidLifecycleTransition = "invalid_lifecycle_transition";
    public const string RefusalBlockedAction = "refusal_blocked_action";
    public const string DependencyDegraded = "dependency_degraded";
    public const string FailedAttachment = "failed_attachment";
    public const string FailedCommand = "failed_command";
    public const string DegradedMailbox = "degraded_mailbox";
    public const string UnresolvedParticipant = "unresolved_participant";
    public const string UnauthorizedParticipant = "unauthorized_participant";
    public const string ParticipantDirectoryDegraded = "participant_directory_degraded";
    public const string InvalidThresholdPolicy = "invalid_threshold_policy";
    public const string UnauthorizedThresholdUpdate = "unauthorized_threshold_update";
    public const string AssociationScorerFailedClosed = "association_scorer_failed_closed";
    public const string AssociationCandidateSuppressed = "association_candidate_suppressed";
}
