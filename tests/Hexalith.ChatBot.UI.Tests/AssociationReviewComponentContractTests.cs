using Shouldly;

namespace Hexalith.ChatBot.UI.Tests;

public sealed class AssociationReviewComponentContractTests
{
    [Fact]
    public void M0GovernedPagesShouldRemainFrontComposerBodyContentWithoutDuplicateShellOwnership()
    {
        string layout = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Layout/MainLayout.razor");
        string app = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/App.razor");
        string program = ReadProjectFile("src/Hexalith.ChatBot.UI/Program.cs");
        string projectConversation = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Pages/ProjectConversation.razor");
        string projectConversationWorkspace = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Governed/ChatBotProjectConversationWorkspace.razor");
        string associationReview = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Pages/AssociationReview.razor");
        string approvalItem = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Governed/ChatBotApprovalConversationItem.razor");
        string aiPreview = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Governed/ChatBotAiActionPreviewSections.razor");

        CountOccurrences(layout, "<FrontComposerShell").ShouldBe(1);
        layout.ShouldContain("AppTitle=\"Hexalith ChatBot\"");
        layout.ShouldContain("@Body");
        app.ShouldContain("css/chatbot.tokens.css");
        (app + layout).ShouldNotContain("<FluentProviders", Case.Sensitive);
        (app + layout).ShouldNotContain("StoreInitializer", Case.Sensitive);
        program.ShouldContain("AddHexalithFrontComposerQuickstart");
        program.ShouldContain("AddHexalithDomain<ChatBotUiFrontComposerMarker>");
        program.ShouldContain("AddHexalithEventStore");
        program.ShouldContain("AddFluentUIComponents", Case.Sensitive);
        program.ShouldNotContain("AddFluxor", Case.Sensitive);

        projectConversation.ShouldContain("@page \"/projects/{ProjectId}/conversation\"");
        projectConversation.ShouldContain("ChatBotProjectConversationWorkspace");
        projectConversationWorkspace.ShouldContain("<ChatBotConversationShell");
        projectConversationWorkspace.ShouldContain("data-chatbot-responsive-fixture=\"@ResponsiveFixture\"");
        projectConversation.ShouldNotContain("FcPageLayoutMode.Constrained");

        associationReview.ShouldContain("@page \"/association-review/{AssociationId}\"");
        associationReview.ShouldContain("<ChatBotConversationShell");
        associationReview.ShouldContain("data-chatbot-responsive-fixture=\"association-review\"");
        associationReview.ShouldNotContain("FcPageLayoutMode.Constrained");

        approvalItem.ShouldContain("SubmitApprovalDecisionAsync");
        approvalItem.ShouldContain("ChatBotAiActionPreviewSections");
        approvalItem.ShouldContain("aria-disabled=\"@ApproveAriaDisabled\"");
        aiPreview.ShouldContain("data-chatbot-ai-action-preview=\"metadata-only\"");
        aiPreview.ShouldNotContain("providerPayload");
        aiPreview.ShouldNotContain("RawAttachmentContent");
    }

    // AC3 (single, non-duplicated FrontComposer ownership) + AC6 (unique landmarks): the migrated M0
    // pages must stay pure body content. A page that re-introduces its own <main>, banner landmark,
    // skip link, or a nested <FrontComposerShell> would silently duplicate the shell-owned landmarks
    // the FrontComposer shell already renders. The static fixtures only approximate the rendered DOM,
    // so this guards the real production source against that regression.
    [Fact]
    public void MigratedM0PagesMustNotDuplicateShellLandmarksOwnedByFrontComposerShell()
    {
        string[] pageFiles =
        [
            "src/Hexalith.ChatBot.UI/Components/Governed/ChatBotProjectConversationWorkspace.razor",
            "src/Hexalith.ChatBot.UI/Components/Pages/AssociationReview.razor",
            "src/Hexalith.ChatBot.UI/Components/Pages/GovernedOperations.razor",
            "src/Hexalith.ChatBot.UI/Components/Pages/ProjectWorkspace.razor",
        ];

        foreach (string pageFile in pageFiles)
        {
            string page = ReadProjectFile(pageFile);

            // Only MainLayout owns the shell; pages render through <ChatBotConversationShell> as body content.
            page.ShouldContain("<ChatBotConversationShell");
            page.ShouldNotContain("<FrontComposerShell", Case.Sensitive);

            // The FrontComposer shell renders the banner, skip link, and main region; pages must not re-emit them.
            page.ShouldNotContain("role=\"banner\"", Case.Sensitive);
            page.ShouldNotContain("<main", Case.Sensitive);
            page.ShouldNotContain("chatbot-skip-link", Case.Sensitive);

            // Pages must not reintroduce an app-owned provider/store-initializer tree.
            page.ShouldNotContain("<FluentProviders", Case.Sensitive);
            page.ShouldNotContain("StoreInitializer", Case.Sensitive);
        }

        // The shared governed inner shell exposes unique, labelled region/complementary landmarks and never
        // re-declares a <main> or banner that would collide with the FrontComposer shell landmarks.
        string conversationShell = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Governed/ChatBotConversationShell.razor");
        conversationShell.ShouldContain("role=\"region\"");
        conversationShell.ShouldContain("role=\"complementary\"");
        conversationShell.ShouldContain("aria-label=");
        conversationShell.ShouldNotContain("<main", Case.Sensitive);
        conversationShell.ShouldNotContain("role=\"banner\"", Case.Sensitive);
        conversationShell.ShouldNotContain("<FrontComposerShell", Case.Sensitive);
    }

