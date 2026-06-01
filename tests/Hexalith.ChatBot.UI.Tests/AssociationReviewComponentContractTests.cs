using Shouldly;

namespace Hexalith.ChatBot.UI.Tests;

public sealed class AssociationReviewComponentContractTests
{
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
        string stream = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Governed/ChatBotConversationStream.razor");
        string item = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Governed/ChatBotEmailConversationItem.razor");
        string decision = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Governed/ChatBotDecisionConversationItem.razor");
        string participant = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Governed/ChatBotParticipantConversationItem.razor");
        string attachment = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Governed/ChatBotAttachmentConversationItem.razor");

        page.ShouldContain("ChatBotConversationShell");
        page.ShouldContain("ChatBotProjectContextHeader");
        page.ShouldContain("ChatBotStatusBanner");
        page.ShouldContain("ChatBotBlockedState");
        page.ShouldContain("@page \"/projects/{ProjectId}/conversation\"");
        stream.ShouldContain("ChatBotEmailConversationItem");
        stream.ShouldContain("ChatBotDecisionConversationItem");
        stream.ShouldContain("ChatBotParticipantConversationItem");
        stream.ShouldContain("ChatBotAttachmentConversationItem");
        item.ShouldContain("ChatBotActorBadge");
        item.ShouldContain("ProjectConversationSystemDecision");
        item.ShouldContain("ChatBotEvidenceChip");
        item.ShouldContain("SourceProviderMessageId");
        item.ShouldContain("InternetMessageId");
        item.ShouldContain("SourceReceivedAtUtc");
        item.ShouldContain("SourceProvenanceDisplayToken");
        item.ShouldContain("ThresholdBandLabel");
        item.ShouldNotContain("SourceContext");
        item.ShouldNotContain("providerPayload");
        decision.ShouldContain("ChatBotActorBadge");
        decision.ShouldContain("ChatBotEvidenceChip");
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
}
