using System.Text.RegularExpressions;

using Hexalith.ChatBot.UI.Design;

using Shouldly;

namespace Hexalith.ChatBot.UI.Tests;

/// <summary>
/// Guards the ChatBot visual foundation: semantic slots are a thin alias layer over Fluent/FrontComposer
/// tokens, not a product-specific palette or second component system.
/// </summary>
public sealed class ChatBotSemanticTokenContractTests
{
    private static readonly string[] RequiredSlots = ["neutral", "brand", "info", "warning", "danger", "success"];

    private static readonly Dictionary<string, (string Background, string Foreground)> ExpectedSemanticMappings =
        new(StringComparer.Ordinal)
        {
            ["neutral"] = ("--colorNeutralBackground1", "--colorNeutralForeground1"),
            ["brand"] = ("--colorBrandBackground", "--colorNeutralForegroundOnBrand"),
            ["info"] = ("--colorStatusInformationBackground1", "--colorStatusInformationForeground1"),
            ["warning"] = ("--colorStatusWarningBackground1", "--colorStatusWarningForeground1"),
            ["danger"] = ("--colorStatusDangerBackground1", "--colorStatusDangerForeground1"),
            ["success"] = ("--colorStatusSuccessBackground1", "--colorStatusSuccessForeground1"),
        };

    [Fact]
    public void SemanticContractShouldDeclareTheExactSlotSetAndMeanings()
    {
        ChatBotSemanticTokenContract.Slots.Select(static slot => slot.Name).ShouldBe(RequiredSlots, ignoreOrder: false);

        ChatBotSemanticTokenContract.GetSlot("neutral").Meaning.ShouldContain("workspace", Case.Insensitive);
        ChatBotSemanticTokenContract.GetSlot("brand").Meaning.ShouldContain("primary actions", Case.Insensitive);
        ChatBotSemanticTokenContract.GetSlot("info").Meaning.ShouldContain("evidence", Case.Insensitive);
        ChatBotSemanticTokenContract.GetSlot("warning").Meaning.ShouldContain("manual review", Case.Insensitive);
        ChatBotSemanticTokenContract.GetSlot("danger").Meaning.ShouldContain("terminal", Case.Insensitive);
        ChatBotSemanticTokenContract.GetSlot("success").Meaning.ShouldContain("projection-complete", Case.Insensitive);
    }

    [Fact]
    public void StylesheetShouldMapSemanticColorsOnlyToFluentOrFrontComposerVariables()
    {
        string css = ReadProjectFile("src/Hexalith.ChatBot.UI/wwwroot/css/chatbot.tokens.css");

        foreach (ChatBotSemanticToken slot in ChatBotSemanticTokenContract.Slots)
        {
            (string expectedBackground, string expectedForeground) = ExpectedSemanticMappings[slot.Name];
            CssVariable(css, $"--chatbot-color-{slot.Name}-background").ShouldBe($"var({expectedBackground})");
            CssVariable(css, $"--chatbot-color-{slot.Name}-foreground").ShouldBe($"var({expectedForeground})");
        }

        css.ShouldContain("--colorStatusInformationBackground1");
        css.ShouldContain("--colorStatusInformationForeground1");
        css.ShouldNotContain("--colorStatusInfoForeground1");
        css.ShouldNotContain("Temporary inheritance bridge", Case.Sensitive);
        css.ShouldNotContain("until the runtime", Case.Sensitive);

        MatchCollection colorAliasAssignments = Regex.Matches(
            css,
            @"^\s*--chatbot-color-[^:]+:\s*(?<value>[^;]+);",
            RegexOptions.CultureInvariant | RegexOptions.Multiline);
        colorAliasAssignments.Count.ShouldBeGreaterThanOrEqualTo(RequiredSlots.Length * 2);

        foreach (Match assignment in colorAliasAssignments)
        {
            string value = assignment.Groups["value"].Value;
            value.ShouldContain("var(--");
            value.ShouldNotContain("#");
            value.ShouldNotContain("rgb(", Case.Insensitive);
            value.ShouldNotContain("hsl(", Case.Insensitive);
        }
    }

    [Fact]
    public void StylesheetShouldNotDeclareChatBotPrimitiveAliasesOrPaletteValues()
    {
        string css = ReadProjectFile("src/Hexalith.ChatBot.UI/wwwroot/css/chatbot.tokens.css");

        foreach (string forbiddenAliasPrefix in new[]
        {
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
    public void StylesheetShouldContainForcedColorsAndNonColorStatusCues()
    {
        string css = ReadProjectFile("src/Hexalith.ChatBot.UI/wwwroot/css/chatbot.tokens.css");

        css.ShouldContain("@media (forced-colors: active)");
        css.ShouldContain("CanvasText");
        css.ShouldContain("Highlight");
        css.ShouldContain(".chatbot-status__label");
        css.ShouldContain(".chatbot-conversation-status-summary");
        css.ShouldContain(".chatbot-conversation-status-summary__health");
        css.ShouldContain("data-chatbot-health=\"failed\"");
        css.ShouldContain("border-inline-start");
        css.ShouldContain("border:");
        css.ShouldContain("outline:");
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
    public void GovernedOperationsShouldRenderVisibleExamplesForRequiredStatusSlots()
    {
        string page = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Pages/GovernedOperations.razor");

        foreach (string slot in RequiredSlots.Where(static slot => slot is "info" or "warning" or "danger" or "success"))
        {
            page.ShouldContain($"ChatBotFeedbackKind.{slot[..1].ToUpperInvariant()}{slot[1..]}");
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

    private static string CssVariable(string css, string alias)
    {
        Match match = Regex.Match(
            css,
            $@"^\s*{Regex.Escape(alias)}:\s*(?<value>[^;]+);",
            RegexOptions.CultureInvariant | RegexOptions.Multiline);

        match.Success.ShouldBeTrue($"CSS variable {alias} should be declared exactly once.");
        return match.Groups["value"].Value.Trim();
    }
}
