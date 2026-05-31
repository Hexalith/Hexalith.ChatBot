using System.Text.RegularExpressions;

using Hexalith.ChatBot.UI.Design;

using Shouldly;

namespace Hexalith.ChatBot.UI.Tests;

/// <summary>
/// Guards the governed UI primitive contracts that future feature stories compose.
/// </summary>
public sealed class ChatBotGovernedPrimitiveContractTests
{
    private static readonly string[] RequiredActorCategories =
    [
        "HumanUser",
        "ExternalParty",
        "ServiceClient",
        "AiActor",
        "BackgroundWorker",
        "Cli",
        "Mcp",
        "MailboxEvent",
    ];

    private static readonly string[] RequiredRiskClasses =
    [
        "ExternallyVisible",
        "FileExposing",
        "ProjectMutating",
        "ToolInvoking",
        "TaskCreating",
        "ParticipantRepresenting",
    ];

    private static readonly string[] RequiredEvidenceStates =
    [
        "Available",
        "Unavailable",
        "Redacted",
        "Unauthorized",
    ];

    private static readonly string[] RequiredBlockedReasons =
    [
        "Denial",
        "UnresolvedAssociation",
        "Quarantine",
        "FailedDependency",
        "UnsafeContext",
    ];

    private static readonly string[] PrimitiveComponents =
    [
        "src/Hexalith.ChatBot.UI/Components/Governed/ChatBotProjectContextHeader.razor",
        "src/Hexalith.ChatBot.UI/Components/Governed/ChatBotConversationShell.razor",
        "src/Hexalith.ChatBot.UI/Components/Governed/ChatBotActorBadge.razor",
        "src/Hexalith.ChatBot.UI/Components/Governed/ChatBotEvidenceChip.razor",
        "src/Hexalith.ChatBot.UI/Components/Governed/ChatBotRiskChip.razor",
        "src/Hexalith.ChatBot.UI/Components/Governed/ChatBotBlockedState.razor",
        "src/Hexalith.ChatBot.UI/Components/Governed/ChatBotStatusBanner.razor",
    ];

    [Fact]
    public void GovernedPrimitiveFilesShouldExistInUiOwnedFolder()
    {
        foreach (string component in PrimitiveComponents)
        {
            File.Exists(ProjectPath(component)).ShouldBeTrue($"{component} should exist.");
        }
    }

    [Fact]
    public void ActorBadgeShouldExposeExactNeutralActorCategoryContract()
    {
        Enum.GetNames<ChatBotActorCategory>().ShouldBe(RequiredActorCategories, ignoreOrder: false);

        foreach (ChatBotActorCategory category in Enum.GetValues<ChatBotActorCategory>())
        {
            ChatBotGovernedUiText.GetActorCategoryLabel(category).ShouldNotBeNullOrWhiteSpace();
            ChatBotGovernedUiText.GetActorCategoryIconText(category).ShouldNotBeNullOrWhiteSpace();
        }

        string badge = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Governed/ChatBotActorBadge.razor");
        badge.ShouldContain("<FluentBadge");
        badge.ShouldContain("aria-label=\"@AccessibleName\"");
        badge.ShouldContain("aria-label=\"@UnresolvedActionAccessibleLabel\"");
        badge.ShouldContain("data-chatbot-actor-category");
        badge.ShouldContain("Unresolved actor");
        badge.ShouldNotContain("data-chatbot-status=\"human");
        badge.ShouldNotContain("data-chatbot-status=\"ai");
    }

    [Fact]
    public void EvidenceAndRiskChipsShouldBeTextFirstKeyboardOperableAndRedactionSafe()
    {
        Enum.GetNames<ChatBotEvidenceState>().ShouldBe(RequiredEvidenceStates, ignoreOrder: false);
        Enum.GetNames<ChatBotRiskActionClass>().ShouldBe(RequiredRiskClasses, ignoreOrder: false);

        string evidence = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Governed/ChatBotEvidenceChip.razor");
        evidence.ShouldContain("<FluentBadge");
        evidence.ShouldContain("type=\"button\"");
        evidence.ShouldContain("@onclick=\"ActivateAsync\"");
        evidence.ShouldNotContain("@onkeydown");
        evidence.ShouldContain("aria-disabled=\"@AriaDisabled\"");
        evidence.ShouldContain("aria-describedby=\"@ReasonElementId\"");
        evidence.ShouldContain("UnavailableReason");
        evidence.ShouldContain("Redacted");
        evidence.ShouldContain("Unauthorized");

        string risk = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Governed/ChatBotRiskChip.razor");
        risk.ShouldContain("<FluentBadge");
        risk.ShouldContain("ChatBotGovernedUiText.GetRiskActionClassLabel");
        risk.ShouldContain("PolicyReason");
        risk.ShouldContain("chatbot-chip__cue");
        risk.ShouldContain("data-chatbot-status=\"warning\"");
    }

    [Fact]
    public void BlockedStateAndStatusBannerShouldUseTerminalAlertOnlyWhenExplicitlyTerminal()
    {
        Enum.GetNames<ChatBotBlockedReason>().ShouldBe(RequiredBlockedReasons, ignoreOrder: false);

        string blocked = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Governed/ChatBotBlockedState.razor");
        blocked.ShouldContain("<FluentBadge");
        blocked.ShouldContain("role=\"@FeedbackContract.AriaRole\"");
        blocked.ShouldContain("aria-live=\"@FeedbackContract.AriaLive\"");
        blocked.ShouldContain("SafeNextAction");
        blocked.ShouldContain("StableId");
        blocked.ShouldNotContain("Exception");

        string banner = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Governed/ChatBotStatusBanner.razor");
        banner.ShouldContain("<FluentBadge");
        banner.ShouldContain("IsTerminalForCurrentUser ? \"alert\" : \"status\"");
        banner.ShouldContain("data-chatbot-status=\"@StatusSlot\"");
        banner.ShouldContain("chatbot-status__label");
    }

    [Fact]
    public void GovernedOperationsShouldConsumeSharedPrimitivesAndKeepUiOrigin()
    {
        string page = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Pages/GovernedOperations.razor");
        string service = ReadProjectFile("src/Hexalith.ChatBot.UI/Services/GovernedOperationService.cs");

        page.ShouldContain("<ChatBotConversationShell");
        page.ShouldContain("<ChatBotProjectContextHeader");
        page.ShouldContain("<ChatBotStatusBanner");
        page.ShouldNotContain("<div class=\"chatbot-status\"");
        service.ShouldContain("ChatBotSurfaceOrigin.Ui");
    }

    [Fact]
    public void PrimitiveStylesShouldUseSemanticTokensAndForcedColorCues()
    {
        string css = ReadProjectFile("src/Hexalith.ChatBot.UI/wwwroot/css/chatbot.tokens.css");

        css.ShouldContain(".chatbot-actor-badge");
        css.ShouldContain(".chatbot-chip");
        css.ShouldContain(".chatbot-blocked-state");
        css.ShouldContain("data-chatbot-status=\"warning\"", Case.Insensitive);
        css.ShouldContain("@media (forced-colors: active)");
        css.ShouldNotContain("#");

        MatchCollection chatbotColorAssignments = Regex.Matches(
            css,
            @"^\s*--chatbot-color-[^:]+:\s*(?<value>[^;]+);",
            RegexOptions.CultureInvariant | RegexOptions.Multiline);

        foreach (Match assignment in chatbotColorAssignments)
        {
            assignment.Groups["value"].Value.ShouldContain("var(--");
        }
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
