using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Gateway.Stages;
using Hexalith.ChatBot.Server.Projections;

using Shouldly;

namespace Hexalith.ChatBot.IntegrationTests.Recovery;

/// <summary>Focused coordinator and immutable-source contract tests for the live projection-rebuild driver.</summary>
public sealed class LiveProjectionRebuildDriverTests
{
    [Fact]
    public async Task RebuildUsesOnlyTheSelectedTenantImmutableSourceAndWormChain()
    {
        string tenantRef = RecoveryValidationTopology.LogicalTenantRef;
        InMemoryWormAuditStore worm = new();
        _ = await worm.AppendAsync(Envelope(tenantRef, "resource-001"), TestContext.Current.CancellationToken);
        _ = await worm.AppendAsync(Envelope("replay-test:neighbor", "foreign-001"), TestContext.Current.CancellationToken);
        InMemoryRecoveryReadModelStore readModels = new();
        await LiveProjectionRebuildDriver.SeedBaselineAsync(
            readModels,
            tenantRef,
            Descriptor(),
            [Source(tenantRef)],
            worm.EnumerateChain(tenantRef),
            TestContext.Current.CancellationToken);
        LiveProjectionRebuildDriver driver = new(
            [Source(tenantRef)],
            worm,
            readModels,
            readModels,
            Descriptor(),
            Options(),
            new SystemClock());

        ProjectionRebuildMeasurement measurement = await driver.RebuildAsync(
            tenantRef,
            "recovery-baseline",
            "01ARZ3NDEKTSV4RRFFQ69G5FAW",
            TestContext.Current.CancellationToken);

        measurement.PreRebuildSnapshot.Count.ShouldBe(2);
        measurement.RebuiltSnapshot.ShouldBe(measurement.PreRebuildSnapshot);
        measurement.PreRebuildSchemaVersion.ShouldBe("project-conversation-v1");
        measurement.RebuiltSchemaVersion.ShouldBe("project-conversation-v1");
        measurement.MeasuredDuration.ShouldBeGreaterThanOrEqualTo(TimeSpan.Zero);
        measurement.PreRebuildSnapshot.ShouldNotContain(digest => digest.ResourceId.Contains("foreign", StringComparison.Ordinal));
        readModels.Writes.ShouldBe(4);
        readModels.Erases.ShouldBe(2);
    }

    [Fact]
    public async Task RebuildCanDivergeFromThePersistedBaseline()
    {
        string tenantRef = RecoveryValidationTopology.LogicalTenantRef;
        InMemoryWormAuditStore worm = new();
        _ = await worm.AppendAsync(Envelope(tenantRef, "resource-001"), TestContext.Current.CancellationToken);
        InMemoryRecoveryReadModelStore readModels = new();
        ProjectConversationSourceEmailView baselineSource = Source(tenantRef);
        await LiveProjectionRebuildDriver.SeedBaselineAsync(
            readModels,
            tenantRef,
            Descriptor(),
            [baselineSource],
            worm.EnumerateChain(tenantRef),
            TestContext.Current.CancellationToken);
        ProjectConversationSourceEmailView changedSource = baselineSource with { SourceVersion = 2 };
        LiveProjectionRebuildDriver driver = new(
            [changedSource],
            worm,
            readModels,
            readModels,
            Descriptor(),
            Options(),
            new SystemClock());

        ProjectionRebuildMeasurement measurement = await driver.RebuildAsync(
            tenantRef,
            "recovery-baseline",
            "01ARZ3NDEKTSV4RRFFQ69G5FAW",
            TestContext.Current.CancellationToken);

        measurement.RebuiltSnapshot.ShouldNotBe(measurement.PreRebuildSnapshot);
        string verdict = ProjectionRebuildEquivalenceEvaluator.Evaluate(
            measurement.PreRebuildSnapshot,
            measurement.RebuiltSnapshot,
            measurement.PreRebuildSchemaVersion,
            measurement.RebuiltSchemaVersion);
        verdict.ShouldBe(ProjectionRebuildVerdicts.Divergent);
    }

