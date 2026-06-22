using System.Text.RegularExpressions;

using Shouldly;

namespace Hexalith.ChatBot.UI.Tests;

/// <summary>
/// Governance guard for the Epic 13 ChatBot UI FrontComposer layout-composition rule. Story 12.1
/// (<see cref="ChatBotFluentConformanceTests"/>) closed leaf-control drift but left page-level
/// composition out of scope: pages still hand-roll their chrome with <c>.chatbot-*</c> CSS that fights
/// the FrontComposer shell (the page-title band overlaps the shell top bar), wrap content in a bordered
/// <c>chatbot-page</c> box, and dump primary data as monospace <c>chatbot-definition-list</c> blocks.
/// This guard bans those hand-rolled patterns through per-pattern shrink-only allowlists and requires
/// every routable <c>@page</c> to compose through FrontComposer <c>FcPageLayout</c> + <c>FcPageHeader</c>
/// (with a shrink-only not-yet-composed backlog), so Stories 13.2-13.8 can burn the allowlists/backlog
/// down to empty and Epic 13 migration progress is build-gated and measurable.
/// <para>
/// Governance-only: this guard performs no page migration, no CSS deletion, and no component
/// substitution — those belong to Stories 13.2-13.9. It mirrors the regex + ratcheting-backlog style of
/// <see cref="ChatBotFluentConformanceTests"/> and <c>Hexalith.Tenants.UI</c>
/// <c>DomainUiFluentConformanceTests</c>. The seeded allowlists come from a 2026-06-22 source scan of
/// <c>src/Hexalith.ChatBot.UI/**/*.razor</c>; the scan is authoritative and supersedes the approximate
/// prose counts in the planning artifacts (chatbot-page 2→6 and chatbot-command-bar 3→4 are upward
/// reconciliations where the prose under-counted multi-class hits, not scope creep).
/// </para>
/// </summary>
[Trait("Category", "Governance")]
public sealed class ChatBotLayoutCompositionConformanceTests
{
    // The hand-rolled page-title <header class="chatbot-page-header"> band that renders inside the shell
    // @Body and overlaps the shell top bar. Token-bounded so other semantic <header> classes
    // (chatbot-project-context-header, chatbot-task-intent-review-panel__header, ...) are NOT matched.
    private static readonly Regex HandRolledPageHeaderClass = new(
        "(?<=[\"'\\s])chatbot-page-header(?=[\"'\\s])",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    // The hand-rolled bordered content-box wrapper. Matched as a WHOLE class token: chatbot-page is a
    // prefix of chatbot-page-header / chatbot-page-title, so a naive Contains over-matches and never goes
    // green. The quote/whitespace boundaries keep the prefixed tokens out (pinned by AC5 fixtures).
    private static readonly Regex HandRolledPageContentBoxClass = new(
        "(?<=[\"'\\s])chatbot-page(?=[\"'\\s])",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    // The hand-rolled command bar. It appears in both lowercase class="..." and Blazor Class="..."
    // attributes; the bespoke token only ever appears as a class value, so a token-bounded match covers
    // both attribute spellings without a separate case-insensitive attribute matcher.
    private static readonly Regex HandRolledCommandBarClass = new(
        "(?<=[\"'\\s])chatbot-command-bar(?=[\"'\\s])",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    // The monospace <dl class="chatbot-definition-list"> primary-data dump. Targets the class
    // specifically; bare semantic <dl>/<dt>/<dd> is a valid landmark and is NOT banned (Tenants keeps it
    // in its StructuralHtmlAllowlist).
    private static readonly Regex HandRolledDefinitionListClass = new(
        "(?<=[\"'\\s])chatbot-definition-list(?=[\"'\\s])",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    // Shrink-only allowlists seeded to EXACTLY today's source-scanned offenders (2026-06-22), as
    // forward-slash paths relative to src/Hexalith.ChatBot.UI. Stories 13.2-13.8 remove entries as they
    // migrate; the three ratchets below (missing-path, offender-outside-allowlist, stale-entry) guarantee
    // each list can only shrink toward empty.

    // Emptied by Story 13.2: every hand-rolled <header class="chatbot-page-header"> band was replaced by the
    // FrontComposer FcPageHeader primitive. The ban now applies to all .razor with no exceptions.
    private static readonly string[] PageHeaderChromeAllowlist = [];

    private static readonly string[] PageContentBoxAllowlist =
    [
        "Components/Governed/ChatBotProjectConversationWorkspace.razor",
        "Components/Pages/AssociationReview.razor",
        "Components/Pages/ComplianceAuditInvestigation.razor",
        "Components/Pages/GovernedOperations.razor",
        "Components/Pages/OperationalDashboards.razor",
        "Components/Pages/ProjectWorkspace.razor",
    ];

    // Emptied by Story 13.2: every chatbot-command-bar token was removed — page-level bars folded into the
    // FcPageHeader Actions slot, inner toolbars converted to FluentStack. The ban now applies with no exceptions.
    private static readonly string[] CommandBarAllowlist = [];

    // Shrunk by Story 13.4: the 23 conversation/governed/page surfaces that dumped primary data through
    // monospace <dl class="chatbot-definition-list"> were migrated to Fluent data presentation (FluentDataGrid
    // for repeated/queue data, structured FluentStack + FluentText for fixed key-value metadata). Story 13.6 then
    // migrated ComplianceAuditInvestigation.razor's audit-timeline metadata dump and removed its entry. The only
    // remaining entry is the dedicated-page surface owned by Story 13.5 (OperationalDashboards.razor health/queue
    // data-viz); Story 13.8 then verifies this list is empty.
    private static readonly string[] DefinitionListAllowlist =
    [
        "Components/Pages/OperationalDashboards.razor",
    ];

    // Emptied by Story 13.2: all 6 routable @page routes now compose FrontComposer FcPageLayout + FcPageHeader.
    // ProjectConversation composes by delegating to the ChatBotProjectConversationWorkspace aggregate-page wrapper
    // (which owns the route header) — see DelegatesToComposedWorkspace. Every @page must now compose, no backlog.
    private static readonly string[] NotYetComposedPageBacklog = [];

    [Fact]
    public void Pages_do_not_hand_roll_page_header_chrome_except_shrinking_allowlist()
        => AssertShrinkOnlyChromeBan(
            HandRolledPageHeaderClass,
            PageHeaderChromeAllowlist,
            "chatbot-page-header",
            "ChatBot pages must compose the route title through FrontComposer FcPageHeader (which renders "
            + "inside the shell's single content landmark) instead of a hand-rolled <header "
            + "class=\"chatbot-page-header\"> band that overlaps the shell top bar.");

    [Fact]
    public void Pages_do_not_hand_roll_content_box_wrapper_except_shrinking_allowlist()
        => AssertShrinkOnlyChromeBan(
            HandRolledPageContentBoxClass,
            PageContentBoxAllowlist,
            "chatbot-page",
            "ChatBot pages must compose content through FrontComposer FcPageLayout + Fluent primitives "
            + "instead of a hand-rolled <section class=\"chatbot-page\"> bordered content box.");

    [Fact]
    public void Pages_do_not_hand_roll_command_bar_except_shrinking_allowlist()
        => AssertShrinkOnlyChromeBan(
            HandRolledCommandBarClass,
            CommandBarAllowlist,
            "chatbot-command-bar",
            "ChatBot pages must express command bars through Fluent layout primitives instead of a "
            + "hand-rolled chatbot-command-bar class (in either class=\"...\" or Blazor Class=\"...\").");

    [Fact]
    public void Components_do_not_dump_primary_data_in_definition_lists_except_shrinking_allowlist()
        => AssertShrinkOnlyChromeBan(
            HandRolledDefinitionListClass,
            DefinitionListAllowlist,
            "chatbot-definition-list",
            "ChatBot components must render primary data through Fluent data components instead of "
            + "monospace <dl class=\"chatbot-definition-list\"> dumps (bare semantic <dl> is allowed).");

    [Fact]
    public void Route_pages_compose_frontcomposer_layout_and_header_except_not_yet_composed_backlog()
    {
        string uiRoot = UiRoot();
        Directory.Exists(uiRoot).ShouldBeTrue($"ChatBot UI source root not found: {uiRoot}");

        string[] razorFiles = EnumerateFiles(uiRoot, "*.razor");
        razorFiles.ShouldNotBeEmpty($"no .razor files found under {uiRoot}");

        AssertAllowlistPathsExist(uiRoot, NotYetComposedPageBacklog, "not-yet-composed page backlog");

        HashSet<string> backlog = new(NotYetComposedPageBacklog, StringComparer.Ordinal);
        List<string> offenders = [];
        List<string> staleBacklog = [];

        foreach (string file in razorFiles)
        {
            string content = File.ReadAllText(file);
            if (!content.Contains("@page", StringComparison.Ordinal))
            {
                continue;
            }

            string relative = RelativePath(uiRoot, file);
            bool composes = (ComposesFrontComposerLayout(content) && DeclaresFrontComposerHeader(content))
                || DelegatesToComposedWorkspace(content);

            if (backlog.Contains(relative))
            {
                if (composes)
                {
                    staleBacklog.Add(relative);
                }

                continue;
            }

            if (!composes)
            {
                offenders.Add(relative);
            }
        }

        offenders.ShouldBeEmpty(
            "Every routable @page in Hexalith.ChatBot.UI must compose through FrontComposer FcPageLayout + "
            + "FcPageHeader, unless it is listed in the shrink-only not-yet-composed backlog that Stories "
            + "13.2-13.8 burn down. Routes that neither compose nor are backlogged: "
            + string.Join("; ", offenders));

        staleBacklog.ShouldBeEmpty(
            "These backlogged routes now compose FcPageLayout + FcPageHeader; remove them from the "
            + "not-yet-composed backlog so it only shrinks toward empty: " + string.Join("; ", staleBacklog));
    }

    // ──────────────────────────────────────────────────────────────────────────────────────
    // Detector-fixture pins (AC5): crafted markup proves the regex logic so a future edit cannot
    // silently reopen a bypass (over-matching chatbot-page, under-matching the Blazor Class= command bar,
    // banning a bare <dl>, or flagging a non-page-header <header>).
    // ──────────────────────────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("<section class=\"chatbot-page\">", true)]
    [InlineData("<section class=\"chatbot-page chatbot-project-workspace\">", true)]
    [InlineData("<header class=\"chatbot-page-header\">", false)]
    [InlineData("<h1 class=\"chatbot-page-title\">", false)]
    public void Page_content_box_matcher_flags_whole_token_only(string markup, bool expected)
        => HandRolledPageContentBoxClass.IsMatch(markup).ShouldBe(expected);

    [Theory]
    [InlineData("<div class=\"chatbot-command-bar\">", true)]
    [InlineData("<FluentStack Class=\"chatbot-command-bar chatbot-association-actions__bar\">", true)]
    [InlineData("<div class=\"chatbot-command-bar-extra\">", false)]
    public void Command_bar_matcher_covers_lowercase_and_blazor_class_attributes(string markup, bool expected)
        => HandRolledCommandBarClass.IsMatch(markup).ShouldBe(expected);

    [Theory]
    [InlineData("<dl class=\"chatbot-definition-list\">", true)]
    [InlineData("<dl class=\"chatbot-definition-list chatbot-labelled-row-list\">", true)]
    [InlineData("<dl>", false)]
    [InlineData("<dl class=\"chatbot-labelled-row-list\">", false)]
    public void Definition_list_matcher_targets_class_not_bare_dl(string markup, bool expected)
        => HandRolledDefinitionListClass.IsMatch(markup).ShouldBe(expected);

    [Theory]
    [InlineData("<header class=\"chatbot-page-header\">", true)]
    [InlineData("<header class=\"chatbot-project-context-header\">", false)]
    [InlineData("<header class=\"chatbot-task-intent-review-panel__header\">", false)]
    [InlineData("<header class=\"chatbot-why-project-panel__header\">", false)]
    public void Page_header_matcher_flags_chatbot_page_header_not_other_headers(string markup, bool expected)
        => HandRolledPageHeaderClass.IsMatch(markup).ShouldBe(expected);

    private static void AssertShrinkOnlyChromeBan(
        Regex detector,
        IReadOnlyList<string> allowlist,
        string patternLabel,
        string banGuidance)
    {
        string uiRoot = UiRoot();
        Directory.Exists(uiRoot).ShouldBeTrue($"ChatBot UI source root not found: {uiRoot}");

        string[] razorFiles = EnumerateFiles(uiRoot, "*.razor");
        razorFiles.ShouldNotBeEmpty($"no .razor files found under {uiRoot}");

        AssertAllowlistPathsExist(uiRoot, allowlist, $"{patternLabel} allowlist");

        HashSet<string> allowed = new(allowlist, StringComparer.Ordinal);
        List<string> offenders = [];
        List<string> activeAllowlist = [];

        foreach (string file in razorFiles)
        {
            if (!detector.IsMatch(File.ReadAllText(file)))
            {
                continue;
            }

            string relative = RelativePath(uiRoot, file);
            if (allowed.Contains(relative))
            {
                activeAllowlist.Add(relative);
                continue;
            }

            offenders.Add(relative);
        }

        offenders.ShouldBeEmpty(
            banGuidance + $" Offenders outside the shrink-only allowlist: {string.Join("; ", offenders)}");

        string[] staleEntries = allowlist.Except(activeAllowlist, StringComparer.Ordinal).ToArray();
        staleEntries.ShouldBeEmpty(
            $"These {patternLabel} allowlist entries no longer contain the banned pattern; remove them so the "
            + $"allowlist only shrinks toward empty: {string.Join("; ", staleEntries)}");
    }

    private static void AssertAllowlistPathsExist(
        string uiRoot,
        IReadOnlyList<string> allowlist,
        string listLabel)
    {
        List<string> missing = [];
        foreach (string entry in allowlist)
        {
            string path = Path.Combine(uiRoot, entry.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(path))
            {
                missing.Add(entry);
            }
        }

        missing.ShouldBeEmpty(
            $"{listLabel} paths must exist so renamed/deleted files cannot silently keep the guard green. "
            + $"Missing entries: {string.Join("; ", missing)}");
    }

    private static bool ComposesFrontComposerLayout(string content)
        => content.Contains("<FcPageLayout", StringComparison.Ordinal);

    private static bool DeclaresFrontComposerHeader(string content)
        => content.Contains("<FcPageHeader", StringComparison.Ordinal);

    // Delegation-aware require-compose (anticipated by Story 13.1's notes, mirroring the Tenants pattern where
    // DeclaresFrontComposerHeader accepts aggregate-page wrappers): a thin @page that renders the shared
    // ChatBotProjectConversationWorkspace delegates its route chrome to that wrapper, which itself composes
    // FcPageLayout + FcPageHeader. Such a delegating @page therefore counts as composing.
    private static bool DelegatesToComposedWorkspace(string content)
        => content.Contains("<ChatBotProjectConversationWorkspace", StringComparison.Ordinal);

    private static string UiRoot()
        => Path.Combine(RepositoryRoot(), "src", "Hexalith.ChatBot.UI");

    private static string[] EnumerateFiles(string root, string searchPattern)
    {
        EnumerationOptions options = new()
        {
            RecurseSubdirectories = true,
            AttributesToSkip = FileAttributes.ReparsePoint | FileAttributes.Hidden,
            IgnoreInaccessible = true,
        };

        return Directory
            .EnumerateFiles(root, searchPattern, options)
            .Where(static file => !IsBuildOutput(file))
            .ToArray();
    }

    private static bool IsBuildOutput(string file)
    {
        string normalized = file.Replace('\\', '/');
        return normalized.Contains("/bin/", StringComparison.Ordinal)
            || normalized.Contains("/obj/", StringComparison.Ordinal);
    }

    private static string RelativePath(string root, string file) =>
        Path.GetRelativePath(root, file).Replace('\\', '/');

    private static string RepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Hexalith.ChatBot.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Could not locate repository root.");
    }
}
