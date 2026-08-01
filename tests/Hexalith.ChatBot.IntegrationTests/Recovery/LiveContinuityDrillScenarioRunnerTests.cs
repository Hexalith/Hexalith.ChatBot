using Hexalith.ChatBot.Server.Audit;

using Shouldly;

namespace Hexalith.ChatBot.IntegrationTests.Recovery;

/// <summary>Story 12.15 Task 3 measurement and restoration tests for the Tier-3 live continuity runner.</summary>
public sealed class LiveContinuityDrillScenarioRunnerTests
{
    [Theory]
    [InlineData(true, true, true)]
    [InlineData(false, false, true)]
    [InlineData(false, true, false)]
    public void StopControlRequiresEitherCommandSuccessOrAnObservedUnavailableEndpoint(
        bool commandSucceeded,
        bool endpointAvailable,
        bool expected)
        => AspireRecoverySandboxOperations
            .StopReachedDependencyBoundary(commandSucceeded, endpointAvailable)
            .ShouldBe(expected);

    private const string Tenant = "replay-test:recovery-validation";
    private const string Correlation = "01ARZ3NDEKTSV4RRFFQ69G5FAW";
    private static readonly DateTimeOffset Started = new(2026, 8, 1, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task EventStoreScenarioDerivesRpoAndRtoFromObservedCheckpointAndRecovery()
    {
        RecordingOperations operations = new();
        LiveContinuityDrillScenarioRunner runner = new(operations, Options());

        ContinuityDrillMeasurement measurement = await runner.RunAsync(
            ContinuityDrillScenarios.EventStoreOutage,
            Tenant,
            Correlation,
            TestContext.Current.CancellationToken);

        measurement.StartedAtUtc.ShouldBe(Started);
        measurement.EndedAtUtc.ShouldBe(Started + TimeSpan.FromMinutes(3));
        measurement.MeasuredRpo.ShouldBe(TimeSpan.Zero);
        measurement.MeasuredRto.ShouldBe(TimeSpan.FromMinutes(2));
        measurement.DataLossDetected.ShouldBeFalse();
        operations.Calls.ShouldBe(["seed", "stop-eventstore", "observe-eventstore", "start-eventstore", "wait-eventstore", "read-eventstore", "cleanup-eventstore"]);
    }

    [Fact]
    public async Task M365ScenarioExercisesWorkerFailureRestorationAndReconciliation()
    {
        RecordingOperations operations = new();
        LiveContinuityDrillScenarioRunner runner = new(operations, Options());

        ContinuityDrillMeasurement measurement = await runner.RunAsync(
            ContinuityDrillScenarios.M365SubscriptionFailure,
            Tenant,
            Correlation,
            TestContext.Current.CancellationToken);

        measurement.StartedAtUtc.ShouldBe(Started);
        measurement.EndedAtUtc.ShouldBe(Started + TimeSpan.FromMinutes(3));
        measurement.MeasuredRpo.ShouldBe(TimeSpan.Zero);
        measurement.MeasuredRto.ShouldBe(TimeSpan.FromMinutes(2));
        measurement.DataLossDetected.ShouldBeFalse();
        operations.Calls.ShouldBe(["expire-subscription", "observe-subscription", "restore-subscription", "reconcile-subscription", "cleanup-subscription"]);
    }

    [Fact]
    public async Task FailedInjectionStillAttemptsRestorationAndCleanup()
    {
        RecordingOperations operations = new() { ThrowOnEventStoreStop = true };
        LiveContinuityDrillScenarioRunner runner = new(operations, Options());

        _ = await Should.ThrowAsync<InvalidOperationException>(() => runner.RunAsync(
            ContinuityDrillScenarios.EventStoreOutage,
            Tenant,
            Correlation,
            TestContext.Current.CancellationToken).AsTask());

        operations.Calls.ShouldContain("start-eventstore");
        operations.Calls.ShouldContain("cleanup-eventstore");
    }

    [Fact]
    public async Task CanceledMeasurementStillRestoresAndCleansTheFaultedResource()
    {
        RecordingOperations operations = new() { WaitForObservationCancellation = true };
        LiveContinuityDrillScenarioRunner runner = new(operations, Options());
        using CancellationTokenSource canceled = CancellationTokenSource.CreateLinkedTokenSource(
            TestContext.Current.CancellationToken);
        canceled.CancelAfter(TimeSpan.FromMilliseconds(25));

        _ = await Should.ThrowAsync<OperationCanceledException>(() => runner.RunAsync(
            ContinuityDrillScenarios.EventStoreOutage,
            Tenant,
            Correlation,
            canceled.Token).AsTask());

        operations.Calls.ShouldContain("start-eventstore");
        operations.Calls.ShouldContain("cleanup-eventstore");
    }

    [Fact]
    public async Task DisabledLiveModeIsRejectedBeforeAnyFaultOperation()
    {
        RecordingOperations operations = new();
        LiveRecoveryValidationOptions options = Options();
        options.Enabled = false;
        LiveContinuityDrillScenarioRunner runner = new(operations, options);

        _ = await Should.ThrowAsync<InvalidOperationException>(() => runner.RunAsync(
            ContinuityDrillScenarios.EventStoreOutage,
            Tenant,
            Correlation,
            TestContext.Current.CancellationToken).AsTask());

        operations.Calls.ShouldBeEmpty();
    }

    [Fact]
    public async Task UnknownScenarioIsRejectedBeforeAnyFaultOperation()
    {
        RecordingOperations operations = new();
        LiveContinuityDrillScenarioRunner runner = new(operations, Options());

        _ = await Should.ThrowAsync<InvalidOperationException>(() => runner.RunAsync(
            "unknown-scenario",
            Tenant,
            Correlation,
            TestContext.Current.CancellationToken).AsTask());

        operations.Calls.ShouldBeEmpty();
    }

    private static LiveRecoveryValidationOptions Options()
        => new()
        {
            Enabled = true,
            EnvironmentName = "Testing",
            TestTenantRef = Tenant,
            DatasetRef = "recovery-baseline",
            DatasetVersion = "v1",
            DatasetVolume = 6,
            ProjectionSchemaVersion = "project-conversation-v1",
            ValidationPartitionRef = "recovery-partition-v1",
            ControllerCapability = LiveRecoveryValidationOptions.AspireControllerCapability,
            ControllerSecret = "tier3-value",
            PerScenarioTimeout = RecoveryTargets.MaxRto,
            WorkflowTimeout = TimeSpan.FromHours(5),
            EvidenceDirectory = Path.GetFullPath("TestResults/live-recovery"),
            EvidenceLocator = "artifact:live-recovery-validation",
        };

    private sealed class RecordingOperations : IRecoverySandboxOperations
    {
        public List<string> Calls { get; } = [];
        public bool ThrowOnEventStoreStop { get; init; }
        public bool WaitForObservationCancellation { get; init; }
        public DateTimeOffset UtcNow => Started;

        public ValueTask<RecoveryOperationCheckpoint> SeedCommittedOperationAsync(string tenantRef, string correlationId, CancellationToken cancellationToken)
        {
            Calls.Add("seed");
            return ValueTask.FromResult(new RecoveryOperationCheckpoint(1, Started, "operation-001"));
        }

        public ValueTask StopEventStoreAsync(CancellationToken cancellationToken)
        {
            Calls.Add("stop-eventstore");
            return ThrowOnEventStoreStop
                ? ValueTask.FromException(new InvalidOperationException("stop failed"))
                : ValueTask.CompletedTask;
        }

        public async ValueTask<RecoveryFaultObservation> ObserveEventStoreFaultAsync(
            string tenantRef,
            string correlationId,
            CancellationToken cancellationToken)
        {
            Calls.Add("observe-eventstore");
            if (WaitForObservationCancellation)
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken).ConfigureAwait(false);
            }

            return new RecoveryFaultObservation(Started + TimeSpan.FromMinutes(1), "eventstore-unavailable");
        }

