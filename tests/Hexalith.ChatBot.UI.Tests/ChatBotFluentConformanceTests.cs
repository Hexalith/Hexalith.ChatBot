using System.Text.RegularExpressions;

using Shouldly;

namespace Hexalith.ChatBot.UI.Tests;

/// <summary>
/// Governance guard for the ChatBot UI Fluent v5 conformance rule.
/// </summary>
[Trait("Category", "Governance")]
public sealed class ChatBotFluentConformanceTests
{
    // Case-sensitive by design: raw HTML controls are lowercase, while Razor component tags such as
    // <FluentButton> and <FluentTextArea> are PascalCase and must not match.
    private static readonly Regex RawInteractiveControl = new(
        "<(button|input|select|textarea)(\\s|/|>)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex LegacyFluentToken = new(
        "--(type-ramp|neutral-foreground|neutral-fill|neutral-stroke|neutral-layer|accent-fill|"
        + "accent-foreground|accent-stroke|accent-base|palette-|design-unit|elevation-shadow|corner-radius|"
        + "focus-stroke|stroke-width|disabled-opacity)[a-z0-9-]*",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex ChatBotTypeAliasDeclaration = new(
        "--chatbot-type-[a-z0-9-]+(?=\\s*:)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex ChatBotRadiusAliasDeclaration = new(
        "--chatbot-radius-[a-z0-9-]+(?=\\s*:)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex ChatBotFontAliasDeclaration = new(
        "--chatbot-font-[a-z0-9-]+(?=\\s*:)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex ChatBotButtonPrimitiveSelector = new(
        "(^|[\\s,{])\\.chatbot-button(?=[:.#\\s,{>+~\\[]|$)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline);

    private static readonly Regex HeadingTypographyDeclaration = new(
        "\\b(font-size|font-weight|line-height)\\s*:",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

    private static readonly Regex ForegroundRoleDeclaration = new(
        "(?<!-)\\bcolor\\s*:",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

    private static readonly Regex NativeControlCssSelector = new(
        "(^|[\\s,{>+~])(?:button|input|select|textarea)(?=[:.#\\s,{>+~\\[]|$)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline);

    private static readonly string[] RawControlMigrationBacklog =
    [
        "Components/Governed/ChatBotActorBadge.razor",
        "Components/Governed/ChatBotApprovalConversationItem.razor",
        "Components/Governed/ChatBotAssociationCandidateRow.razor",
        "Components/Governed/ChatBotAssociationReviewActions.razor",
        "Components/Governed/ChatBotEscalationPolicyEditor.razor",
        "Components/Governed/ChatBotEvidenceChip.razor",
        "Components/Governed/ChatBotNotificationRoutingEditor.razor",
        "Components/Governed/ChatBotTaskIntentReviewPanel.razor",
        "Components/Governed/ChatBotTenantPolicyEditor.razor",
        "Components/Governed/ChatBotWhyProjectPanel.razor",
        "Components/Pages/ComplianceAuditInvestigation.razor",
    ];

    private static readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, int>> PrimitiveMigrationBacklog =
        new Dictionary<string, IReadOnlyDictionary<string, int>>(StringComparer.Ordinal)
        {
            ["wwwroot/css/chatbot.tokens.css"] = new Dictionary<string, int>(StringComparer.Ordinal)
            {
                ["--chatbot-type-* aliases"] = 11,
                ["--chatbot-radius-* aliases"] = 3,
                ["--chatbot-font-* aliases"] = 5,
                [".chatbot-button selector"] = 0,
                ["heading typography declarations"] = 51,
                ["foreground color declarations"] = 34,
                ["native control CSS selectors"] = 4,
            },
        };

    [Fact]
    public void ChatBot_components_use_fluent_v5_only_except_temporary_raw_control_backlog()
    {
        string uiRoot = Path.Combine(RepositoryRoot(), "src", "Hexalith.ChatBot.UI");
        Directory.Exists(uiRoot).ShouldBeTrue($"ChatBot UI source root not found: {uiRoot}");

        string[] razorFiles = EnumerateFiles(uiRoot, "*.razor");
        razorFiles.ShouldNotBeEmpty($"no .razor files found under {uiRoot}");

        HashSet<string> backlog = new(RawControlMigrationBacklog, StringComparer.Ordinal);
        List<string> missingBacklogEntries = [];
        List<string> activeBacklogEntries = [];
        List<string> offenders = [];

        foreach (string backlogEntry in RawControlMigrationBacklog)
        {
            string backlogPath = Path.Combine(uiRoot, backlogEntry.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(backlogPath))
            {
                missingBacklogEntries.Add(backlogEntry);
            }
        }

        missingBacklogEntries.ShouldBeEmpty(
            "Raw-control migration backlog paths must exist so renamed/deleted files cannot silently keep "
            + $"the guard green. Missing backlog entries: {string.Join("; ", missingBacklogEntries)}");

        foreach (string file in razorFiles)
        {
            string relative = RelativePath(uiRoot, file);
            MatchCollection matches = RawInteractiveControl.Matches(File.ReadAllText(file));
            if (matches.Count == 0)
            {
                continue;
            }

            string tags = string.Join(
                ", ",
                matches.Select(match => match.Groups[1].Value).Distinct(StringComparer.Ordinal));

            if (backlog.Contains(relative))
            {
                activeBacklogEntries.Add(relative);
                continue;
            }

            offenders.Add($"{relative} ({tags})");
        }

        offenders.ShouldBeEmpty(
            "ChatBot .razor components must use FrontComposer/Fluent v5 components only (no raw "
            + "<button>/<input>/<select>/<textarea>; raw <a> navigation links are allowed). Raw interactive "
            + $"controls found outside the temporary migration backlog: {string.Join("; ", offenders)}");

        string[] staleBacklogEntries = RawControlMigrationBacklog
            .Except(activeBacklogEntries, StringComparer.Ordinal)
            .ToArray();

        staleBacklogEntries.ShouldBeEmpty(
            "These raw-control backlog entries no longer contain raw interactive controls; remove them so the "
            + $"temporary backlog only shrinks: {string.Join("; ", staleBacklogEntries)}");
    }

    [Fact]
    public void ChatBot_styles_do_not_redefine_fluent_theme_primitives_except_temporary_css_backlog()
    {
        string uiRoot = Path.Combine(RepositoryRoot(), "src", "Hexalith.ChatBot.UI");
        Directory.Exists(uiRoot).ShouldBeTrue($"ChatBot UI source root not found: {uiRoot}");

        string[] styleFiles = EnumerateFiles(uiRoot, "*.css")
            .Concat(EnumerateFiles(uiRoot, "*.razor"))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        styleFiles.ShouldNotBeEmpty($"no .css/.razor files found under {uiRoot}");

        List<string> legacyTokenOffenders = [];
        List<string> primitiveOffenders = [];
        List<string> staleBacklogEntries = [];
        List<string> backlogDrift = [];

        foreach (string backlogEntry in PrimitiveMigrationBacklog.Keys)
        {
            string backlogPath = Path.Combine(uiRoot, backlogEntry.Replace('/', Path.DirectorySeparatorChar));
            File.Exists(backlogPath).ShouldBeTrue(
                $"CSS primitive migration backlog path must exist so renamed/deleted files cannot silently keep "
                + $"the guard green: {backlogEntry}");
        }

        foreach (string file in styleFiles)
        {
            string relative = RelativePath(uiRoot, file);
            string content = File.ReadAllText(file);

            MatchCollection legacyMatches = LegacyFluentToken.Matches(content);
            if (legacyMatches.Count > 0)
            {
                string tokens = string.Join(
                    ", ",
                    legacyMatches.Select(match => match.Value).Distinct(StringComparer.Ordinal));
                legacyTokenOffenders.Add($"{relative} ({tokens})");
            }

            IReadOnlyDictionary<string, int> actualDebt = CountPrimitiveDebt(content);
            if (!PrimitiveMigrationBacklog.TryGetValue(relative, out IReadOnlyDictionary<string, int>? expectedDebt))
            {
                if (actualDebt.Values.Any(static count => count > 0))
                {
                    primitiveOffenders.Add($"{relative} ({FormatDebt(actualDebt)})");
                }

                continue;
            }

            if (actualDebt.Values.All(static count => count == 0))
            {
                staleBacklogEntries.Add(relative);
            }

            foreach ((string kind, int expectedCount) in expectedDebt)
            {
                int actualCount = actualDebt.TryGetValue(kind, out int count) ? count : 0;
                if (actualCount != expectedCount)
                {
                    backlogDrift.Add($"{relative}: {kind} expected {expectedCount}, actual {actualCount}");
                }
            }

            foreach ((string kind, int actualCount) in actualDebt)
            {
                if (!expectedDebt.ContainsKey(kind) && actualCount > 0)
                {
                    backlogDrift.Add($"{relative}: unexpected {kind} count {actualCount}");
                }
            }
        }

        legacyTokenOffenders.ShouldBeEmpty(
            "ChatBot UI styles must not use legacy Fluent v4 / FAST tokens (--type-ramp-*, --neutral-*, "
            + "--accent-*, --palette-*, --design-unit, ...). Legacy tokens found in: "
            + string.Join("; ", legacyTokenOffenders));

        primitiveOffenders.ShouldBeEmpty(
            "ChatBot UI styles must not recreate Fluent-provided primitives such as button styling, heading "
            + "type ramps, foreground color roles, native-control selectors, or custom ChatBot primitive aliases. "
            + $"Primitive style debt found outside the temporary migration backlog: {string.Join("; ", primitiveOffenders)}");

        staleBacklogEntries.ShouldBeEmpty(
            "These CSS primitive backlog entries no longer contain primitive debt; remove them so the temporary "
            + $"backlog only shrinks: {string.Join("; ", staleBacklogEntries)}");

        backlogDrift.ShouldBeEmpty(
            "CSS primitive migration debt changed. Story 12.1 permits only the exact pre-migration baseline, "
            + "so new primitive declarations fail and migration stories must update the shrinking backlog. Drift: "
            + string.Join("; ", backlogDrift));
    }

    [Fact]
    public void Raw_interactive_control_detector_flags_lowercase_html_controls_without_false_positives()
    {
        const string markup = """
            <FluentButton Appearance="ButtonAppearance.Accent">Submit</FluentButton>
            <FluentTextInput />
            <InputText inputmode="numeric" />
            <a href="/governed-operations">Open governed operations</a>
            <button type="button">Submit</button>
            <input aria-label="Name" />
            <select aria-label="Decision"><option>Approve</option></select>
            <textarea aria-label="Rationale"></textarea>
            """;

        string[] tags = RawInteractiveControl
            .Matches(markup)
            .Select(static match => match.Groups[1].Value)
            .ToArray();

        tags.ShouldBe(new[] { "button", "input", "select", "textarea" });
    }

    [Fact]
    public void Legacy_fluent_token_detector_blocks_v4_fast_tokens_without_blocking_fluent2_tokens()
    {
        const string content = """
            :root {
                --type-ramp-base-font-size: 1rem;
                --neutral-fill-rest: #fff;
                --accent-fill-rest: #000;
                --palette-red: #f00;
                --design-unit: 4;
                --colorNeutralForeground1: #242424;
                --fontSizeBase300: 0.875rem;
                --fc-spacing-unit: 0.5rem;
            }
            """;

        string[] tokens = LegacyFluentToken
            .Matches(content)
            .Select(static match => match.Value)
            .ToArray();

        tokens.ShouldBe(
            new[]
            {
                "--type-ramp-base-font-size",
                "--neutral-fill-rest",
                "--accent-fill-rest",
                "--palette-red",
                "--design-unit",
            });
    }

    [Fact]
    public void Primitive_debt_detector_counts_chatbot_owned_theme_redefinition_patterns()
    {
        const string content = """
            :root {
                --chatbot-type-body-size: 1rem;
                --chatbot-radius-control: 4px;
                --chatbot-font-weight-strong: 600;
            }

            .chatbot-button {
                color: var(--colorNeutralForeground1);
                background-color: var(--colorNeutralBackground1);
            }

            h2 {
                font-size: 1rem;
                font-weight: 600;
                line-height: 1.4;
                color: var(--colorNeutralForeground2);
            }

            button,
            input:focus,
            select[data-kind],
            textarea {
                border: 0;
            }
            """;

        IReadOnlyDictionary<string, int> debt = CountPrimitiveDebt(content);

        debt["--chatbot-type-* aliases"].ShouldBe(1);
        debt["--chatbot-radius-* aliases"].ShouldBe(1);
        debt["--chatbot-font-* aliases"].ShouldBe(1);
        debt[".chatbot-button selector"].ShouldBe(1);
        debt["heading typography declarations"].ShouldBe(3);
        debt["foreground color declarations"].ShouldBe(2);
        debt["native control CSS selectors"].ShouldBe(4);
    }

    [Fact]
    public void Primitive_debt_detector_ignores_fluent2_tokens_and_layout_only_css()
    {
        const string content = """
            :root {
                --colorNeutralForeground1: #242424;
                --fontSizeBase300: 0.875rem;
                --lineHeightBase300: 1.25rem;
                --fc-spacing-unit: 0.5rem;
            }

            .chatbot-shell {
                display: grid;
                gap: var(--fc-spacing-unit);
                background-color: var(--colorNeutralBackground1);
                border-color: var(--colorNeutralStroke1);
            }
            """;

        CountPrimitiveDebt(content).Values.ShouldAllBe(static count => count == 0);
    }

    private static IReadOnlyDictionary<string, int> CountPrimitiveDebt(string content) =>
        new Dictionary<string, int>(StringComparer.Ordinal)
        {
            ["--chatbot-type-* aliases"] = ChatBotTypeAliasDeclaration.Matches(content).Count,
            ["--chatbot-radius-* aliases"] = ChatBotRadiusAliasDeclaration.Matches(content).Count,
            ["--chatbot-font-* aliases"] = ChatBotFontAliasDeclaration.Matches(content).Count,
            [".chatbot-button selector"] = ChatBotButtonPrimitiveSelector.Matches(content).Count,
            ["heading typography declarations"] = HeadingTypographyDeclaration.Matches(content).Count,
            ["foreground color declarations"] = ForegroundRoleDeclaration.Matches(content).Count,
            ["native control CSS selectors"] = NativeControlCssSelector.Matches(content).Count,
        };

    private static string FormatDebt(IReadOnlyDictionary<string, int> debt) =>
        string.Join(
            ", ",
            debt.Where(static pair => pair.Value > 0).Select(static pair => $"{pair.Key}: {pair.Value}"));

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
