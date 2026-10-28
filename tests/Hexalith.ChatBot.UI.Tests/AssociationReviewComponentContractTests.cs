using Shouldly;

using static Hexalith.ChatBot.UI.Tests.ComponentContractSource;

namespace Hexalith.ChatBot.UI.Tests;

public sealed class AssociationReviewComponentContractTests
{
    [Fact]
    public void AssociationReviewPageShouldUseGovernedPrimitivesAndKeepActionsDiscoverable()
    {
        string page = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Pages/AssociationReview.razor");
        string actions = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Governed/ChatBotAssociationReviewActions.razor");
        string banner = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Governed/ChatBotStatusBanner.razor");
        string row = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Governed/ChatBotAssociationCandidateRow.razor");
        string comparison = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Governed/ChatBotAssociationEvidenceComparison.razor");

        page.ShouldContain("ChatBotConversationShell");
        page.ShouldContain("<FluentCard", Case.Sensitive);
        page.ShouldContain("<FluentStack", Case.Sensitive);
        page.ShouldContain("<FluentText", Case.Sensitive);
        page.ShouldContain("ChatBotProjectContextHeader");
        page.ShouldContain("ChatBotStatusBanner");
        page.ShouldContain("ChatBotBlockedState");
        page.ShouldContain("ChatBotAssociationEvidenceComparison");
        actions.ShouldContain("ChatBotGovernedAction");
        actions.ShouldContain("<FluentLabel", Case.Sensitive);
        actions.ShouldContain("<FluentTextArea", Case.Sensitive);
        actions.ShouldContain("ValueChanged=\"UpdateNote\"");
        actions.ShouldContain("ValueChanged=\"UpdateCorrectionRationale\"");
        actions.ShouldNotContain("<textarea", Case.Sensitive);
        actions.ShouldNotContain("<label", Case.Sensitive);
        actions.ShouldContain("association-correction-submit");
        actions.ShouldContain("AssociationReviewCorrectionRationale");
        actions.ShouldContain("RecoverySafeNextActionCorrection");
        actions.ShouldContain("already-decided");
        actions.ShouldContain("already-corrected");
        actions.ShouldContain("evidence-expired");
        actions.ShouldContain("not-authorized");
        actions.ShouldContain("projection-pending");
        row.ShouldContain("<FluentButton", Case.Sensitive);
        row.ShouldContain("Type=\"ButtonType.Button\"");
        row.ShouldContain("role=\"radio\"");
        row.ShouldContain("aria-checked=\"@IsSelectedText\"");
        row.ShouldContain("data-chatbot-association-candidate=\"@Candidate.ProjectId\"");
        row.ShouldContain("data-chatbot-selected=\"@IsSelectedText\"");
        row.ShouldNotContain("<button", Case.Sensitive);
        row.ShouldContain("ChatBotEvidenceChip");
        row.ShouldContain("AssociationReviewRendering.EvidenceText(evidence, UiText)");
        comparison.ShouldContain("<FluentCard", Case.Sensitive);
        comparison.ShouldContain("<FluentStack", Case.Sensitive);
        comparison.ShouldContain("<FluentText", Case.Sensitive);
        comparison.ShouldContain("data-chatbot-association-comparison=\"true\"");
        comparison.ShouldContain("<article");
        ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Governed/AssociationReviewRendering.cs").ShouldContain("ChatBotMetadataRendering.CodeRow");

        // The <dl> dump was replaced by a Fluent key-value block. The previous assertion here checked for
        // "<dl" and passed only because those characters appear in a code comment describing the removal -
        // while Story13DefinitionListMigrationTests forbids the element in this very file. Assert the
        // replacement structure and the element's absence instead.
        comparison.ShouldContain("CodeRow(", Case.Sensitive);
        StripComments(comparison).ShouldNotContain("<dl");
    }

