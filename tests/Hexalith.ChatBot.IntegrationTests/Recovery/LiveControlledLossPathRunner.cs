using Hexalith.ChatBot.Server.Audit;

namespace Hexalith.ChatBot.IntegrationTests.Recovery;

/// <summary>
/// Tier-3-only controlled-loss runner that surrounds one rejected notification with authoritative EventStore commits.
/// Restoration and cleanup use independent deadlines even when the caller is canceled.
/// </summary>
internal sealed class LiveControlledLossPathRunner(
    IControlledLossPathOperations operations,
    LiveRecoveryValidationOptions options)
{
    /// <summary>Runs the single closed controlled-loss scenario and returns its metadata-only report.</summary>
    public async ValueTask<ControlledLossPathReport> RunAsync(
        string tenantRef,
        string correlationId,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantRef);
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);
        string? validationError = options.Validate();
        if (validationError is not null)
        {
            throw new InvalidOperationException(validationError);
        }

        if (!options.Enabled || !ReplayTenantPolicy.IsTestTenant(tenantRef) ||
            !string.Equals(options.TestTenantRef, tenantRef, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("The controlled-loss run is outside the configured closed sandbox boundary.");
        }

        using CancellationTokenSource deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(options.PerScenarioTimeout);
        DateTimeOffset startedAtUtc = operations.UtcNow.ToUniversalTime();
        DurableCommitObservation? preFault = null;
        ControlledLossCandidateObservation? candidate = null;
        DurableCommitObservation? postRecovery = null;
        ControlledLossPathSafetyObservation? safety = null;
        bool cleanupComplete = false;
        bool faultAttempted = false;
        Exception? runFailure = null;
        try
        {
            preFault = await operations.WitnessPreFaultCommitAsync(tenantRef, deadline.Token).ConfigureAwait(false);
            RequireRequestedTenant(preFault, tenantRef, "pre-fault");
            Exception? faultFailure = null;
            Exception? restorationFailure = null;
            try
            {
                faultAttempted = true;
                await operations.InjectSubscriptionFaultAsync(tenantRef, deadline.Token).ConfigureAwait(false);
                candidate = await operations.RejectFaultWindowCandidateAsync(tenantRef, deadline.Token).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                faultFailure = exception;
            }
            finally
            {
                if (faultAttempted)
                {
                    try
                    {
                        using CancellationTokenSource restoration = new(options.RestorationTimeout);
                        await operations.RestoreSubscriptionAsync(tenantRef, restoration.Token).ConfigureAwait(false);
                    }
                    catch (Exception exception)
                    {
                        restorationFailure = exception;
                    }
                }
            }

            if (faultFailure is not null || restorationFailure is not null)
            {
                throw Combine(faultFailure, restorationFailure);
            }

            postRecovery = await operations.WitnessPostRecoveryCommitAsync(tenantRef, deadline.Token).ConfigureAwait(false);
            RequireRequestedTenant(postRecovery, tenantRef, "post-recovery");
            safety = await operations.ReadSafetyObservationAsync(tenantRef, deadline.Token).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            runFailure = exception;
        }

        Exception? cleanupFailure = null;
        try
        {
            using CancellationTokenSource cleanup = new(options.RestorationTimeout);
            cleanupComplete = await operations.CleanupAsync(tenantRef, cleanup.Token).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            cleanupFailure = exception;
        }

        if (runFailure is not null || cleanupFailure is not null)
        {
            throw CombineRunAndCleanup(runFailure, cleanupFailure);
        }

        if (preFault is null || candidate is null || postRecovery is null || safety is null)
        {
            throw new InvalidOperationException("The controlled-loss run did not produce complete observations.");
        }

        DateTimeOffset endedAtUtc = operations.UtcNow.ToUniversalTime();
        ControlledLossPathMeasurement measurement = new(
            tenantRef,
            startedAtUtc,
            endedAtUtc,
            preFault.AggregateRef,
            preFault.EventRef,
            preFault.SequenceNumber,
            preFault.CommittedAtUtc,
            candidate.CandidateRef,
            candidate.ObservedAtUtc,
            postRecovery.AggregateRef,
            postRecovery.EventRef,
            postRecovery.SequenceNumber,
            postRecovery.CommittedAtUtc,
            safety.PreFaultRetained,
            candidate.Rejected,
            safety.CandidateAbsent,
            safety.PostRecoveryRetained,
            safety.TenantIsolationPreserved,
            safety.UnauthorizedMutationAbsent,
            cleanupComplete);
        IReadOnlyList<string> deviations = ControlledLossPathEvaluator.Deviations(measurement);
        string verdict = ControlledLossPathEvaluator.Evaluate(measurement);
        return new ControlledLossPathReport(
            tenantRef,
            ControlledLossPathReport.SubscriptionNotificationRejectionScenario,
            startedAtUtc,
            endedAtUtc,
            ControlledLossPathEvaluator.MeasureRpo(measurement),
            preFault.AggregateRef,
            preFault.EventRef,
            preFault.SequenceNumber,
            preFault.CommittedAtUtc,
            candidate.CandidateRef,
            candidate.ObservedAtUtc,
            postRecovery.AggregateRef,
            postRecovery.EventRef,
            postRecovery.SequenceNumber,
            postRecovery.CommittedAtUtc,
            safety.PreFaultRetained,
            candidate.Rejected,
            safety.CandidateAbsent,
            safety.PostRecoveryRetained,
            safety.TenantIsolationPreserved,
            safety.UnauthorizedMutationAbsent,
            cleanupComplete,
            verdict,
            deviations,
            correlationId,
            verdict switch
            {
                ControlledLossPathVerdicts.Met => ControlledLossPathReport.CompletedReasonCode,
                ControlledLossPathVerdicts.Missed => ControlledLossPathReport.TargetMissedReasonCode,
                _ => ControlledLossPathReport.UnmeasurableReasonCode,
            });
    }

    /// <summary>
    /// Binds an authoritative observation to the requested tenant. EventStore cannot carry the <c>:</c> in
    /// <see cref="ReplayTenantPolicy.ReplayTestTenantPrefix"/>, so durable state for a <c>replay-test:</c> tenant is
    /// written under the physical name <see cref="ReplayTenantPolicy.StorageTenantFor"/> derives. Comparing the
    /// persisted envelope against the logical label instead of that derived physical name rejected every genuine
    /// observation, so the check must resolve the same single-source derivation the topology writes through.
    /// </summary>
    private static void RequireRequestedTenant(
        DurableCommitObservation observation,
        string requestedTenantRef,
        string bound)
    {
        // RunAsync already fails closed unless the requested tenant satisfies IsTestTenant, so the derivation is
        // never null here; treat a null as a mismatch rather than silently widening the accepted set.
        string? storageTenantRef = ReplayTenantPolicy.StorageTenantFor(requestedTenantRef);
        if (storageTenantRef is null ||
            !string.Equals(observation.TenantRef, storageTenantRef, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"The controlled-loss {bound} durable observation belongs to another tenant.");
        }
    }

    private static Exception Combine(Exception? faultFailure, Exception? restorationFailure)
    {
        if (faultFailure is OperationCanceledException canceled && restorationFailure is null)
        {
            return canceled;
        }

        Exception[] failures = new[] { faultFailure, restorationFailure }.OfType<Exception>().ToArray();
        return failures.Length == 1
            ? new InvalidOperationException("The controlled-loss scenario failed.", failures[0])
            : new AggregateException("The controlled-loss scenario failed during injection and restoration.", failures);
    }

    private static Exception CombineRunAndCleanup(Exception? runFailure, Exception? cleanupFailure)
    {
        if (runFailure is OperationCanceledException canceled && cleanupFailure is null)
        {
            return canceled;
        }

        if (runFailure is not null && cleanupFailure is not null)
        {
            return new AggregateException(
                "The controlled-loss run failed and cleanup also failed.",
                runFailure,
                cleanupFailure);
        }

        return runFailure ?? new InvalidOperationException(
            "The controlled-loss cleanup failed.",
            cleanupFailure);
    }
}
