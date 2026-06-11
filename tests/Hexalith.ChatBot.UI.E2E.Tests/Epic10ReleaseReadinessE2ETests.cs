using System.Text.RegularExpressions;

using Shouldly;

namespace Hexalith.ChatBot.UI.E2E.Tests;

public sealed class Epic10ReleaseReadinessE2ETests
{
    private static readonly Epic10GateRow[] GateRows =
    [
        new(
            "shell",
            ["tests/Hexalith.ChatBot.UI.E2E.Tests/FrontComposerShellIntegrationE2ETests.cs"],
            [
                "FrontComposerShellRuntimeShouldExposeSingleProviderTreeAndBodyRegion",
                "SourceWiringShouldUseFrontComposerBootstrapOrderAndNoDuplicateProviders",
                "AssertSourceWiring",
                "data-chatbot-owned-provider",
                "data-chatbot-owned-store-initializer",
            ]),
        new(
            "project-workspace",
            ["tests/Hexalith.ChatBot.UI.E2E.Tests/ProjectWorkspaceE2ETests.cs"],
            [
                "ProjectWorkspaceFixtureShouldExposeRootPickerStatesInsideSingleFrontComposerShell",
                "ProjectWorkspaceSourceShouldKeepSelectedProjectConversationContextFilesInOneShell",
                "ProjectWorkspaceFixtureShouldExposeAllUxDr5StatesWithoutUnauthorizedDetailLeakage",
                "Secret Project",
                "provider-payload",
            ]),
        new(
            "project-conversation-and-composer",
            ["tests/Hexalith.ChatBot.UI.E2E.Tests/ProjectConversationE2ETests.cs"],
            [
                "ChatBotGovernedComposer",
                "ProjectConversationComposer",
                "Project.AppendConversationMessage",
                "ChatBotSurfaceOrigin.Ui",
                "raw provider payload",
                "restricted@example.com",
            ]),
        new(
            "association-review-and-ai-approval",
            ["tests/Hexalith.ChatBot.UI.E2E.Tests/GovernedOperationsVisualFoundationE2ETests.cs", "tests/Hexalith.ChatBot.UI.E2E.Tests/ProjectConversationE2ETests.cs"],
            [
                "AssociationReviewShouldSelectCandidateCompareEvidenceAndKeepDisabledReasonsReachable",
                "AssociationReviewShouldPreserveForcedColorsReducedMotionAndBlockedRedactionStates",
                "Approval",
                "ChatBotAiActionPreviewSections",
                "aria-describedby",
                "not-authorized",
            ]),
        new(
            "operational-queues-and-dashboards",
            ["tests/Hexalith.ChatBot.UI.E2E.Tests/GovernedOperationsVisualFoundationE2ETests.cs", "tests/Hexalith.ChatBot.UI.E2E.Tests/OperationalDashboardsAccessibilityE2ETests.cs"],
            [
                "BuildOperationalQueueManagementFixture",
                "DashboardShouldExposeLandmarksKeyboardRowsAndLiveFreshnessAnnouncements",
                "role=\"table\"",
                "role=\"row\"",
                "tabindex=\"0\"",
                "ForcedColorsShouldPreserveVisibleStatusLabelsAndNonColorCues",
            ]),
        new(
            "audit-investigation",
            ["tests/Hexalith.ChatBot.UI.E2E.Tests/ComplianceAdministrationE2ETests.cs"],
            [
                "ComplianceAuditInvestigationShouldExposeMetadataOnlyTimelineAndSafeEscalation",
                "CompliancePhoneFallbackShouldKeepReadOnlySummaryAndEscalationReachable",
                "data-chatbot-surface=\"audit-investigation-s9\"",
                "aria-describedby=\"compliance-operate-denied\"",
                "Compliance audit timeline",
            ]),
        new(
            "streaming-stop-primitive-only",
            ["tests/Hexalith.ChatBot.UI.E2E.Tests/GovernedOperationsVisualFoundationE2ETests.cs"],
            [
                "AssertStreamingStopControlWithoutBrowser",
                "ChatBotStreamingStopControl",
                "StopResponseAnnouncement",
                "data-chatbot-streaming",
            ]),
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
            "streaming-stop-primitive-only",
        ], ignoreOrder: false);

        foreach (Epic10GateRow row in GateRows)
        {
            string combined = string.Concat(row.E2ETestPaths.Select(ReadProjectFile));
            combined.ShouldContain("TryStartAsync");

            foreach (string marker in row.RequiredMarkers)
            {
                combined.ShouldContain(marker);
            }
        }
    }

    [Fact]
    public void BrowserUnavailableFallbacksShouldAssertSourceContractsInsteadOfReturningVacuously()
    {
        foreach (string path in GateRows.SelectMany(static row => row.E2ETestPaths).Distinct(StringComparer.Ordinal))
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

        layout.ShouldContain("<FrontComposerShell AppTitle=\"Hexalith ChatBot\">");
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

        string e2e = string.Concat(GateRows.SelectMany(static row => row.E2ETestPaths).Distinct(StringComparer.Ordinal).Select(ReadProjectFile));
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
    public void StreamingVerificationShouldRemainPrimitiveOnlyUntilStory106IsImplemented()
    {
        string sprint = ReadProjectFile("_bmad-output/implementation-artifacts/sprint-status.yaml");
        string stopControl = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Governed/ChatBotStreamingStopControl.razor");
        string e2e = ReadProjectFile("tests/Hexalith.ChatBot.UI.E2E.Tests/GovernedOperationsVisualFoundationE2ETests.cs");

        sprint.ShouldContain("10-6a-streaming-transport-adr: backlog");
        sprint.ShouldContain("10-6b-streaming-ai-response-and-stop-cancel: backlog");
        stopControl.ShouldContain("StopResponseAnnouncement");
        e2e.ShouldContain("AssertStreamingStopControlWithoutBrowser");
        e2e.ShouldNotContain("streaming transport complete", Case.Insensitive);
        e2e.ShouldNotContain("progressive response rendering complete", Case.Insensitive);
        e2e.ShouldNotContain("full stop/cancel production verification", Case.Insensitive);
    }

    private static string ReadProjectFile(string relativePath)
        => File.ReadAllText(Path.Combine(FindSolutionRoot(), relativePath));

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

    private sealed record Epic10GateRow(
        string Surface,
        IReadOnlyList<string> E2ETestPaths,
        IReadOnlyList<string> RequiredMarkers);
}
