using Shouldly;

using static Hexalith.ChatBot.UI.Tests.ComponentContractSource;

namespace Hexalith.ChatBot.UI.Tests;

public sealed class ProjectConversationComponentContractTests
{
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
        whyPanel.ShouldContain("<FluentButton", Case.Sensitive);
        whyPanel.ShouldContain("Type=\"ButtonType.Button\"", Case.Sensitive);
        whyPanel.ShouldContain("OnClick=\"CloseAsync\"", Case.Sensitive);
        whyPanel.ShouldContain("OnClick=\"OpenSupersedingCorrectionAsync\"", Case.Sensitive);
        whyPanel.ShouldContain("EvidenceState(evidence)");
        whyPanel.ShouldContain("WhyProjectEvidenceRedactedExplanation");
        whyPanel.ShouldContain("SupersedingCorrection");
        whyPanel.ShouldNotContain("<button", Case.Sensitive);
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
        foreach (string selector in new[] { ".chatbot-email-conversation-item__header", ".chatbot-decision-conversation-item__header", ".chatbot-participant-conversation-item__header", ".chatbot-attachment-conversation-item__header" })
        {
            CssContract.Value(css, "(max-width: 48rem)", selector, "flex-direction").ShouldBe("column");
        }

        foreach (string selector in new[] { ".chatbot-email-conversation-item", ".chatbot-decision-conversation-item", ".chatbot-participant-conversation-item", ".chatbot-attachment-conversation-item", ".chatbot-why-project-panel" })
        {
            CssContract.Value(css, "(prefers-reduced-motion: reduce)", selector, "animation").ShouldBe("none !important");
        }

        CssContract.Value(css, "(forced-colors: active)", ".chatbot-why-project-panel:focus-within", "outline")
            .ShouldBe("2px solid Highlight");
        CssContract.LiteralColors(css).ShouldBeEmpty();
    }
}
