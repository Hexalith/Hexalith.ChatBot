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
    ];

    public static ChatBotMessageCatalogEntry Resolve(string code)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);

        return Entries.First(entry => string.Equals(entry.Code, code, StringComparison.Ordinal));
    }
}