    [Fact]
    public void AssociationReviewActionsShouldPreserveValidationBannerAndDisabledReasonCatalog()
    {
        string actions = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Governed/ChatBotAssociationReviewActions.razor");
        string banner = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Governed/ChatBotStatusBanner.razor");
        string policy = ReadProjectFile("src/Hexalith.ChatBot.UI/State/AssociationReview/AssociationReviewActionPolicy.cs");

        actions.ShouldContain("<ChatBotStatusBanner", Case.Sensitive);
        actions.ShouldContain("StableId=\"@Id(\"association-review-validation\")\"");
        actions.ShouldContain("StateFamily=\"@ChatBotFeedbackStateFamily.ValidationError\"");
        // The banner renders id="@StableId", so this reference resolves to a real element; it is composed at
        // runtime by DecisionNoteDescribedBy rather than hardcoded.
        actions.ShouldContain("DecisionNoteDescribedBy", Case.Sensitive);
        actions.ShouldContain("Id(\"association-review-validation\")");
        actions.ShouldContain("Id(\"association-decision-note-counter\")");
        banner.ShouldContain("id=\"@StableId\"");
        // ARIA state attributes must resolve to explicit "true"/"false" strings, not a .NET bool (which Blazor
        // would drop entirely when false). See DecisionNoteInvalidText/CorrectionRationaleInvalidText.
        actions.ShouldContain("aria-invalid=\"@DecisionNoteInvalidText\"");
        actions.ShouldContain("aria-invalid=\"@CorrectionRationaleInvalidText\"");
        actions.ShouldContain("DecisionNoteInvalidText", Case.Sensitive);
        actions.ShouldContain("CorrectionRationaleInvalidText", Case.Sensitive);
        actions.ShouldNotContain("aria-invalid=\"@(", Case.Sensitive);
        actions.ShouldContain("ValueChanged=\"UpdateNote\"");
        actions.ShouldContain("ValueChanged=\"UpdateCorrectionRationale\"");

        foreach (string validationCode in new[]
        {
            "candidate-required",
            "correction-invalid-lifecycle",
            "correction-target-required",
            "stale-evidence",
            "association-review-note-too-long",
        })
        {
            actions.ShouldContain(validationCode, Case.Sensitive);
        }

        foreach (string disabledReasonCode in new[]
        {
            "candidate-required",
            "evidence-expired",
            "not-authorized",
            "projection-pending",
            "already-decided",
            "already-corrected",
            "audit-unavailable",
            "corrected-context-stale",
            "correction-delayed",
            "correction-invalid-lifecycle",
            "correction-target-required",
            "policy-blocked",
            "projection-invalidation-unavailable",
            "stale-evidence",
            "target-unauthorized",
            "terminal-state",
        })
        {
            actions.ShouldContain(disabledReasonCode, Case.Sensitive);
        }

        policy.ShouldContain("DisabledReasonPriority", Case.Sensitive);
        policy.ShouldContain("ResolveDecisionDisabledReasonCode", Case.Sensitive);
        policy.ShouldContain("ResolveCorrectionDisabledReasonCode", Case.Sensitive);
        actions.ShouldContain("DisabledReasonText", Case.Sensitive);
    }

    /// <summary>
    /// The suppression rule was verified only at the service layer. These assert it at the two components that
    /// actually render evidence: a restricted item shows the localized placeholder, and neither the evidence
    /// kind/reference nor a reference-derived id is emitted.
    /// </summary>
    [Fact]
    public void EvidenceRenderersSuppressRestrictedEvidenceAndNeverDeriveIdsFromTheReference()
    {
        foreach (string path in new[]
        {
            "src/Hexalith.ChatBot.UI/Components/Governed/ChatBotAssociationCandidateRow.razor",
            "src/Hexalith.ChatBot.UI/Components/Governed/ChatBotAssociationEvidenceComparison.razor",
        })
        {
            string source = StripComments(ReadProjectFile(path));

            // Kind/Reference render only on the Available branch.
            source.ShouldContain("AssociationReviewRendering.EvidenceText(evidence, UiText)");
            string helper = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Governed/AssociationReviewRendering.cs");
            helper.ShouldContain("evidence.State is ChatBotEvidenceState.Available");
            helper.ShouldContain("AssociationReviewEvidenceRestricted");

            // The chip must not advertise an open affordance for evidence it cannot open.
            source.ShouldContain("CanOpenEvidence=\"@(evidence.State is ChatBotEvidenceState.Available)\"");
            source.ShouldNotContain("CanOpenEvidence=\"true\"");

            // Ids are derived from the instance and position, never from the free-text reference - the chip
            // renders its reason id precisely when the evidence is restricted.
            source.ShouldContain("EvidenceStableId(");
            source.ShouldNotContain("{evidence.Reference}");
        }
    }

    /// <summary>
    /// The candidate row is a role="radio"; nesting another interactive control inside it is invalid markup
    /// and breaks the radio's accessible name.
    /// </summary>
    [Fact]
    public void EvidenceChipsAreNotNestedInsideTheCandidateRadio()
    {
        string row = StripComments(ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Governed/ChatBotAssociationCandidateRow.razor"));

        int closeButton = row.IndexOf("</FluentButton>", StringComparison.Ordinal);
        int chip = row.IndexOf("<ChatBotEvidenceChip", StringComparison.Ordinal);

        closeButton.ShouldBeGreaterThan(-1);
        chip.ShouldBeGreaterThan(closeButton, "evidence chips must render after the radio button closes");
    }

}
