using Hexalith.ChatBot.Server.Audit;

using Shouldly;

namespace Hexalith.ChatBot.IntegrationTests.Recovery;

/// <summary>Story 12.15 Task 3 measurement and restoration tests for the Tier-3 live continuity runner.</summary>
public sealed class LiveContinuityDrillScenarioRunnerTests
{
    [Theory]
    [InlineData(true, false, true)]
    [InlineData(true, true, false)]
    [InlineData(false, false, false)]
    [InlineData(false, true, false)]
    public void StopControlRequiresCommandSuccessAndAnObservedUnavailableEndpoint(
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
        measurement.ExecutionAssertions!.CleanupComplete.ShouldBeTrue();
        // Not observed by this drill — must never alias TenantIsolationPreserved (a past regression).
        measurement.ExecutionAssertions!.IndependentControlSucceeded.ShouldBeFalse();
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
        measurement.ExecutionAssertions!.CleanupComplete.ShouldBeTrue();
        // Not observed by this drill — must never alias TenantIsolationPreserved (a past regression).
        measurement.ExecutionAssertions!.IndependentControlSucceeded.ShouldBeFalse();
        operations.Calls.ShouldBe([
            "checkpoint-subscription",
            "expire-subscription",
            "observe-subscription",
            "restore-subscription",
            "reconcile-subscription",
            "cleanup-subscription"]);
    }

    [Fact]
    public async Task EventStoreDataLossDerivesRpoFromCommittedBeforeOutageBound()
    {
        RecordingOperations operations = new() { EventStoreReconstructableCount = 0 };
        LiveContinuityDrillScenarioRunner runner = new(operations, Options());

        ContinuityDrillMeasurement measurement = await runner.RunAsync(
            ContinuityDrillScenarios.EventStoreOutage,
            Tenant,
            Correlation,
            TestContext.Current.CancellationToken);

        measurement.DataLossDetected.ShouldBeTrue();
        measurement.MeasuredRpo.ShouldBe(TimeSpan.FromMinutes(1));
        measurement.ExecutionAssertions!.CleanupComplete.ShouldBeTrue();
    }

