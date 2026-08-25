using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Gateway.Stages;

using Shouldly;

namespace Hexalith.ChatBot.IntegrationTests.Recovery;

/// <summary>Focused measurement, coverage, and restoration tests for the live scoped-outage driver.</summary>
public sealed class LiveScopedOutageInjectionDriverTests
{
    [Theory]
    [InlineData(ScopedOutageDependencies.Graph, ScopedOutageScopes.Mailbox)]
    [InlineData(ScopedOutageDependencies.Identity, ScopedOutageScopes.ServiceClient)]
    [InlineData(ScopedOutageDependencies.AiProvider, ScopedOutageScopes.Operation)]
    [InlineData(ScopedOutageDependencies.CommandExecution, ScopedOutageScopes.Operation)]
    [InlineData(ScopedOutageDependencies.AuditStore, ScopedOutageScopes.CommandSurface)]
    [InlineData(ScopedOutageDependencies.AttachmentProcessing, ScopedOutageScopes.WorkflowItem)]
    public void ExpectedScopeMapsEveryCanonicalDependency(string dependency, string expectedScope)
        => LiveScopedOutageInjectionDriver.ExpectedScope(dependency).ShouldBe(expectedScope);

    [Fact]
    public void CheckpointRestoreRouteCannotAcquireAnUnsupportedCorrelationSegment()
    {
        string route = RecoverySandboxRoute.ScopedOutage(
            RecoveryValidationTopology.LogicalTenantRef,
            ScopedOutageDependencies.AiProvider,
            "restore");

        route.ShouldBe("/recovery/replay-test%3Arecovery-validation/scoped-outage/ai-provider/restore");
        _ = Should.Throw<InvalidOperationException>(() => RecoverySandboxRoute.ScopedOutage(
            RecoveryValidationTopology.LogicalTenantRef,
            ScopedOutageDependencies.AiProvider,
            "restore",
            "01ARZ3NDEKTSV4RRFFQ69G5FAW"));
    }

    [Fact]
    public async Task CoordinatorRunsAllSixObservedScenariosThroughTheLiveDriver()
    {
        RecordingOperations operations = new();
        LiveScopedOutageInjectionDriver driver = new(operations, Options());
        ScopedOutageDegradationValidationCoordinator coordinator = new(
            driver,
            new InMemoryAuditWriter(),
            new InMemoryOperatorAlertSink(),
            new SystemClock());

        ScopedOutageDegradationOutcome outcome = await coordinator.RunAllScenariosAsync(
            RecoveryValidationTopology.LogicalTenantRef,
            "01ARZ3NDEKTSV4RRFFQ69G5FAW",
            TestContext.Current.CancellationToken);

        outcome.ScenariosValidated.ShouldBe(ScopedOutageDependencies.All.Count);
        outcome.Contained.ShouldBe(ScopedOutageDependencies.All.Count);
        outcome.Breached.ShouldBe(0);
        outcome.ScopeRecordingExceeded.ShouldBe(0);
        outcome.Unmeasurable.ShouldBe(0);
        outcome.Alerted.ShouldBe(0);
        operations.Faulted.ShouldBe(ScopedOutageDependencies.All, ignoreOrder: true);
        operations.Restored.ShouldBe(ScopedOutageDependencies.All, ignoreOrder: true);
        operations.Cleaned.ShouldBe(ScopedOutageDependencies.All, ignoreOrder: true);
    }

    [Fact]
    public async Task ObservationFailureStillRestoresBeforeTheDriverFailsClosed()
    {
        RecordingOperations operations = new() { FailObservation = true };
        LiveScopedOutageInjectionDriver driver = new(operations, Options());

        _ = await Should.ThrowAsync<InvalidOperationException>(() => driver.InjectAndMeasureAsync(
            ScopedOutageDependencies.AuditStore,
            RecoveryValidationTopology.LogicalTenantRef,
            "01ARZ3NDEKTSV4RRFFQ69G5FAW",
            TestContext.Current.CancellationToken).AsTask());

        operations.Restored.ShouldContain(ScopedOutageDependencies.AuditStore);
        operations.Cleaned.ShouldContain(ScopedOutageDependencies.AuditStore);
    }

