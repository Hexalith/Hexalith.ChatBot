namespace Hexalith.ChatBot.Contracts.Messages;

public static class ChatBotMessageCatalog
{
    public static IReadOnlyList<ChatBotMessageCatalogEntry> Entries { get; } =
    [
        new(
            ChatBotMessageCodes.AuthenticationDenied,
            "Authentication required.",
            "Authentication is required before this operation can continue.",
            ChatBotMessageNextActions.Authenticate,
            ChatBotDisabledActionReasons.InsufficientAuthority,
            ChatBotDetailVisibility.MetadataOnly),
        new(
            ChatBotMessageCodes.AuthorizationDenied,
            "Authorization denied.",
            "The operation is not available to this caller.",
            ChatBotMessageNextActions.RequestAccess,
            ChatBotDisabledActionReasons.InsufficientAuthority,
            ChatBotDetailVisibility.MetadataOnly),
        new(
            ChatBotMessageCodes.AuditUnavailable,
            "Audit is unavailable.",
            "The command cannot be accepted until audit recording is available.",
            ChatBotMessageNextActions.RetryLater,
            ChatBotDisabledActionReasons.DependencyDegraded,
            ChatBotDetailVisibility.MetadataOnly),
        new(
            ChatBotMessageCodes.IdempotencyConflictCommandExecution,
            "Idempotency conflict.",
            "The command conflicts with an existing submission record.",
            ChatBotMessageNextActions.None,
            ChatBotDisabledActionReasons.StateNotPermitted,
            ChatBotDetailVisibility.MetadataOnly),
        new(
            ChatBotMessageCodes.IdempotencyConflictMessageIntake,
            "Message already captured.",
            "The mailbox message has already been accepted for intake.",
            ChatBotMessageNextActions.None,
            ChatBotDisabledActionReasons.StateNotPermitted,
            ChatBotDetailVisibility.MetadataOnly),
        new(
            ChatBotMessageCodes.InvalidLifecycleTransition,
            "Invalid lifecycle transition.",
            "The requested lifecycle transition is not allowed for this state.",
            ChatBotMessageNextActions.None,
            ChatBotDisabledActionReasons.StateNotPermitted,
            ChatBotDetailVisibility.MetadataOnly),
        new(
            ChatBotMessageCodes.RefusalBlockedAction,
            "Action blocked.",
            "The requested action is blocked by policy.",
            ChatBotMessageNextActions.Escalate,
            ChatBotDisabledActionReasons.PolicyBlocked,
            ChatBotDetailVisibility.MetadataOnly),
        new(
            ChatBotMessageCodes.DependencyDegraded,
            "Dependency degraded.",
            "A required dependency is temporarily degraded.",
            ChatBotMessageNextActions.RetryLater,
            ChatBotDisabledActionReasons.DependencyDegraded,
            ChatBotDetailVisibility.MetadataOnly),
        new(
            ChatBotMessageCodes.FailedAttachment,
            "Attachment failed.",
            "The attachment could not be processed safely.",
            ChatBotMessageNextActions.RetryLater,
            ChatBotDisabledActionReasons.DependencyDegraded,
            ChatBotDetailVisibility.MetadataOnly),
        new(
            ChatBotMessageCodes.FailedCommand,
            "Command failed.",
            "The command could not be completed safely.",
            ChatBotMessageNextActions.Escalate,
            ChatBotDisabledActionReasons.DependencyDegraded,
            ChatBotDetailVisibility.MetadataOnly),
        new(
            ChatBotMessageCodes.DegradedMailbox,
            "Mailbox degraded.",
            "Mailbox processing is temporarily degraded.",
            ChatBotMessageNextActions.RetryLater,
            ChatBotDisabledActionReasons.DependencyDegraded,
            ChatBotDetailVisibility.MetadataOnly),
        new(
            ChatBotMessageCodes.UnresolvedParticipant,
            "Participant needs review.",
            "The operation is unavailable until participant identity is reviewed.",
            ChatBotMessageNextActions.RequestAccess,
            ChatBotDisabledActionReasons.UnresolvedParticipant,
            ChatBotDetailVisibility.MetadataOnly),
        new(
            ChatBotMessageCodes.UnauthorizedParticipant,
            "Participant not authorized.",
            "The operation is not available to this participant.",
            ChatBotMessageNextActions.RequestAccess,
            ChatBotDisabledActionReasons.InsufficientAuthority,
            ChatBotDetailVisibility.MetadataOnly),
        new(
            ChatBotMessageCodes.ParticipantDirectoryDegraded,
            "Participant directory degraded.",
            "Participant authority cannot be verified until the directory recovers.",
            ChatBotMessageNextActions.RetryLater,
            ChatBotDisabledActionReasons.ParticipantDirectoryDegraded,
            ChatBotDetailVisibility.MetadataOnly),
        new(
            ChatBotMessageCodes.InvalidThresholdPolicy,
            "Invalid threshold policy.",
            "The threshold policy values are outside the accepted bounds.",
            ChatBotMessageNextActions.CorrectRequest,
            ChatBotDisabledActionReasons.PolicyBlocked,
            ChatBotDetailVisibility.MetadataOnly),
        new(
            ChatBotMessageCodes.UnauthorizedThresholdUpdate,
            "Threshold update denied.",
            "The threshold policy update is not available to this caller.",
            ChatBotMessageNextActions.RequestAccess,
            ChatBotDisabledActionReasons.InsufficientAuthority,
            ChatBotDetailVisibility.MetadataOnly),
        new(
            ChatBotMessageCodes.AssociationAmbiguousRouted,
            "Association needs review.",
            "Association evidence matched more than one safe routing option.",
            ChatBotMessageNextActions.Escalate,
            ChatBotDisabledActionReasons.AwaitingOtherActor,
            ChatBotDetailVisibility.MetadataOnly),
        new(
            ChatBotMessageCodes.AssociationScorerFailedClosed,
            "Association needs review.",
            "Association scoring could not safely select a project.",
            ChatBotMessageNextActions.Escalate,
            ChatBotDisabledActionReasons.DependencyDegraded,
            ChatBotDetailVisibility.MetadataOnly),
        new(
            ChatBotMessageCodes.AssociationScorerUnavailable,
            "Association review required.",
            "Association scoring is unavailable for this item.",
            ChatBotMessageNextActions.RetryLater,
            ChatBotDisabledActionReasons.DependencyDegraded,
            ChatBotDetailVisibility.MetadataOnly),
        new(
            ChatBotMessageCodes.AssociationConflictingDeterministicEvidence,
            "Association review required.",
            "Association evidence contains conflicting project signals.",
            ChatBotMessageNextActions.Escalate,
            ChatBotDisabledActionReasons.AwaitingOtherActor,
            ChatBotDetailVisibility.MetadataOnly),
        new(
            ChatBotMessageCodes.AssociationContextUnavailable,
            "Association review required.",
            "Association context is unavailable for this item.",
            ChatBotMessageNextActions.RetryLater,
            ChatBotDisabledActionReasons.DependencyDegraded,
            ChatBotDetailVisibility.MetadataOnly),
        new(
            ChatBotMessageCodes.AssociationCandidateSuppressed,
            "Candidate unavailable.",
            "One or more candidates cannot be shown for this operation.",
            ChatBotMessageNextActions.RequestAccess,
            ChatBotDisabledActionReasons.InsufficientAuthority,
            ChatBotDetailVisibility.MetadataOnly),
    ];

    public static ChatBotMessageCatalogEntry Resolve(string code)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);

        return Entries.First(entry => string.Equals(entry.Code, code, StringComparison.Ordinal));
    }
}