    [Fact]
    public void AssociationReviewPageShouldUseGovernedPrimitivesAndKeepActionsDiscoverable()
    {
        string page = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Pages/AssociationReview.razor");
        string actions = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Governed/ChatBotAssociationReviewActions.razor");
        string row = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Governed/ChatBotAssociationCandidateRow.razor");

        page.ShouldContain("ChatBotConversationShell");
        page.ShouldContain("ChatBotProjectContextHeader");
        page.ShouldContain("ChatBotStatusBanner");
        page.ShouldContain("ChatBotBlockedState");
        page.ShouldContain("ChatBotAssociationEvidenceComparison");
        actions.ShouldContain("ChatBotGovernedAction");
        actions.ShouldContain("association-correction-submit");
        actions.ShouldContain("AssociationReviewCorrectionRationale");
        actions.ShouldContain("RecoverySafeNextActionCorrection");
        actions.ShouldContain("already-decided");
        actions.ShouldContain("already-corrected");
        actions.ShouldContain("evidence-expired");
        actions.ShouldContain("not-authorized");
        actions.ShouldContain("projection-pending");
        row.ShouldContain("role=\"radio\"");
        row.ShouldContain("ChatBotEvidenceChip");
        row.ShouldContain("AssociationReviewEvidenceRestricted");
    }

    [Fact]
    public void AssociationReviewCssShouldCoverResponsiveForcedColorsAndReducedMotionWithoutRawColors()
    {
        string css = ReadProjectFile("src/Hexalith.ChatBot.UI/wwwroot/css/chatbot.tokens.css");

        css.ShouldContain(".chatbot-association-candidate");
        css.ShouldContain("@media (max-width: 48rem)");
        css.ShouldContain("@media (forced-colors: active)");
        css.ShouldContain("@media (prefers-reduced-motion: reduce)");
        css.ShouldNotContain("#");
        css.ShouldNotContain("rgb(");
        css.ShouldNotContain("hsl(");
    }