    [Fact]
    public async Task SubscriptionDataLossDerivesRpoFromCommittedBeforeOutageCheckpoint()
    {
        RecordingOperations operations = new() { SubscriptionNoSilentLoss = false };
        LiveContinuityDrillScenarioRunner runner = new(operations, Options());

        ContinuityDrillMeasurement measurement = await runner.RunAsync(
            ContinuityDrillScenarios.M365SubscriptionFailure,
            Tenant,
            Correlation,
            TestContext.Current.CancellationToken);

        measurement.DataLossDetected.ShouldBeTrue();
        measurement.MeasuredRpo.ShouldBe(TimeSpan.FromMinutes(1));
        operations.Calls.ShouldContain("checkpoint-subscription");
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
    public async Task SubscriptionInjectionFailureStillRestoresAndCleans()
    {
        RecordingOperations operations = new() { ThrowOnExpireSubscription = true };
        LiveContinuityDrillScenarioRunner runner = new(operations, Options());

        _ = await Should.ThrowAsync<InvalidOperationException>(() => runner.RunAsync(
            ContinuityDrillScenarios.M365SubscriptionFailure,
            Tenant,
            Correlation,
            TestContext.Current.CancellationToken).AsTask());

        operations.Calls.ShouldContain("restore-subscription");
        operations.Calls.ShouldContain("cleanup-subscription");
    }

    [Fact]
    public async Task CanceledMeasurementStillRestoresAndCleansTheFaultedResource()
    {
        using CancellationTokenSource canceled = new();
        RecordingOperations operations = new() { CancelObservationToken = canceled };
        LiveContinuityDrillScenarioRunner runner = new(operations, Options());

        _ = await Should.ThrowAsync<OperationCanceledException>(() => runner.RunAsync(
            ContinuityDrillScenarios.EventStoreOutage,
            Tenant,
            Correlation,
            canceled.Token).AsTask());

        operations.Calls.ShouldContain("start-eventstore");
        operations.Calls.ShouldContain("cleanup-eventstore");
        operations.StartEventStoreTokenWasCanceled.ShouldBe(false);
        operations.WaitEventStoreTokenWasCanceled.ShouldBe(false);
        operations.CleanupEventStoreTokenWasCanceled.ShouldBe(false);
    }

    [Fact]
    public async Task CanceledObservationWithFailedRestoreIsScenarioFailureNotBareCancel()
    {
        using CancellationTokenSource canceled = new();
        RecordingOperations operations = new()
        {
            CancelObservationToken = canceled,
            ThrowOnEventStoreStart = true,
        };
        LiveContinuityDrillScenarioRunner runner = new(operations, Options());

        AggregateException thrown = await Should.ThrowAsync<AggregateException>(() => runner.RunAsync(
            ContinuityDrillScenarios.EventStoreOutage,
            Tenant,
            Correlation,
            canceled.Token).AsTask());

        thrown.InnerExceptions.ShouldContain(static ex => ex is OperationCanceledException);
        thrown.InnerExceptions.ShouldContain(static ex => ex is InvalidOperationException);
        operations.Calls.ShouldContain("cleanup-eventstore");
    }

    [Fact]
    public async Task SubSecondReversedRtoBoundsClampToZero()
    {
        RecordingOperations operations = new()
        {
            FaultObservedAt = Started + TimeSpan.FromMinutes(1),
            RecoveredAt = Started + TimeSpan.FromMinutes(1) - TimeSpan.FromMilliseconds(500),
        };
        LiveContinuityDrillScenarioRunner runner = new(operations, Options());

        ContinuityDrillMeasurement measurement = await runner.RunAsync(
            ContinuityDrillScenarios.EventStoreOutage,
            Tenant,
            Correlation,
            TestContext.Current.CancellationToken);

        measurement.MeasuredRto.ShouldBe(TimeSpan.Zero);
    }

    [Fact]
    public async Task LargerReversedRtoBoundsStayUnmeasurable()
    {
        RecordingOperations operations = new()
        {
            FaultObservedAt = Started + TimeSpan.FromMinutes(1),
            RecoveredAt = Started + TimeSpan.FromMinutes(1) - TimeSpan.FromSeconds(2),
        };
        LiveContinuityDrillScenarioRunner runner = new(operations, Options());

        InvalidOperationException thrown = await Should.ThrowAsync<InvalidOperationException>(() => runner.RunAsync(
            ContinuityDrillScenarios.EventStoreOutage,
            Tenant,
            Correlation,
            TestContext.Current.CancellationToken).AsTask());

        thrown.Message.ShouldContain("reversed");
    }

    [Fact]
    public async Task CleanupFailureAfterSuccessfulMeasurementPropagates()
    {
        RecordingOperations operations = new() { ThrowOnEventStoreCleanup = true };
        LiveContinuityDrillScenarioRunner runner = new(operations, Options());

        _ = await Should.ThrowAsync<InvalidOperationException>(() => runner.RunAsync(
            ContinuityDrillScenarios.EventStoreOutage,
            Tenant,
            Correlation,
            TestContext.Current.CancellationToken).AsTask());
    }

    [Fact]
    public async Task CleanupFailureAfterSuccessfulRestoreButFailedDrillPropagates()
    {
        RecordingOperations operations = new()
        {
            ThrowOnEventStoreStop = true,
            ThrowOnEventStoreCleanup = true,
        };
        LiveContinuityDrillScenarioRunner runner = new(operations, Options());

        InvalidOperationException thrown = await Should.ThrowAsync<InvalidOperationException>(() => runner.RunAsync(
            ContinuityDrillScenarios.EventStoreOutage,
            Tenant,
            Correlation,
            TestContext.Current.CancellationToken).AsTask());

        thrown.Message.ShouldContain("cleanup");
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

    [Fact]
    public async Task NullCorrelationIsRejectedBeforeAnyFaultOperation()
    {
        RecordingOperations operations = new();
        LiveContinuityDrillScenarioRunner runner = new(operations, Options());

        _ = await Should.ThrowAsync<ArgumentException>(() => runner.RunAsync(
            ContinuityDrillScenarios.EventStoreOutage,
            Tenant,
            "  ",
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

    private sealed class RecordingOperations : IRecoverySandboxOperations
    {
        public List<string> Calls { get; } = [];
        public bool ThrowOnEventStoreStop { get; init; }
        public bool ThrowOnEventStoreStart { get; init; }
        public bool ThrowOnExpireSubscription { get; init; }
        public bool ThrowOnEventStoreCleanup { get; init; }
        public CancellationTokenSource? CancelObservationToken { get; init; }
        public int EventStoreReconstructableCount { get; init; } = 1;
        public bool SubscriptionNoSilentLoss { get; init; } = true;
        public DateTimeOffset? FaultObservedAt { get; init; }
        public DateTimeOffset? RecoveredAt { get; init; }
        public bool? StartEventStoreTokenWasCanceled { get; private set; }
        public bool? WaitEventStoreTokenWasCanceled { get; private set; }
        public bool? CleanupEventStoreTokenWasCanceled { get; private set; }
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

        public ValueTask<RecoveryFaultObservation> ObserveEventStoreFaultAsync(
            string tenantRef,
            string correlationId,
            CancellationToken cancellationToken)
        {
            Calls.Add("observe-eventstore");
            if (CancelObservationToken is not null)
            {
                CancelObservationToken.Cancel();
                cancellationToken.ThrowIfCancellationRequested();
            }

            return ValueTask.FromResult(new RecoveryFaultObservation(
                FaultObservedAt ?? Started + TimeSpan.FromMinutes(1),
                "eventstore-unavailable"));
        }

        public ValueTask StartEventStoreAsync(CancellationToken cancellationToken)
        {
            Calls.Add("start-eventstore");
            StartEventStoreTokenWasCanceled = cancellationToken.IsCancellationRequested;
            return ThrowOnEventStoreStart
                ? ValueTask.FromException(new InvalidOperationException("start failed"))
                : ValueTask.CompletedTask;
        }

        public ValueTask<DateTimeOffset> WaitForEventStoreRecoveryAsync(CancellationToken cancellationToken)
        {
            Calls.Add("wait-eventstore");
            WaitEventStoreTokenWasCanceled = cancellationToken.IsCancellationRequested;
            return ValueTask.FromResult(RecoveredAt ?? Started + TimeSpan.FromMinutes(3));
        }

        public ValueTask<RecoveryEventStoreEndState> ReadEventStoreEndStateAsync(RecoveryOperationCheckpoint checkpoint, CancellationToken cancellationToken)
        {
            Calls.Add("read-eventstore");
            return ValueTask.FromResult(new RecoveryEventStoreEndState(
                RecoveredAt ?? Started + TimeSpan.FromMinutes(3),
                EventStoreReconstructableCount,
                TenantIsolationPreserved: true,
                UnauthorizedMutationAbsent: true));
        }

        public ValueTask<bool> CleanupEventStoreScenarioAsync(CancellationToken cancellationToken)
        {
            Calls.Add("cleanup-eventstore");
            CleanupEventStoreTokenWasCanceled = cancellationToken.IsCancellationRequested;
            return ThrowOnEventStoreCleanup
                ? ValueTask.FromException<bool>(new InvalidOperationException("cleanup failed"))
                : ValueTask.FromResult(true);
        }

        public ValueTask<RecoveryOperationCheckpoint> CheckpointSubscriptionCommittedBoundAsync(
            string tenantRef,
            string correlationId,
            CancellationToken cancellationToken)
        {
            Calls.Add("checkpoint-subscription");
            return ValueTask.FromResult(new RecoveryOperationCheckpoint(1, Started, "subscription-bound-001"));
        }

        public ValueTask ExpireSubscriptionAsync(string tenantRef, CancellationToken cancellationToken)
        {
            Calls.Add("expire-subscription");
            return ThrowOnExpireSubscription
                ? ValueTask.FromException(new InvalidOperationException("expire failed"))
                : ValueTask.CompletedTask;
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
                NoSilentLoss: SubscriptionNoSilentLoss,
                NoDuplicateSideEffects: true,
                TenantIsolationPreserved: true,
                UnauthorizedMutationAbsent: true));
        }

        public ValueTask<bool> CleanupSubscriptionScenarioAsync(string tenantRef, CancellationToken cancellationToken)
        {
            Calls.Add("cleanup-subscription");
            return ValueTask.FromResult(true);
        }
    }
}
