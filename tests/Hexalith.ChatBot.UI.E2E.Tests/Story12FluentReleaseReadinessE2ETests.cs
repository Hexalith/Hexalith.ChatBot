using System.Text.RegularExpressions;

using Shouldly;

namespace Hexalith.ChatBot.UI.E2E.Tests;

public sealed class Story12FluentReleaseReadinessE2ETests
{
    private static readonly Story12GateRow[] GateRows =
    [
        new(
            "governed-composer",
            ["tests/Hexalith.ChatBot.UI.E2E.Tests/ProjectConversationE2ETests.cs"],
            ["AssertGovernedComposerWithoutBrowser", "<fluent-button", "<fluent-label", "<fluent-text-area", "project-conversation-composer-error"]),
        new(
            "conversation-stream-and-items",
            ["tests/Hexalith.ChatBot.UI.E2E.Tests/ProjectConversationE2ETests.cs"],
            ["AssertPopulatedWithoutBrowser", "data-chatbot-conversation-stream=\"metadata-only\"", "data-chatbot-conversation-item-kind", "data-chatbot-live-announced", "raw provider payload"]),
        new(
            "association-review",
            ["tests/Hexalith.ChatBot.UI.E2E.Tests/GovernedOperationsVisualFoundationE2ETests.cs"],
            ["AssociationReviewShouldSelectCandidateCompareEvidenceAndKeepDisabledReasonsReachable", "<fluent-button", "<fluent-text-area", "data-chatbot-association-candidate", "projection-invalidation-unavailable"]),
        new(
            "approval-and-governed-actions",
            ["tests/Hexalith.ChatBot.UI.E2E.Tests/ProjectConversationE2ETests.cs", "tests/Hexalith.ChatBot.UI.E2E.Tests/ApprovalQueuePriorityE2ETests.cs"],
            ["AssertApprovalDecisionSurfaceCoverageWithoutBrowser", "AssertTaskIntentReviewPanelCoverageWithoutBrowser", "<fluent-button", "aria-disabled=\"true\"", "ApprovalQueuePriority"]),
        new(
            "policy-notification-escalation-editors",
            ["tests/Hexalith.ChatBot.UI.E2E.Tests/EscalationPolicyEditorE2ETests.cs", "tests/Hexalith.ChatBot.UI.E2E.Tests/NotificationRoutingEditorE2ETests.cs", "tests/Hexalith.ChatBot.UI.E2E.Tests/TenantPolicyEditorE2ETests.cs"],
            ["<fluent-label", "<fluent-text-input", "<fluent-select", "<fluent-number-input", "aria-label", "permission-freshness"]),
        new(
            "operational-dashboard-and-queues",
            ["tests/Hexalith.ChatBot.UI.E2E.Tests/GovernedOperationsVisualFoundationE2ETests.cs", "tests/Hexalith.ChatBot.UI.E2E.Tests/OperationalDashboardsAccessibilityE2ETests.cs", "tests/Hexalith.ChatBot.UI.E2E.Tests/Story12CssRetirementE2ETests.cs"],
            ["TouchTargetsShouldMeetPrimaryAndDenseMinimumsAtPhoneAndTabletWidths", "DashboardShouldExposeLandmarksKeyboardRowsAndLiveFreshnessAnnouncements", "data-chatbot-operational-queue", "data-chatbot-dashboard-view", "forced-colors"]),
        new(
            "audit-investigation",
            ["tests/Hexalith.ChatBot.UI.E2E.Tests/ComplianceAdministrationE2ETests.cs"],
            ["ComplianceAuditInvestigationShouldExposeMetadataOnlyTimelineAndSafeEscalation", "<fluent-text-input", "<fluent-number-input", "aria-describedby=\"compliance-operate-denied\"", "Compliance audit timeline"]),
        new(
            "streaming-stop",
            ["tests/Hexalith.ChatBot.UI.E2E.Tests/GovernedOperationsVisualFoundationE2ETests.cs"],
            ["AssertStreamingStopControlWithoutBrowser", "data-chatbot-streaming", "chatbot-touch-target-primary", "StopResponseAnnouncement"]),
    ];

    [Fact]
    public void Story12GateMatrixShouldPinEveryFluentMigratedSurface()
    {
        GateRows.Select(static row => row.Surface).ShouldBe(
        [
            "governed-composer",
            "conversation-stream-and-items",
            "association-review",
            "approval-and-governed-actions",
            "policy-notification-escalation-editors",
            "operational-dashboard-and-queues",
            "audit-investigation",
            "streaming-stop",
        ], ignoreOrder: false);

        foreach (Story12GateRow row in GateRows)
        {
            string combined = string.Concat(row.E2ETestPaths.Select(ReadProjectFile));
            combined.ShouldContain("TryStartAsync");

            foreach (string marker in row.RequiredMarkers)
            {
                combined.ShouldContain(marker, Case.Sensitive);
            }
        }
    }