    [Fact]
    public void ProjectConversationPageShouldUseGovernedPrimitivesAndLabelSystemDecisions()
    {
        string page = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Pages/ProjectConversation.razor");
        string workspace = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Governed/ChatBotProjectConversationWorkspace.razor");
        string stream = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Governed/ChatBotConversationStream.razor");
        string item = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Governed/ChatBotEmailConversationItem.razor");
        string decision = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Governed/ChatBotDecisionConversationItem.razor");
        string participant = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Governed/ChatBotParticipantConversationItem.razor");
        string attachment = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Governed/ChatBotAttachmentConversationItem.razor");
        string whyPanel = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Governed/ChatBotWhyProjectPanel.razor");

        page.ShouldContain("ChatBotProjectConversationWorkspace");
        workspace.ShouldContain("ChatBotConversationShell");
        workspace.ShouldContain("ChatBotProjectContextHeader");
        workspace.ShouldContain("ChatBotStatusBanner");
        workspace.ShouldContain("ChatBotBlockedState");
        workspace.ShouldContain("ChatBotWhyProjectPanel");
        workspace.ShouldContain("OpenProjectAssociationWhyPanelAction");
        page.ShouldContain("@page \"/projects/{ProjectId}/conversation\"");
        stream.ShouldContain("ChatBotEmailConversationItem");
        stream.ShouldContain("ChatBotDecisionConversationItem");
        stream.ShouldContain("OnWhyThisProjectRequested");
        stream.ShouldContain("ChatBotParticipantConversationItem");
        stream.ShouldContain("ChatBotAttachmentConversationItem");
        item.ShouldContain("ChatBotActorBadge");
        item.ShouldContain("ProjectConversationSystemDecision");
        item.ShouldContain("ChatBotEvidenceChip");
        item.ShouldContain("WhyProjectOpenAction");
        item.ShouldContain("SourceProviderMessageId");
        item.ShouldContain("InternetMessageId");
        item.ShouldContain("SourceReceivedAtUtc");
        item.ShouldContain("SourceProvenanceDisplayToken");
        item.ShouldContain("ThresholdBandLabel");
        item.ShouldNotContain("SourceContext");
        item.ShouldNotContain("providerPayload");
        decision.ShouldContain("ChatBotActorBadge");
        decision.ShouldContain("ChatBotEvidenceChip");
        decision.ShouldContain("WhyProjectOpenAction");
        decision.ShouldContain("ProjectConversationDecisionItemAccessible");
        decision.ShouldContain("DecisionKindLabel");
        decision.ShouldContain("CorrectionKindLabel");
        decision.ShouldContain("EvidenceReferenceSummary");
        decision.ShouldContain("SupersedesAssociationId");
        decision.ShouldContain("PropagationProgress");
        decision.ShouldContain("DecisionNoteRedactionState");
        decision.ShouldContain("CorrectionRationaleRedactionState");
        decision.ShouldContain("DecisionUnavailableReason");
        decision.ShouldNotContain("DecisionNote\"");
        decision.ShouldNotContain("CorrectionRationale\"");
        decision.ShouldNotContain("SourceContext");
        decision.ShouldNotContain("providerPayload");
        participant.ShouldContain("ChatBotActorBadge");
        participant.ShouldContain("ChatBotEvidenceChip");
        participant.ShouldContain("ParticipantAllowedReviewActions");
        participant.ShouldContain("WhyUnavailable");
        participant.ShouldNotContain("AddressEvidence");
        participant.ShouldNotContain("ProviderDisplayName");
        attachment.ShouldContain("ChatBotActorBadge");
        attachment.ShouldContain("ChatBotEvidenceChip");
        attachment.ShouldContain("AttachmentStatusLabel");
        attachment.ShouldContain("WhyUnavailable");
        attachment.ShouldContain("RedactedMetadataValue");
        attachment.ShouldContain("SourceProviderAttachmentId");
        attachment.ShouldContain("AttachmentDisplayName");
        attachment.ShouldNotContain("SourceContext");
        attachment.ShouldNotContain("providerPayload");
        attachment.ShouldNotContain("RawAttachmentContent");
        whyPanel.ShouldContain("data-chatbot-why-project-panel");
        whyPanel.ShouldContain("WhyProjectPanelAccessible");
        whyPanel.ShouldContain("EvidenceState(evidence)");
        whyPanel.ShouldContain("WhyProjectEvidenceRedactedExplanation");
        whyPanel.ShouldContain("SupersedingCorrection");
        whyPanel.ShouldNotContain("DecisionNote\"");
        whyPanel.ShouldNotContain("CorrectionRationale\"");
        whyPanel.ShouldNotContain("SourceContext");
        whyPanel.ShouldNotContain("providerPayload");
    }

    [Fact]
    public void ProjectConversationCssShouldCoverResponsiveForcedColorsAndReducedMotionWithoutRawColors()
    {
        string css = ReadProjectFile("src/Hexalith.ChatBot.UI/wwwroot/css/chatbot.tokens.css");

        css.ShouldContain(".chatbot-project-conversation");
        css.ShouldContain(".chatbot-email-conversation-item");
        css.ShouldContain(".chatbot-decision-conversation-item");
        css.ShouldContain(".chatbot-participant-conversation-item");
        css.ShouldContain(".chatbot-attachment-conversation-item");
        css.ShouldContain(".chatbot-why-project-panel");
        css.ShouldContain("@media (max-width: 48rem)");
        css.ShouldContain("@media (forced-colors: active)");
        css.ShouldContain("@media (prefers-reduced-motion: reduce)");
        css.ShouldNotContain("#");
        css.ShouldNotContain("rgb(");
        css.ShouldNotContain("hsl(");
    }

    private static string ReadProjectFile(string relativePath)
        => File.ReadAllText(ProjectPath(relativePath));

    private static string ProjectPath(string relativePath)
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Hexalith.ChatBot.slnx")))
        {
            directory = directory.Parent;
        }

        directory.ShouldNotBeNull();
        return Path.Combine(directory.FullName, relativePath);
    }

    private static int CountOccurrences(string value, string marker)
    {
        int count = 0;
        int startIndex = 0;
        while ((startIndex = value.IndexOf(marker, startIndex, StringComparison.Ordinal)) >= 0)
        {
            count++;
            startIndex += marker.Length;
        }

        return count;
    }
}
