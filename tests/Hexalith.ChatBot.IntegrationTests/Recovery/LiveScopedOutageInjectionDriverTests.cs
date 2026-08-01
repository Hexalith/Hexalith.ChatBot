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
            PerScenarioTimeout = RecoveryTargets.MaxRto,
            RestorationTimeout = TimeSpan.FromSeconds(5),
            WorkflowTimeout = TimeSpan.FromHours(5),
            EvidenceDirectory = Path.GetFullPath("TestResults/live-recovery"),
            EvidenceLocator = "artifact:live-recovery-validation",
        };

    private sealed class RecordingOperations : IScopedOutageSandboxOperations
    {
        private DateTimeOffset _now = DateTimeOffset.Parse(
            "2026-08-01T00:00:00Z",
            System.Globalization.CultureInfo.InvariantCulture);

        public List<string> Faulted { get; } = [];

        public List<string> Restored { get; } = [];

        public List<string> Cleaned { get; } = [];

        public bool FailObservation { get; init; }

        public bool FailRestoration { get; init; }

        public bool FailCleanup { get; init; }

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
            return ValueTask.CompletedTask;
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
            cancellationToken.ThrowIfCancellationRequested();
            if (FailObservation)
            {
                throw new InvalidOperationException("fault-not-observed");
            }

            DateTimeOffset observed = UtcNow;
            return ValueTask.FromResult(new ScopedOutageFaultObservation(
                observed,
                observed + TimeSpan.FromMilliseconds(25),
                LiveScopedOutageInjectionDriver.ExpectedScope(dependency),
                IndependentControlSucceeded: true,
                UnauthorizedMutationDetected: false));
        }

        public ValueTask RestoreAsync(string dependency, string tenantRef, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Restored.Add(dependency);
            return FailRestoration
                ? ValueTask.FromException(new InvalidOperationException("restore-failed"))
                : ValueTask.CompletedTask;
        }

        public ValueTask<ScopedOutageRecoveryEndState> VerifyRecoveryAsync(
            string dependency,
            string tenantRef,
            string correlationId,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.FromResult(new ScopedOutageRecoveryEndState(
                AffectedOperationRecovered: true,
                CrossTenantLeakageDetected: false,
                SilentDataLossDetected: false,
                DuplicateSideEffectDetected: false));
        }

        public ValueTask CleanupAsync(string dependency, string tenantRef, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Cleaned.Add(dependency);
            return FailCleanup
                ? ValueTask.FromException(new InvalidOperationException("cleanup-failed"))
                : ValueTask.CompletedTask;
        }
    }
}