    [Fact]
    public async Task CoordinatorRunsThePopulatedDatasetThroughTheLiveDriver()
    {
        string tenantRef = RecoveryValidationTopology.LogicalTenantRef;
        InMemoryWormAuditStore worm = new();
        _ = await worm.AppendAsync(Envelope(tenantRef, "resource-001"), TestContext.Current.CancellationToken);
        InMemoryRecoveryReadModelStore readModels = new();
        await LiveProjectionRebuildDriver.SeedBaselineAsync(
            readModels,
            tenantRef,
            Descriptor(),
            [Source(tenantRef)],
            worm.EnumerateChain(tenantRef),
            TestContext.Current.CancellationToken);
        LiveProjectionRebuildDriver driver = new(
            [Source(tenantRef)],
            worm,
            readModels,
            readModels,
            Descriptor(),
            Options(),
            new SystemClock());
        ProjectionRebuildValidationCoordinator coordinator = new(
            driver,
            new InMemoryAuditWriter(),
            new InMemoryOperatorAlertSink(),
            new SystemClock());

        ProjectionRebuildOutcome outcome = await coordinator.RunAllAsync(
            tenantRef,
            ["recovery-baseline"],
            "01ARZ3NDEKTSV4RRFFQ69G5FAW",
            TestContext.Current.CancellationToken);

        outcome.TenantsValidated.ShouldBe(1);
        outcome.Equivalent.ShouldBe(1);
        outcome.Divergent.ShouldBe(0);
        outcome.DurationExceeded.ShouldBe(0);
        outcome.Unmeasurable.ShouldBe(0);
        outcome.Alerted.ShouldBe(0);
    }

    [Fact]
    public async Task DisabledLiveModeIsRejectedBeforeTheRebuildReadsItsPopulation()
    {
        LiveRecoveryValidationOptions options = Options();
        options.Enabled = false;
        InMemoryRecoveryReadModelStore readModels = new();
        LiveProjectionRebuildDriver driver = new(
            [],
            new InMemoryWormAuditStore(),
            readModels,
            readModels,
            Descriptor(),
            options,
            new SystemClock());

        _ = await Should.ThrowAsync<InvalidOperationException>(() => driver.RebuildAsync(
            RecoveryValidationTopology.LogicalTenantRef,
            "recovery-baseline",
            "01ARZ3NDEKTSV4RRFFQ69G5FAW",
            TestContext.Current.CancellationToken).AsTask());
    }

    private static LiveRecoveryValidationOptions Options()
        => new()
        {
            Enabled = true,
            EnvironmentName = "Testing",
            TestTenantRef = RecoveryValidationTopology.LogicalTenantRef,
            DatasetRef = "recovery-baseline",
            DatasetVersion = "v1",
            DatasetVolume = 6,
            ProjectionSchemaVersion = "project-conversation-v1",
            ValidationPartitionRef = "recovery-partition-v1",
            ControllerCapability = LiveRecoveryValidationOptions.AspireControllerCapability,
            ControllerSecret = "tier3-value",
            PerScenarioTimeout = TimeSpan.FromMinutes(25),
            RestorationTimeout = TimeSpan.FromSeconds(5),
            WorkflowTimeout = TimeSpan.FromHours(5),
            EvidenceDirectory = Path.GetFullPath("TestResults/live-recovery"),
            EvidenceLocator = "artifact:live-recovery-validation",
        };

    private static RecoveryValidationDatasetDescriptor Descriptor()
        => new(
            "recovery-baseline",
            "v1",
            "project-conversation-v1",
            "recovery-partition-v1",
            SourceRecordCount: 1,
            WormAuditRecordCount: 1,
            GovernedCommandCount: 1,
            ApprovalCount: 1,
            PolicySnapshotCount: 1,
            AttachmentMetadataCount: 1,
            UsesIsolatedValidationStore: true);

    private static ProjectConversationSourceEmailView Source(string tenantRef)
        => new(
            tenantRef,
            "intake-001",
            "mailbox-001",
            "provider-message-001",
            "internet-message-001",
            "conversation-001",
            SourceThreadId: null,
            DateTimeOffset.Parse("2026-08-01T00:00:00Z", System.Globalization.CultureInfo.InvariantCulture),
            SourceSentAtUtc: null,
            SourceCreatedAtUtc: null,
            "UTC",
            "Microsoft-365-mailbox",
            "m365-mailbox",
            "metadata-only",
            "standard",
            ProjectConversationSourceEmailView.CurrentSchemaVersion,
            SourceVersion: 1,
            "01ARZ3NDEKTSV4RRFFQ69G5FAW");

    private static AuditEnvelope Envelope(string tenantRef, string resourceRef)
        => new(
            tenantRef,
            "recovery-validator",
            "human",
            "RecordGovernedNote",
            resourceRef,
            "allow",
            "accepted",
            "01ARZ3NDEKTSV4RRFFQ69G5FAW",
            DateTimeOffset.Parse("2026-08-01T00:00:01Z", System.Globalization.CultureInfo.InvariantCulture),
            "policy-v1",
            ["dataset:recovery-baseline"],
            IdempotencyKey: null,
            "Received-Proposed",
            "metadata-only",
            "accepted",
            AuditCommitPhase.PostCommit,
            "audit-envelope-v1",
            PredecessorHash: null,
            "api");
}
