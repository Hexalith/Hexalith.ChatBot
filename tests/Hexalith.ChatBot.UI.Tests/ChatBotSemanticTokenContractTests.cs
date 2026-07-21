using System.Text.RegularExpressions;

using Shouldly;

namespace Hexalith.ChatBot.UI.Tests;

/// <summary>
/// Guards the ChatBot visual foundation: Fluent/FrontComposer owns semantic color roles and visual
/// primitives, while ChatBot CSS is limited to product layout, responsive behavior, and accessibility.
/// </summary>
public sealed class ChatBotSemanticTokenContractTests
{
    private static readonly string[] RequiredFeedbackKinds = ["Info", "Warning", "Danger", "Success"];

    [Fact]
    public void StylesheetShouldDelegateSemanticRolesAndVisualPrimitivesToFluent()
    {
        string css = ReadProjectFile("src/Hexalith.ChatBot.UI/wwwroot/css/chatbot.tokens.css");
        string banner = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Governed/ChatBotStatusBanner.razor");
        string blocked = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Governed/ChatBotBlockedState.razor");
        string composer = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Governed/ChatBotGovernedComposer.razor");
        string workspace = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Pages/ProjectWorkspace.razor");

        css.ShouldNotContain("--chatbot-color-", Case.Sensitive);
        css.ShouldNotContain(".chatbot-status__label", Case.Sensitive);
        css.ShouldNotContain(".chatbot-validation-summary", Case.Sensitive);
        css.ShouldNotContain(".chatbot-project-picker__link", Case.Sensitive);

        banner.ShouldContain("<FluentMessageBar");
        banner.ShouldContain("Intent=\"@Intent\"");
        blocked.ShouldContain("<FluentMessageBar");
        blocked.ShouldContain("Intent=\"MessageBarIntent.Error\"");
        composer.ShouldContain("<FluentMessageBar Intent=\"MessageBarIntent.Error\"");
        workspace.ShouldContain("<FluentAnchorButton");
        workspace.ShouldNotContain("<a class=\"chatbot-project-picker__link\"");
    }

    [Fact]
    public void StylesheetShouldNotDeclareChatBotPrimitiveAliasesOrPaletteValues()
    {
        string css = ReadProjectFile("src/Hexalith.ChatBot.UI/wwwroot/css/chatbot.tokens.css");

        foreach (string forbiddenAliasPrefix in new[]
        {
            "--chatbot-color-",
            "--chatbot-type-",
            "--chatbot-font-",
            "--chatbot-radius-",
            "--chatbot-space-",
            "--chatbot-density-",
            "--chatbot-panel-gap",
            "--chatbot-row-gap",
        })
        {
            css.ShouldNotContain(forbiddenAliasPrefix, Case.Sensitive);
        }

        foreach (string legacyTokenPrefix in new[]
        {
            "--type-ramp-",
            "--neutral-foreground-",
            "--neutral-fill-",
            "--accent-",
            "--palette-",
            "--design-unit",
        })
        {
            css.ShouldNotContain(legacyTokenPrefix, Case.Insensitive);
        }

        css.ShouldNotContain("#");
        css.ShouldNotContain("rgb(", Case.Insensitive);
        css.ShouldNotContain("hsl(", Case.Insensitive);
        css.ShouldNotContain("Temporary inheritance bridge", Case.Sensitive);
        css.ShouldNotContain("until the runtime", Case.Sensitive);
    }

    [Fact]
    public void StylesheetShouldKeepForcedColorFocusAndComponentsShouldKeepNonColorStatusCues()
    {
        string css = ReadProjectFile("src/Hexalith.ChatBot.UI/wwwroot/css/chatbot.tokens.css");
        string banner = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Governed/ChatBotStatusBanner.razor");
        string summary = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Governed/ChatBotConversationItemStatusSummary.razor");

        css.ShouldContain("@media (forced-colors: active)");
        css.ShouldContain("Highlight");
        css.ShouldContain("outline:");
        banner.ShouldContain("Title=\"@UiText.FeedbackKindLabel(Kind)\"");
        banner.ShouldContain("data-chatbot-status=\"@StatusSlot\"");
        summary.ShouldContain("<FluentBadge");
        summary.ShouldContain("@UiText.StatusSummaryHealthLabel(facet.Health)");
        summary.ShouldContain("data-chatbot-health=\"@facet.Health\"");
    }

