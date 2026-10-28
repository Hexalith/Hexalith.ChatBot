using Shouldly;

using static Hexalith.ChatBot.UI.Tests.ComponentContractSource;

namespace Hexalith.ChatBot.UI.Tests;

public sealed class FrontComposerBodyContractTests
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
        layout.ShouldContain("ShowAccountMenu=\"false\"");
        layout.ShouldContain("@Body");
        app.ShouldContain("css/chatbot.tokens.css");
        (app + layout).ShouldNotContain("<FluentProviders", Case.Sensitive);
        (app + layout).ShouldNotContain("StoreInitializer", Case.Sensitive);
        program.ShouldContain("AddHexalithFrontComposerQuickstart");
        program.ShouldContain("AddHexalithDomain<ChatBotUiFrontComposerMarker>");
        program.ShouldNotContain("AddHexalithEventStore", Case.Sensitive);
        program.ShouldNotContain("EventStore:BaseAddress", Case.Sensitive);
        program.ShouldContain("AddFluentUIComponents", Case.Sensitive);
        program.ShouldContain("AddChatBotUiHostDefaults", Case.Sensitive);
        program.ShouldContain("MapChatBotUiHealthEndpoints", Case.Sensitive);
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
}
