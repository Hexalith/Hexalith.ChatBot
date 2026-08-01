using Shouldly;

namespace Hexalith.ChatBot.Architecture.Tests;

public static class LiveRecoveryValidationArchitectureTests
{
    [Fact]
    public static void LiveRecoveryValidationAdr_ShouldKeepFaultAuthorityAtTheTierThreeBoundary()
    {
        string adr = ReadProjectFile("docs/adrs/live-recovery-validation-drivers.md");

        // The ADR's lifecycle status is deliberately NOT pinned here. Pinning the exact string "Accepted (...)" meant
        // that correcting the status to `Proposed` — the right move while the story is still in-progress — broke the
        // build, so the guard locked in a status the engineering state did not support. Assert only that the story is
        // named and that a recognised status is declared; the architecture invariants below are what this guard exists
        // to protect.
        bool declaresStatus =
            adr.Contains("Proposed (2026-08-01, Story 12.15)", StringComparison.Ordinal) ||
            adr.Contains("Accepted (2026-08-01, Story 12.15)", StringComparison.Ordinal);

        declaresStatus.ShouldBeTrue("the live-recovery ADR must declare either Proposed or Accepted status for Story 12.15");
        adr.ShouldContain("Tier-3 orchestration boundary owns every fault and restoration action");
        adr.ShouldContain("ResourceCommandService");
        adr.ShouldContain("KnownResourceCommands.StopCommand");
        adr.ShouldContain("KnownResourceCommands.StartCommand");
        adr.ShouldContain("KnownResourceCommands.RestartCommand");
        adr.ShouldContain("production Server never receives AppHost or DCP resource-lifecycle authority");
        adr.ShouldContain("ReplayTenantPolicy.IsTestTenant");
        adr.ShouldContain("replay-test:");
        adr.ShouldContain("closed scenario tokens");
        adr.ShouldContain("restoration runs in `finally`");
    }

    [Fact]
    public static void LiveRecoveryValidationAdr_ShouldDefineAClosedScenarioMechanismMatrix()
    {
        string adr = ReadProjectFile("docs/adrs/live-recovery-validation-drivers.md");

        foreach (string scenario in new[]
        {
            "eventstore-outage",
            "m365-subscription-failure",
            "graph",
            "identity",
            "ai-provider",
            "command-execution",
            "audit-store",
            "attachment-processing",
            "projection-rebuild",
        })
        {
            adr.ShouldContain($"`{scenario}`");
        }

        adr.ShouldContain("Faulted boundary");
        adr.ShouldContain("Observed-fault proof");
        adr.ShouldContain("Recovery proof");
        adr.ShouldContain("Production-equivalence residual");
        adr.ShouldContain("zero scenario coverage");
        adr.ShouldContain("Unmeasurable");
    }

    [Fact]
    public static void LiveRecoveryValidationAdr_ShouldRecordSandboxSuitabilityAndEvidenceRetentionLimits()
    {
        string adr = ReadProjectFile("docs/adrs/live-recovery-validation-drivers.md");

        adr.ShouldContain("Conditionally suitable");
        adr.ShouldContain("no live Graph/M365 resource");
        adr.ShouldContain("no hosted Workers resource");
        adr.ShouldContain("process-local `InMemoryWormAuditStore`");
        adr.ShouldContain("no production AKS or multi-replica control plane");
        adr.ShouldContain("metadata-only");
        adr.ShouldContain("30-day retention policy");
        adr.ShouldContain("raw `.trx` plus generated reports/manifests");
        adr.ShouldContain("must not be described as production-equivalent");
    }

