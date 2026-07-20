using System.Text.RegularExpressions;

using Shouldly;

namespace Hexalith.ChatBot.UI.E2E.Tests;

public sealed class Epic10ReleaseReadinessE2ETests
{
    private static readonly Epic10GateRow[] GateRows =
    [
        new(
            "shell",
            new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal)
            {
                ["tests/Hexalith.ChatBot.UI.E2E.Tests/FrontComposerShellIntegrationE2ETests.cs"] =
                [
                    "FrontComposerShellRuntimeShouldExposeSingleProviderTreeAndBodyRegion",
                    "SourceWiringShouldUseFrontComposerBootstrapOrderAndNoDuplicateProviders",
                    "AssertSourceWiring",
                    "data-chatbot-owned-provider",
                    "data-chatbot-owned-store-initializer",
                ],
            }),
        new(
            "project-workspace",
            new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal)
            {
                ["tests/Hexalith.ChatBot.UI.E2E.Tests/ProjectWorkspaceE2ETests.cs"] =
                [
                    "ProjectWorkspaceFixtureShouldExposeRootPickerStatesInsideSingleFrontComposerShell",
                    "ProjectWorkspaceSourceShouldKeepSelectedProjectConversationContextFilesInOneShell",
                    "ProjectWorkspaceFixtureShouldExposeAllUxDr5StatesWithoutUnauthorizedDetailLeakage",
                    "Secret Project",
                    "provider-payload",
                ],
            }),
        new(
            "project-conversation-and-composer",
            new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal)
            {
                ["tests/Hexalith.ChatBot.UI.E2E.Tests/ProjectConversationE2ETests.cs"] =
                [
                    "ChatBotGovernedComposer",
                    "ProjectConversationComposer",
                    "Project.AppendConversationMessage",
                    "ChatBotSurfaceOrigin.Ui",
                    "raw provider payload",
                    "restricted@example.com",
                ],
            }),
        new(
            "association-review-and-ai-approval",
            new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal)
            {
                ["tests/Hexalith.ChatBot.UI.E2E.Tests/GovernedOperationsVisualFoundationE2ETests.cs"] =
                [
                    "AssociationReviewShouldSelectCandidateCompareEvidenceAndKeepDisabledReasonsReachable",
                    "AssociationReviewShouldPreserveForcedColorsReducedMotionAndBlockedRedactionStates",
                    "Approval",
                    "aria-describedby",
                    "not-authorized",
                ],
                ["tests/Hexalith.ChatBot.UI.E2E.Tests/ProjectConversationE2ETests.cs"] =
                [
                    "Approval",
                    "ChatBotAiActionPreviewSections",
                    "aria-describedby",
                    "not-authorized",
                ],
            }),
        new(
            "operational-queues-and-dashboards",
            new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal)
            {
                ["tests/Hexalith.ChatBot.UI.E2E.Tests/GovernedOperationsVisualFoundationE2ETests.cs"] =
                [
                    "BuildOperationalQueueManagementFixture",
                    "role=\"table\"",
                    "role=\"row\"",
                    "tabindex=\"0\"",
                    "ForcedColorsShouldPreserveVisibleStatusLabelsAndNonColorCues",
                ],
                ["tests/Hexalith.ChatBot.UI.E2E.Tests/OperationalDashboardsAccessibilityE2ETests.cs"] =
                [
                    "DashboardShouldExposeLandmarksKeyboardRowsAndLiveFreshnessAnnouncements",
                    "role=\"table\"",
                    "role=\"row\"",
                    "tabindex=\"0\"",
                ],
            }),
        new(
            "audit-investigation",
            new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal)
            {
                ["tests/Hexalith.ChatBot.UI.E2E.Tests/ComplianceAdministrationE2ETests.cs"] =
                [
                    "ComplianceAuditInvestigationShouldExposeMetadataOnlyTimelineAndSafeEscalation",
                    "CompliancePhoneFallbackShouldKeepReadOnlySummaryAndEscalationReachable",
                    "data-chatbot-surface=\"audit-investigation-s9\"",
                    "aria-describedby=\"compliance-operate-denied\"",
                    "Compliance audit timeline",
                ],
            }),
        new(
            "streaming-stop-and-progressive-response",
            new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal)
            {
                ["tests/Hexalith.ChatBot.UI.E2E.Tests/GovernedOperationsVisualFoundationE2ETests.cs"] =
                [
                    "AssertStreamingStopControlWithoutBrowser",
                    "ChatBotStreamingStopControl",
                    "StopResponseAnnouncement",
                    "data-chatbot-streaming",
                ],
                ["tests/Hexalith.ChatBot.UI.E2E.Tests/ProjectConversationE2ETests.cs"] =
                [
                    "ProjectConversationStreamingStopShouldRenderKeyboardReachableControlAndPoliteLocalizedStatus",
                    "ChatBotStreamingStopControl",
                    "data-chatbot-streaming",
                    "data-chatbot-streaming-terminal",
                ],
            }),
    ];

    [Fact]
    public void Epic10GateMatrixShouldPinEveryReleaseReadinessSurface()
    {
        GateRows.Select(static row => row.Surface).ShouldBe(
        [
            "shell",
            "project-workspace",
            "project-conversation-and-composer",
            "association-review-and-ai-approval",
            "operational-queues-and-dashboards",
            "audit-investigation",
            "streaming-stop-and-progressive-response",
        ], ignoreOrder: false);

        foreach (Epic10GateRow row in GateRows)
        {
            foreach ((string path, IReadOnlyList<string> requiredMarkers) in row.RequiredMarkersByPath)
            {
                string source = ReadProjectFile(path);
                source.ShouldContain("TryStartAsync");
                foreach (string marker in requiredMarkers)
                {
                    source.ShouldContain(
                        marker,
                        customMessage: $"{row.Surface} must pin '{marker}' in {path} itself.");
                }
            }
        }
    }

    [Fact]
    public void BrowserUnavailableFallbacksShouldAssertSourceContractsInsteadOfReturningVacuously()
    {
        foreach (string path in GateRows.SelectMany(static row => row.RequiredMarkersByPath.Keys).Distinct(StringComparer.Ordinal))
        {
            string source = ReadProjectFile(path);
            MatchCollection fallbacks = Regex.Matches(
                source,
                @"if \(harness is null\)\s*\{\s*(?<body>.*?)\s*return;",
                RegexOptions.CultureInvariant | RegexOptions.Singleline);

            fallbacks.Count.ShouldBeGreaterThan(0, $"{path} should keep at least one browser-unavailable fallback.");
            foreach (Match fallback in fallbacks)
            {
                string body = fallback.Groups["body"].Value;
                body.ShouldContain("Assert");

                // The fallback must delegate to a dedicated source-assertion helper
                // (the project's "...WithoutBrowser()" convention) rather than a bare
                // `return;`. A vacuous fallback would have no such call and fail here.
                body.ShouldContain("WithoutBrowser");
            }

            source.ShouldContain("ShouldContain");
        }
    }

    [Fact]
    public void Epic10SourceFixturesShouldKeepShellOwnershipAndLeakageSentinelsGuarded()
    {
        string layout = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Layout/MainLayout.razor");
        string app = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/App.razor");
        string[] pagePaths =
        [
            "src/Hexalith.ChatBot.UI/Components/Pages/ProjectWorkspace.razor",
            "src/Hexalith.ChatBot.UI/Components/Pages/ProjectConversation.razor",
            "src/Hexalith.ChatBot.UI/Components/Pages/AssociationReview.razor",
            "src/Hexalith.ChatBot.UI/Components/Pages/GovernedOperations.razor",
            "src/Hexalith.ChatBot.UI/Components/Pages/OperationalDashboards.razor",
            "src/Hexalith.ChatBot.UI/Components/Pages/ComplianceAuditInvestigation.razor",
        ];

        layout.ShouldContain("<FrontComposerShell AppTitle=\"Hexalith ChatBot\" ShowAccountMenu=\"false\">");
        app.ShouldContain("css/chatbot.tokens.css");
        (layout + app).ShouldNotContain("<FluentProviders", Case.Sensitive);
        (layout + app).ShouldNotContain("StoreInitializer", Case.Sensitive);

        foreach (string path in pagePaths)
        {
            string source = ReadProjectFile(path);
            bool usesConversationShell = source.Contains("<ChatBotConversationShell", StringComparison.Ordinal)
                || source.Contains("<ChatBotProjectConversationWorkspace", StringComparison.Ordinal);
            usesConversationShell.ShouldBeTrue();
            source.ShouldNotContain("<FrontComposerShell", Case.Sensitive);
            source.ShouldNotContain("<FluentProviders", Case.Sensitive);
            source.ShouldNotContain("StoreInitializer", Case.Sensitive);
            source.ShouldNotContain("<main", Case.Sensitive);
            source.ShouldNotContain("role=\"banner\"", Case.Sensitive);
            source.ShouldNotContain("marketing", Case.Insensitive);
            source.ShouldNotContain("hero", Case.Insensitive);
        }

        string e2e = string.Concat(GateRows
            .SelectMany(static row => row.RequiredMarkersByPath.Keys)
            .Distinct(StringComparer.Ordinal)
            .Select(ReadProjectFile));
        foreach (string sentinel in new[]
        {
            "Secret Project",
            "restricted@example.com",
            "private-mailbox@example.test",
            "provider-payload",
            "raw exception",
            "stack trace",
            "unauthorized file name",
            "raw audit envelope",
        })
        {
            e2e.ShouldContain(sentinel);
        }
    }

    [Fact]
    public void StreamingVerificationShouldTrackCanonicalStoryThirteenTwo()
    {
        string sprint = ReadProjectFile("_bmad-output/implementation-artifacts/sprint-status.yaml");
        string stopControl = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Governed/ChatBotStreamingStopControl.razor");
        string e2e = ReadProjectFile("tests/Hexalith.ChatBot.UI.E2E.Tests/ProjectConversationE2ETests.cs");

        string canonicalStoryStatus = ReadDevelopmentStatus(
            sprint,
            "13-2-work-converse-and-interrupt-ai-safely-in-project-context");
        canonicalStoryStatus.ShouldBeOneOf(["review", "done"]);
        stopControl.ShouldContain("StopVerified");
        e2e.ShouldContain("ProjectConversationStreamingStopShouldRenderKeyboardReachableControlAndPoliteLocalizedStatus");
        e2e.ShouldContain("AssertStreamingStopWithoutBrowser");
        e2e.ShouldContain("data-chatbot-streaming-terminal");
        e2e.ShouldContain("Stop response generation");
    }

    [Fact]
    public void DevelopmentStatusReaderShouldIgnoreFullLineAndInlineYamlComments()
    {
        const string yaml = """
            development_status:   # release board
              # Canonical story status remains human-annotated.
              13-2-work-converse-and-interrupt-ai-safely-in-project-context: review # ready for release review
            # A root-level comment does not close the mapping.
            unrelated_root: value
            """;

        ReadDevelopmentStatus(
            yaml,
            "13-2-work-converse-and-interrupt-ai-safely-in-project-context").ShouldBe("review");
    }

    private static string ReadProjectFile(string relativePath)
        => File.ReadAllText(Path.Combine(FindSolutionRoot(), relativePath));

    private static string ReadDevelopmentStatus(string yaml, string storyKey)
    {
        ArgumentNullException.ThrowIfNull(yaml);
        ArgumentException.ThrowIfNullOrWhiteSpace(storyKey);
        bool inDevelopmentStatus = false;
        List<string> matches = [];
        Regex exactEntry = new(
            $@"^  {Regex.Escape(storyKey)}:\s*(?<status>[a-z][a-z-]*)\s*(?:#.*)?$",
            RegexOptions.CultureInvariant);
        Regex developmentStatusHeader = new(
            @"^development_status:\s*(?:#.*)?$",
            RegexOptions.CultureInvariant);

        foreach (string line in yaml.Split('\n'))
        {
            string normalized = line.TrimEnd('\r');
            if (!inDevelopmentStatus)
            {
                inDevelopmentStatus = developmentStatusHeader.IsMatch(normalized);
                continue;
            }

            if (normalized.TrimStart().StartsWith('#'))
            {
                continue;
            }

            if (normalized.Length > 0 && !char.IsWhiteSpace(normalized[0]))
            {
                break;
            }

            Match match = exactEntry.Match(normalized);
            if (match.Success)
            {
                matches.Add(match.Groups["status"].Value);
            }
        }

        if (matches.Count != 1)
        {
            throw new InvalidOperationException(
                $"Expected exactly one development_status entry for '{storyKey}', but found {matches.Count}.");
        }

        return matches[0];
    }

    private static string FindSolutionRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Hexalith.ChatBot.slnx")))
        {
            directory = directory.Parent;
        }

        directory.ShouldNotBeNull("The test process should run from or beneath the ChatBot repository.");
        return directory.FullName;
    }
}
