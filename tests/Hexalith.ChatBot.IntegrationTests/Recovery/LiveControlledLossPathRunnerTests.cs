using Hexalith.ChatBot.Server.Audit;

using Shouldly;

namespace Hexalith.ChatBot.IntegrationTests.Recovery;

/// <summary>Boundary, structural-loss, and cancellation tests for the distinct controlled-loss runner.</summary>
public sealed class LiveControlledLossPathRunnerTests
{
    private static readonly DateTimeOffset Start = DateTimeOffset.Parse("2026-08-28T00:00:00Z");

    [Theory]
    [InlineData(1, ControlledLossPathVerdicts.Met)]
    [InlineData(900, ControlledLossPathVerdicts.Met)]
    [InlineData(901, ControlledLossPathVerdicts.Missed)]
    public async Task PositiveDurableRpoUsesCanonicalBoundary(int seconds, string expectedVerdict)
    {
        FakeOperations operations = new(TimeSpan.FromSeconds(seconds));
        ControlledLossPathReport report = await CreateRunner(operations).RunAsync(
            "replay-test:recovery-validation",
            "01ARZ3NDEKTSV4RRFFQ69G5FAV",
            TestContext.Current.CancellationToken);

        report.MeasuredRpo.ShouldBe(TimeSpan.FromSeconds(seconds));
        report.Verdict.ShouldBe(expectedVerdict);
        report.ReasonCode.ShouldBe(seconds > RecoveryTargets.MaxRpo.TotalSeconds
            ? ControlledLossPathReport.TargetMissedReasonCode
            : ControlledLossPathReport.CompletedReasonCode);
        report.CandidateAbsent.ShouldBeTrue();
        operations.Restored.ShouldBeTrue();
        operations.Cleaned.ShouldBeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task ZeroOrReversedDurableBoundsFailClosed(int seconds)
    {
        FakeOperations operations = new(TimeSpan.FromSeconds(seconds));
        ControlledLossPathReport report = await CreateRunner(operations).RunAsync(
            "replay-test:recovery-validation",
            "01ARZ3NDEKTSV4RRFFQ69G5FAV",
            TestContext.Current.CancellationToken);

        report.Verdict.ShouldBe(ControlledLossPathVerdicts.Unmeasurable);
        report.Deviations.ShouldContain(seconds == 0
            ? ControlledLossPathEvaluator.NonPositiveRpoDeviation
            : ControlledLossPathEvaluator.InvalidDurableBoundsDeviation);
    }

    [Fact]
    public async Task ResidualCommittedCandidateIsStructuralLoss()
    {
        FakeOperations operations = new(TimeSpan.FromMinutes(1)) { CandidateAbsent = false };
        ControlledLossPathReport report = await CreateRunner(operations).RunAsync(
            "replay-test:recovery-validation",
            "01ARZ3NDEKTSV4RRFFQ69G5FAV",
            TestContext.Current.CancellationToken);

        report.Verdict.ShouldBe(ControlledLossPathVerdicts.Unmeasurable);
        report.Deviations.ShouldContain(ControlledLossPathEvaluator.CandidateCommittedDeviation);
    }

    [Fact]
    public async Task CancellationStillRestoresAndCleansUp()
    {
        FakeOperations operations = new(TimeSpan.FromMinutes(1)) { CancelDuringRejection = true };

        _ = await Should.ThrowAsync<OperationCanceledException>(() => CreateRunner(operations).RunAsync(
            "replay-test:recovery-validation",
            "01ARZ3NDEKTSV4RRFFQ69G5FAV",
            TestContext.Current.CancellationToken).AsTask());

        operations.Restored.ShouldBeTrue();
        operations.Cleaned.ShouldBeTrue();
    }

    [Fact]
    public async Task CleanupFailureCannotQualifyEvidence()
    {
        FakeOperations operations = new(TimeSpan.FromMinutes(1)) { CleanupComplete = false };
        ControlledLossPathReport report = await CreateRunner(operations).RunAsync(
            "replay-test:recovery-validation",
            "01ARZ3NDEKTSV4RRFFQ69G5FAV",
            TestContext.Current.CancellationToken);

        report.Verdict.ShouldBe(ControlledLossPathVerdicts.Unmeasurable);
        report.Deviations.ShouldContain(ControlledLossPathEvaluator.CleanupIncompleteDeviation);
    }

    [Fact]
    public async Task DurableBoundsMustBelongToTheRequestedTenant()
    {
        FakeOperations operations = new(TimeSpan.FromMinutes(1))
        {
            PostRecoveryTenantRef = "another-tenant",
        };

        InvalidOperationException exception = await Should.ThrowAsync<InvalidOperationException>(() =>
            CreateRunner(operations).RunAsync(
                "replay-test:recovery-validation",
                "01ARZ3NDEKTSV4RRFFQ69G5FAV",
                TestContext.Current.CancellationToken).AsTask());

        exception.Message.ShouldContain("another tenant");
        operations.Cleaned.ShouldBeTrue();
    }

    /// <summary>
    /// The topology writes durable state under the physical tenant that <c>ReplayTenantPolicy.StorageTenantFor</c>
    /// derives, because EventStore cannot carry the `:` in the `replay-test:` label. Comparing the persisted
    /// envelope against the logical label instead rejected every genuine hosted observation as foreign, so the
    /// derived physical name must be the accepted binding on both bounds.
    /// </summary>
    [Fact]
    public async Task DurableBoundsBindToThePhysicalTenantDerivedFromTheRequestedLabel()
    {
        FakeOperations operations = new(TimeSpan.FromMinutes(1))
        {
            PreFaultTenantRef = ReplayTenantPolicy.StorageTenantFor("replay-test:recovery-validation")!,
            PostRecoveryTenantRef = ReplayTenantPolicy.StorageTenantFor("replay-test:recovery-validation")!,
        };

        ControlledLossPathReport report = await CreateRunner(operations).RunAsync(
            "replay-test:recovery-validation",
            "01ARZ3NDEKTSV4RRFFQ69G5FAV",
            TestContext.Current.CancellationToken);

        report.Verdict.ShouldBe(ControlledLossPathVerdicts.Met);
        report.MeasuredRpo.ShouldBe(TimeSpan.FromMinutes(1));
    }

    /// <summary>The logical `replay-test:` label is never what EventStore persists, so it must not be accepted.</summary>
    [Fact]
    public async Task TheLogicalReplayTestLabelIsNotAcceptedAsADurableBound()
    {
        FakeOperations operations = new(TimeSpan.FromMinutes(1))
        {
            PreFaultTenantRef = "replay-test:recovery-validation",
            PostRecoveryTenantRef = "replay-test:recovery-validation",
        };

        InvalidOperationException exception = await Should.ThrowAsync<InvalidOperationException>(() =>
            CreateRunner(operations).RunAsync(
                "replay-test:recovery-validation",
                "01ARZ3NDEKTSV4RRFFQ69G5FAV",
                TestContext.Current.CancellationToken).AsTask());

        exception.Message.ShouldContain("pre-fault");
    }

    [Fact]
    public async Task IndependentClockDomainsAreNotCrossOrdered()
    {
        FakeOperations operations = new(TimeSpan.FromMinutes(1))
        {
            CandidateObservedAtUtc = Start.AddYears(10),
        };

        ControlledLossPathReport report = await CreateRunner(operations).RunAsync(
            "replay-test:recovery-validation",
            "01ARZ3NDEKTSV4RRFFQ69G5FAV",
            TestContext.Current.CancellationToken);

        report.Verdict.ShouldBe(ControlledLossPathVerdicts.Met);
    }

    [Fact]
    public async Task HostedDeadlineCanRetainAnOverTargetMeasurementAsMissed()
    {
        LiveRecoveryValidationOptions options = CreateOptions();
        options.PerScenarioTimeout.ShouldBeGreaterThan(RecoveryTargets.MaxRpo);
        options.Validate().ShouldBeNull();

        ControlledLossPathReport report = await new LiveControlledLossPathRunner(
            new FakeOperations(TimeSpan.FromSeconds(901)),
            options).RunAsync(
                "replay-test:recovery-validation",
                "01ARZ3NDEKTSV4RRFFQ69G5FAV",
                TestContext.Current.CancellationToken);

        report.Verdict.ShouldBe(ControlledLossPathVerdicts.Missed);
        report.ReasonCode.ShouldBe(ControlledLossPathReport.TargetMissedReasonCode);
    }

    [Fact]
    public async Task CleanupFailureCannotReplaceThePrimaryInjectionFailure()
    {
        InvalidOperationException injectionFailure = new("injection-primary");
        InvalidOperationException cleanupFailure = new("cleanup-secondary");
        FakeOperations operations = new(TimeSpan.FromMinutes(1))
        {
            InjectionFailure = injectionFailure,
            CleanupFailure = cleanupFailure,
        };

        AggregateException exception = await Should.ThrowAsync<AggregateException>(() =>
            CreateRunner(operations).RunAsync(
                "replay-test:recovery-validation",
                "01ARZ3NDEKTSV4RRFFQ69G5FAV",
                TestContext.Current.CancellationToken).AsTask());

        exception.InnerExceptions.Count.ShouldBe(2);
        exception.InnerExceptions[0].InnerException.ShouldBeSameAs(injectionFailure);
        exception.InnerExceptions[1].ShouldBeSameAs(cleanupFailure);
        operations.Restored.ShouldBeTrue();
        operations.Cleaned.ShouldBeTrue();
    }

    [Fact]
    public void CrossTenantDuplicateFailsTheControlledLossSafetyDecision()
    {
        ControlledLossPathSafetyObservation safety = AspireRecoverySandboxOperations.EvaluateControlledLossSafety(
            preFaultRetained: true,
            candidateAggregateAbsent: true,
            candidateReadModelsAbsent: true,
            postRecoveryRetained: true,
            controlPreAggregateAbsent: true,
            controlPreReadModelsAbsent: true,
            controlCandidateAggregateAbsent: true,
            controlCandidateReadModelsAbsent: true,
            controlPostAggregateAbsent: false,
            controlPostReadModelsAbsent: true,
            sentinelsUnchangedAfterRecovery: true,
            sentinelsUnchangedDuringFault: true);

        safety.TenantIsolationPreserved.ShouldBeFalse();
    }

    /// <summary>
    /// Every conjunct must reach the published fact it belongs to. These five booleans are what the gate turns into
    /// `structural_breach` and failed safety assertions, and with only the one isolation case above, dropping
    /// `candidateReadModelsAbsent` or `sentinelsUnchangedDuringFault` from the aggregation published
    /// `candidate-absent: true` / `unauthorized-mutation-absent: true` on a run where the rejected candidate had
    /// materialized or the fault had mutated a tenant.
    /// </summary>
    /// <param name="falseInput">Index of the single observation flipped to false.</param>
    /// <param name="expectedFalseFact">Name of the published fact that must become false.</param>
    [Theory]
    [InlineData(0, nameof(ControlledLossPathSafetyObservation.PreFaultRetained))]
    [InlineData(1, nameof(ControlledLossPathSafetyObservation.CandidateAbsent))]
    [InlineData(2, nameof(ControlledLossPathSafetyObservation.CandidateAbsent))]
    [InlineData(3, nameof(ControlledLossPathSafetyObservation.PostRecoveryRetained))]
    [InlineData(4, nameof(ControlledLossPathSafetyObservation.TenantIsolationPreserved))]
    [InlineData(5, nameof(ControlledLossPathSafetyObservation.TenantIsolationPreserved))]
    [InlineData(6, nameof(ControlledLossPathSafetyObservation.TenantIsolationPreserved))]
    [InlineData(7, nameof(ControlledLossPathSafetyObservation.TenantIsolationPreserved))]
    [InlineData(8, nameof(ControlledLossPathSafetyObservation.TenantIsolationPreserved))]
    [InlineData(9, nameof(ControlledLossPathSafetyObservation.TenantIsolationPreserved))]
    [InlineData(11, nameof(ControlledLossPathSafetyObservation.UnauthorizedMutationAbsent))]
    public void EachControlledLossObservationFalsifiesItsPublishedSafetyFact(int falseInput, string expectedFalseFact)
    {
        bool[] inputs = [.. Enumerable.Repeat(true, 12)];
        inputs[falseInput] = false;

        ControlledLossPathSafetyObservation safety = Evaluate(inputs);

        Fact(safety, expectedFalseFact).ShouldBeFalse();
        foreach (string other in PublishedSafetyFacts.Where(name => !string.Equals(name, expectedFalseFact, StringComparison.Ordinal)))
        {
            Fact(safety, other).ShouldBeTrue();
        }

        Evaluate([.. Enumerable.Repeat(true, 12)]).ShouldBe(
            new ControlledLossPathSafetyObservation(true, true, true, true, true));
    }

    /// <summary>
    /// `sentinelsUnchangedAfterRecovery` is deliberately shared by two facts, so it cannot be covered by the
    /// one-fact-at-a-time theory above.
    /// </summary>
    [Fact]
    public void PostRecoverySentinelChangeFailsBothIsolationAndUnauthorizedMutation()
    {
        bool[] inputs = [.. Enumerable.Repeat(true, 12)];
        inputs[10] = false;

        ControlledLossPathSafetyObservation safety = Evaluate(inputs);

        safety.TenantIsolationPreserved.ShouldBeFalse();
        safety.UnauthorizedMutationAbsent.ShouldBeFalse();
        safety.PreFaultRetained.ShouldBeTrue();
        safety.CandidateAbsent.ShouldBeTrue();
        safety.PostRecoveryRetained.ShouldBeTrue();
    }

    private static readonly string[] PublishedSafetyFacts =
    [
        nameof(ControlledLossPathSafetyObservation.PreFaultRetained),
        nameof(ControlledLossPathSafetyObservation.CandidateAbsent),
        nameof(ControlledLossPathSafetyObservation.PostRecoveryRetained),
        nameof(ControlledLossPathSafetyObservation.TenantIsolationPreserved),
        nameof(ControlledLossPathSafetyObservation.UnauthorizedMutationAbsent),
    ];

    private static ControlledLossPathSafetyObservation Evaluate(bool[] inputs)
        => AspireRecoverySandboxOperations.EvaluateControlledLossSafety(
            inputs[0],
            inputs[1],
            inputs[2],
            inputs[3],
            inputs[4],
            inputs[5],
            inputs[6],
            inputs[7],
            inputs[8],
            inputs[9],
            inputs[10],
            inputs[11]);

    private static bool Fact(ControlledLossPathSafetyObservation safety, string name)
        => name switch
        {
            nameof(ControlledLossPathSafetyObservation.PreFaultRetained) => safety.PreFaultRetained,
            nameof(ControlledLossPathSafetyObservation.CandidateAbsent) => safety.CandidateAbsent,
            nameof(ControlledLossPathSafetyObservation.PostRecoveryRetained) => safety.PostRecoveryRetained,
            nameof(ControlledLossPathSafetyObservation.TenantIsolationPreserved) => safety.TenantIsolationPreserved,
            nameof(ControlledLossPathSafetyObservation.UnauthorizedMutationAbsent) => safety.UnauthorizedMutationAbsent,
            _ => throw new ArgumentOutOfRangeException(nameof(name), name, "Unknown published safety fact."),
        };

    private static LiveControlledLossPathRunner CreateRunner(IControlledLossPathOperations operations)
        => new(operations, CreateOptions());

    private static LiveRecoveryValidationOptions CreateOptions()
        => new()
        {
            Enabled = true,
            EnvironmentName = "Testing",
            TestTenantRef = "replay-test:recovery-validation",
            DatasetRef = "recovery-baseline",
            DatasetVersion = "v1",
            DatasetVolume = 6,
            ProjectionSchemaVersion = "schema-v1",
            ValidationPartitionRef = "recovery-partition-v1",
            ControllerCapability = LiveRecoveryValidationOptions.AspireControllerCapability,
            ControllerSecret = "test-secret",
            PerScenarioTimeout = TimeSpan.FromMinutes(20),
            RestorationTimeout = TimeSpan.FromMinutes(1),
            WorkflowTimeout = TimeSpan.FromHours(4),
            RunnerBudget = TimeSpan.FromHours(5),
            EvidenceDirectory = Path.GetTempPath(),
            EvidenceLocator = "artifact://live-recovery/test",
        };

    private sealed class FakeOperations(TimeSpan rpo) : IControlledLossPathOperations
    {
        private int _clockReads;

        public bool CandidateAbsent { get; set; } = true;
        public bool CancelDuringRejection { get; set; }
        public bool CleanupComplete { get; set; } = true;
        public DateTimeOffset? CandidateObservedAtUtc { get; set; }
        // The live probe reads the physical EventStore tenant, never the `replay-test:` label, so the fake must
        // publish the same derived name a genuine observation carries.
        public string PreFaultTenantRef { get; set; } = "recovery-validation";

        public string PostRecoveryTenantRef { get; set; } = "recovery-validation";
        public Exception? InjectionFailure { get; set; }
        public Exception? CleanupFailure { get; set; }
        public bool Restored { get; private set; }
        public bool Cleaned { get; private set; }

        public DateTimeOffset UtcNow => _clockReads++ == 0 ? Start : Start.AddMinutes(20);

        public ValueTask<DurableCommitObservation> WitnessPreFaultCommitAsync(
            string tenantRef,
            CancellationToken cancellationToken)
            => ValueTask.FromResult(new DurableCommitObservation(
                PreFaultTenantRef,
                "01ARZ3NDEKTSV4RRFFQ69G5FAA",
                "01ARZ3NDEKTSV4RRFFQ69G5FAB",
                1,
                Start.AddMinutes(1)));

        public ValueTask InjectSubscriptionFaultAsync(string tenantRef, CancellationToken cancellationToken)
            => InjectionFailure is null
                ? ValueTask.CompletedTask
                : ValueTask.FromException(InjectionFailure);

        public ValueTask<ControlledLossCandidateObservation> RejectFaultWindowCandidateAsync(
            string tenantRef,
            CancellationToken cancellationToken)
            => CancelDuringRejection
                ? ValueTask.FromCanceled<ControlledLossCandidateObservation>(new CancellationToken(canceled: true))
                : ValueTask.FromResult(new ControlledLossCandidateObservation(
                    "01ARZ3NDEKTSV4RRFFQ69G5FAC",
                    CandidateObservedAtUtc ?? (rpo < TimeSpan.Zero
                        ? Start.AddMinutes(2)
                        : Start.AddMinutes(1).Add(rpo / 2)),
                    Rejected: true));

        public ValueTask RestoreSubscriptionAsync(string tenantRef, CancellationToken cancellationToken)
        {
            Restored = true;
            return ValueTask.CompletedTask;
        }

        public ValueTask<DurableCommitObservation> WitnessPostRecoveryCommitAsync(
            string tenantRef,
            CancellationToken cancellationToken)
            => ValueTask.FromResult(new DurableCommitObservation(
                PostRecoveryTenantRef,
                "01ARZ3NDEKTSV4RRFFQ69G5FAD",
                "01ARZ3NDEKTSV4RRFFQ69G5FAE",
                1,
                Start.AddMinutes(1).Add(rpo)));

        public ValueTask<ControlledLossPathSafetyObservation> ReadSafetyObservationAsync(
            string tenantRef,
            CancellationToken cancellationToken)
            => ValueTask.FromResult(new ControlledLossPathSafetyObservation(
                PreFaultRetained: true,
                CandidateAbsent,
                PostRecoveryRetained: true,
                TenantIsolationPreserved: true,
                UnauthorizedMutationAbsent: true));

        public ValueTask<bool> CleanupAsync(string tenantRef, CancellationToken cancellationToken)
        {
            Cleaned = true;
            return CleanupFailure is null
                ? ValueTask.FromResult(CleanupComplete)
                : ValueTask.FromException<bool>(CleanupFailure);
        }
    }
}
