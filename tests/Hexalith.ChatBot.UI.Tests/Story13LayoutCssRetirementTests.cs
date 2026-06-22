using System.Text.RegularExpressions;

using Shouldly;

namespace Hexalith.ChatBot.UI.Tests;

/// <summary>
/// Story 13.8 CSS-side retirement ratchet. Stories 13.2-13.7 drove every routable page off the hand-rolled
/// <c>.chatbot-*</c> page/shell chrome (emptying the five <see cref="ChatBotLayoutCompositionConformanceTests"/>
/// allowlists), which left their CSS rules in <c>chatbot.tokens.css</c> dead. Story 13.8 deletes those rules;
/// this guard is the build-enforced counterpart that asserts the 10 retired selectors stay gone and cannot be
/// silently reintroduced — mirroring the razor-side conformance guard's selector-boundary regex style.
/// <para>
/// The FrontComposer shell (<c>fc-skip-link</c> → <c>#fc-main-content</c>) plus <c>FcPageLayout</c>/
/// <c>FcPageHeader</c> and Fluent primitives now own the page-header band, the bordered content box, the
/// command bar, the monospace definition-list, the skip-link, and the Epic-10 shell wrappers. What remains in
/// <c>chatbot.tokens.css</c> is only the layout/accessibility CSS the design system does not own.
/// </para>
/// <para>
/// Each retired token is matched as a SELECTOR token with a trailing word boundary so a live prefix neighbor is
/// never a false positive — <c>.chatbot-page</c> must not catch <c>.chatbot-section</c> or <c>.chatbot-page-header</c>,
/// and <c>.chatbot-page-title</c> must not catch the live <c>.chatbot-section-title</c>. The scan is non-vacuous:
/// it asserts the file is found and still carries its live anchors before asserting dead-selector absence.
/// Source-scan based (the ChatBot UI has no bUnit — see memory <c>chatbot-ui-no-bunit-test-strategy</c>); the
/// real-render visual gate for these surfaces is Story 13.9.
/// </para>
/// </summary>
[Trait("Category", "Governance")]
public sealed class Story13LayoutCssRetirementTests
{
    // The 10 hand-rolled page/shell layout selectors Stories 13.2-13.7 retired from the razor markup and
    // Story 13.8 deletes from chatbot.tokens.css: 5 epic-AC-named (page-header band, bordered page box,
    // command bar, monospace definition list, custom skip-link) + 5 coupled dead chrome (the page title inside
    // the deleted header band, and the Epic-10 layout/shell wrappers the FrontComposerShell replaced).
    private static readonly string[] RetiredSelectors =
    [
        "chatbot-page-header",
        "chatbot-page",
        "chatbot-command-bar",
        "chatbot-definition-list",
        "chatbot-skip-link",
        "chatbot-page-title",
        "chatbot-layout",
        "chatbot-shell-header",
        "chatbot-shell-main",
        "chatbot-dense-row",
    ];

    // Live anchors that MUST remain so (a) the scan is non-vacuous and (b) the deletion did not collapse a
    // shared selector list, a forced-colors block, or a semantic alias. .chatbot-section/.chatbot-section-title
    // are the 13.7 accordion bodies/headings that share the grid/min-width/margin lists with the deleted tokens.
    private static readonly string[] RequiredLiveAnchors =
    [
        "--chatbot-color-neutral-background",
        ".chatbot-status__label",
        "@media (forced-colors: active)",
        ".chatbot-section,",
        ".chatbot-section-title,",
        "CanvasText",
        "Highlight",
    ];

