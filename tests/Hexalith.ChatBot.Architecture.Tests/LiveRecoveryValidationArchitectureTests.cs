using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml.Linq;

using Hexalith.ChatBot.Server.Audit;

using Shouldly;

namespace Hexalith.ChatBot.Architecture.Tests;

public static class LiveRecoveryValidationArchitectureTests
{
    [Fact]
    public static void LiveRecoveryFaultVocabularies_ShouldBeClosedAndDeterministicallyOrdered()
    {
        string[] expectedDependencies =
        [
            ScopedOutageDependencies.Graph,
            ScopedOutageDependencies.AiProvider,
            ScopedOutageDependencies.CommandExecution,
            ScopedOutageDependencies.AuditStore,
            ScopedOutageDependencies.AttachmentProcessing,
            ScopedOutageDependencies.Identity,
        ];
        string[] expectedContinuityScenarios =
        [
            ContinuityDrillScenarios.EventStoreOutage,
            ContinuityDrillScenarios.M365SubscriptionFailure,
        ];

        ScopedOutageDependencies.SweepOrder.ShouldBe(expectedDependencies);
        ScopedOutageDependencies.All.SetEquals(expectedDependencies).ShouldBeTrue();
        ContinuityDrillScenarios.SweepOrder.ShouldBe(expectedContinuityScenarios);
        ContinuityDrillScenarios.All.SetEquals(expectedContinuityScenarios).ShouldBeTrue();
        ScopedOutageDependencies.Contains("unknown-dependency").ShouldBeFalse();
        ContinuityDrillScenarios.Contains("unknown-scenario").ShouldBeFalse();
        LiveRecoveryValidationOptions.MinimumSweepScenarioCount.ShouldBe(9);
    }

    [Fact]
    public static void LiveRecoveryEvidenceGate_ShouldRequireEachJobsBehavioralProof()
    {
        LiveRecoveryValidationEvidenceGate.RequiredAssertionsFor(LiveRecoveryValidationJobs.Continuity)
            .ShouldContain("state-reconstructable");
        LiveRecoveryValidationEvidenceGate.RequiredAssertionsFor(LiveRecoveryValidationJobs.ProjectionRebuild)
            .ShouldContain("structurally-equivalent");
        LiveRecoveryValidationEvidenceGate.RequiredAssertionsFor(LiveRecoveryValidationJobs.ScopedOutage)
            .ShouldContain("scope-contained");

        LiveRecoveryValidationEvidenceGate.CanonicalTargetsFor(LiveRecoveryValidationJobs.Continuity)["rto"]
            .ShouldBe(RecoveryTargets.MaxRto.TotalSeconds);
        LiveRecoveryValidationEvidenceGate.CanonicalTargetsFor(LiveRecoveryValidationJobs.ProjectionRebuild)["rebuild-duration"]
            .ShouldBe(RecoveryTargets.MaxRto.TotalSeconds);
        LiveRecoveryValidationEvidenceGate.CanonicalTargetsFor(LiveRecoveryValidationJobs.ScopedOutage)["scope-recording-latency"]
            .ShouldBe(RecoveryTargets.MaxScopeRecordingLatency.TotalSeconds);
    }

