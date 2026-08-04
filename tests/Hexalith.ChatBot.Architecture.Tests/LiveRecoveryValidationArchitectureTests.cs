using Hexalith.ChatBot.Server.Audit;

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
        driver.ShouldContain("FreshPartitionTenant(testTenantRef, dataset.ValidationPartitionRef, correlationId)");
        driver.ShouldContain("RebuildPartitionThroughRealHandlerAsync");
        driver.ShouldContain("AssertPartitionAbsentAsync");
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
        sandbox.ShouldContain("RecoverySandboxAuthorization.Authorized(");
        sandbox.ShouldContain("X-Recovery-Controller-Secret");
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

        // Keyed by COMMIT. Repository-wide let a third push inside the window cancel the older PENDING required check;
        // keying by REF did not fix that, because every push to `main` carries the same github.ref, so the pending run
        // was still cancelled and semantic-release still skipped silently for that commit. Ref-keying separates only
        // different release branches. Asserting the absence of the ref-keyed form keeps the weaker shape from returning.
        release.ShouldContain("live-recovery-validation-release-${{ github.repository }}-${{ github.sha }}");
        release.ShouldNotContain("live-recovery-validation-release-${{ github.repository }}-${{ github.ref }}");

        // Branch protection matches required checks by job NAME. Identical names across the two workflows made the CI
        // copy — which is SKIPPED on every push and pull request — indistinguishable from the release gate.
        foreach (string ciJobName in new[]
        {
            "name: scheduled live recovery validation sweep",
            "name: scheduled live recovery evidence gate",
        })
        {
            ci.ShouldContain(ciJobName);
            release.ShouldNotContain(ciJobName);
        }

        release.ShouldContain("name: required live recovery validation sweep");
        release.ShouldContain("name: required independent live recovery evidence gate");
        release.ShouldNotContain("name: required serialized live recovery evidence gate");

        // The gate must be evaluated OUT OF PROCESS, in a job chained after the run that produced the evidence. The
        // previous shape of this guard asserted only the producing test's own name, so it would have passed happily
        // while the only "gate" was the run grading its own homework.
        foreach (string workflow in new[] { ".github/workflows/ci.yml", ".github/workflows/release.yml" })
        {
            string source = ReadProjectFile(workflow);
            source.ShouldContain("live-recovery-evidence-gate:");
            source.ShouldContain("needs: live-recovery-validation");
            // Producer failure must still reach the independent judge; skip only when the producer was skipped.
            source.ShouldContain("if: always() && needs.live-recovery-validation.result != 'skipped'");
            source.ShouldContain("actions/download-artifact@v4");
            source.ShouldContain("RetainedLiveRecoveryEvidenceShouldPassTheReleaseGateOutOfProcess");
            source.ShouldContain("HEXALITH_CHATBOT_RECOVERY_EVIDENCE_REQUIRED");

            // `dotnet test --filter` EXITS 0 when the filter matches nothing, so renaming or moving either lane's test
            // turned a required job into a silent no-op that still reported success. Count per producer+gate invocation
            // rather than a single whole-file Contains (which stays green if only one job keeps the flag).
            int settingsUses = CountOccurrences(source, "--settings live-recovery.runsettings");
            settingsUses.ShouldBeGreaterThanOrEqualTo(2);

            // The release path anchors what the run may claim about itself; without these the run still declared its
            // own dataset size and which tree its evidence came from.
            source.ShouldContain("HEXALITH_CHATBOT_RECOVERY_REQUIRED_COMMIT: ${{ github.sha }}");
            source.ShouldContain("HEXALITH_CHATBOT_RECOVERY_MINIMUM_DATASET_VOLUME");
            source.ShouldContain("HEXALITH_CHATBOT_RECOVERY_MAX_MEASURABLE_CEILING_SECONDS");
            source.ShouldContain("HEXALITH_CHATBOT_RECOVERY_EXPECTED_DATASETS: recovery-baseline");
            source.ShouldContain("HEXALITH_CHATBOT_RECOVERY_EXPECTED_DATASET_VERSION: v1");
            source.ShouldContain("HEXALITH_CHATBOT_RECOVERY_MAX_EVIDENCE_AGE_HOURS: \"192\"");
            source.ShouldContain("retention-days: 30");

            // Least privilege on the destructive lane and its gate.
            source.ShouldContain("permissions:\n      contents: read");
        }

        // The release must depend on the independent gate, not merely on the run that produced the evidence.
        release.ShouldContain("- live-recovery-evidence-gate");

        string runSettings = ReadProjectFile("live-recovery.runsettings");
        runSettings.ShouldContain("<TreatNoTestsAsError>true</TreatNoTestsAsError>");
    }

    [Fact]
    public static void RunnerBudgetAndCadence_ShouldMatchTheWorkflowValuesTheyDescribe()
    {
        // Both options existed only inside their own validator: nothing tied RunnerBudget to the workflow's
        // timeout-minutes or Cadence to the cron, in either direction, so lowering timeout-minutes left Validate()
        // happily approving a WorkflowTimeout past the point the runner kills the job mid-injection.
        LiveRecoveryValidationOptions defaults = new();
        string ci = ReadProjectFile(".github/workflows/ci.yml");
        string release = ReadProjectFile(".github/workflows/release.yml");

        string expectedTimeout = $"timeout-minutes: {(int)defaults.RunnerBudget.TotalMinutes}";
        ci.ShouldContain(expectedTimeout);
        release.ShouldContain(expectedTimeout);

        // The cron fires weekly; Cadence must agree or the configured value governs nothing.
        ci.ShouldContain("cron: \"0 2 * * 0\"");
        defaults.Cadence.ShouldBe(TimeSpan.FromDays(7));

        // MaximumEvidenceAge defaults to 8 days; both gate jobs must pin the same hour budget.
        string expectedAge = $"HEXALITH_CHATBOT_RECOVERY_MAX_EVIDENCE_AGE_HOURS: \"{(int)defaults.MaximumEvidenceAge.TotalHours}\"";
        ci.ShouldContain(expectedAge);
        release.ShouldContain(expectedAge);
        defaults.MaximumEvidenceAge.ShouldBe(TimeSpan.FromDays(8));
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

    private static int CountOccurrences(string source, string needle)
    {
        int count = 0;
        int index = 0;
        while ((index = source.IndexOf(needle, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += needle.Length;
        }

        return count;
    }

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
