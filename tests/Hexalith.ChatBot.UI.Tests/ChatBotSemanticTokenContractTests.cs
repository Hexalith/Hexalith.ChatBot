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

    private static readonly Dictionary<string, string> ExpectedSpacingAndRadiusAliases =
        new(StringComparer.Ordinal)
        {
            ["--chatbot-space-1"] = "4px",
            ["--chatbot-space-2"] = "8px",
            ["--chatbot-space-3"] = "12px",
            ["--chatbot-space-4"] = "16px",
            ["--chatbot-space-6"] = "24px",
            ["--chatbot-density-compact"] = "8px",
            ["--chatbot-density-comfortable"] = "12px",
            ["--chatbot-panel-gap"] = "16px",
            ["--chatbot-row-gap"] = "8px",
            ["--chatbot-radius-sm"] = "4px",
            ["--chatbot-radius-md"] = "8px",
            ["--chatbot-radius-lg"] = "12px",
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
    public void StylesheetShouldDeclareDesignSpacingRadiusAndTypographyAliases()
    {
        string css = ReadProjectFile("src/Hexalith.ChatBot.UI/wwwroot/css/chatbot.tokens.css");

        foreach ((string alias, string expectedValue) in ExpectedSpacingAndRadiusAliases)
        {
            CssVariable(css, alias).ShouldBe(expectedValue);
        }

        string[] typographyAliases =
        [
            "--chatbot-font-page-title",
            "--chatbot-font-section-title",
            "--chatbot-font-body",
            "--chatbot-font-metadata",
            "--chatbot-font-code",
            "--chatbot-type-page-title-size",
            "--chatbot-type-section-title-size",
            "--chatbot-type-body-size",
            "--chatbot-type-metadata-size",
            "--chatbot-type-code-size",
        ];

        foreach (string alias in typographyAliases)
        {
            CssVariable(css, alias).ShouldNotBeNullOrWhiteSpace();
        }

        css.ShouldContain("font-size: var(--chatbot-type-page-title-size);");
        css.ShouldContain("font-size: var(--chatbot-type-section-title-size);");
        css.ShouldContain("font-size: var(--chatbot-type-body-size);");
        css.ShouldContain("font-size: var(--chatbot-type-metadata-size);");
        css.ShouldContain("font-size: var(--chatbot-type-code-size);");
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
    public void AppShouldRegisterTokenStylesheetAndOneFluentProviderSet()
    {
        string app = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/App.razor");

        app.ShouldContain("css/chatbot.tokens.css");
        Regex.Matches(app, "<FluentProviders\\b", RegexOptions.CultureInvariant).Count.ShouldBe(1);
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
