using Shouldly;

using static Hexalith.ChatBot.UI.Tests.ComponentContractSource;

namespace Hexalith.ChatBot.UI.Tests;

public sealed class AssociationReviewCssContractTests
{
    [Fact]
    public void AssociationRulesShouldRetainTheirResponsiveAndAccessibilityDeclarations()
    {
        AssertAssociationRules(ReadProjectFile("src/Hexalith.ChatBot.UI/wwwroot/css/chatbot.tokens.css"));
        ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Governed/ChatBotAssociationReviewActions.razor")
            .ShouldContain("<ChatBotGovernedAction");
        ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Governed/ChatBotGovernedAction.razor")
            .ShouldContain("chatbot-governed-action__reason");
    }

    [Theory]
    [InlineData(".chatbot-association-candidate__evidence")]
    [InlineData(".chatbot-association-action-wrap")]
    [InlineData(".chatbot-association-candidate,")]
    [InlineData(".chatbot-association-comparison__panel,")]
    [InlineData(".chatbot-governed-action__reason:focus-visible")]
    public void RemovingAnAssociationSelectorShouldFailEvenWhileMediaBlocksRemain(string selector)
    {
        string css = ReadProjectFile("src/Hexalith.ChatBot.UI/wwwroot/css/chatbot.tokens.css");
        string mutated = css.Replace(selector, ".unrelated-selector", StringComparison.Ordinal);
        Should.Throw<ShouldAssertException>(() => AssertAssociationRules(mutated));
    }

    [Theory]
    [InlineData("width: 100%", "min-width: 100%")]
    [InlineData("transition-delay: 0s !important", "transition-delay: 5s !important")]
    public void ReplacingAnExactAccessibilityDeclarationShouldFail(string original, string replacement)
    {
        string css = ReadProjectFile("src/Hexalith.ChatBot.UI/wwwroot/css/chatbot.tokens.css");
        Should.Throw<ShouldAssertException>(() => AssertAssociationRules(css.Replace(original, replacement, StringComparison.Ordinal)));
    }

    [Theory]
    [InlineData("@media (max-width: 48rem) { .chatbot-association-action-wrap { width: auto; } }")]
    [InlineData("@media (prefers-reduced-motion: reduce) { .chatbot-association-candidate { animation: spin 1s !important; } }")]
    public void LaterOverridesOfTheSameSelectorShouldFail(string rule)
    {
        string css = ReadProjectFile("src/Hexalith.ChatBot.UI/wwwroot/css/chatbot.tokens.css");
        Should.Throw<ShouldAssertException>(() => AssertAssociationRules(css + rule));
    }

    [Fact]
    public void ProductStylesShouldUseThemeColors()
        => CssContract.LiteralColors(ReadProjectFile("src/Hexalith.ChatBot.UI/wwwroot/css/chatbot.tokens.css"))
            .ShouldBeEmpty();

    [Theory]
    [InlineData("#abc")]
    [InlineData("#abcd")]
    [InlineData("#AABBCC")]
    [InlineData("#AABBCCDD")]
    [InlineData("rgb(1 2 3)")]
    [InlineData("rgba(1, 2, 3, .5)")]
    [InlineData("hsl(0 0% 50%)")]
    [InlineData("hsla(0, 0%, 50%, .5)")]
    [InlineData("hwb(0 0% 0%)")]
    [InlineData("lab(50% 0 0)")]
    [InlineData("lch(50% 20 0)")]
    [InlineData("oklab(50% 0 0)")]
    [InlineData("oklch(50% 0 0)")]
    [InlineData("color(display-p3 1 0 0)")]
    [InlineData("color-mix(in srgb, var(--colorNeutralStroke1), white)")]
    [InlineData("light-dark(white, black)")]
    [InlineData("ReD")]
    [InlineData("rebeccapurple")]
    [InlineData("var(--colorNeutralStroke1, red)")]
    [InlineData("1px solid blue")]
    public void LiteralColorDeclarationsShouldBeDetected(string value)
        => CssContract.LiteralColors($".subject {{ border-color: {value}; }}").ShouldNotBeEmpty();

    [Theory]
    [InlineData("/* #abc rgba(1,2,3,.5) red */ #abc { color: var(--colorNeutralForeground1); }")]
    [InlineData("#red { filter: url(#abc); }")]
    [InlineData(".subject { background-image: url(\"#AABBCC\"); }")]
    [InlineData(".subject { content: '#abc red rgb('; }")]
    [InlineData(".subject { animation-name: red; font-family: tan; grid-area: blue; }")]
    [InlineData(".subject { color: color-mix(in srgb, var(--colorNeutralForeground1), var(--colorNeutralForeground2)); }")]
    [InlineData(".subject { color: light-dark(var(--colorNeutralForeground1), var(--colorNeutralForeground2)); }")]
    [InlineData(".subject { outline: 2px solid Highlight; color: CanvasText; background: transparent; border-color: currentColor; }")]
    public void NonColorHashesAndAdaptiveKeywordsShouldBeAllowed(string css)
        => CssContract.LiteralColors(css).ShouldBeEmpty();

    private static void AssertAssociationRules(string css)
    {
        CssContract.Value(css, "(max-width: 48rem)", ".chatbot-association-candidate__evidence", "grid-column")
            .ShouldBe("1 / -1");
        CssContract.Value(css, "(max-width: 48rem)", ".chatbot-association-action-wrap", "max-inline-size").ShouldBe("none");
        CssContract.Value(css, "(max-width: 48rem)", ".chatbot-association-action-wrap", "width").ShouldBe("100%");
        foreach (string selector in new[] { ".chatbot-association-candidate", ".chatbot-association-comparison__panel" })
        {
            CssContract.Value(css, "(prefers-reduced-motion: reduce)", selector, "animation").ShouldBe("none !important");
            CssContract.Value(css, "(prefers-reduced-motion: reduce)", selector, "transition-duration").ShouldBe("0.01ms !important");
            CssContract.Value(css, "(prefers-reduced-motion: reduce)", selector, "transition-delay").ShouldBe("0s !important");
            CssContract.Value(css, "(prefers-reduced-motion: reduce)", selector, "scroll-behavior").ShouldBe("auto !important");
        }

        CssContract.Value(css, "(forced-colors: active)", ".chatbot-governed-action__reason:focus-visible", "outline")
            .ShouldBe("2px solid Highlight");
        CssContract.Value(css, "(forced-colors: active)", ".chatbot-governed-action__reason:focus-visible", "outline-offset")
            .ShouldBe("2px");
    }
}