    [Fact]
    public async Task AMismatchedObservedScopeSurvivesIntoTheMeasurementRatherThanBeingCoercedToMatch()
    {
        // Regression guard for the tautology the round-2 review flagged: a fixture that always fed
        // ExpectedScope(dependency) back as the observed value made a real scope escape structurally
        // unreachable. This asserts the driver is a faithful pass-through, not a second copy of the table.
        RecordingOperations operations = new() { ObservedScopeOverride = ScopedOutageScopes.Tenant };
        LiveScopedOutageInjectionDriver driver = new(operations, Options());

        ScopedOutageDegradationMeasurement measurement = await driver.InjectAndMeasureAsync(
            ScopedOutageDependencies.AiProvider,
            RecoveryValidationTopology.LogicalTenantRef,
            "01ARZ3NDEKTSV4RRFFQ69G5FAW",
            TestContext.Current.CancellationToken);

        measurement.ExpectedScope.ShouldBe(ScopedOutageScopes.Operation);
        measurement.ObservedScope.ShouldBe(ScopedOutageScopes.Tenant);
        measurement.ObservedScope.ShouldNotBe(measurement.ExpectedScope);
    }

    [Theory]
    [InlineData(ScopedOutageDependencies.Graph, "tenant-alpha")]
    [InlineData("unknown-dependency", RecoveryValidationTopology.LogicalTenantRef)]
    public async Task UnsafeTenantOrUnknownDependencyIsRejectedBeforeInjection(string dependency, string tenantRef)
    {
        RecordingOperations operations = new();
        LiveScopedOutageInjectionDriver driver = new(operations, Options());

        _ = await Should.ThrowAsync<InvalidOperationException>(() => driver.InjectAndMeasureAsync(
            dependency,
            tenantRef,
            "01ARZ3NDEKTSV4RRFFQ69G5FAW",
            TestContext.Current.CancellationToken).AsTask());

        operations.Faulted.ShouldBeEmpty();
        operations.Restored.ShouldBeEmpty();
        operations.Cleaned.ShouldBeEmpty();
    }