    [Fact]
    public static void ProductionServer_ShouldNotReferenceAspireResourceLifecycleAuthority()
    {
        string root = RepositoryRoot();
        string[] violations = Directory
            .EnumerateFiles(Path.Combine(root, "src", "Hexalith.ChatBot.Server"), "*.cs", SearchOption.AllDirectories)
            .Where(static path => !path.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar, StringComparison.Ordinal)
                && !path.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar, StringComparison.Ordinal))
            .Where(path =>
            {
                string source = File.ReadAllText(path);
                return source.Contains("ResourceCommandService", StringComparison.Ordinal)
                    || source.Contains("KnownResourceCommands", StringComparison.Ordinal)
                    || source.Contains("Aspire.Hosting", StringComparison.Ordinal);
            })
            .Select(path => Path.GetRelativePath(root, path))
            .ToArray();

        violations.ShouldBeEmpty();
    }

    [Fact]
    public static void LiveProjectionRebuildDriver_ShouldUseOnlyImmutableTenantSourceAndWormReads()
    {
        string driver = ReadProjectFile("tests/Hexalith.ChatBot.IntegrationTests/Recovery/LiveProjectionRebuildDriver.cs");

        driver.ShouldContain("IReadOnlyList<ProjectConversationSourceEmailView> immutableSourceRecords");
        driver.ShouldContain("wormAuditStore.EnumerateChain(testTenantRef)");
        driver.ShouldContain("ReplayTenantPolicy.IsTestTenant(testTenantRef)");
        driver.ShouldContain("freshPartition");
        driver.ShouldContain("ReadModelProjectConversationProjectionStore");
        driver.ShouldContain("ReadModelGovernedOperationViewStore");
        driver.ShouldContain("TryEraseAsync");
        driver.ShouldNotContain("EnumerateTenants(");
        driver.ShouldNotContain("AppendAsync(");
        driver.ShouldNotContain("GraphServiceClient");
        driver.ShouldNotContain("CaptureMailboxMessageIntake");
        driver.ShouldNotContain("PartyClient");
        driver.ShouldNotContain("FolderClient");
    }

    [Fact]
    public static void RecoverySandbox_ShouldBeDoublyOptedInClosedAndNonExternal()
    {
        string appHost = ReadProjectFile("src/Hexalith.ChatBot.AppHost/Program.cs");
        string appHostProject = ReadProjectFile("src/Hexalith.ChatBot.AppHost/Hexalith.ChatBot.AppHost.csproj");
        string composer = ReadProjectFile("tests/Hexalith.ChatBot.IntegrationTests/Recovery/RecoverySandboxTopologyComposer.cs");
        string sandbox = ReadProjectFile("tests/Hexalith.ChatBot.RecoverySandbox/Program.cs");

        appHost.ShouldNotContain("recovery-sandbox");
        appHostProject.ShouldNotContain("Hexalith.ChatBot.RecoverySandbox");
        composer.ShouldContain("this IDistributedApplicationTestingBuilder builder");
        composer.ShouldContain("AddProject(\"recovery-sandbox\", projectPath)");
        composer.ShouldContain("LiveRecoveryValidationOptions.AspireControllerCapability");
        composer.ShouldContain("ReplayTenantPolicy.IsTestTenant(tenantRef)");
        // Aspire gives the AppHost no compile reference to Server types, so the tenant prefix is necessarily
        // repeated at the composition boundary. Pin it to the single authoritative constant so it cannot drift.
        string policy = ReadProjectFile("src/Hexalith.ChatBot.Server/Audit/ReplayTenantPolicy.cs");
        int prefixStart = policy.IndexOf("ReplayTestTenantPrefix = \"", StringComparison.Ordinal);
        prefixStart.ShouldBeGreaterThan(-1);
        prefixStart += "ReplayTestTenantPrefix = \"".Length;
        string prefix = policy[prefixStart..policy.IndexOf('"', prefixStart)];
        prefix.ShouldBe("replay-test:");

        // Doubly opted in: `Enabled=true` alone must never compose the destructive sandbox.
        composer.ShouldContain("Development");
        composer.ShouldContain("Testing");
        composer.ShouldNotContain("WithExternalHttpEndpoints");
        sandbox.ShouldContain("RecoveryScopedOutageState.Contains(dependency)");
        sandbox.ShouldContain("Authorized(request, requestedTenant, tenantRef, controllerSecret)");
        sandbox.ShouldNotContain("{resource}");
    }

    [Fact]
    public static void RequiredWorkflows_ShouldInvokeAndAlwaysRetainTheLiveEvidenceGate()
    {
        foreach (string workflow in new[] { ".github/workflows/ci.yml", ".github/workflows/release.yml" })
        {
            string source = ReadProjectFile(workflow);
            source.ShouldContain("live-recovery-validation:");
            source.ShouldContain("timeout-minutes: 330");
            source.ShouldContain("cancel-in-progress: false");
            source.ShouldContain("-m:1");
            source.ShouldContain("LiveRecoveryValidationRunsAllThreeCoordinatorsAndPassesEvidenceGate");
            source.ShouldContain("if: always()");
            source.ShouldContain("path: TestResults");
        }

        string ci = ReadProjectFile(".github/workflows/ci.yml");
        ci.ShouldContain("schedule:");
        ci.ShouldContain("workflow_dispatch:");
        ci.ShouldContain("github.event_name == 'schedule' || github.event_name == 'workflow_dispatch'");
        ci.ShouldContain("live-recovery-validation-ci-");

        string release = ReadProjectFile(".github/workflows/release.yml");
        release.ShouldContain("live-recovery-validation-release-");

        // Keyed by ref, not repository-wide: a repository-wide group let a third push inside the window cancel the
        // older PENDING required check and silently skip semantic-release for that commit.
        release.ShouldContain("live-recovery-validation-release-${{ github.repository }}-${{ github.ref }}");

        // The gate must be evaluated OUT OF PROCESS, in a job chained after the run that produced the evidence. The
        // previous shape of this guard asserted only the producing test's own name, so it would have passed happily
        // while the only "gate" was the run grading its own homework.
        foreach (string workflow in new[] { ".github/workflows/ci.yml", ".github/workflows/release.yml" })
        {
            string source = ReadProjectFile(workflow);
            source.ShouldContain("live-recovery-evidence-gate:");
            source.ShouldContain("needs: live-recovery-validation");
            source.ShouldContain("actions/download-artifact@v4");
            source.ShouldContain("RetainedLiveRecoveryEvidenceShouldPassTheReleaseGateOutOfProcess");
            source.ShouldContain("HEXALITH_CHATBOT_RECOVERY_EVIDENCE_REQUIRED");

            // Least privilege on the destructive lane and its gate.
            source.ShouldContain("permissions:\n      contents: read");
        }

        // The release must depend on the independent gate, not merely on the run that produced the evidence.
        release.ShouldContain("- live-recovery-evidence-gate");
    }

    [Fact]
    public static void FaultControlTypes_ShouldNotLeakIntoContractsDomainOrUi()
    {
        string root = RepositoryRoot();
        string[] guardedProjects =
        [
            "Hexalith.ChatBot.Contracts",
            "Hexalith.ChatBot.UI",
            "Hexalith.ChatBot.Client",
        ];
        string[] forbidden =
        [
            "ResourceCommandService",
            "KnownResourceCommands",
            "Aspire.Hosting",
            "LiveRecoveryValidationOptions",
            "IScopedOutageInjectionDriver",
            "IContinuityDrillScenarioRunner",
            "IProjectionRebuildDriver",
        ];
        string[] violations = guardedProjects
            .Select(project => Path.Combine(root, "src", project))
            .Where(Directory.Exists)
            .SelectMany(directory => Directory.EnumerateFiles(directory, "*.cs", SearchOption.AllDirectories))
            .Where(static path => !path.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar, StringComparison.Ordinal)
                && !path.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar, StringComparison.Ordinal))
            .Where(path =>
            {
                string source = File.ReadAllText(path);
                return forbidden.Any(token => source.Contains(token, StringComparison.Ordinal));
            })
            .Select(path => Path.GetRelativePath(root, path))
            .ToArray();

        violations.ShouldBeEmpty();
    }

    [Fact]
    public static void ScopedOutageSweepOrder_ShouldBeDeterministicWithIdentityLast()
    {
        // Identity stops the topology's security root, so it must run last. An IReadOnlySet cannot express that:
        // HashSet<T> enumeration order is an unspecified implementation detail. This assembly has no access to
        // Server internals, so the ordering contract is verified from source.
        string dependencies = ReadProjectFile("src/Hexalith.ChatBot.Server/Audit/ScopedOutageDependencies.cs");
        dependencies.ShouldContain("IReadOnlyList<string> SweepOrder");
        dependencies.ShouldContain("new HashSet<string>(SweepOrder, StringComparer.Ordinal)");

        int sweepOrderStart = dependencies.IndexOf("SweepOrder =", StringComparison.Ordinal);
        sweepOrderStart.ShouldBeGreaterThan(-1);
        int sweepOrderEnd = dependencies.IndexOf("];", sweepOrderStart, StringComparison.Ordinal);
        sweepOrderEnd.ShouldBeGreaterThan(sweepOrderStart);
        string sweepOrder = dependencies[sweepOrderStart..sweepOrderEnd];
        sweepOrder.IndexOf("Identity", StringComparison.Ordinal)
            .ShouldBeGreaterThan(sweepOrder.IndexOf("AttachmentProcessing", StringComparison.Ordinal));

        // A duplicate in SweepOrder dedupes silently into All, so the sweep would inject one outage twice while the
        // gate saw one manifest too many and reported incomplete_scenario_set on every run thereafter.
        dependencies.ShouldContain("closed.Count == SweepOrder.Count");

        ReadProjectFile("src/Hexalith.ChatBot.Server/Audit/ScopedOutageDegradationValidationCoordinator.cs")
            .ShouldContain("foreach (string dependency in ScopedOutageDependencies.SweepOrder)");

        // The continuity sweep is equally destructive and was left on HashSet enumeration order when its sibling was
        // hardened. Both destructive sweeps must be ordered.
        ReadProjectFile("src/Hexalith.ChatBot.Server/Audit/ContinuityDrillScenarios.cs")
            .ShouldContain("IReadOnlyList<string> SweepOrder");
        ReadProjectFile("src/Hexalith.ChatBot.Server/Audit/ContinuityDrillCoordinator.cs")
            .ShouldContain("foreach (string scenario in ContinuityDrillScenarios.SweepOrder)");
    }

    private static string ReadProjectFile(string relativePath)
        => File.ReadAllText(Path.Combine(RepositoryRoot(), relativePath));

    private static string RepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Hexalith.ChatBot.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("Could not locate repository root.");
    }
}
