using Hexalith.ChatBot.Server.Audit;

namespace Hexalith.ChatBot.IntegrationTests.Recovery;

/// <summary>
/// Tier-3-only live continuity runner. It derives measurements from witnessed transitions and always attempts
/// restoration/cleanup with an independent deadline, even when injection, observation, or the caller is canceled.
/// </summary>
internal sealed class LiveContinuityDrillScenarioRunner(
    IRecoverySandboxOperations operations,
    LiveRecoveryValidationOptions options) : IContinuityDrillScenarioRunner
{
    private static readonly TimeSpan ClockSkewTolerance = TimeSpan.FromSeconds(1);

    /// <inheritdoc />
    public async ValueTask<ContinuityDrillMeasurement> RunAsync(
        string scenario,
        string testTenantRef,
        string correlationId,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(scenario);
        ArgumentException.ThrowIfNullOrWhiteSpace(testTenantRef);
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);
        string? validationError = options.Validate();
        if (validationError is not null)
        {
            throw new InvalidOperationException(validationError);
        }

        if (!options.Enabled || !string.Equals(testTenantRef, options.TestTenantRef, StringComparison.Ordinal) ||
            !ReplayTenantPolicy.IsTestTenant(testTenantRef) || !ContinuityDrillScenarios.Contains(scenario))
        {
            throw new InvalidOperationException("The live continuity scenario is outside the configured closed sandbox boundary.");
        }

        using CancellationTokenSource scenarioDeadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        scenarioDeadline.CancelAfter(options.PerScenarioTimeout);
        return scenario switch
        {
            ContinuityDrillScenarios.EventStoreOutage =>
                await RunEventStoreOutageAsync(testTenantRef, correlationId, scenarioDeadline.Token).ConfigureAwait(false),
            ContinuityDrillScenarios.M365SubscriptionFailure =>
                await RunSubscriptionFailureAsync(testTenantRef, correlationId, scenarioDeadline.Token).ConfigureAwait(false),
            _ => throw new InvalidOperationException("The live continuity scenario is outside the configured closed sandbox boundary."),
        };
    }

    private async ValueTask<ContinuityDrillMeasurement> RunEventStoreOutageAsync(
        string tenantRef,
        string correlationId,
        CancellationToken cancellationToken)
    {
        DateTimeOffset startedAtUtc = operations.UtcNow;
        bool measurementProduced = false;
        bool restorationSucceeded = false;
        bool cleanupComplete = false;
        ContinuityDrillMeasurement? measurement;
        try
        {
            // Seed is inside the try so a partial seed still reaches cleanup in finally.
            RecoveryOperationCheckpoint checkpoint = await operations
                .SeedCommittedOperationAsync(tenantRef, correlationId, cancellationToken)
                .ConfigureAwait(false);
            RecoveryFaultObservation? observation = null;
            DateTimeOffset recoveredAtUtc = default;
            Exception? injectionFailure = null;
            Exception? restorationFailure = null;
            try
            {
                await operations.StopEventStoreAsync(cancellationToken).ConfigureAwait(false);
                observation = await operations
                    .ObserveEventStoreFaultAsync(tenantRef, correlationId, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (Exception failure)
            {
                injectionFailure = failure;
            }
            finally
            {
                // An unguarded restoration would replace (and destroy) the original diagnostic exception AND leave
                // the real EventStore resource stopped for every remaining scenario in the sweep. Both failures are
                // part of the verdict, so surface them together instead of letting one mask the other.
                try
                {
                    using CancellationTokenSource restoration = new(options.RestorationTimeout);
                    await operations.StartEventStoreAsync(restoration.Token).ConfigureAwait(false);
                    recoveredAtUtc = await operations.WaitForEventStoreRecoveryAsync(restoration.Token).ConfigureAwait(false);
                    restorationSucceeded = true;
                }
                catch (Exception failure)
                {
                    restorationFailure = failure;
                }
            }

            if (injectionFailure is not null || restorationFailure is not null)
            {
                throw Combine("EventStore continuity", injectionFailure, restorationFailure);
            }

            if (observation is null)
            {
                throw new InvalidOperationException("The application did not produce an EventStore fault observation.");
            }

            RecoveryEventStoreEndState endState = await operations
                .ReadEventStoreEndStateAsync(checkpoint, cancellationToken)
                .ConfigureAwait(false);
            bool dataLossDetected = endState.ReconstructableCommittedCount != checkpoint.CommittedCount ||
                !endState.TenantIsolationPreserved ||
                !endState.UnauthorizedMutationAbsent;
            // No detected loss ⇒ MeasuredRpo = Zero (classic RPO: the loss window is empty). This is not evidence that
            // the A10 ≤15 min budget was exercised on a green run; only a loss-path OrderedDuration measures that budget.
            TimeSpan rpo = dataLossDetected
                ? OrderedDuration(checkpoint.LastCommittedAtUtc, observation.ObservedAtUtc, "EventStore RPO")
                : TimeSpan.Zero;
            TimeSpan rto = OrderedDuration(observation.ObservedAtUtc, recoveredAtUtc, "EventStore RTO");
            measurement = new ContinuityDrillMeasurement(
                startedAtUtc,
                recoveredAtUtc,
                rpo,
                rto,
                dataLossDetected,
                new RecoveryValidationExecutionAssertions(
                    CleanupComplete: false, // patched below with the cleanup step's real, independently observed outcome
                    FaultObserved: true,
                    RecoveryObserved: recoveredAtUtc != default,
                    IndependentControlSucceeded: endState.TenantIsolationPreserved,
                    TenantIsolationPreserved: endState.TenantIsolationPreserved,
                    UnauthorizedMutationAbsent: endState.UnauthorizedMutationAbsent,
                    StateReconstructable: endState.ReconstructableCommittedCount == checkpoint.CommittedCount,
                    ImmutableSourceOnly: false,
                    MailboxReingestionAbsent: false));
            measurementProduced = true;
        }
        finally
        {
            // Guarded, mirroring the restoration finally above. CleanupEventStoreScenarioAsync throws when EventStore
            // is unavailable — exactly the state that exists after a failed restoration — so an unguarded call here
            // replaced the Combine(...) exception carrying both root causes with a generic cleanup message.
            // When restoration succeeded, cleanup failure must still surface even if the drill itself failed (seeded
            // residue is real); only a broken-topology cleanup after failed restoration may be swallowed.
            try
            {
                using CancellationTokenSource cleanup = new(options.RestorationTimeout);
                cleanupComplete = await operations.CleanupEventStoreScenarioAsync(cleanup.Token).ConfigureAwait(false);
            }
            catch (Exception) when (!measurementProduced && !restorationSucceeded)
            {
                // Topology is already broken; the primary Combine/cancel diagnostic must survive.
            }
        }

        return measurement with { ExecutionAssertions = measurement.ExecutionAssertions! with { CleanupComplete = cleanupComplete } };
    }

    private async ValueTask<ContinuityDrillMeasurement> RunSubscriptionFailureAsync(
        string tenantRef,
        string correlationId,
        CancellationToken cancellationToken)
    {
        DateTimeOffset startedAtUtc = operations.UtcNow;
        bool measurementProduced = false;
        bool restorationSucceeded = false;
        bool cleanupComplete = false;
        ContinuityDrillMeasurement? measurement;
        try
        {
            // Committed-before-outage bound for loss-path RPO (decision 1.2). No-loss still reports Zero.
            RecoveryOperationCheckpoint committedBeforeOutage = await operations
                .CheckpointSubscriptionCommittedBoundAsync(tenantRef, correlationId, cancellationToken)
                .ConfigureAwait(false);
            RecoveryFaultObservation? observation = null;
            Exception? injectionFailure = null;
            Exception? restorationFailure = null;
            try
            {
                await operations.ExpireSubscriptionAsync(tenantRef, cancellationToken).ConfigureAwait(false);
                observation = await operations
                    .ObserveSubscriptionFaultAsync(tenantRef, correlationId, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (Exception failure)
            {
                injectionFailure = failure;
            }
            finally
            {
                try
                {
                    using CancellationTokenSource restoration = new(options.RestorationTimeout);
                    await operations.RestoreSubscriptionAsync(tenantRef, restoration.Token).ConfigureAwait(false);
                    restorationSucceeded = true;
                }
                catch (Exception failure)
                {
                    restorationFailure = failure;
                }
            }

            if (injectionFailure is not null || restorationFailure is not null)
            {
                throw Combine("subscription continuity", injectionFailure, restorationFailure);
            }

            if (observation is null)
            {
                throw new InvalidOperationException("The Worker did not produce a subscription-failure observation.");
            }

            RecoverySubscriptionEndState endState = await operations
                .ReconcileSubscriptionAsync(tenantRef, correlationId, cancellationToken)
                .ConfigureAwait(false);
            bool dataLossDetected = endState.DeliveredCount <= 0 ||
                !endState.NoSilentLoss ||
                !endState.NoDuplicateSideEffects ||
                !endState.TenantIsolationPreserved ||
                !endState.UnauthorizedMutationAbsent;
            // Same no-loss Zero semantics as EventStore; loss-path uses the witnessed committed-before-outage bound.
            TimeSpan rpo = dataLossDetected
                ? OrderedDuration(committedBeforeOutage.LastCommittedAtUtc, observation.ObservedAtUtc, "subscription RPO")
                : TimeSpan.Zero;
            TimeSpan rto = OrderedDuration(observation.ObservedAtUtc, endState.RecoveredAtUtc, "subscription RTO");
            measurement = new ContinuityDrillMeasurement(
                startedAtUtc,
                endState.RecoveredAtUtc,
                rpo,
                rto,
                dataLossDetected,
                new RecoveryValidationExecutionAssertions(
                    CleanupComplete: false, // patched below with the cleanup step's real, independently observed outcome
                    FaultObserved: true,
                    RecoveryObserved: endState.DeliveredCount > 0,
                    IndependentControlSucceeded: endState.TenantIsolationPreserved,
                    TenantIsolationPreserved: endState.TenantIsolationPreserved,
                    UnauthorizedMutationAbsent: endState.UnauthorizedMutationAbsent,
                    StateReconstructable: endState.NoSilentLoss && endState.NoDuplicateSideEffects,
                    ImmutableSourceOnly: false,
                    MailboxReingestionAbsent: false));
            measurementProduced = true;
        }
        finally
        {
            try
            {
                using CancellationTokenSource cleanup = new(options.RestorationTimeout);
                cleanupComplete = await operations.CleanupSubscriptionScenarioAsync(tenantRef, cleanup.Token).ConfigureAwait(false);
            }
            catch (Exception) when (!measurementProduced && !restorationSucceeded)
            {
                // Primary drill diagnostic must survive a broken-topology cleanup.
            }
        }

        return measurement with { ExecutionAssertions = measurement.ExecutionAssertions! with { CleanupComplete = cleanupComplete } };
    }

    /// <summary>
    /// The two bounds are read from different processes (the sandbox stamps one, the harness the other), so a small
    /// clock difference is expected and is not evidence of a reversed measurement. Clamp skew inside the tolerance to
    /// zero; a larger reversal is still a real ordering fault and stays unmeasurable.
    /// </summary>
    private static TimeSpan OrderedDuration(DateTimeOffset start, DateTimeOffset end, string measurement)
    {
        TimeSpan duration = end - start;
        if (duration >= TimeSpan.Zero)
        {
            return duration;
        }

        return duration.Negate() <= ClockSkewTolerance
            ? TimeSpan.Zero
            : throw new InvalidOperationException($"{measurement} bounds are reversed.");
    }

    private static Exception Combine(string stage, Exception? injectionFailure, Exception? restorationFailure)
    {
        // Cancellation must stay cancellation. Prefer bare OperationCanceledException whenever injection was canceled,
        // even if restoration also failed — otherwise the coordinator cannot distinguish cancel from scenario failure.
        if (injectionFailure is OperationCanceledException canceled)
        {
            return canceled;
        }

        if (restorationFailure is null && injectionFailure is not null)
        {
            return injectionFailure is InvalidOperationException
                ? injectionFailure
                : new InvalidOperationException($"The live {stage} scenario failed.", injectionFailure);
        }

        Exception[] failures = new[] { injectionFailure, restorationFailure }
            .OfType<Exception>()
            .ToArray();
        return failures.Length == 1
            ? new InvalidOperationException($"The live {stage} scenario failed.", failures[0])
            : new AggregateException($"The live {stage} scenario failed during injection and restoration.", failures);
    }
}