    [Fact]
    public async Task DisabledLiveModeIsRejectedBeforeInjection()
    {
        RecordingOperations operations = new();
        LiveRecoveryValidationOptions options = Options();
        options.Enabled = false;
        LiveScopedOutageInjectionDriver driver = new(operations, options);

        _ = await Should.ThrowAsync<InvalidOperationException>(() => driver.InjectAndMeasureAsync(
            ScopedOutageDependencies.Graph,
            RecoveryValidationTopology.LogicalTenantRef,
            "01ARZ3NDEKTSV4RRFFQ69G5FAW",
            TestContext.Current.CancellationToken).AsTask());

        operations.Faulted.ShouldBeEmpty();
        operations.Restored.ShouldBeEmpty();
        operations.Cleaned.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public async Task RestorationOrCleanupFailureFailsClosedAfterBothAreAttempted(
        bool failRestoration,
        bool failCleanup)
    {
        RecordingOperations operations = new()
        {
            FailRestoration = failRestoration,
            FailCleanup = failCleanup,
        };
        LiveScopedOutageInjectionDriver driver = new(operations, Options());

        _ = await Should.ThrowAsync<InvalidOperationException>(() => driver.InjectAndMeasureAsync(
            ScopedOutageDependencies.AttachmentProcessing,
            RecoveryValidationTopology.LogicalTenantRef,
            "01ARZ3NDEKTSV4RRFFQ69G5FAW",
            TestContext.Current.CancellationToken).AsTask());

        operations.Restored.ShouldContain(ScopedOutageDependencies.AttachmentProcessing);
        operations.Cleaned.ShouldContain(ScopedOutageDependencies.AttachmentProcessing);
    }

    [Fact]
    public async Task DualRestorationAndCleanupFailureSurfacesAggregateException()
    {
        RecordingOperations operations = new()
        {
            FailRestoration = true,
            FailCleanup = true,
        };
        LiveScopedOutageInjectionDriver driver = new(operations, Options());

        AggregateException thrown = await Should.ThrowAsync<AggregateException>(() => driver.InjectAndMeasureAsync(
            ScopedOutageDependencies.AttachmentProcessing,
            RecoveryValidationTopology.LogicalTenantRef,
            "01ARZ3NDEKTSV4RRFFQ69G5FAW",
            TestContext.Current.CancellationToken).AsTask());

        thrown.InnerExceptions.Count.ShouldBe(2);
        operations.Restored.ShouldContain(ScopedOutageDependencies.AttachmentProcessing);
        operations.Cleaned.ShouldContain(ScopedOutageDependencies.AttachmentProcessing);
    }

    [Fact]
    public async Task ObservedNonRecoverySurvivesIntoTheMeasurement()
    {
        RecordingOperations operations = new() { AffectedOperationRecovered = false };
        LiveScopedOutageInjectionDriver driver = new(operations, Options());

        ScopedOutageDegradationMeasurement measurement = await driver.InjectAndMeasureAsync(
            ScopedOutageDependencies.CommandExecution,
            RecoveryValidationTopology.LogicalTenantRef,
            "01ARZ3NDEKTSV4RRFFQ69G5FAW",
            TestContext.Current.CancellationToken);

        measurement.InflightItemsRecoverable.ShouldBeFalse();
        measurement.ExecutionAssertions!.CleanupComplete.ShouldBeTrue();
    }

    [Fact]
    public async Task ACrossTenantEffectDetectedBeforeRestoreFailsIsolationEvenWhenThePostRestoreCheckReportsClean()
    {
        // Restoration clears the sandbox's effect ledger for the affected dependency, so a leak that happened during
        // the fault window (the highest-risk moment) is otherwise invisible to VerifyRecoveryAsync's post-restore
        // probes. RestoreAsync must report what it observed before clearing, and the driver must fail closed on it
        // even though the fake's VerifyRecoveryAsync (unconditionally CrossTenantLeakageDetected: false) alone would
        // report a clean run.
        RecordingOperations operations = new() { CrossTenantEffectDetectedBeforeRestore = true };
        LiveScopedOutageInjectionDriver driver = new(operations, Options());

        ScopedOutageDegradationMeasurement measurement = await driver.InjectAndMeasureAsync(
            ScopedOutageDependencies.CommandExecution,
            RecoveryValidationTopology.LogicalTenantRef,
            "01ARZ3NDEKTSV4RRFFQ69G5FAW",
            TestContext.Current.CancellationToken);

        measurement.CrossTenantLeakageDetected.ShouldBeTrue();
        measurement.ExecutionAssertions!.TenantIsolationPreserved.ShouldBeFalse();
    }

    [Fact]
    public async Task FailedIndependentControlFailsClosedAfterRestoreAndCleanup()
    {
        RecordingOperations operations = new() { IndependentControlSucceeded = false };
        LiveScopedOutageInjectionDriver driver = new(operations, Options());

        _ = await Should.ThrowAsync<InvalidOperationException>(() => driver.InjectAndMeasureAsync(
            ScopedOutageDependencies.AiProvider,
            RecoveryValidationTopology.LogicalTenantRef,
            "01ARZ3NDEKTSV4RRFFQ69G5FAW",
            TestContext.Current.CancellationToken).AsTask());

        operations.Restored.ShouldContain(ScopedOutageDependencies.AiProvider);
        operations.Cleaned.ShouldContain(ScopedOutageDependencies.AiProvider);
    }

    /// <summary>
    /// A control probe that was never answered and one that answered a non-<c>202</c> are both fail-closed, but
    /// they are different investigations — missing containment evidence versus negative containment evidence — so
    /// the driver must report which one happened instead of collapsing both into one message.
    /// </summary>
    /// <param name="unobserved">Whether the control probe timed out rather than answering a non-202.</param>
    /// <param name="expectedCause">The cause the driver must name.</param>
    [Theory]
    [InlineData(true, "was never observed")]
    [InlineData(false, "was refused")]
    public async Task IndependentControlFailureNamesWhetherTheProbeWasAnsweredOrNegative(
        bool unobserved,
        string expectedCause)
    {
        RecordingOperations operations = new()
        {
            IndependentControlSucceeded = false,
            IndependentControlUnobserved = unobserved,
        };
        LiveScopedOutageInjectionDriver driver = new(operations, Options());

        InvalidOperationException failure = await Should.ThrowAsync<InvalidOperationException>(() =>
            driver.InjectAndMeasureAsync(
                ScopedOutageDependencies.Graph,
                RecoveryValidationTopology.LogicalTenantRef,
                "01ARZ3NDEKTSV4RRFFQ69G5FAW",
                TestContext.Current.CancellationToken).AsTask());

        // Both shapes must stay fail-closed; only the reported cause differs.
        failure.ToString().ShouldContain(expectedCause);
        operations.Restored.ShouldContain(ScopedOutageDependencies.Graph);
        operations.Cleaned.ShouldContain(ScopedOutageDependencies.Graph);
    }

    [Fact]
    public async Task CheckpointFailureStillCleansAndSurfacesBothFailures()
    {
        RecordingOperations operations = new()
        {
            FailCheckpoint = true,
            FailCleanup = true,
        };
        LiveScopedOutageInjectionDriver driver = new(operations, Options());

        AggregateException thrown = await Should.ThrowAsync<AggregateException>(() => driver.InjectAndMeasureAsync(
            ScopedOutageDependencies.Graph,
            RecoveryValidationTopology.LogicalTenantRef,
            "01ARZ3NDEKTSV4RRFFQ69G5FAW",
            TestContext.Current.CancellationToken).AsTask());

        thrown.InnerExceptions.Count.ShouldBe(2);
        operations.Cleaned.ShouldContain(ScopedOutageDependencies.Graph);
        operations.Faulted.ShouldBeEmpty();
    }

    [Fact]
    public async Task CanceledObservationStillRestoresAndCleansOnIndependentTokens()
    {
        using CancellationTokenSource canceled = new();
        RecordingOperations operations = new() { CancelObservationToken = canceled };
        LiveScopedOutageInjectionDriver driver = new(operations, Options());

        _ = await Should.ThrowAsync<OperationCanceledException>(() => driver.InjectAndMeasureAsync(
            ScopedOutageDependencies.AiProvider,
            RecoveryValidationTopology.LogicalTenantRef,
            "01ARZ3NDEKTSV4RRFFQ69G5FAW",
            canceled.Token).AsTask());

        operations.Restored.ShouldContain(ScopedOutageDependencies.AiProvider);
        operations.Cleaned.ShouldContain(ScopedOutageDependencies.AiProvider);
        operations.RestoreTokenWasCanceled.ShouldAllBe(static canceledToken => !canceledToken);
        operations.CleanupTokenWasCanceled.ShouldAllBe(static canceledToken => !canceledToken);
    }

    [Fact]
    public async Task CanceledObservationWithFailedRestoreIsScenarioFailureNotBareCancel()
    {
        using CancellationTokenSource canceled = new();
        RecordingOperations operations = new()
        {
            CancelObservationToken = canceled,
            FailRestoration = true,
        };
        LiveScopedOutageInjectionDriver driver = new(operations, Options());

        InvalidOperationException thrown = await Should.ThrowAsync<InvalidOperationException>(() => driver.InjectAndMeasureAsync(
            ScopedOutageDependencies.AiProvider,
            RecoveryValidationTopology.LogicalTenantRef,
            "01ARZ3NDEKTSV4RRFFQ69G5FAW",
            canceled.Token).AsTask());

        thrown.ShouldNotBeOfType<OperationCanceledException>();
        operations.Restored.ShouldContain(ScopedOutageDependencies.AiProvider);
        operations.Cleaned.ShouldContain(ScopedOutageDependencies.AiProvider);
    }

    [Fact]
    public async Task SubSecondReversedScopeRecordingLatencyClampsToZero()
    {
        RecordingOperations operations = new() { ScopeRecordingSkew = TimeSpan.FromMilliseconds(500) };
        LiveScopedOutageInjectionDriver driver = new(operations, Options());

        ScopedOutageDegradationMeasurement measurement = await driver.InjectAndMeasureAsync(
            ScopedOutageDependencies.CommandExecution,
            RecoveryValidationTopology.LogicalTenantRef,
            "01ARZ3NDEKTSV4RRFFQ69G5FAW",
            TestContext.Current.CancellationToken);

        measurement.ScopeRecordingLatency.ShouldBe(TimeSpan.Zero);
    }

    [Fact]
    public async Task LargerReversedScopeRecordingLatencyStaysUnmeasurable()
    {
        RecordingOperations operations = new() { ScopeRecordingSkew = TimeSpan.FromSeconds(2) };
        LiveScopedOutageInjectionDriver driver = new(operations, Options());

        InvalidOperationException thrown = await Should.ThrowAsync<InvalidOperationException>(() => driver.InjectAndMeasureAsync(
            ScopedOutageDependencies.CommandExecution,
            RecoveryValidationTopology.LogicalTenantRef,
            "01ARZ3NDEKTSV4RRFFQ69G5FAW",
            TestContext.Current.CancellationToken).AsTask());

        thrown.Message.ShouldContain("preceded");
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

    private sealed class RecordingOperations : IScopedOutageSandboxOperations
    {
        private DateTimeOffset _now = DateTimeOffset.Parse(
            "2026-08-01T00:00:00Z",
            System.Globalization.CultureInfo.InvariantCulture);

        public List<string> Faulted { get; } = [];

        public List<string> Restored { get; } = [];

        public List<string> Cleaned { get; } = [];

        public List<bool> RestoreTokenWasCanceled { get; } = [];

        public List<bool> CleanupTokenWasCanceled { get; } = [];

        public bool FailObservation { get; init; }

        public bool FailRestoration { get; init; }

        public bool CrossTenantEffectDetectedBeforeRestore { get; init; }

        public bool FailCleanup { get; init; }

        public bool FailCheckpoint { get; init; }

        public bool AffectedOperationRecovered { get; init; } = true;

        public bool IndependentControlSucceeded { get; init; } = true;

        public bool IndependentControlUnobserved { get; init; }

        public CancellationTokenSource? CancelObservationToken { get; init; }

        public TimeSpan? ScopeRecordingSkew { get; init; }

        public DateTimeOffset UtcNow
        {
            get
            {
                _now += TimeSpan.FromSeconds(1);
                return _now;
            }
        }

        public ValueTask CheckpointAsync(
            string dependency,
            string tenantRef,
            string correlationId,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return FailCheckpoint
                ? ValueTask.FromException(new InvalidOperationException("checkpoint-failed"))
                : ValueTask.CompletedTask;
        }

        public ValueTask FaultAsync(string dependency, string tenantRef, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Faulted.Add(dependency);
            return ValueTask.CompletedTask;
        }

        public ValueTask<ScopedOutageFaultObservation> ObserveFaultAsync(
            string dependency,
            string tenantRef,
            string correlationId,
            CancellationToken cancellationToken)
        {
            if (CancelObservationToken is not null)
            {
                CancelObservationToken.Cancel();
                cancellationToken.ThrowIfCancellationRequested();
            }

            cancellationToken.ThrowIfCancellationRequested();
            if (FailObservation)
            {
                throw new InvalidOperationException("fault-not-observed");
            }

            DateTimeOffset observed = UtcNow;
            DateTimeOffset recorded = ScopeRecordingSkew is { } skew
                ? observed - skew
                : observed + TimeSpan.FromMilliseconds(25);
            return ValueTask.FromResult(new ScopedOutageFaultObservation(
                observed,
                recorded,
                ObservedScopeOverride ?? IndependentlyObservedScope(dependency),
                IndependentControlSucceeded: IndependentControlSucceeded,
                UnauthorizedMutationDetected: false)
            {
                IndependentControlUnobserved = IndependentControlUnobserved,
            });
        }

        public string? ObservedScopeOverride { get; init; }

        // Deliberately independent from LiveScopedOutageInjectionDriver.ExpectedScope. If the driver's expectation
        // table drifts from what the sandbox fixture observes, the coordinator test must surface a scope breach rather
        // than feeding the changed expectation straight back as its own observation.
        private static string IndependentlyObservedScope(string dependency)
            => dependency switch
            {
                ScopedOutageDependencies.Graph => ScopedOutageScopes.Mailbox,
                ScopedOutageDependencies.Identity => ScopedOutageScopes.ServiceClient,
                ScopedOutageDependencies.AiProvider => ScopedOutageScopes.Operation,
                ScopedOutageDependencies.CommandExecution => ScopedOutageScopes.Operation,
                ScopedOutageDependencies.AuditStore => ScopedOutageScopes.CommandSurface,
                ScopedOutageDependencies.AttachmentProcessing => ScopedOutageScopes.WorkflowItem,
                _ => throw new InvalidOperationException("The test fixture received an unknown scoped-outage dependency."),
            };

        public ValueTask<bool> RestoreAsync(string dependency, string tenantRef, CancellationToken cancellationToken)
        {
            RestoreTokenWasCanceled.Add(cancellationToken.IsCancellationRequested);
            Restored.Add(dependency);
            return FailRestoration
                ? ValueTask.FromException<bool>(new InvalidOperationException("restore-failed"))
                : ValueTask.FromResult(CrossTenantEffectDetectedBeforeRestore);
        }

        public ValueTask<ScopedOutageRecoveryEndState> VerifyRecoveryAsync(
            string dependency,
            string tenantRef,
            string correlationId,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.FromResult(new ScopedOutageRecoveryEndState(
                AffectedOperationRecovered: AffectedOperationRecovered,
                CrossTenantLeakageDetected: false,
                SilentDataLossDetected: false,
                DuplicateSideEffectDetected: false));
        }

        public ValueTask<bool> CleanupAsync(string dependency, string tenantRef, CancellationToken cancellationToken)
        {
            CleanupTokenWasCanceled.Add(cancellationToken.IsCancellationRequested);
            Cleaned.Add(dependency);
            return FailCleanup
                ? ValueTask.FromException<bool>(new InvalidOperationException("cleanup-failed"))
                : ValueTask.FromResult(true);
        }
    }
}