    [Fact]
    public void StylesheetShouldNotContainRetiredLayoutChromeSelectors()
    {
        string css = ReadCss();

        // Non-vacuous: the file was found, is non-trivial, and still carries its live anchors — so a future
        // edit that empties/moves/renames the stylesheet cannot make this guard pass by accident.
        css.Length.ShouldBeGreaterThan(
            1000,
            "chatbot.tokens.css was not found or is unexpectedly small — the retirement scan would be vacuous.");

        foreach (string anchor in RequiredLiveAnchors)
        {
            css.ShouldContain(
                anchor,
                Case.Sensitive,
                $"live anchor '{anchor}' is missing — the retirement scan would be vacuous or a live rule "
                + "(shared selector list / forced-colors block / semantic alias) was deleted by mistake.");
        }

        List<string> survivors = RetiredSelectors
            .Where(selector => RetiredSelectorMatcher(selector).IsMatch(css))
            .ToList();

        survivors.ShouldBeEmpty(
            "Story 13.8 retired these hand-rolled .chatbot-* page/shell layout selectors from "
            + "chatbot.tokens.css (the FrontComposer shell + FcPageLayout/FcPageHeader + Fluent primitives now "
            + "own these affordances); they must not reappear: " + string.Join("; ", survivors));
    }

    // ──────────────────────────────────────────────────────────────────────────────────────
    // Detector-fixture pins: prove the selector-boundary logic so a future edit cannot silently reopen a
    // bypass (matching a live prefix neighbor, or under-matching the bare dead token in any of its spellings).
    // ──────────────────────────────────────────────────────────────────────────────────────
    [Theory]
    [InlineData("chatbot-page", ".chatbot-page {\n    max-width: min(100%, 70rem);\n}", true)]
    [InlineData("chatbot-page", ".chatbot-page,\n.chatbot-conversation-shell {", true)]
    [InlineData("chatbot-page", ".chatbot-section {\n    display: grid;\n}", false)]
    [InlineData("chatbot-page", ".chatbot-page-header {\n    display: grid;\n}", false)]
    [InlineData("chatbot-page-header", ".chatbot-page-header,\n.chatbot-section,", true)]
    [InlineData("chatbot-page-title", ".chatbot-page-title,\n.chatbot-section-title,", true)]
    [InlineData("chatbot-page-title", ".chatbot-section-title {\n    margin: 0;\n}", false)]
    [InlineData("chatbot-skip-link", ".chatbot-skip-link:focus,\n.chatbot-skip-link:focus-visible {", true)]
    [InlineData("chatbot-definition-list", ".chatbot-definition-list dd {\n    margin: 0;\n}", true)]
    [InlineData("chatbot-dense-row", ".chatbot-governed-action__reason,\n    .chatbot-dense-row,", true)]
    [InlineData("chatbot-shell-main", ".chatbot-conversation-shell__main:focus-within,", false)]
    public void Retired_selector_matcher_flags_dead_token_not_live_prefix_neighbor(
        string selector,
        string css,
        bool expected)
        => RetiredSelectorMatcher(selector).IsMatch(css).ShouldBe(expected);

    // A retired class as a CSS selector token: a literal '.' then the class, with a trailing boundary that
    // rejects a longer class continuing in [a-z0-9-] (so .chatbot-page does not match .chatbot-page-header,
    // and .chatbot-page-title does not match the live .chatbot-section-title). A pseudo/attribute/combinator
    // following the token (':', ' ', ',', '{') is not in the boundary set, so :focus / dd / , / { still match.
    private static Regex RetiredSelectorMatcher(string selector) => new(
        $@"\.{Regex.Escape(selector)}(?![a-z0-9-])",
        RegexOptions.CultureInvariant);

    private static string ReadCss()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Hexalith.ChatBot.slnx")))
        {
            directory = directory.Parent;
        }

        directory.ShouldNotBeNull("could not locate repository root (Hexalith.ChatBot.slnx).");
        string path = Path.Combine(directory.FullName, "src/Hexalith.ChatBot.UI/wwwroot/css/chatbot.tokens.css");
        File.Exists(path).ShouldBeTrue($"chatbot.tokens.css not found at {path}");
        return File.ReadAllText(path);
    }
}