    [Fact]
    public static void LiveRecoveryReleasePolicy_ShouldRejectUnpinnedDatasetEvidence()
    {
        _ = Should.Throw<ArgumentException>(() => LiveRecoveryValidationGatePolicy.ForRelease(
            ["recovery-baseline"],
            targetDeviationsBlockRelease: true,
            requiredDriverMode: RecoveryValidationEvidenceManifest.LiveDriverMode,
            maximumEvidenceAge: TimeSpan.FromDays(8),
            expectedDatasetVersion: null,
            minimumDatasetVolume: 6,
            maximumMeasurableRecoveryCeilingSeconds: 180,
            requiredRepositoryCommit: new string('a', 40)));
        _ = Should.Throw<ArgumentOutOfRangeException>(() => LiveRecoveryValidationGatePolicy.ForRelease(
            ["recovery-baseline"],
            targetDeviationsBlockRelease: true,
            requiredDriverMode: RecoveryValidationEvidenceManifest.LiveDriverMode,
            maximumEvidenceAge: TimeSpan.FromDays(8),
            expectedDatasetVersion: "v1",
            minimumDatasetVolume: 0,
            maximumMeasurableRecoveryCeilingSeconds: 180,
            requiredRepositoryCommit: new string('a', 40)));
        _ = Should.Throw<ArgumentException>(() => LiveRecoveryValidationGatePolicy.ForRelease(
            ["recovery-baseline"],
            targetDeviationsBlockRelease: true,
            requiredDriverMode: RecoveryValidationEvidenceManifest.LiveDriverMode,
            maximumEvidenceAge: TimeSpan.FromDays(8),
            expectedDatasetVersion: "v1",
            minimumDatasetVolume: 6,
            maximumMeasurableRecoveryCeilingSeconds: 180,
            requiredRepositoryCommit: null));
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
        int prefixEnd = policy.IndexOf('"', prefixStart);
        prefixEnd.ShouldBeGreaterThan(-1);
        string prefix = policy[prefixStart..prefixEnd];
        prefix.ShouldBe("replay-test:");

        // Doubly opted in: `Enabled=true` alone must never compose the destructive sandbox.
        Regex.IsMatch(
            composer,
            @"\(!string\.Equals\(environmentName,\s*""Development"",\s*StringComparison\.OrdinalIgnoreCase\)\s*&&\s*!string\.Equals\(environmentName,\s*""Testing"",\s*StringComparison\.OrdinalIgnoreCase\)\)",
            RegexOptions.CultureInvariant).ShouldBeTrue("Development and Testing must be joined by AND in the fail-closed environment guard.");
        composer.ShouldNotContain("WithExternalHttpEndpoints");
        sandbox.ShouldContain("RecoveryScopedOutageState.Contains(dependency)");
        sandbox.ShouldContain("RecoverySandboxAuthorization.Authorized(");
        sandbox.ShouldContain("X-Recovery-Controller-Secret");
        sandbox.ShouldNotContain("{resource}");
    }

    /// <summary>
    /// The attempt envelope must be written where <c>actions/checkout</c> cannot delete it.
    /// </summary>
    /// <remarks>
    /// It was written to a workspace-relative <c>TestResults/</c> path immediately before checkout, which deletes
    /// every entry in the workspace when no <c>.git</c> is present. The always-run finalize step then ran
    /// <c>jq</c> on a missing file under <c>set -euo pipefail</c>, reddening both producer jobs on every run --
    /// including a run whose drill passed. Presence assertions could not see it; only the path can.
    /// </remarks>
    [Fact]
    public static void ProducerAttemptEnvelope_ShouldSurviveCheckout()
    {
        foreach (string workflow in new[] { ".github/workflows/ci.yml", ".github/workflows/release.yml" })
        {
            string source = ReadProjectFile(workflow);
            source.ShouldContain("ATTEMPT_PATH: ${{ runner.temp }}/workflow-attempt/producer-attempt.json");
            source.ShouldNotContain("ATTEMPT_PATH: TestResults/");
            foreach (Match match in Regex.Matches(source, @"ATTEMPT_PATH: (?<path>.+)"))
            {
                match.Groups["path"].Value.StartsWith("${{ runner.temp }}", StringComparison.Ordinal)
                    .ShouldBeTrue(
                        "a pre-checkout marker written into the workspace is deleted by actions/checkout.");
            }
        }
    }