        public ValueTask StartEventStoreAsync(CancellationToken cancellationToken)
        {
            Calls.Add("start-eventstore");
            return ValueTask.CompletedTask;
        }

        public ValueTask<DateTimeOffset> WaitForEventStoreRecoveryAsync(CancellationToken cancellationToken)
        {
            Calls.Add("wait-eventstore");
            return ValueTask.FromResult(Started + TimeSpan.FromMinutes(3));
        }

        public ValueTask<RecoveryEventStoreEndState> ReadEventStoreEndStateAsync(RecoveryOperationCheckpoint checkpoint, CancellationToken cancellationToken)
        {
            Calls.Add("read-eventstore");
            return ValueTask.FromResult(new RecoveryEventStoreEndState(1, TenantIsolationPreserved: true, UnauthorizedMutationAbsent: true));
        }

        public ValueTask CleanupEventStoreScenarioAsync(CancellationToken cancellationToken)
        {
            Calls.Add("cleanup-eventstore");
            return ValueTask.CompletedTask;
        }

        public ValueTask ExpireSubscriptionAsync(string tenantRef, CancellationToken cancellationToken)
        {
            Calls.Add("expire-subscription");
            return ValueTask.CompletedTask;
        }

        public ValueTask<RecoveryFaultObservation> ObserveSubscriptionFaultAsync(string tenantRef, string correlationId, CancellationToken cancellationToken)
        {
            Calls.Add("observe-subscription");
            return ValueTask.FromResult(new RecoveryFaultObservation(Started + TimeSpan.FromMinutes(1), "graph-subscription-expired"));
        }

        public ValueTask RestoreSubscriptionAsync(string tenantRef, CancellationToken cancellationToken)
        {
            Calls.Add("restore-subscription");
            return ValueTask.CompletedTask;
        }

        public ValueTask<RecoverySubscriptionEndState> ReconcileSubscriptionAsync(string tenantRef, string correlationId, CancellationToken cancellationToken)
        {
            Calls.Add("reconcile-subscription");
            return ValueTask.FromResult(new RecoverySubscriptionEndState(
                Started + TimeSpan.FromMinutes(3),
                DeliveredCount: 1,
                NoSilentLoss: true,
                NoDuplicateSideEffects: true,
                TenantIsolationPreserved: true,
                UnauthorizedMutationAbsent: true));
        }

        public ValueTask CleanupSubscriptionScenarioAsync(string tenantRef, CancellationToken cancellationToken)
        {
            Calls.Add("cleanup-subscription");
            return ValueTask.CompletedTask;
        }
    }
}
