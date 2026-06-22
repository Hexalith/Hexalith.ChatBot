using System.Text.RegularExpressions;

using Shouldly;

namespace Hexalith.ChatBot.UI.Tests;

/// <summary>
/// QA automation for Story 13.4 (migrate definition-list data dumps to Fluent data presentation).
/// <para>
/// The Story 13.1 guard (<see cref="ChatBotLayoutCompositionConformanceTests"/>) only bans the
/// <c>chatbot-definition-list</c> CSS class token, which — as the story's own regression traps warn — can be
/// "gamed" by deleting just the class while keeping the underlying <c>&lt;dl&gt;</c>/<c>&lt;dt&gt;</c>/<c>&lt;dd&gt;</c>
/// monospace dump. This suite is the positive counterpart: it proves the 23 owned surfaces actually render
/// primary data through Fluent data components (structured <c>FluentStack</c> + <c>FluentText</c>/<c>&lt;code&gt;</c>
/// rows), that no definition-list element markup survives, that the allowlist end-state is exactly the
/// page-owned file left for Story 13.5 (after Story 13.6 migrated the compliance-audit page), and that the AC3/AC4 invariants (monospace dropped for
/// non-code values but kept for opaque tokens; aria/data/<c>&lt;time&gt;</c> machine attributes preserved) hold.
/// </para>
/// <para>
/// Source-scan based (the ChatBot UI has no bUnit — see memory <c>chatbot-ui-no-bunit-test-strategy</c>); the
/// real-render screenshot gate for these surfaces is Story 13.9. Lives in the build-gated Governance lane so a
/// future regression that reintroduces a dump (or merely strips the class) fails the build.
/// </para>
/// </summary>
[Trait("Category", "Governance")]
public sealed class Story13DefinitionListMigrationTests
{
    // The 23 surfaces Story 13.4 migrated (the 25 DefinitionListAllowlist seed minus the two page-owned files
    // OperationalDashboards.razor [Story 13.5] and ComplianceAuditInvestigation.razor [Story 13.6]).
    // Forward-slash, relative to src/Hexalith.ChatBot.UI.
    private static readonly string[] MigratedSurfaces =
    [
        "Components/Governed/ChatBotAiActionPreviewSections.razor",
        "Components/Governed/ChatBotAiOutcomeConversationItem.razor",
        "Components/Governed/ChatBotApprovalConversationItem.razor",
        "Components/Governed/ChatBotApprovalQueuePriorityView.razor",
        "Components/Governed/ChatBotAssociationEvidenceComparison.razor",
        "Components/Governed/ChatBotAssociationReviewActions.razor",
        "Components/Governed/ChatBotAttachmentConversationItem.razor",
        "Components/Governed/ChatBotConversationItemClassificationBadge.razor",
        "Components/Governed/ChatBotConversationItemReviewHistory.razor",
        "Components/Governed/ChatBotConversationItemStatusSummary.razor",
        "Components/Governed/ChatBotDecisionConversationItem.razor",
        "Components/Governed/ChatBotEmailConversationItem.razor",
        "Components/Governed/ChatBotEscalationPolicyEditor.razor",
        "Components/Governed/ChatBotFailureStateConversationItem.razor",
        "Components/Governed/ChatBotNotificationRoutingEditor.razor",
        "Components/Governed/ChatBotParticipantConversationItem.razor",
        "Components/Governed/ChatBotProjectConversationWorkspace.razor",
        "Components/Governed/ChatBotTaskIntentReviewPanel.razor",
        "Components/Governed/ChatBotTenantPolicyEditor.razor",
        "Components/Governed/ChatBotWhyProjectPanel.razor",
        "Components/Pages/AssociationReview.razor",
        "Components/Pages/GovernedOperations.razor",
        "Components/Pages/ProjectWorkspace.razor",
    ];

    // The dedicated-page surface still on the allowlist. Story 13.6 migrated ComplianceAuditInvestigation.razor's
    // audit-timeline dump, leaving only OperationalDashboards.razor (Story 13.5 owns it; Story 13.8 then empties it).
    private static readonly string[] PageOwnedDefinitionListSurfaces =
    [
        "Components/Pages/OperationalDashboards.razor",
    ];

