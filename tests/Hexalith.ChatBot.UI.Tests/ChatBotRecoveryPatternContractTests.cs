using Hexalith.ChatBot.UI.Design;

using Shouldly;

namespace Hexalith.ChatBot.UI.Tests;

public sealed class ChatBotRecoveryPatternContractTests
{
    [Fact]
    public void RecoveryFlowEnumShouldCoverUxDr40Flows()
    {
        Enum.GetNames<ChatBotRecoveryFlow>().ShouldBe(
            [
                "AssociationReview",
                "AiActionReview",
                "QueueRetry",
                "Correction",
                "TenantConfiguration",
            ],
            ignoreOrder: false);
    }

    [Fact]
    public void AssociationReviewShouldPreserveSelectionFocusSummaryAndSafeActionsOnly()
    {
        ChatBotRecoveryPatternContract contract = ChatBotRecoveryPatternContract.ForAssociationReview(
            safeFailureCategory: "ambiguous_association",
            preservedSelectionState: "candidate-selection-preserved-when-valid",
            focusTargetId: "association-review-summary",
            stillValidActions: ["confirm", "reject", "defer", "escalate"],
            safeNextAction: "Review candidate metadata before choosing an action.",
            restrictedSourceTextMarkers: ["Secret Project", "restricted-file.txt"]);

        contract.IsComplete.ShouldBeTrue();
        contract.IsSafeForDisplay.ShouldBeTrue();

        (contract with { StillValidActions = ["confirm", "delete"] }).IsComplete.ShouldBeFalse();
        (contract with { FocusTargetId = string.Empty }).IsComplete.ShouldBeFalse();
        (contract with { RecoveryMessage = "Secret Project is unavailable." }).IsComplete.ShouldBeFalse();
    }

    [Fact]
    public void AiActionReviewShouldRequireExplicitConfirmationAndAuditVisibleOutcomes()
    {
        ChatBotRecoveryPatternContract contract = ChatBotRecoveryPatternContract.ForAiActionReview(
            safeFailureCategory: "approval_required",
            focusTargetId: "ai-action-review-summary",
            explicitConfirmationCopy: "Confirm before external, file, project, tool, or participant action.",
            auditVisibleOutcomes: ["reject", "revise", "cancel"],
            safeNextAction: "Confirm, revise, reject, or cancel with audit-visible outcome.",
            restrictedSourceTextMarkers: ["raw provider payload"]);

        contract.IsComplete.ShouldBeTrue();
        (contract with { ExplicitConfirmationCopy = string.Empty }).IsComplete.ShouldBeFalse();
        (contract with { ExplicitConfirmationCopy = "Confirm before external action." }).IsComplete.ShouldBeFalse();
        (contract with { AuditVisibleOutcomes = ["reject"] }).IsComplete.ShouldBeFalse();
    }

    [Fact]
    public void QueueRetryShouldRequireDuplicateSafetyRetryCountFocusAndNextAction()
    {
        ChatBotRecoveryPatternContract contract = ChatBotRecoveryPatternContract.ForQueueRetry(
            safeFailureCategory: "retryable_dependency",
            focusTargetId: "queue-row-status",
            duplicateSafetyText: "Retry is duplicate-safe and will not create a second command.",
            retryCount: 1,
            safeNextAction: "Retry only while duplicate-safety text is visible.",
            restrictedSourceTextMarkers: ["raw exception"]);

        contract.IsComplete.ShouldBeTrue();
        contract.RetryCount.ShouldBe(1);

        (contract with { SafeFailureCategory = "raw exception" }).IsComplete.ShouldBeFalse();
        (contract with { DuplicateSafetyText = string.Empty }).IsComplete.ShouldBeFalse();
        (contract with { DuplicateSafetyText = "raw exception: dependency stack trace" }).IsComplete.ShouldBeFalse();
        (contract with { RetryCount = null }).IsComplete.ShouldBeFalse();
        (contract with { FocusTargetId = "queue-summary" }).IsComplete.ShouldBeFalse();
        (contract with { RecoveryMessage = "raw exception: stack trace" }).IsComplete.ShouldBeFalse();
    }

    [Fact]
    public void CorrectionAndTenantConfigurationShouldConstrainPreviewValidationAndConflictCause()
    {
        ChatBotRecoveryPatternContract correction = ChatBotRecoveryPatternContract.ForCorrection(
            safeFailureCategory: "policy_rationale_required",
            focusTargetId: "correction-summary",
            policyRationale: "Policy rationale is required before correction.",
            affectedContextPreview: "2 attachments and derived AI context will be refreshed.",
            correctionStatus: ChatBotCorrectionRecoveryStatus.Partial,
            safeNextAction: "Review affected context preview before saving.",
            restrictedSourceTextMarkers: ["secret attachment body"]);

        correction.IsComplete.ShouldBeTrue();
        correction.CorrectionStatus.ShouldBe(ChatBotCorrectionRecoveryStatus.Partial);
        (correction with { AffectedContextPreview = string.Empty }).IsComplete.ShouldBeFalse();
        (correction with { AffectedContextPreview = "raw payload contains secret attachment body" }).IsComplete.ShouldBeFalse();
        (correction with { CorrectionStatus = ChatBotCorrectionRecoveryStatus.RawFailure }).IsComplete.ShouldBeFalse();

        ChatBotRecoveryPatternContract tenant = ChatBotRecoveryPatternContract.ForTenantConfiguration(
            safeFailureCategory: "stale_data",
            focusTargetId: "tenant-config-validation-summary",
            validationSummaryPlacement: "before-fields",
            fieldMessageAssociations: new Dictionary<string, string>
            {
                ["retention-days"] = "retention-days-message",
            },
            saveConflictCause: ChatBotSaveConflictCause.StaleData,
            safeNextAction: "Review validation summary before saving.",
            restrictedSourceTextMarkers: ["tenant secret"]);

        tenant.IsComplete.ShouldBeTrue();
        (tenant with { SaveConflictCause = ChatBotSaveConflictCause.RawException }).IsComplete.ShouldBeFalse();
        (tenant with { FieldMessageAssociations = new Dictionary<string, string>() }).IsComplete.ShouldBeFalse();
        (tenant with { FieldMessageAssociations = new Dictionary<string, string> { ["tenant secret"] = "field-message" } }).IsComplete.ShouldBeFalse();
    }
}
