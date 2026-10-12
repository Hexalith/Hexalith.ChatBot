namespace Hexalith.ChatBot.Contracts.Messages;

public sealed record ChatBotMessageCatalogEntry(
    string Code,
    string Headline,
    string Reason,
    string NextAction,
    string? DisabledActionReason,
    string DetailVisibility)
{
    /// <summary>Version of the safe message contract.</summary>
    public string Version { get; } = ChatBotMessageCatalogVersion.Current;

    /// <summary>Safe state conveyed to a public surface.</summary>
    public string State => Code switch
    {
        ChatBotMessageCodes.CommandAccepted or
        ChatBotMessageCodes.AssociationDecisionAccepted or
        ChatBotMessageCodes.AssociationCorrectionAccepted or
        ChatBotMessageCodes.AssociationDecisionDeferred or
        ChatBotMessageCodes.AssociationDecisionNeedsReview or
        ChatBotMessageCodes.AssociationCorrectionPropagationPending or
        ChatBotMessageCodes.RetryQueued or
        ChatBotMessageCodes.RetryAccepted => "waiting",
        ChatBotMessageCodes.DuplicateSuppressed or
        ChatBotMessageCodes.AssociationDecisionRejected or
        ChatBotMessageCodes.AssociationCorrectionPropagationComplete or
        ChatBotMessageCodes.ReprocessCreated => "completed",
        ChatBotMessageCodes.RefusalBlockedAction or
        ChatBotMessageCodes.AssociationCorrectionPolicyBlocked or
        ChatBotMessageCodes.AssociationAiContextBlocked => "blocked",
        ChatBotMessageCodes.TerminalFailure or
        ChatBotMessageCodes.FailedCommand or
        ChatBotMessageCodes.FailedAttachment or
        ChatBotMessageCodes.AssociationCorrectionPropagationFailed => "failed",
        ChatBotMessageCodes.DependencyDegraded or
        ChatBotMessageCodes.DegradedMailbox or
        ChatBotMessageCodes.RecoverableMailboxDegradation => "degraded",
        _ when NextAction == ChatBotMessageNextActions.RetryLater => "retryable",
        _ => "denied",
    };

    /// <summary>Whether the represented outcome has no pending work.</summary>
    public bool IsTerminal => State != "waiting" && NextAction != ChatBotMessageNextActions.RetryLater;

    /// <summary>Complete text for assistive technology.</summary>
    public string AccessibleDescription => $"{Headline} {Reason} Next action: {NextAction}.";
}