    [Fact]
    public void StatusSummaryHealthBadgeShouldMapUnknownAndDegradedToWarning()
    {
        // Story 13.4 status-cue semantics, pinned during the Story 13.1 review: an UNKNOWN health posture renders
        // amber (Warning) rather than neutral (Subtle) so an unreported/unknown facet is not mistaken for healthy.
        // Source-scan (this project renders no bUnit) over the private HealthBadgeColor switch; a revert of the
        // UNKNOWN arm fails this gate. The color is paired with a distinct text label (StatusSummaryHealthLabel
        // above), so DEGRADED and UNKNOWN sharing amber is not a color-only cue (NFR6 / UX-DR4).
        string summary = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Governed/ChatBotConversationItemStatusSummary.razor");

        summary.ShouldContain("\"HEALTHY\" => BadgeColor.Success");
        summary.ShouldContain("\"DEGRADED\" => BadgeColor.Warning");
        summary.ShouldContain("\"UNKNOWN\" => BadgeColor.Warning");
        summary.ShouldContain("\"FAILED\" => BadgeColor.Danger");
        summary.ShouldContain("_ => BadgeColor.Subtle");
    }

    [Fact]
    public void AppShouldRegisterTokenStylesheetAndDelegateProvidersToFrontComposerShell()
    {
        string app = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/App.razor");
        string layout = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Layout/MainLayout.razor");

        app.ShouldContain("css/chatbot.tokens.css");
        Regex.Matches(app, "<FluentProviders\\b", RegexOptions.CultureInvariant).Count.ShouldBe(0);
        Regex.Matches(app, "<Fluxor\\.Blazor\\.Web\\.StoreInitializer\\b", RegexOptions.CultureInvariant).Count.ShouldBe(0);
        Regex.Matches(layout, "<FluentProviders\\b", RegexOptions.CultureInvariant).Count.ShouldBe(0);
        Regex.Matches(layout, "<Fluxor\\.Blazor\\.Web\\.StoreInitializer\\b", RegexOptions.CultureInvariant).Count.ShouldBe(0);
    }

    [Fact]
    public void MainLayoutShouldUseFrontComposerShellAsTheSingleShellBoundary()
    {
        string layout = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Layout/MainLayout.razor");

        layout.ShouldContain("<FrontComposerShell");
        layout.ShouldContain("AppTitle=\"Hexalith ChatBot\"");
        layout.ShouldContain("ShowAccountMenu=\"false\"");
        layout.ShouldContain("@Body");
        layout.ShouldNotContain("chatbot-layout");
        layout.ShouldNotContain("chatbot-shell-header");
        layout.ShouldNotContain("chatbot-shell-main");
    }

    [Fact]
    public void ProgramShouldWireFrontComposerBootstrapBeforeDomainWithoutDirectEventStore()
    {
        string program = ReadProjectFile("src/Hexalith.ChatBot.UI/Program.cs");

        int fluent = program.IndexOf("AddFluentUIComponents", StringComparison.Ordinal);
        int quickstart = program.IndexOf("AddHexalithFrontComposerQuickstart", StringComparison.Ordinal);
        int domain = program.IndexOf("AddHexalithDomain<ChatBotUiFrontComposerMarker>", StringComparison.Ordinal);

        fluent.ShouldBeGreaterThanOrEqualTo(0);
        quickstart.ShouldBeGreaterThanOrEqualTo(0);
        quickstart.ShouldBeGreaterThan(fluent);
        domain.ShouldBeGreaterThan(quickstart);
        program.ShouldNotContain("AddHexalithEventStore", Case.Sensitive);
        program.ShouldNotContain("services:eventstore", Case.Sensitive);
        program.ShouldNotContain("AddFluxor", Case.Sensitive);
    }

    [Fact]
    public void GovernedOperationsShouldRenderVisibleExamplesForRequiredStatusKinds()
    {
        string page = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Pages/GovernedOperations.razor");

        foreach (string kind in RequiredFeedbackKinds)
        {
            page.ShouldContain($"ChatBotFeedbackKind.{kind}");
        }

        page.ShouldContain("<ChatBotStatusBanner");
        page.ShouldContain("StateFamily=\"@ChatBotFeedbackStateFamily.RetryableFailure\"");
        page.ShouldNotContain("<div class=\"chatbot-status\"");
    }

    private static string ReadProjectFile(string relativePath)
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Hexalith.ChatBot.slnx")))
        {
            directory = directory.Parent;
        }

        directory.ShouldNotBeNull();
        return File.ReadAllText(Path.Combine(directory.FullName, relativePath));
    }
}