    [Fact]
    public void BrowserUnavailableFallbacksShouldRemainNonVacuousOrVisibleSkips()
    {
        foreach (string path in GateRows.SelectMany(static row => row.E2ETestPaths).Distinct(StringComparer.Ordinal))
        {
            string source = ReadProjectFile(path);
            if (!source.Contains("TryStartAsync", StringComparison.Ordinal))
            {
                continue;
            }

            MatchCollection fallbacks = Regex.Matches(
                source,
                @"if \(harness is null\)\s*\{\s*(?<body>.*?)\s*return;",
                RegexOptions.CultureInvariant | RegexOptions.Singleline);

            fallbacks.Count.ShouldBeGreaterThan(0, $"{path} should keep at least one explicit no-browser branch.");
            foreach (Match fallback in fallbacks)
            {
                string body = fallback.Groups["body"].Value;
                bool isNonVacuousSourceAssertion = body.Contains("Assert", StringComparison.Ordinal)
                    && body.Contains("WithoutBrowser", StringComparison.Ordinal);
                bool isVisibleSkip = body.Contains("Assert.Skip", StringComparison.Ordinal);

                (isNonVacuousSourceAssertion || isVisibleSkip).ShouldBeTrue(
                    $"{path} contains a no-browser branch that neither asserts source contracts nor visibly skips.");
            }
        }
    }

    [Fact]
    public void Story12SourceFixturesShouldCoverVisualModesLocalizationAndRetiredPrimitiveGuards()
    {
        string combined = string.Concat(GateRows.SelectMany(static row => row.E2ETestPaths).Distinct(StringComparer.Ordinal).Select(ReadProjectFile));
        string cssRetirement = ReadProjectFile("tests/Hexalith.ChatBot.UI.E2E.Tests/Story12CssRetirementE2ETests.cs");

        foreach (string marker in new[]
        {
            "forced-colors",
            "prefers-reduced-motion",
            "phone",
            "tablet",
            "desktop",
            "fr",
            "horizontal",
            "overflow",
            "raw provider payload",
            "restricted@example.com",
            "Secret Project",
        })
        {
            combined.ShouldContain(marker, Case.Insensitive);
        }

        cssRetirement.ShouldContain("RetiredPresentationHooksShouldNotRemainInProductionSourceOrE2EFixtures");
        cssRetirement.ShouldContain("RetiredCssPrimitiveSelectorsShouldStayAbsentFromProductionStylesheet");
        cssRetirement.ShouldContain("RetiredControlClassesShouldBeReplacedByFluentAndSemanticBehaviorContracts");
    }

    [Fact]
    public void FluentOnlyGovernanceAndCliMcpParityProofShouldStayConnectedToBlockingLanes()
    {
        string fluentConformance = ReadProjectFile("tests/Hexalith.ChatBot.UI.Tests/ChatBotFluentConformanceTests.cs");
        string auditSource = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Pages/ComplianceAuditInvestigation.razor");
        string architecture = ReadProjectFile("tests/Hexalith.ChatBot.Architecture.Tests/ScaffoldArchitectureTests.cs");
        string cli = ReadProjectFile("tests/Hexalith.ChatBot.Cli.Tests/ChatBotCliCommandTests.cs");
        string mcp = ReadProjectFile("tests/Hexalith.ChatBot.Mcp.Tests/ChatBotMcpServiceTests.cs");

        fluentConformance.ShouldContain("RawControlMigrationBacklog = []");
        fluentConformance.ShouldContain("PrimitiveMigrationBacklog =");
        fluentConformance.ShouldContain("new Dictionary<string, IReadOnlyDictionary<string, int>>(StringComparer.Ordinal)");
        auditSource.ShouldContain("data-compliance-operate-denied=\"true\"");

        architecture.ShouldContain("ChatBotCliAdapterMustDependOnlyOnClientFacadeAndNeverServerOrDataPlaneInternals");
        architecture.ShouldContain("ChatBotMcpAdapterMustDependOnlyOnClientFacadeAndNeverServerOrDataPlaneInternals");
        architecture.ShouldContain("ChatBotUiAdapterMustDependOnlyOnClientFacadeAndNeverServerInternals");

        cli.ShouldContain("ChatBotSurfaceOrigin.Cli");
        cli.ShouldContain("IChatBotClient");
        mcp.ShouldContain("FocusedCliAndMcpParityShouldConstructEquivalentWorkflowCalls");
        mcp.ShouldContain("ChatBotSurfaceOrigin.Mcp");
        mcp.ShouldContain("ReadToolsUseClientFacadeReadMethodsOnly");
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

    private sealed record Story12GateRow(
        string Surface,
        IReadOnlyList<string> E2ETestPaths,
        IReadOnlyList<string> RequiredMarkers);
}
