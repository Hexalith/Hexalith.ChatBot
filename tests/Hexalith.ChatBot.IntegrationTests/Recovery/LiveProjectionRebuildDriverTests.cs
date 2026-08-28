using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Gateway.Stages;
using Hexalith.ChatBot.Server.Projections;
using Hexalith.EventStore.Client.Projections;

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
        ProjectionRebuildBaselineEvidence baselineEvidence = await LiveProjectionRebuildDriver.SeedBaselineAsync(
            readModels,
            tenantRef,
            Descriptor(),
            [Source(tenantRef)],
            worm.EnumerateChain(tenantRef),
            TestContext.Current.CancellationToken);
        LiveProjectionRebuildDriver driver = new(
            [Source(tenantRef)],
            worm,
            baselineEvidence,
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
        measurement.PreRebuildSnapshot.ShouldContain(digest => digest.ResourceId.StartsWith("source-", StringComparison.Ordinal));
        measurement.PreRebuildSnapshot.ShouldContain(digest => digest.ResourceId.StartsWith("governed-", StringComparison.Ordinal));
        measurement.RebuiltSnapshot.ShouldBe(measurement.PreRebuildSnapshot);
        measurement.PreRebuildSchemaVersion.ShouldContain(ProjectConversationSourceEmailView.CurrentSchemaVersion);
        measurement.PreRebuildSchemaVersion.ShouldContain(GovernedOperationView.CurrentSchemaVersion);
        measurement.RebuiltSchemaVersion.ShouldBe(measurement.PreRebuildSchemaVersion);
        measurement.SourceResourceCount.ShouldBe(1);
        measurement.GovernedResourceCount.ShouldBe(1);
        measurement.WormRecordCount.ShouldBe(1);
        measurement.WormOperationCount.ShouldBe(1);
        measurement.MeasuredDuration.ShouldBeGreaterThanOrEqualTo(TimeSpan.Zero);
        measurement.ExecutionAssertions!.CleanupComplete.ShouldBeTrue();
        measurement.ExecutionAssertions.TenantIsolationPreserved.ShouldBeTrue();
        measurement.ExecutionAssertions.UnauthorizedMutationAbsent.ShouldBeFalse();
        measurement.ExecutionAssertions.MailboxReingestionAbsent.ShouldBeTrue();
        measurement.ExecutionAssertions.IndependentControlSucceeded.ShouldBeFalse();
        measurement.ExecutionAssertions.StateReconstructable.ShouldBeTrue();
        measurement.PreRebuildSnapshot.ShouldNotContain(digest => digest.ResourceId.Contains("foreign", StringComparison.Ordinal));
        readModels.Writes.ShouldBe(4);
        readModels.Erases.ShouldBe(2);
    }

    [Fact]
    public async Task FailedRebuildSkipsEraseSoTheFreshPartitionRemainsForCapture()
    {
        string tenantRef = RecoveryValidationTopology.LogicalTenantRef;
        InMemoryWormAuditStore worm = new();
        _ = await worm.AppendAsync(Envelope(tenantRef, "resource-001"), TestContext.Current.CancellationToken);
        InMemoryRecoveryReadModelStore readModels = new();
        ProjectionRebuildBaselineEvidence baselineEvidence = await LiveProjectionRebuildDriver.SeedBaselineAsync(
            readModels,
            tenantRef,
            Descriptor(),
            [Source(tenantRef)],
            worm.EnumerateChain(tenantRef),
            TestContext.Current.CancellationToken);
        // Seed wrote the baseline (2 writes: source + governed record). Let the rebuild's first fresh-partition
        // write (the reconstructed source) land, then fail on its second (the governed record) — rejecting every
        // write instead would leave zero fresh keys ever written, so a regression that erased them anyway would not
        // fail this test.
        int writesAfterSeed = readModels.Writes;
        readModels.FailOnWriteNumber = writesAfterSeed + 2;
        int erasesBefore = readModels.Erases;
        LiveProjectionRebuildDriver driver = new(
            [Source(tenantRef)],
            worm,
            baselineEvidence,
            readModels,
            readModels,
            Descriptor(),
            Options(),
            new SystemClock());

        _ = await Should.ThrowAsync<InvalidOperationException>(() => driver.RebuildAsync(
            tenantRef,
            "recovery-baseline",
            "01ARZ3NDEKTSV4RRFFQ69G5FAW",
            TestContext.Current.CancellationToken).AsTask());

        readModels.Erases.ShouldBe(erasesBefore);
        string freshTenant = LiveProjectionRebuildDriver.FreshPartitionTenant(
            tenantRef,
            "recovery-partition-v1",
            "01ARZ3NDEKTSV4RRFFQ69G5FAW");
        IReadOnlyList<string> freshKeys = LiveProjectionRebuildDriver.ProjectionKeys(
            freshTenant,
            [Source(tenantRef)],
            worm.EnumerateChain(tenantRef).ToArray());
        bool anyFreshKeyPresent = false;
        foreach (string key in freshKeys)
        {
            (bool present, _) = await readModels
                .TryReadEtagAsync(ChatBotReadModelStoreNames.StateStoreName, key, TestContext.Current.CancellationToken);
            anyFreshKeyPresent |= present;
        }

        // The property this test is named for: the write that landed before the injected failure is still there,
        // because erase was skipped. A regression that erased on failure regardless would leave every fresh key
        // absent and fail this assertion.
        anyFreshKeyPresent.ShouldBeTrue();
        readModels.Erases.ShouldBe(erasesBefore);
    }

    [Fact]
    public async Task RebuildCanDivergeFromThePersistedBaseline()
    {
        string tenantRef = RecoveryValidationTopology.LogicalTenantRef;
        InMemoryWormAuditStore worm = new();
        _ = await worm.AppendAsync(Envelope(tenantRef, "resource-001"), TestContext.Current.CancellationToken);
        InMemoryRecoveryReadModelStore readModels = new();
        ProjectConversationSourceEmailView baselineSource = Source(tenantRef);
        ProjectionRebuildBaselineEvidence baselineEvidence = await LiveProjectionRebuildDriver.SeedBaselineAsync(
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
            baselineEvidence,
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
        ProjectionRebuildBaselineEvidence baselineEvidence = await LiveProjectionRebuildDriver.SeedBaselineAsync(
            readModels,
            tenantRef,
            Descriptor(),
            [Source(tenantRef)],
            worm.EnumerateChain(tenantRef),
            TestContext.Current.CancellationToken);
        LiveProjectionRebuildDriver driver = new(
            [Source(tenantRef)],
            worm,
            baselineEvidence,
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
    public async Task SameResourceWormStructuralMutationChangesTheGovernedDigest()
    {
        string tenantRef = RecoveryValidationTopology.LogicalTenantRef;
        InMemoryWormAuditStore seedWorm = new();
        InMemoryWormAuditStore rebuildWorm = new();
        _ = await seedWorm.AppendAsync(Envelope(tenantRef, "resource-001"), TestContext.Current.CancellationToken);
        _ = await rebuildWorm.AppendAsync(
            Envelope(tenantRef, "resource-001") with { Outcome = "rejected", ReasonCode = "policy-rejected" },
            TestContext.Current.CancellationToken);
        InMemoryRecoveryReadModelStore readModels = new();
        ProjectionRebuildBaselineEvidence baselineEvidence = await LiveProjectionRebuildDriver.SeedBaselineAsync(
            readModels,
            tenantRef,
            Descriptor(),
            [Source(tenantRef)],
            seedWorm.EnumerateChain(tenantRef),
            TestContext.Current.CancellationToken);
        LiveProjectionRebuildDriver driver = new(
            [Source(tenantRef)], rebuildWorm, baselineEvidence, readModels, readModels, Descriptor(), Options(), new SystemClock());

        ProjectionRebuildValidationCoordinator coordinator = new(
            driver,
            new InMemoryAuditWriter(),
            new InMemoryOperatorAlertSink(),
            new SystemClock());
        ProjectionRebuildReport report = await coordinator.RunValidationAndRecordAsync(
            tenantRef,
            "recovery-baseline",
            "01ARZ3NDEKTSV4RRFFQ69G5FAW",
            TestContext.Current.CancellationToken);

        report.Verdict.ShouldBe(ProjectionRebuildVerdicts.Divergent);
        report.FirstDivergingResourceLocator.ShouldBe("resource:governed-resource-001");
        report.PreRebuildDigests!.Single(static digest => digest.ResourceId == "governed-resource-001")
            .ShouldNotBe(report.RebuiltDigests!.Single(static digest => digest.ResourceId == "governed-resource-001"));
        report.PreRebuildFingerprint.ShouldNotBe(report.RebuiltFingerprint);
    }

    [Fact]
    public async Task MultiEnvelopeOperationIsGroupedAndReplayedOnce()
    {
        string tenantRef = RecoveryValidationTopology.LogicalTenantRef;
        InMemoryWormAuditStore seedWorm = new();
        InMemoryWormAuditStore rebuildWorm = new();
        AuditEnvelope preCommit = Envelope(tenantRef, "resource-001") with
        {
            Phase = AuditCommitPhase.PreCommit,
            Outcome = "proposed",
        };
        AuditEnvelope postCommit = Envelope(tenantRef, "resource-001");
        foreach (InMemoryWormAuditStore worm in new[] { seedWorm, rebuildWorm })
        {
            _ = await worm.AppendAsync(preCommit, TestContext.Current.CancellationToken);
            _ = await worm.AppendAsync(postCommit, TestContext.Current.CancellationToken);
        }

        RecoveryValidationDatasetDescriptor descriptor = Descriptor(wormAuditRecordCount: 2);
        InMemoryRecoveryReadModelStore readModels = new();
        ProjectionRebuildBaselineEvidence baselineEvidence = await LiveProjectionRebuildDriver.SeedBaselineAsync(
            readModels,
            tenantRef,
            descriptor,
            [Source(tenantRef)],
            seedWorm.EnumerateChain(tenantRef),
            TestContext.Current.CancellationToken);
        LiveProjectionRebuildDriver driver = new(
            [Source(tenantRef)], rebuildWorm, baselineEvidence, readModels, readModels, descriptor, Options(datasetVolume: 7), new SystemClock());

        ProjectionRebuildMeasurement measurement = await driver.RebuildAsync(
            tenantRef,
            "recovery-baseline",
            "01ARZ3NDEKTSV4RRFFQ69G5FAW",
            TestContext.Current.CancellationToken);

        measurement.PreRebuildSnapshot.ShouldBe(measurement.RebuiltSnapshot);
        measurement.WormRecordCount.ShouldBe(2);
        measurement.WormOperationCount.ShouldBe(1);
        measurement.GovernedResourceCount.ShouldBe(1);
    }

    [Fact]
    public async Task SeveralOperationsMayTargetOneGovernedResource()
    {
        string tenantRef = RecoveryValidationTopology.LogicalTenantRef;
        InMemoryWormAuditStore seedWorm = new();
        InMemoryWormAuditStore rebuildWorm = new();
        AuditEnvelope first = Envelope(tenantRef, "resource-001", "01ARZ3NDEKTSV4RRFFQ69G5FAW");
        AuditEnvelope second = Envelope(tenantRef, "resource-001", "01ARZ3NDEKTSV4RRFFQ69G5FAX") with
        {
            StateTransition = "Proposed-Recorded",
        };
        foreach (InMemoryWormAuditStore worm in new[] { seedWorm, rebuildWorm })
        {
            _ = await worm.AppendAsync(first, TestContext.Current.CancellationToken);
            _ = await worm.AppendAsync(second, TestContext.Current.CancellationToken);
        }

        RecoveryValidationDatasetDescriptor descriptor = Descriptor(wormAuditRecordCount: 2);
        InMemoryRecoveryReadModelStore readModels = new();
        ProjectionRebuildBaselineEvidence baselineEvidence = await LiveProjectionRebuildDriver.SeedBaselineAsync(
            readModels,
            tenantRef,
            descriptor,
            [Source(tenantRef)],
            seedWorm.EnumerateChain(tenantRef),
            TestContext.Current.CancellationToken);
        LiveProjectionRebuildDriver driver = new(
            [Source(tenantRef)], rebuildWorm, baselineEvidence, readModels, readModels, descriptor, Options(datasetVolume: 7), new SystemClock());

        ProjectionRebuildMeasurement measurement = await driver.RebuildAsync(
            tenantRef,
            "recovery-baseline",
            "01ARZ3NDEKTSV4RRFFQ69G5FAW",
            TestContext.Current.CancellationToken);

        measurement.PreRebuildSnapshot.ShouldBe(measurement.RebuiltSnapshot);
        measurement.WormRecordCount.ShouldBe(2);
        measurement.WormOperationCount.ShouldBe(2);
        measurement.GovernedResourceCount.ShouldBe(1);
        measurement.PreRebuildSnapshot.Count.ShouldBe(2);
    }

    [Fact]
    public async Task UnsafeWormResourceIdIsRejectedInsteadOfCollapsed()
    {
        string tenantRef = RecoveryValidationTopology.LogicalTenantRef;
        InMemoryWormAuditStore worm = new();
        _ = await worm.AppendAsync(Envelope(tenantRef, "unsafe/resource"), TestContext.Current.CancellationToken);
        InMemoryRecoveryReadModelStore readModels = new();

        _ = await Should.ThrowAsync<InvalidOperationException>(() => LiveProjectionRebuildDriver.SeedBaselineAsync(
            readModels,
            tenantRef,
            Descriptor(),
            [Source(tenantRef)],
            worm.EnumerateChain(tenantRef),
            TestContext.Current.CancellationToken));
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
            new ProjectionRebuildBaselineEvidence([], new Dictionary<string, string>(), "schema", 0, 0),
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

        readModels.Writes.ShouldBe(0);
        readModels.Erases.ShouldBe(0);
    }

    private static LiveRecoveryValidationOptions Options(int datasetVolume = 6)
        => new()
        {
            Enabled = true,
            EnvironmentName = "Testing",
            TestTenantRef = RecoveryValidationTopology.LogicalTenantRef,
            DatasetRef = "recovery-baseline",
            DatasetVersion = "v1",
            DatasetVolume = datasetVolume,
            ProjectionSchemaVersion = "chatbot.project-conversation-source-email.v1",
            ValidationPartitionRef = "recovery-partition-v1",
            ControllerCapability = LiveRecoveryValidationOptions.AspireControllerCapability,
            ControllerSecret = "tier3-value",
            PerScenarioTimeout = TimeSpan.FromMinutes(25),
            RestorationTimeout = TimeSpan.FromSeconds(5),
            WorkflowTimeout = TimeSpan.FromHours(5),
            EvidenceDirectory = Path.GetFullPath("TestResults/live-recovery"),
            EvidenceLocator = "artifact:live-recovery-validation-evidence",
        };

    private static RecoveryValidationDatasetDescriptor Descriptor(int wormAuditRecordCount = 1)
        => new(
            "recovery-baseline",
            "v1",
            "chatbot.project-conversation-source-email.v1",
            "recovery-partition-v1",
            SourceRecordCount: 1,
            WormAuditRecordCount: wormAuditRecordCount,
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
            "Microsoft 365 mailbox",
            AssociationCandidateView.MailboxSourceProvenance,
            GovernedOperationView.MetadataOnlyRedactionState,
            "standard",
            ProjectConversationSourceEmailView.CurrentSchemaVersion,
            SourceVersion: 1,
            "01ARZ3NDEKTSV4RRFFQ69G5FAW");

    private static AuditEnvelope Envelope(
        string tenantRef,
        string resourceRef,
        string correlationId = "01ARZ3NDEKTSV4RRFFQ69G5FAW")
        => new(
            tenantRef,
            "recovery-validator",
            "human",
            "RecordGovernedNote",
            resourceRef,
            "allow",
            "accepted",
            correlationId,
            DateTimeOffset.Parse("2026-08-01T00:00:01Z", System.Globalization.CultureInfo.InvariantCulture),
            "policy-v1",
            ["dataset:recovery-baseline"],
            IdempotencyKey: null,
            "Received-Proposed",
            GovernedOperationView.MetadataOnlyRedactionState,
            "accepted",
            AuditCommitPhase.PostCommit,
            "audit-envelope-v1",
            PredecessorHash: null,
            "api");
}
