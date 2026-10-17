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
        ChatBotMessageCodes.IdempotencyConflictMessageIntake or
        ChatBotMessageCodes.IdempotencyConflictCorrection or
        ChatBotMessageCodes.IdempotencyConflictAssociationDecision or
        ChatBotMessageCodes.IdempotencyConflictRetry or
        ChatBotMessageCodes.IdempotencyConflictOutboundDraftCreation or
        ChatBotMessageCodes.IdempotencyConflictParticipantResolution or
        ChatBotMessageCodes.IdempotencyConflictAssociationScoring or
        ChatBotMessageCodes.IdempotencyConflictAssociationThresholdPolicy or
        ChatBotMessageCodes.IdempotencyConflictLowRiskAiAssistance or
        ChatBotMessageCodes.IdempotencyConflictApprovalDecision or
        ChatBotMessageCodes.IdempotencyConflictApprovedAiActionExecution or
        ChatBotMessageCodes.IdempotencyConflictAiActionProposal or
        ChatBotMessageCodes.AssociationDecisionRejected or
        ChatBotMessageCodes.AssociationCorrectionPropagationComplete or
        ChatBotMessageCodes.ReprocessCreated => "completed",
        ChatBotMessageCodes.CanonicalExecutionUnavailable or
        ChatBotMessageCodes.RefusalBlockedAction or
        ChatBotMessageCodes.AssociationCorrectionPolicyBlocked or
        ChatBotMessageCodes.AssociationAiContextBlocked => "blocked",
        ChatBotMessageCodes.TerminalFailure or
        ChatBotMessageCodes.FailedCommand or
        ChatBotMessageCodes.MemoriesCorrectionFailed or
        ChatBotMessageCodes.VectorReindexFailed or
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
    public string AccessibleDescription => $"{Headline} {Reason} Next action: {NextAction switch
    {
        ChatBotMessageNextActions.Authenticate => "Authenticate to continue",
        ChatBotMessageNextActions.CorrectRequest => "Correct the request",
        ChatBotMessageNextActions.RetryLater => "Retry later",
        ChatBotMessageNextActions.RequestAccess => "Request access",
        ChatBotMessageNextActions.Escalate => "Escalate for review",
        ChatBotMessageNextActions.Dismiss => "Dismiss this message",
        _ => "No action is available",
    }}.";
}
