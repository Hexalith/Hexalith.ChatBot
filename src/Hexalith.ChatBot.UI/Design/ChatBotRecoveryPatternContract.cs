namespace Hexalith.ChatBot.UI.Design;

/// <summary>
/// Typed recovery contract for governed UI error-recovery patterns.
/// </summary>
public sealed record ChatBotRecoveryPatternContract(
    ChatBotRecoveryFlow Flow,
    string SafeFailureCategory,
    string FocusTargetId,
    string PreservedState,
    IReadOnlyList<string> StillValidActions,
    string DuplicateSafetyText,
    int? RetryCount,
    string AffectedContextPreview,
    string ValidationSummaryPlacement,
    IReadOnlyDictionary<string, string> FieldMessageAssociations,
    ChatBotSaveConflictCause SaveConflictCause,
    ChatBotCorrectionRecoveryStatus CorrectionStatus,
    string ExplicitConfirmationCopy,
    IReadOnlyList<string> AuditVisibleOutcomes,
    string PolicyRationale,
    string SafeNextAction,
    string RecoveryMessage,
    IReadOnlyList<string> RestrictedSourceTextMarkers)
{
    private static readonly string[] AssociationActions = ["confirm", "reject", "defer", "escalate"];
    private static readonly string[] AiReviewOutcomes = ["reject", "revise", "cancel"];

    /// <summary>Gets a value indicating whether this recovery message excludes restricted detail.</summary>
    public bool IsSafeForDisplay
        => !ContainsRestrictedSourceText
            && !ContainsUnsafeRawFailureText(SafeFailureCategory)
            && !ContainsUnsafeRawFailureText(PreservedState)
            && !ContainsUnsafeRawFailureText(DuplicateSafetyText)
            && !ContainsUnsafeRawFailureText(AffectedContextPreview)
            && !ContainsUnsafeRawFailureText(ValidationSummaryPlacement)
            && !FieldMessageAssociationsContainUnsafeRawFailureText
            && !ContainsUnsafeRawFailureText(ExplicitConfirmationCopy)
            && !AuditVisibleOutcomesContainUnsafeRawFailureText
            && !ContainsUnsafeRawFailureText(PolicyRationale)
            && !ContainsUnsafeRawFailureText(SafeNextAction)
            && !ContainsUnsafeRawFailureText(RecoveryMessage);

    /// <summary>Gets a value indicating whether raw source markers appear in recovery text.</summary>
    public bool ContainsRestrictedSourceText
        => RestrictedSourceTextMarkers is not null
            && RestrictedSourceTextMarkers
                .Where(static marker => !string.IsNullOrWhiteSpace(marker))
                .Any(marker =>
                    ContainsOrdinalIgnoreCase(SafeFailureCategory, marker)
                    || ContainsOrdinalIgnoreCase(RecoveryMessage, marker)
                    || ContainsOrdinalIgnoreCase(SafeNextAction, marker)
                    || ContainsOrdinalIgnoreCase(PreservedState, marker)
                    || ContainsOrdinalIgnoreCase(DuplicateSafetyText, marker)
                    || ContainsOrdinalIgnoreCase(AffectedContextPreview, marker)
                    || ContainsOrdinalIgnoreCase(ValidationSummaryPlacement, marker)
                    || FieldMessageAssociationsContain(marker)
                    || ContainsOrdinalIgnoreCase(ExplicitConfirmationCopy, marker)
                    || AuditVisibleOutcomesContain(marker)
                    || ContainsOrdinalIgnoreCase(PolicyRationale, marker));

    /// <summary>Gets a value indicating whether the flow-specific recovery contract is complete.</summary>
    public bool IsComplete
        => IsCommonComplete
            && IsSafeForDisplay
            && (Flow switch
            {
                ChatBotRecoveryFlow.AssociationReview => IsAssociationReviewComplete,
                ChatBotRecoveryFlow.AiActionReview => IsAiActionReviewComplete,
                ChatBotRecoveryFlow.QueueRetry => IsQueueRetryComplete,
                ChatBotRecoveryFlow.Correction => IsCorrectionComplete,
                ChatBotRecoveryFlow.TenantConfiguration => IsTenantConfigurationComplete,
                _ => false,
            });

    public static ChatBotRecoveryPatternContract ForAssociationReview(
        string safeFailureCategory,
        string preservedSelectionState,
        string focusTargetId,
        IReadOnlyList<string> stillValidActions,
        string safeNextAction,
        IReadOnlyList<string> restrictedSourceTextMarkers)
        => new(
            ChatBotRecoveryFlow.AssociationReview,
            safeFailureCategory,
            focusTargetId,
            preservedSelectionState,
            stillValidActions,
            string.Empty,
            null,
            string.Empty,
            string.Empty,
            new Dictionary<string, string>(),
            ChatBotSaveConflictCause.None,
            ChatBotCorrectionRecoveryStatus.Success,
            string.Empty,
            [],
            string.Empty,
            safeNextAction,
            safeNextAction,
            restrictedSourceTextMarkers);

    public static ChatBotRecoveryPatternContract ForAiActionReview(
        string safeFailureCategory,
        string focusTargetId,
        string explicitConfirmationCopy,
        IReadOnlyList<string> auditVisibleOutcomes,
        string safeNextAction,
        IReadOnlyList<string> restrictedSourceTextMarkers)
        => new(
            ChatBotRecoveryFlow.AiActionReview,
            safeFailureCategory,
            focusTargetId,
            string.Empty,
            [],
            string.Empty,
            null,
            string.Empty,
            string.Empty,
            new Dictionary<string, string>(),
            ChatBotSaveConflictCause.None,
            ChatBotCorrectionRecoveryStatus.Success,
            explicitConfirmationCopy,
            auditVisibleOutcomes,
            string.Empty,
            safeNextAction,
            safeNextAction,
            restrictedSourceTextMarkers);

    public static ChatBotRecoveryPatternContract ForQueueRetry(
        string safeFailureCategory,
        string focusTargetId,
        string duplicateSafetyText,
        int retryCount,
        string safeNextAction,
        IReadOnlyList<string> restrictedSourceTextMarkers)
        => new(
            ChatBotRecoveryFlow.QueueRetry,
            safeFailureCategory,
            focusTargetId,
            string.Empty,
            [],
            duplicateSafetyText,
            retryCount,
            string.Empty,
            string.Empty,
            new Dictionary<string, string>(),
            ChatBotSaveConflictCause.None,
            ChatBotCorrectionRecoveryStatus.Success,
            string.Empty,
            [],
            string.Empty,
            safeNextAction,
            safeNextAction,
            restrictedSourceTextMarkers);

    public static ChatBotRecoveryPatternContract ForCorrection(
        string safeFailureCategory,
        string focusTargetId,
        string policyRationale,
        string affectedContextPreview,
        ChatBotCorrectionRecoveryStatus correctionStatus,
        string safeNextAction,
        IReadOnlyList<string> restrictedSourceTextMarkers)
        => new(
            ChatBotRecoveryFlow.Correction,
            safeFailureCategory,
            focusTargetId,
            string.Empty,
            [],
            string.Empty,
            null,
            affectedContextPreview,
            string.Empty,
            new Dictionary<string, string>(),
            ChatBotSaveConflictCause.None,
            correctionStatus,
            string.Empty,
            [],
            policyRationale,
            safeNextAction,
            safeNextAction,
            restrictedSourceTextMarkers);

    public static ChatBotRecoveryPatternContract ForTenantConfiguration(
        string safeFailureCategory,
        string focusTargetId,
        string validationSummaryPlacement,
        IReadOnlyDictionary<string, string> fieldMessageAssociations,
        ChatBotSaveConflictCause saveConflictCause,
        string safeNextAction,
        IReadOnlyList<string> restrictedSourceTextMarkers)
        => new(
            ChatBotRecoveryFlow.TenantConfiguration,
            safeFailureCategory,
            focusTargetId,
            string.Empty,
            [],
            string.Empty,
            null,
            string.Empty,
            validationSummaryPlacement,
            fieldMessageAssociations,
            saveConflictCause,
            ChatBotCorrectionRecoveryStatus.Success,
            string.Empty,
            [],
            string.Empty,
            safeNextAction,
            safeNextAction,
            restrictedSourceTextMarkers);

    private bool IsCommonComplete
        => !string.IsNullOrWhiteSpace(SafeFailureCategory)
            && !string.IsNullOrWhiteSpace(FocusTargetId)
            && !string.IsNullOrWhiteSpace(SafeNextAction);

    private bool IsAssociationReviewComplete
        => !string.IsNullOrWhiteSpace(PreservedState)
            && StillValidActions is { Count: > 0 }
            && StillValidActions.All(static action => AssociationActions.Contains(action, StringComparer.Ordinal));

    private bool IsAiActionReviewComplete
        => !string.IsNullOrWhiteSpace(ExplicitConfirmationCopy)
            && ContainsAiRiskConfirmationCopy
            && AiReviewOutcomes.All(outcome => AuditVisibleOutcomes.Contains(outcome, StringComparer.Ordinal));

    private bool IsQueueRetryComplete
        => !string.IsNullOrWhiteSpace(DuplicateSafetyText)
            && ContainsOrdinalIgnoreCase(FocusTargetId, "row-status")
            && RetryCount >= 0;

    private bool IsCorrectionComplete
        => !string.IsNullOrWhiteSpace(PolicyRationale)
            && !string.IsNullOrWhiteSpace(AffectedContextPreview)
            && CorrectionStatus is ChatBotCorrectionRecoveryStatus.Success
                or ChatBotCorrectionRecoveryStatus.Partial
                or ChatBotCorrectionRecoveryStatus.Blocked;

    private bool IsTenantConfigurationComplete
        => string.Equals(ValidationSummaryPlacement, "before-fields", StringComparison.Ordinal)
            && FieldMessageAssociations is { Count: > 0 }
            && FieldMessageAssociations.All(static pair =>
                !string.IsNullOrWhiteSpace(pair.Key)
                && !string.IsNullOrWhiteSpace(pair.Value))
            && SaveConflictCause is ChatBotSaveConflictCause.Policy or ChatBotSaveConflictCause.Permission or ChatBotSaveConflictCause.StaleData;

    private bool ContainsAiRiskConfirmationCopy
        => ContainsOrdinalIgnoreCase(ExplicitConfirmationCopy, "external")
            && ContainsOrdinalIgnoreCase(ExplicitConfirmationCopy, "file")
            && ContainsOrdinalIgnoreCase(ExplicitConfirmationCopy, "project")
            && ContainsOrdinalIgnoreCase(ExplicitConfirmationCopy, "tool")
            && ContainsOrdinalIgnoreCase(ExplicitConfirmationCopy, "participant");

    private bool AuditVisibleOutcomesContainUnsafeRawFailureText
        => AuditVisibleOutcomes is not null
            && AuditVisibleOutcomes.Any(static outcome => ContainsUnsafeRawFailureText(outcome));

    private bool FieldMessageAssociationsContainUnsafeRawFailureText
        => FieldMessageAssociations is not null
            && FieldMessageAssociations.Any(static pair =>
                ContainsUnsafeRawFailureText(pair.Key)
                || ContainsUnsafeRawFailureText(pair.Value));

    private bool AuditVisibleOutcomesContain(string marker)
        => AuditVisibleOutcomes is not null
            && AuditVisibleOutcomes.Any(outcome => ContainsOrdinalIgnoreCase(outcome, marker));

    private bool FieldMessageAssociationsContain(string marker)
        => FieldMessageAssociations is not null
            && FieldMessageAssociations.Any(pair =>
                ContainsOrdinalIgnoreCase(pair.Key, marker)
                || ContainsOrdinalIgnoreCase(pair.Value, marker));

    private static bool ContainsOrdinalIgnoreCase(string? value, string marker)
        => value?.Contains(marker, StringComparison.OrdinalIgnoreCase) == true;

    private static bool ContainsUnsafeRawFailureText(string? value)
        => ContainsOrdinalIgnoreCase(value, "exception")
            || ContainsOrdinalIgnoreCase(value, "stack trace")
            || ContainsOrdinalIgnoreCase(value, "raw payload");
}
