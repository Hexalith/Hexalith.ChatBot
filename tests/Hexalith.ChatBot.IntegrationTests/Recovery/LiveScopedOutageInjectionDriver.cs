using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Gateway.Stages;

namespace Hexalith.ChatBot.IntegrationTests.Recovery;

/// <summary>Tier-3 implementation that measures each closed scoped-outage scenario from observed operations.</summary>
internal sealed class LiveScopedOutageInjectionDriver(
    IScopedOutageSandboxOperations operations,
    LiveRecoveryValidationOptions options) : IScopedOutageInjectionDriver
{
    private static readonly TimeSpan ClockSkewTolerance = TimeSpan.FromSeconds(1);

    /// <inheritdoc />
    public async ValueTask<ScopedOutageDegradationMeasurement> InjectAndMeasureAsync(
        string dependency,
        string testTenantRef,
        string correlationId,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dependency);
        ArgumentException.ThrowIfNullOrWhiteSpace(testTenantRef);
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);
        string? validationError = options.Validate();
        if (validationError is not null)
        {
            throw new InvalidOperationException(validationError);
        }

        if (!options.Enabled || !string.Equals(testTenantRef, options.TestTenantRef, StringComparison.Ordinal) ||
            !ScopedOutageDependencies.Contains(dependency) || !ReplayTenantPolicy.IsTestTenant(testTenantRef))
        {
            throw new InvalidOperationException(
                "Live scoped-outage validation requires enabled configuration, a closed dependency, and the configured replay-test tenant.");
        }

        using CancellationTokenSource scenarioDeadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        scenarioDeadline.CancelAfter(options.PerScenarioTimeout);
        CancellationToken scenarioToken = scenarioDeadline.Token;
        string expectedScope = ExpectedScope(dependency);
        DateTimeOffset startedAtUtc = operations.UtcNow;
        ScopedOutageFaultObservation? observation = null;
        ScopedOutageRecoveryEndState? endState = null;
        Exception? failure = null;
        try
        {
            // Inside the try: CheckpointAsync already mutates sandbox state (the graph branch POSTs `restore` and
            // captures counters), so a throw here previously skipped the outer cleanup and left the sandbox mutated
            // for the next dependency in the sweep.
            await operations.CheckpointAsync(dependency, testTenantRef, correlationId, scenarioToken).ConfigureAwait(false);
            try
            {
                await operations.FaultAsync(dependency, testTenantRef, scenarioToken).ConfigureAwait(false);
                observation = await operations
                    .ObserveFaultAsync(dependency, testTenantRef, correlationId, scenarioToken)
                    .ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                failure = exception;
            }
            finally
            {
                using CancellationTokenSource restoration = new(options.RestorationTimeout);
                try
                {
                    await operations.RestoreAsync(dependency, testTenantRef, restoration.Token).ConfigureAwait(false);
                }
                catch (Exception exception)
                {
                    failure = Combine(failure, exception);
                }
            }

            if (failure is null && (observation is null || !observation.IndependentControlSucceeded))
            {
                failure = new InvalidOperationException(
                    "The scoped-outage fault or independent control operation was not observed.");
            }

            if (failure is null)
            {
                try
                {
                    endState = await operations
                        .VerifyRecoveryAsync(dependency, testTenantRef, correlationId, scenarioToken)
                        .ConfigureAwait(false);
                    if (!endState.AffectedOperationRecovered)
                    {
                        failure = new InvalidOperationException("The affected operation did not recover after restoration.");
                    }
                }
                catch (Exception exception)
                {
                    failure = exception;
                }
            }
        }
        finally
        {
            using CancellationTokenSource cleanup = new(options.RestorationTimeout);
            try
            {
                await operations.CleanupAsync(dependency, testTenantRef, cleanup.Token).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                failure = Combine(failure, exception);
            }
        }

        if (failure is not null)
        {
            throw new InvalidOperationException("The scoped-outage exercise or cleanup did not complete.", failure);
        }

        ScopedOutageFaultObservation completedObservation = observation!;
        ScopedOutageRecoveryEndState completedEndState = endState!;
        TimeSpan recordingLatency = completedObservation.ScopeRecordedAtUtc - completedObservation.DependencyFailureObservedAtUtc;
        if (recordingLatency < TimeSpan.Zero)
        {
            // The two bounds are stamped by different processes, so sub-second skew is expected and is not evidence
            // of a reversed measurement. A larger reversal is still a real ordering fault and stays unmeasurable.
            recordingLatency = recordingLatency.Negate() <= ClockSkewTolerance
                ? TimeSpan.Zero
                : throw new InvalidOperationException("The scope-recording timestamp preceded the dependency observation.");
        }

        return new ScopedOutageDegradationMeasurement(
            expectedScope,
            completedObservation.ObservedScope,
            completedEndState.CrossTenantLeakageDetected,
            completedObservation.UnauthorizedMutationDetected,
            completedEndState.SilentDataLossDetected,
            completedEndState.AffectedOperationRecovered,
            completedEndState.DuplicateSideEffectDetected,
            recordingLatency,
            startedAtUtc,
            operations.UtcNow,
            new RecoveryValidationExecutionAssertions(
                CleanupComplete: true,
                FaultObserved: observation is not null,
                RecoveryObserved: completedEndState.AffectedOperationRecovered,
                IndependentControlSucceeded: completedObservation.IndependentControlSucceeded,
                TenantIsolationPreserved: !completedEndState.CrossTenantLeakageDetected,
                UnauthorizedMutationAbsent: !completedObservation.UnauthorizedMutationDetected,
                StateReconstructable: !completedEndState.SilentDataLossDetected &&
                    !completedEndState.DuplicateSideEffectDetected,
                ImmutableSourceOnly: false,
                MailboxReingestionAbsent: false));
    }

    private static Exception Combine(Exception? first, Exception second)
        => first is null ? second : new AggregateException(first, second);

    internal static string ExpectedScope(string dependency)
        => dependency switch
        {
            ScopedOutageDependencies.Graph => ScopedOutageScopes.Mailbox,
            ScopedOutageDependencies.Identity => ScopedOutageScopes.ServiceClient,
            ScopedOutageDependencies.AiProvider => ScopedOutageScopes.Operation,
            ScopedOutageDependencies.CommandExecution => ScopedOutageScopes.Operation,
            ScopedOutageDependencies.AuditStore => ScopedOutageScopes.CommandSurface,
            ScopedOutageDependencies.AttachmentProcessing => ScopedOutageScopes.WorkflowItem,
            _ => throw new InvalidOperationException("Unknown scoped-outage dependency."),
        };
}
