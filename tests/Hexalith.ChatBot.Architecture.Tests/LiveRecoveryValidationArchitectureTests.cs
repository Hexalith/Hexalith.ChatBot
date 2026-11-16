using System.Reflection;
using System.Text.RegularExpressions;

using Hexalith.ChatBot.Architecture.Tests.Fitness;
using Hexalith.ChatBot.Server.Audit;

using NetArchTest.Rules;

using Shouldly;

using NetArchTestResult = NetArchTest.Rules.TestResult;

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
            "LiveRecoverySweep.cs"));
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
        NetArchTestResult result = Types.InAssembly(FitnessAssemblies.Server)
            .Should()
            .NotHaveDependencyOnAny("Aspire.Hosting", "CommunityToolkit.Aspire.Hosting")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(FitnessRule.Describe(result));
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

    [Fact]
    public static void FaultControlTypes_ShouldNotLeakIntoContractsDomainOrUi()
    {
        Assembly ui = FitnessAssemblies.Adapters.Single(static assembly =>
            assembly.GetName().Name == "Hexalith.ChatBot.UI");
        Assembly[] guardedAssemblies = [FitnessAssemblies.Contracts, FitnessAssemblies.Client, ui];
        string[] forbidden =
        [
            "Aspire.Hosting",
            "CommunityToolkit.Aspire.Hosting",
            typeof(LiveRecoveryValidationOptions).FullName!,
            typeof(IScopedOutageInjectionDriver).FullName!,
            typeof(IContinuityDrillScenarioRunner).FullName!,
            typeof(IProjectionRebuildDriver).FullName!,
        ];
        foreach (Assembly assembly in guardedAssemblies)
        {
            assembly.GetTypes().ShouldNotBeEmpty(assembly.GetName().Name);
            NetArchTestResult result = Types.InAssembly(assembly)
                .Should()
                .NotHaveDependencyOnAny(forbidden)
                .GetResult();

            result.IsSuccessful.ShouldBeTrue($"{assembly.GetName().Name}: {FitnessRule.Describe(result)}");
        }
    }

    /// <summary>Proves each recovery-type dependency guard detects a real compiled consumer.</summary>
    /// <param name="forbiddenType">The type that a guarded surface must not depend on.</param>
    /// <param name="consumer">An existing Server consumer used to prove rule detection.</param>
    [Theory]
    [InlineData(typeof(LiveRecoveryValidationOptions), typeof(LiveRecoveryValidationOptionsValidator))]
    [InlineData(typeof(IScopedOutageInjectionDriver), typeof(ScopedOutageDegradationValidationCoordinator))]
    [InlineData(typeof(IContinuityDrillScenarioRunner), typeof(ContinuityDrillCoordinator))]
    [InlineData(typeof(IProjectionRebuildDriver), typeof(ProjectionRebuildValidationCoordinator))]
    public static void FaultControlDependencyRuleDetectsRealCompiledReferences(Type forbiddenType, Type consumer)
    {
        ArgumentNullException.ThrowIfNull(forbiddenType);
        ArgumentNullException.ThrowIfNull(consumer);

        NetArchTestResult result = Types.InAssembly(FitnessAssemblies.Server)
            .Should()
            .NotHaveDependencyOnAny(forbiddenType.FullName!)
            .GetResult();

        result.IsSuccessful.ShouldBeFalse("the deliberately false rule must detect the real recovery dependency");
        FitnessRule.FailingTypeNames(result).ShouldContain(consumer.FullName);
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