    /// <summary>
    /// Every recovery producer must bound itself strictly inside its external interrupt.
    /// </summary>
    /// <remarks>
    /// The in-process deadline exists so a hung scenario unwinds its own fault in <c>finally</c>. When it equals
    /// the external <c>timeout</c> bound the signal always wins -- the in-process clock starts later, inside
    /// <c>dotnet test</c> -- and the guard is unreachable. The scheduled and release producers had no in-process
    /// bound, no wrapper and no step timeout at all.
    /// </remarks>
    [Fact]
    public static void RecoveryProducers_ShouldBoundInProcessDeadlineStrictlyInsideTheExternalInterrupt()
    {
        foreach (string workflow in new[] { ".github/workflows/ci.yml", ".github/workflows/release.yml" })
        {
            string source = ReadProjectFile(workflow);
            MatchCollection declared = Regex.Matches(
                source,
                @"HEXALITH_CHATBOT_RECOVERY_WORKFLOW_TIMEOUT_MINUTES: ""(?<minutes>\d+)""");
            declared.Count.ShouldBeGreaterThan(
                0,
                $"{workflow} must bound its recovery producer in-process.");
            foreach (Match match in declared)
            {
                int inProcessMinutes = int.Parse(match.Groups["minutes"].Value, CultureInfo.InvariantCulture);
                inProcessMinutes.ShouldBeLessThan(
                    280,
                    "the in-process deadline must expire before the external SIGINT, or it can never fire.");
            }

            source.ShouldContain("timeout --signal=INT --kill-after=15m");
        }
    }

    /// <summary>
    /// The only current-run lane produced inside the completion job must be produced after the drill.
    /// </summary>
    /// <remarks>
    /// Inserting an up-to-four-hour producer ahead of the attestation clock pushed every earlier current-run lane
    /// past the 60-minute freshness ceiling, so the completion path failed <c>evidence_stale_or_unbound</c> on
    /// lanes that had passed. Ordering fixes the in-job lane; the upstream lanes carry an explicit per-lane
    /// ceiling instead.
    /// </remarks>
    [Fact]
    public static void CompletionJob_ShouldProduceItsInJobCurrentRunLaneAfterTheRecoveryProducer()
    {
        string ci = ReadProjectFile(".github/workflows/ci.yml");
        int producerIndex = ci.IndexOf(
            "- name: Produce transition-declared current recovery primary result",
            StringComparison.Ordinal);
        int selfTestIndex = ci.IndexOf(
            "- name: Run gate self-tests with current-run TRX",
            StringComparison.Ordinal);
        producerIndex.ShouldBeGreaterThan(0);
        selfTestIndex.ShouldBeGreaterThan(
            producerIndex,
            "the in-job current-run TRX must be produced after the drill, or it is hours stale at attestation.");
    }

    /// <summary>
    /// A lane's freshness ceiling must cover the producer budget the workflow grants it.
    /// </summary>
    /// <remarks>
    /// These two numbers live in different files and were only ever asserted independently, so nothing noticed
    /// that the workflow authorized a producer four times longer than the evidence it produced was allowed to be.
    /// </remarks>
    [Fact]
    public static void RecoveryLaneCeiling_ShouldCoverTheWorkflowProducerBudget()
    {
        string policy = ReadProjectFile("story-evidence-policy.json");
        string ci = ReadProjectFile(".github/workflows/ci.yml");
        int laneCeiling = Regex
            .Matches(policy, @"""maximumCurrentRunAgeMinutes"": (?<minutes>\d+)")
            .Select(match => int.Parse(match.Groups["minutes"].Value, CultureInfo.InvariantCulture))
            .Max();
        int longestProducer = Regex
            .Matches(ci, @"HEXALITH_CHATBOT_RECOVERY_WORKFLOW_TIMEOUT_MINUTES: ""(?<minutes>\d+)""")
            .Select(match => int.Parse(match.Groups["minutes"].Value, CultureInfo.InvariantCulture))
            .Max();

        laneCeiling.ShouldBeGreaterThan(
            longestProducer,
            "recovery evidence would be stale on arrival if its ceiling did not exceed the producer budget.");
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
            source.ShouldContain("Initialize metadata-only live recovery attempt envelope");
            source.ShouldContain("Finalize metadata-only live recovery attempt envelope");
            source.ShouldContain("live-recovery-producer-attempt");
            source.ShouldContain("if-no-files-found: error");
            source.ShouldContain("actions/upload-artifact@v7");
            source.ShouldContain("actions/download-artifact@v8");
            source.ShouldContain("bash .github/scripts/install-dapr-cli.sh");
            source.ShouldContain("dapr init --runtime-version 1.18.0");
            source.ShouldNotContain("dapr/setup-dapr@v2");
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
            source.ShouldContain("actions/download-artifact@v8");
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

        // Three layers, because each is strictly weaker than the lane's real requirement:
        //   1. the document parses (catches the double-hyphen digraph that broke every lane), and
        //   2. TreatNoTestsAsError is actually true in the parsed tree, and
        //   3. the TEST PLATFORM accepts it — a well-formed but schema-invalid settings file reproduces the same
        //      "guard green, required lane dead" class, so parsing alone is not the bar.
        // Layer 3 lives in RunSettingsIsAcceptedByTheTestPlatform below.
        string runSettingsPath = Path.Combine(RepositoryRoot(), "live-recovery.runsettings");
        XDocument runSettings = Should.NotThrow(() => XDocument.Load(runSettingsPath, LoadOptions.None));
        XElement? treatNoTestsAsError = runSettings
            .Element("RunSettings")?
            .Element("RunConfiguration")?
            .Element("TreatNoTestsAsError");
        treatNoTestsAsError.ShouldNotBeNull();
        bool.TryParse(treatNoTestsAsError.Value.Trim(), out bool treatNoTestsAsErrorValue).ShouldBeTrue();
        treatNoTestsAsErrorValue.ShouldBeTrue();
    }