    // Opening tags <dl/<dt/<dd as real elements (followed by whitespace, '/', or '>'). Closing tags </dl> begin
    // with '/' so they are not matched — absence of opening tags is sufficient for the dump-removal contract.
    private static readonly Regex DefinitionListElement = new(
        "<d[ltd](?=[\\s/>])",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex RazorComment = new(
        "@\\*.*?\\*@",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Singleline);

    private static readonly Regex HtmlComment = new(
        "<!--.*?-->",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Singleline);

    // Full-line C# // comments only (the @code blocks reference "the former <dl>/<dt>/<dd>"); stripping these
    // keeps the element scan from false-positiving on documentation that names the markup being removed.
    private static readonly Regex FullLineComment = new(
        "^[ \\t]*//.*$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline);

    // A <time> element that still carries the chatbot-code monospace class (AC3: timestamps drop monospace).
    private static readonly Regex MonospaceTimeElement = new(
        "<time[^>]*chatbot-code",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    [Fact]
    public void Migrated_surfaces_render_data_through_fluent_components_and_drop_definition_list_markup()
    {
        string uiRoot = UiRoot();
        AssertSurfacesExist(uiRoot, MigratedSurfaces);

        List<string> missingFluent = [];
        List<string> residualClass = [];
        List<string> residualMarkup = [];

        foreach (string surface in MigratedSurfaces)
        {
            string content = File.ReadAllText(Resolve(uiRoot, surface));

            // Positive: data is composed through a structured FluentStack (AC1). Single-column opaque lists may
            // render FluentStack-of-<code> rows; key-value blocks render FluentStack + FluentText — both qualify.
            if (!content.Contains("FluentStack", StringComparison.Ordinal))
            {
                missingFluent.Add(surface);
            }

            // Negative: the banned monospace class must be gone from every owned file (file-level allowlist exit).
            if (content.Contains("chatbot-definition-list", StringComparison.Ordinal))
            {
                residualClass.Add(surface);
            }

            // Anti-gaming: no <dl>/<dt>/<dd> element markup may remain (comment references to the former markup
            // are stripped first), so the file cannot "pass" the class ban while keeping the underlying dump.
            if (DefinitionListElement.IsMatch(StripComments(content)))
            {
                residualMarkup.Add(surface);
            }
        }

        missingFluent.ShouldBeEmpty(
            "Story 13.4 surfaces must render primary data through Fluent data components (structured FluentStack "
            + "+ FluentText/<code> rows). Files with no FluentStack: " + string.Join("; ", missingFluent));

        residualClass.ShouldBeEmpty(
            "Story 13.4 surfaces must not retain the monospace chatbot-definition-list class. Offenders: "
            + string.Join("; ", residualClass));

        residualMarkup.ShouldBeEmpty(
            "Story 13.4 surfaces must not keep <dl>/<dt>/<dd> dump markup (deleting only the class while keeping "
            + "the definition list is the documented gaming vector). Offenders: " + string.Join("; ", residualMarkup));
    }

    [Fact]
    public void Definition_list_class_end_state_is_exactly_the_remaining_page_owned_surfaces()
    {
        string uiRoot = UiRoot();
        string[] razorFiles = EnumerateRazor(uiRoot);
        razorFiles.ShouldNotBeEmpty($"no .razor files found under {uiRoot}");

        string[] remaining = razorFiles
            .Where(file => File.ReadAllText(file).Contains("chatbot-definition-list", StringComparison.Ordinal))
            .Select(file => RelativePath(uiRoot, file))
            .OrderBy(static path => path, StringComparer.Ordinal)
            .ToArray();

        remaining.ShouldBe(
            PageOwnedDefinitionListSurfaces.OrderBy(static path => path, StringComparer.Ordinal).ToArray(),
            "After Stories 13.4 and 13.6 only the remaining page-owned surface (Story 13.5 "
            + "OperationalDashboards.razor) may still use chatbot-definition-list; the migrated surfaces "
            + "must stay migrated and none may be re-introduced.");
    }

    [Fact]
    public void Migration_preserves_accessibility_data_and_time_machine_attributes()
    {
        string uiRoot = UiRoot();

        // AC4: the data-* markers asserted by the static E2E fixtures survive the TenantPolicyEditor migration.
        string tenantPolicy = ReadSurface(uiRoot, "Components/Governed/ChatBotTenantPolicyEditor.razor");
        tenantPolicy.ShouldContain("data-mailbox-status-row", Case.Sensitive);
        tenantPolicy.ShouldContain("data-mailbox-action-row", Case.Sensitive);

        // AC4: the queue/editor containers keep their localized aria-label (moved off the former <dl>).
        foreach (string surface in new[]
        {
            "Components/Governed/ChatBotApprovalQueuePriorityView.razor",
            "Components/Governed/ChatBotEscalationPolicyEditor.razor",
            "Components/Governed/ChatBotNotificationRoutingEditor.razor",
            "Components/Governed/ChatBotTenantPolicyEditor.razor",
            "Components/Pages/GovernedOperations.razor",
        })
        {
            ReadSurface(uiRoot, surface).ShouldContain(
                "aria-label=\"@UiText[",
                Case.Sensitive,
                $"{surface} must keep a localized aria-label on the migrated data container.");
        }

        // AC4: timestamp machine values survive as <time datetime="..."> on the conversation surfaces.
        foreach (string surface in new[]
        {
            "Components/Governed/ChatBotConversationItemReviewHistory.razor",
            "Components/Governed/ChatBotEmailConversationItem.razor",
            "Components/Governed/ChatBotDecisionConversationItem.razor",
            "Components/Governed/ChatBotFailureStateConversationItem.razor",
            "Components/Governed/ChatBotApprovalConversationItem.razor",
        })
        {
            ReadSurface(uiRoot, surface).ShouldContain(
                "<time datetime=",
                Case.Sensitive,
                $"{surface} must keep the <time datetime=\"...\"> machine value.");
        }
    }

    [Fact]
    public void Timestamps_drop_monospace_but_opaque_tokens_keep_chatbot_code()
    {
        string uiRoot = UiRoot();

        List<string> monospacedTime = [];
        List<string> missingTokenMonospace = [];

        foreach (string surface in MigratedSurfaces)
        {
            string content = ReadSurface(uiRoot, surface);

            // AC3: no value-bearing <time> may keep the monospace chatbot-code class.
            if (MonospaceTimeElement.IsMatch(content))
            {
                monospacedTime.Add(surface);
            }

            // AC3: genuine opaque tokens keep <code class="chatbot-code"> — monospace must not be blanket-removed.
            if (!content.Contains("chatbot-code", StringComparison.Ordinal))
            {
                missingTokenMonospace.Add(surface);
            }
        }

        monospacedTime.ShouldBeEmpty(
            "AC3: timestamps render as plain <time> (no chatbot-code monospace). Offenders: "
            + string.Join("; ", monospacedTime));

        missingTokenMonospace.ShouldBeEmpty(
            "AC3: every migrated surface keeps <code class=\"chatbot-code\"> for its genuine opaque tokens "
            + "(ids, reason/message codes, versions, raw enum tokens); monospace must not be removed wholesale. "
            + "Surfaces missing chatbot-code: " + string.Join("; ", missingTokenMonospace));
    }

    [Fact]
    public void Task_intent_review_panel_keeps_its_preexisting_hard_coded_labels()
    {
        // AC4 / Tasks note: ChatBotTaskIntentReviewPanel's labels are pre-existing hard-coded English (not
        // localization keys). Story 13.4 must preserve them verbatim and add no new localization key.
        string panel = ReadSurface(UiRoot(), "Components/Governed/ChatBotTaskIntentReviewPanel.razor");
        panel.ShouldContain("\"Project\"", Case.Sensitive);
        panel.ShouldContain("\"Source version\"", Case.Sensitive);
        panel.ShouldContain("\"Correlation\"", Case.Sensitive);
    }

    // ──────────────────────────────────────────────────────────────────────────────────────
    // Detector-fixture pins: prove the comment-strip + element-tag logic so a future edit cannot silently
    // re-open a bypass (matching a comment reference to the former markup, or missing a real <dl>/<dt>/<dd>).
    // ──────────────────────────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("<dl class=\"chatbot-definition-list\">", true)]
    [InlineData("<dl class=\"chatbot-labelled-row-list\">", true)]
    [InlineData("<dt>", true)]
    [InlineData("<dd>", true)]
    [InlineData("    // Story 13.4: the former <dl> data dump renders as a FluentStack", false)]
    [InlineData("@* re-targets the former <dt>/<dd> row to FluentText *@", false)]
    [InlineData("<!-- former <dl> -->", false)]
    [InlineData("<FluentStack Orientation=\"Orientation.Vertical\">", false)]
    [InlineData("<div class=\"chatbot-labelled-row-list\">", false)]
    public void Definition_list_element_detector_matches_real_tags_not_comment_references(string markup, bool expected)
        => DefinitionListElement.IsMatch(StripComments(markup)).ShouldBe(expected);

    [Theory]
    [InlineData("<time class=\"chatbot-code\" datetime=\"2026-06-22\">x</time>", true)]
    [InlineData("<time datetime=\"2026-06-22\">x</time>", false)]
    [InlineData("<code class=\"chatbot-code\">token</code>", false)]
    public void Monospace_time_detector_flags_only_chatbot_code_time_elements(string markup, bool expected)
        => MonospaceTimeElement.IsMatch(markup).ShouldBe(expected);

    private static string StripComments(string content)
    {
        content = RazorComment.Replace(content, " ");
        content = HtmlComment.Replace(content, " ");
        return FullLineComment.Replace(content, " ");
    }

    private static void AssertSurfacesExist(string uiRoot, IReadOnlyList<string> surfaces)
    {
        List<string> missing = surfaces.Where(surface => !File.Exists(Resolve(uiRoot, surface))).ToList();
        missing.ShouldBeEmpty(
            "Story 13.4 surface paths must exist so a renamed/deleted file cannot silently keep this suite green. "
            + "Missing: " + string.Join("; ", missing));
    }

    private static string ReadSurface(string uiRoot, string surface)
    {
        string path = Resolve(uiRoot, surface);
        File.Exists(path).ShouldBeTrue($"expected migrated surface not found: {surface}");
        return File.ReadAllText(path);
    }

    private static string Resolve(string uiRoot, string surface)
        => Path.Combine(uiRoot, surface.Replace('/', Path.DirectorySeparatorChar));

    private static string UiRoot()
    {
        string uiRoot = Path.Combine(RepositoryRoot(), "src", "Hexalith.ChatBot.UI");
        Directory.Exists(uiRoot).ShouldBeTrue($"ChatBot UI source root not found: {uiRoot}");
        return uiRoot;
    }

    private static string[] EnumerateRazor(string root)
    {
        EnumerationOptions options = new()
        {
            RecurseSubdirectories = true,
            AttributesToSkip = FileAttributes.ReparsePoint | FileAttributes.Hidden,
            IgnoreInaccessible = true,
        };

        return Directory
            .EnumerateFiles(root, "*.razor", options)
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
