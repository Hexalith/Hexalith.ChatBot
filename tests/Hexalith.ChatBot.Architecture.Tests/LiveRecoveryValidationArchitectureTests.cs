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
        ContinuityDrillScenarios.All.ShouldNotContain(ControlledLossPathReport.SubscriptionNotificationRejectionScenario);
        LiveRecoveryValidationJobs.All.SetEquals(
        [
            LiveRecoveryValidationJobs.Continuity,
            LiveRecoveryValidationJobs.ControlledLossPath,
            LiveRecoveryValidationJobs.ProjectionRebuild,
            LiveRecoveryValidationJobs.ScopedOutage,
        ]).ShouldBeTrue();
        LiveRecoveryValidationOptions.MinimumSweepScenarioCount.ShouldBe(10);
    }

    [Fact]
    public static void LiveRecoveryEvidenceGate_ShouldRequireEachJobsBehavioralProof()
    {
        LiveRecoveryValidationEvidenceGate.RequiredAssertionsFor(LiveRecoveryValidationJobs.Continuity)
            .ShouldContain("state-reconstructable");
        LiveRecoveryValidationEvidenceGate.RequiredAssertionsFor(LiveRecoveryValidationJobs.ControlledLossPath)
            .ShouldContain("durable-bounds-valid");
        LiveRecoveryValidationEvidenceGate.RequiredAssertionsFor(LiveRecoveryValidationJobs.ProjectionRebuild)
            .ShouldContain("structurally-equivalent");
        LiveRecoveryValidationEvidenceGate.RequiredAssertionsFor(LiveRecoveryValidationJobs.ScopedOutage)
            .ShouldContain("scope-contained");

        LiveRecoveryValidationEvidenceGate.CanonicalTargetsFor(LiveRecoveryValidationJobs.Continuity)["rto"]
            .ShouldBe(RecoveryTargets.MaxRto.TotalSeconds);
        LiveRecoveryValidationEvidenceGate.CanonicalTargetsFor(LiveRecoveryValidationJobs.ControlledLossPath)["rpo"]
            .ShouldBe(RecoveryTargets.MaxRpo.TotalSeconds);
        LiveRecoveryValidationEvidenceGate.CanonicalTargetsFor(LiveRecoveryValidationJobs.ProjectionRebuild)["rebuild-duration"]
            .ShouldBe(RecoveryTargets.MaxRto.TotalSeconds);
        LiveRecoveryValidationEvidenceGate.CanonicalTargetsFor(LiveRecoveryValidationJobs.ScopedOutage)["scope-recording-latency"]
            .ShouldBe(RecoveryTargets.MaxScopeRecordingLatency.TotalSeconds);
    }

    [Fact]
    public static void ProjectionRebuildTopology_ShouldKeepIndependentSeedAndProductionReplayPaths()
    {
        string root = RepositoryRoot();
        string e2e = File.ReadAllText(Path.Combine(
            root,
            "tests",
            "Hexalith.ChatBot.IntegrationTests",
            "Recovery",
            "LiveContinuityAspireE2eTests.cs"));
        string driver = File.ReadAllText(Path.Combine(
            root,
            "tests",
            "Hexalith.ChatBot.IntegrationTests",
            "Recovery",
            "LiveProjectionRebuildDriver.cs"));

        e2e.ShouldContain("RecoveryValidationDataset seedDataset");
        e2e.ShouldContain("RecoveryValidationDataset rebuildDataset");
        e2e.ShouldContain("InMemoryWormAuditStore seedWorm");
        e2e.ShouldContain("InMemoryWormAuditStore rebuildWorm");
        driver.ShouldContain("AuditOperationReconstructor.Reconstruct");
        driver.ShouldContain("GovernedOperationProjectionHandler governedHandler");
        driver.ShouldNotContain("ToGovernedOperationView");
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
        string notificationIdentity = ReadProjectFile(
            "tests/Hexalith.ChatBot.RecoverySandbox/RecoveryNotificationIdentity.cs");
        string graphSource = ReadProjectFile(
            "tests/Hexalith.ChatBot.RecoverySandbox/ControlledGraphMailboxMessageSource.cs");

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
        sandbox.ShouldContain("RecoveryNotificationIdentity.LossPhase");
        notificationIdentity.ShouldContain("ControlledLossLane = \"controlled-loss\"");
        notificationIdentity.ShouldContain("(ControlledLossLane, PreFaultPhase or LossPhase or PostRecoveryPhase)");
        graphSource.ShouldContain("controlledLossCandidate && !state.IsFaulted()");
        graphSource.ShouldContain("controlled_loss_fault_not_active");
        sandbox.ShouldContain("CapturingRecoveryChatBotClient");
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
            // Paired PER PRODUCER STEP, not globally. The previous form compared every in-process deadline against
            // the hardcoded literal 280, which described only the scheduled/release lane's 16800s SIGINT: lowering
            // 16800s failed nothing, and the completion lane -- whose interrupt is computed as
            // remaining_seconds - 900 and capped at 15900s (265 min) -- was never checked against its own bound at
            // all. A global comparison is also wrong in the other direction: the scheduled lane's 265-minute
            // deadline is legitimate against its own 280-minute SIGINT but would fail against the completion
            // lane's 265-minute cap. Each step is therefore matched to the external bound that governs it.
            string[] stepBlocks = source.Split("\n      - name: ", StringSplitOptions.None);
            int pairedProducers = 0;
            foreach (string block in stepBlocks)
            {
                Match inProcess = Regex.Match(
                    block,
                    "HEXALITH_CHATBOT_RECOVERY_WORKFLOW_TIMEOUT_MINUTES: \"(?<minutes>[0-9]+)\"");
                if (!inProcess.Success)
                {
                    continue;
                }

                block.Contains("timeout --signal=INT --kill-after=15m", StringComparison.Ordinal).ShouldBeTrue(
                    "every in-process-bounded recovery producer must carry an external interrupt of its own.");

                Match literal = Regex.Match(block, "--kill-after=15m (?<seconds>[0-9]+)s");
                Match capped = Regex.Match(block, "interrupt_after_seconds > (?<seconds>[0-9]+)");
                Match external = literal.Success ? literal : capped;
                external.Success.ShouldBeTrue(
                    "the external interrupt's own bound must be readable, or this guard asserts nothing.");

                int inProcessMinutes = int.Parse(inProcess.Groups["minutes"].Value, CultureInfo.InvariantCulture);
                int externalMinutes = int.Parse(external.Groups["seconds"].Value, CultureInfo.InvariantCulture) / 60;
                inProcessMinutes.ShouldBeLessThan(
                    externalMinutes,
                    "the in-process deadline must expire before THIS producer's own external SIGINT, or its "
                    + "cleanup guard can never fire.");
                pairedProducers++;
            }

            pairedProducers.ShouldBe(
                declared.Count,
                $"{workflow} must pair every in-process deadline with an external interrupt in the same step.");
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
    /// The completion job must build the gate tool before the first step that runs it with
    /// <c>--no-build</c>.
    /// </summary>
    /// <remarks>
    /// The gate self-tests are a full <c>dotnet test</c> and used to sit ahead of <c>plan</c>, supplying that
    /// build as a side effect. Moving them after the drill -- correct on its own terms, since their TRX is an
    /// in-job current-run lane -- removed the only build in the job, and <c>plan --no-build</c> then had no
    /// assembly to run. Nothing caught it: this job's steps are pinned by name and by relative position, and
    /// neither notion can see what a step builds. Pin the dependency itself.
    /// </remarks>
    [Fact]
    public static void CompletionJob_ShouldBuildTheGateToolBeforeItsFirstNoBuildInvocation()
    {
        string ci = ReadProjectFile(".github/workflows/ci.yml");
        int buildIndex = ci.IndexOf(
            "- name: Build the story-evidence gate tool",
            StringComparison.Ordinal);
        int firstNoBuildIndex = ci.IndexOf(
            "--configuration Release --no-build -- plan",
            StringComparison.Ordinal);
        buildIndex.ShouldBeGreaterThan(0, "the completion job must build the gate tool it later runs.");
        firstNoBuildIndex.ShouldBeGreaterThan(
            buildIndex,
            "plan runs the gate tool with --no-build, so a build of that tool must precede it in the job.");
    }

    /// <summary>
    /// EVERY declared primary lane must carry the per-lane ceiling, and it must cover the producer budget.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The previous form took <c>Max()</c> over every ceiling in the policy and <c>Max()</c> over every producer
    /// budget in the workflow and asserted one exceeded the other. That binds nothing to anything: dropping
    /// <c>recovery-primary</c> back to the global 60 while any unrelated lane kept 360 left it green, which is
    /// precisely the self-invalidation it was added to prevent.
    /// </para>
    /// <para>
    /// Ratified 2026-08-26: the ceiling is carried by ALL declared lanes, not only <c>recovery-primary</c>. The
    /// four upstream lanes finish in the <c>build</c> job and age by the whole drill, so they are the ones that
    /// actually need it; <c>recovery-primary</c> is sanitized minutes before attestation and needs it least. The
    /// count is asserted so a lane cannot quietly lose its ceiling.
    /// </para>
    /// </remarks>
    [Fact]
    public static void RecoveryLaneCeiling_ShouldCoverTheWorkflowProducerBudget()
    {
        string policy = ReadProjectFile("story-evidence-policy.json");
        string ci = ReadProjectFile(".github/workflows/ci.yml");

        int[] laneCeilings =
        [
            .. Regex
                .Matches(policy, "\"maximumCurrentRunAgeMinutes\": (?<minutes>[0-9]+)")
                .Select(match => int.Parse(match.Groups["minutes"].Value, CultureInfo.InvariantCulture)),
        ];
        int declaredLanes = Regex.Matches(policy, "\"recognizedLaneBindings\"").Count;

        // One global bound plus one per declared lane binding. A lane silently losing its ceiling would otherwise
        // fall back to the global 60 and fail stale on arrival after a multi-hour drill.
        laneCeilings.Length.ShouldBe(
            declaredLanes + 1,
            "every declared primary lane must carry an explicit per-lane current-run ceiling.");

        int longestProducer = Regex
            .Matches(ci, "HEXALITH_CHATBOT_RECOVERY_WORKFLOW_TIMEOUT_MINUTES: \"(?<minutes>[0-9]+)\"")
            .Select(match => int.Parse(match.Groups["minutes"].Value, CultureInfo.InvariantCulture))
            .Max();

        // The tightest per-lane ceiling, not the loosest: a lane whose ceiling is below the producer budget is
        // stale on arrival however generous some other lane's ceiling happens to be.
        laneCeilings
            .Where(ceiling => ceiling > 60)
            .Min()
            .ShouldBeGreaterThan(
                longestProducer,
                "every per-lane ceiling must exceed the longest producer budget the workflow authorizes, or that "
                + "lane's evidence is stale on arrival.");
    }

    [Fact]
    public static void RequiredWorkflows_ShouldInvokeAndAlwaysRetainTheLiveEvidenceGate()
    {
        foreach (string workflow in new[] { ".github/workflows/ci.yml", ".github/workflows/release.yml" })
        {
            string source = ReadProjectFile(workflow);
            Match producerJob = Regex.Match(
                source,
                "(?ms)^  live-recovery-validation:.*?(?=^  live-recovery-evidence-gate:)");
            producerJob.Success.ShouldBeTrue($"{workflow} must contain the live recovery producer job");
            Match uploadStep = Regex.Match(
                producerJob.Value,
                "(?ms)^      - name: Upload live recovery reports, manifests, and raw test evidence\\n"
                + ".*?(?=^      - name: |\\z)");
            uploadStep.Success.ShouldBeTrue($"{workflow} must contain the live recovery upload step");
            source.ShouldContain("live-recovery-validation:");
            source.ShouldContain("timeout-minutes: 330");
            source.ShouldContain("cancel-in-progress: false");
            source.ShouldContain("-m:1");
            source.ShouldContain("LiveRecoveryValidationRunsAllThreeCoordinatorsAndPassesEvidenceGate");
            uploadStep.Value.ShouldContain("if: always()");
            uploadStep.Value.ShouldContain("path: TestResults");
            uploadStep.Value.ShouldNotContain("path: |");
            producerJob.Value.ShouldContain(
                "ATTEMPT_PATH: ${{ runner.temp }}/workflow-attempt/producer-attempt.json");
            producerJob.Value.ShouldContain(
                "cp \"$ATTEMPT_PATH\" TestResults/workflow-attempt/producer-attempt.json");
            producerJob.Value.ShouldContain("console;verbosity=minimal");
            producerJob.Value.ShouldNotContain("console;verbosity=detailed");
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

            int liveInvocations = CountOccurrences(
                source,
                "LiveRecoveryValidationRunsAllThreeCoordinatorsAndPassesEvidenceGate");
            // Every assertion below compares a count against `liveInvocations`, so a workflow that stopped invoking
            // the live test at all would satisfy all of them with zero -- the guard would disappear at exactly the
            // moment the env var and the staging step were dropped wholesale.
            liveInvocations.ShouldBeGreaterThan(
                0,
                $"{workflow} must invoke the live recovery test, or every marker-retention assertion below is vacuous.");
            CountOccurrences(
                source,
                "HEXALITH_CHATBOT_RECOVERY_RETENTION_FAILURE_DIR: ${{ runner.temp }}/live-recovery-retention-failures")
                .ShouldBe(
                    liveInvocations,
                    $"{workflow} must give every live recovery invocation an independent runner-temp marker root.");
            CountOccurrences(
                source,
                "cp -R \"$RETENTION_FAILURE_ROOT\"/. TestResults/retention-failures/")
                .ShouldBe(
                    liveInvocations,
                    $"{workflow} must always stage every invocation's marker root under its uploaded TestResults artifact.");

            string[] finalizationBlocks = source
                .Split("\n      - name: ", StringSplitOptions.None)
                .Where(static block => block.Contains(
                    "cp -R \"$RETENTION_FAILURE_ROOT\"/. TestResults/retention-failures/",
                    StringComparison.Ordinal))
                .ToArray();
            finalizationBlocks.Length.ShouldBe(liveInvocations);
            foreach (string block in finalizationBlocks)
            {
                int markerStage = block.IndexOf(
                    "cp -R \"$RETENTION_FAILURE_ROOT\"/. TestResults/retention-failures/",
                    StringComparison.Ordinal);
                foreach (string failureProneCommand in new[]
                {
                    "jq \\",
                    "mv \"$temporary_path\"",
                    "cp \"$ATTEMPT_PATH\"",
                    "cp \"$sanitized\"",
                })
                {
                    int commandIndex = block.IndexOf(failureProneCommand, StringComparison.Ordinal);
                    if (commandIndex >= 0)
                    {
                        markerStage.ShouldBeLessThan(
                            commandIndex,
                            $"{workflow} must stage retention failures before '{failureProneCommand}' can abort finalization.");
                    }
                }

                // "Always stage" is a claim about the step's WHOLE condition, not only about the presence of its
                // copy command or of the `always()` token. Counting the command alone stayed green with `always() &&`
                // removed; asserting `Contains("if: always()")` alone stayed green with
                // `if: always() && steps.recovery.outcome == 'success'`, which skips staging on exactly the failed
                // producer run whose marker the gate needs. Read the condition line and reject any conjunct that
                // reintroduces success-dependence.
                string condition = block
                    .Split('\n')
                    .Select(static line => line.Trim())
                    .FirstOrDefault(static line => line.StartsWith("if:", StringComparison.Ordinal))
                    ?? string.Empty;
                condition.StartsWith("if: always()", StringComparison.Ordinal)
                    .ShouldBeTrue($"{workflow} must stage retention failures even when the recovery producer step fails.");
                foreach (string successDependence in new[] { ".outcome", "success()", "failure()", ".result" })
                {
                    condition.Contains(successDependence, StringComparison.Ordinal)
                        .ShouldBeFalse(
                            $"{workflow} must not make retention staging depend on '{successDependence}': "
                            + $"the condition was '{condition}'.");
                }

                // The producer writes markers to HEXALITH_CHATBOT_RECOVERY_RETENTION_FAILURE_DIR and the staging step
                // reads RETENTION_FAILURE_ROOT. Two independent literals with a `[[ -d ]]` guard meant a drifted value
                // staged nothing at all, silently, and the job fell back to ordinary missing_evidence.
                block.Contains("RETENTION_FAILURE_ROOT: ${{ runner.temp }}/live-recovery-retention-failures", StringComparison.Ordinal)
                    .ShouldBeTrue($"{workflow} must stage the same runner-temp root the producer was given.");

                // The copy must not be able to abort the always-run finalization commands staged after it.
                block.Contains("cp -R \"$RETENTION_FAILURE_ROOT\"/. TestResults/retention-failures/ || true", StringComparison.Ordinal)
                    .ShouldBeTrue($"{workflow} must keep marker staging non-blocking under `set -e`.");
            }

            // Least privilege on the destructive lane and its gate.
            source.ShouldContain("permissions:\n      contents: read");
        }

        // The release must depend on the independent gate, not merely on the run that produced the evidence.
        release.ShouldContain("- live-recovery-evidence-gate");

        string liveE2e = ReadProjectFile(
            "tests/Hexalith.ChatBot.IntegrationTests/Recovery/LiveContinuityAspireE2eTests.cs");
        liveE2e.ShouldContain("WriteAttemptSummaryAsync(");
        liveE2e.ShouldContain("Path.Combine(directory, LiveRecoveryValidationAttemptSummary.FileName)");
        liveE2e.ShouldNotContain(
            "Path.Combine(evidenceDirectory, LiveRecoveryValidationAttemptSummary.FileName)");
        string replay = ReadProjectFile(
            "tests/Hexalith.ChatBot.IntegrationTests/Recovery/LiveRecoveryEvidenceGateReplayTests.cs");
        replay.ShouldContain("Path.Combine(artifactRoot, RetentionFailuresDirectoryName)");
        replay.ShouldContain("SearchOption.TopDirectoryOnly");

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

    [Fact]
    public static void ReleasePublication_ShouldRequireFreshMainHeadImmediatelyBeforeSemanticRelease()
    {
        string release = ReadProjectFile(".github/workflows/release.yml");
        // Bounded by the next top-level job key: an open-ended match would let a job added after semantic-release
        // satisfy the job-level assertions below.
        Match semanticReleaseJob = Regex.Match(release, "(?ms)^  semantic-release:.*?(?=^  \\S|\\z)");
        semanticReleaseJob.Success.ShouldBeTrue("release workflow must contain the semantic-release job");
        string job = semanticReleaseJob.Value;
        Match publicationGuard = Regex.Match(
            job,
            "(?ms)^      - name: Guard main publication against a stale validated commit\\n"
            + ".*?(?=^      - |\\z)");
        publicationGuard.Success.ShouldBeTrue("release workflow must contain the publication guard step");
        string afterPublicationGuard = job[(publicationGuard.Index + publicationGuard.Length)..];
        afterPublicationGuard.StartsWith("      - name: Release\n", StringComparison.Ordinal).ShouldBeTrue(
            "no named or unnamed workflow step may intervene between the publication guard and semantic-release");

        job.ShouldContain("fetch-depth: 0");
        job.ShouldContain("timeout-minutes: 30");
        publicationGuard.Value.ShouldContain("id: publication_guard");
        publicationGuard.Value.ShouldContain("RELEASE_BRANCH: ${{ github.ref_name }}");
        publicationGuard.Value.ShouldContain("VALIDATED_SHA: ${{ github.sha }}");
        publicationGuard.Value.ShouldContain("bash .github/scripts/guard-main-publication.sh");
        job.ShouldContain(
            "- name: Release\n"
            + "        if: steps.publication_guard.outputs.should_publish == 'true'");

        // Recovery validation remains independently authoritative for every SHA. Publication eligibility is not
        // allowed to replace it with branch-keyed serialization or cancellation.
        release.ShouldContain(
            "group: live-recovery-validation-release-${{ github.repository }}-${{ github.sha }}");
        release.ShouldContain(
            "group: live-recovery-evidence-gate-release-${{ github.repository }}-${{ github.sha }}");
        release.ShouldNotContain(
            "group: live-recovery-validation-release-${{ github.repository }}-${{ github.ref }}");
        release.ShouldNotContain(
            "group: live-recovery-evidence-gate-release-${{ github.repository }}-${{ github.ref }}");

        // Bound to each group in turn: an unscoped `cancel-in-progress: false` search is satisfied by either
        // concurrency block, so one lane could flip to cancellation while the assertion stayed green.
        release.ShouldContain(
            "group: live-recovery-validation-release-${{ github.repository }}-${{ github.sha }}\n"
            + "      cancel-in-progress: false");
        release.ShouldContain(
            "group: live-recovery-evidence-gate-release-${{ github.repository }}-${{ github.sha }}\n"
            + "      cancel-in-progress: false");
        Regex.Matches(release, "^      cancel-in-progress: ", RegexOptions.Multiline).Count.ShouldBe(
            2,
            "the release workflow must declare exactly the two per-commit recovery concurrency blocks");
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
    public static async Task RunSettingsIsAcceptedByTheTestPlatform()
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

            // Release, matching the configuration CI builds and runs. Without it this defaulted to Debug, so on a
            // hosted runner the child died on a missing assembly, the rejection message never appeared, and the
            // assertion passed -- guard-green/lane-dead, the very class this test exists to close.
            "--configuration",
            "Release",
            "--filter",
            "FullyQualifiedName=Hexalith.ChatBot.Architecture.Tests.LiveRecoveryValidationArchitectureTests.RunSettingsPlatformAcceptanceProbe",
            "--settings",
            "live-recovery.runsettings",
            "--logger",
            "console;verbosity=detailed",
        })
        {
            process.StartInfo.ArgumentList.Add(argument);
        }

        process.Start().ShouldBeTrue("the run-settings acceptance probe process must start");

        // Drained concurrently. Reading stdout to completion and only then stderr deadlocks whenever the child
        // fills the stderr pipe buffer and prevents the bounded exit wait from observing completion.
        Task<string> standardOutput = process.StandardOutput.ReadToEndAsync(TestContext.Current.CancellationToken);
        Task<string> standardError = process.StandardError.ReadToEndAsync(TestContext.Current.CancellationToken);
        try
        {
            await process.WaitForExitAsync(TestContext.Current.CancellationToken)
                .WaitAsync(TimeSpan.FromMinutes(5), TestContext.Current.CancellationToken)
                .ConfigureAwait(true);
        }
        catch (TimeoutException)
        {
            process.Kill(entireProcessTree: true);
            throw new TimeoutException("The run-settings acceptance probe did not exit within its budget.");
        }

        string output = (await standardOutput.ConfigureAwait(true))
            + (await standardError.ConfigureAwait(true));

        output.ShouldNotContain(
            "does not conform to required format",
            Case.Insensitive,
            "the test platform rejected the run settings, so every lane that passes it dies before running.");

        // Positive signal, not merely the absence of one error string. Without this the probe passed whenever the
        // child failed for ANY other reason -- a missing assembly, a localized runner, a crash before startup.
        process.ExitCode.ShouldBe(
            0,
            $"the acceptance probe must actually run under the settings file. Output: {output}");
        output.ShouldContain(
            "RunSettingsPlatformAcceptanceProbe",
            Case.Insensitive,
            "the named probe test must actually have been selected and executed by the child run.");
    }

    /// <summary>
    /// The test the acceptance probe selects. Exists so that run resolves to exactly one executed test.
    /// </summary>
    /// <remarks>
    /// The filter previously named a test that did not exist anywhere in the repository, so the child run matched
    /// nothing. Combined with an assertion that only checked for the absence of one error string, that made the
    /// whole guard vacuous.
    /// </remarks>
    [Fact]
    public static void RunSettingsPlatformAcceptanceProbe() => true.ShouldBeTrue();

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