    /// <summary>
    /// Proves the test platform itself accepts the run settings, not merely that it is well-formed XML.
    /// </summary>
    /// <remarks>
    /// The lane needs `dotnet test --settings live-recovery.runsettings` to start. A file can parse cleanly and
    /// still be rejected ("Settings file provided does not conform to required format"), which is the same
    /// guard-green/lane-dead failure class the parse check was added for. This runs the real runner against the
    /// real file with a filter that matches nothing, and asserts the platform did not reject the settings.
    /// </remarks>
    [Fact]
    public static void RunSettingsIsAcceptedByTheTestPlatform()
    {
        string repositoryRoot = RepositoryRoot();
        using Process process = new()
        {
            StartInfo = new ProcessStartInfo("dotnet")
            {
                WorkingDirectory = repositoryRoot,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            },
        };
        foreach (string argument in new[]
        {
            "test",
            Path.Combine("tests", "Hexalith.ChatBot.Architecture.Tests", "Hexalith.ChatBot.Architecture.Tests.csproj"),
            "--no-build",
            "--filter",
            "FullyQualifiedName=Hexalith.ChatBot.Architecture.Tests.RunSettingsPlatformAcceptanceProbe",
            "--settings",
            "live-recovery.runsettings",
        })
        {
            process.StartInfo.ArgumentList.Add(argument);
        }

        _ = process.Start();
        string output = process.StandardOutput.ReadToEnd() + process.StandardError.ReadToEnd();
        _ = process.WaitForExit(TimeSpan.FromMinutes(5));

        output.ShouldNotContain(
            "does not conform to required format",
            Case.Insensitive,
            "the test platform rejected the run settings, so every lane that passes it dies before running.");
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
        ScopedOutageDependencies.SweepOrder[^1].ShouldBe(ScopedOutageDependencies.Identity);
        ScopedOutageDependencies.SweepOrder.Distinct(StringComparer.Ordinal).Count()
            .ShouldBe(ScopedOutageDependencies.SweepOrder.Count);
        ScopedOutageDependencies.All.SetEquals(ScopedOutageDependencies.SweepOrder).ShouldBeTrue();

        ContinuityDrillScenarios.SweepOrder.Distinct(StringComparer.Ordinal).Count()
            .ShouldBe(ContinuityDrillScenarios.SweepOrder.Count);
        ContinuityDrillScenarios.All.SetEquals(ContinuityDrillScenarios.SweepOrder).ShouldBeTrue();
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
