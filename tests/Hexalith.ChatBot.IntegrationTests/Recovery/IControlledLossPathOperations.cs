namespace Hexalith.ChatBot.IntegrationTests.Recovery;

/// <summary>Closed granular operations used only by the retained controlled-loss RPO runner.</summary>
internal interface IControlledLossPathOperations
{
    /// <summary>Gets the current observation time.</summary>
    DateTimeOffset UtcNow { get; }

    /// <summary>Creates and witnesses the retained commit before deliberate loss.</summary>
    ValueTask<DurableCommitObservation> WitnessPreFaultCommitAsync(string tenantRef, CancellationToken cancellationToken);

    /// <summary>Injects the closed subscription fault.</summary>
    ValueTask InjectSubscriptionFaultAsync(string tenantRef, CancellationToken cancellationToken);

    /// <summary>Submits and deliberately rejects the one known loss candidate.</summary>
    ValueTask<ControlledLossCandidateObservation> RejectFaultWindowCandidateAsync(string tenantRef, CancellationToken cancellationToken);

    /// <summary>Restores the closed subscription boundary.</summary>
    ValueTask RestoreSubscriptionAsync(string tenantRef, CancellationToken cancellationToken);

    /// <summary>Creates and witnesses the retained commit after recovery.</summary>
    ValueTask<DurableCommitObservation> WitnessPostRecoveryCommitAsync(string tenantRef, CancellationToken cancellationToken);

    /// <summary>Reads candidate absence, retained bounds, isolation, and mutation safety.</summary>
    ValueTask<ControlledLossPathSafetyObservation> ReadSafetyObservationAsync(string tenantRef, CancellationToken cancellationToken);

    /// <summary>Erases sandbox state created by the scenario.</summary>
    ValueTask<bool> CleanupAsync(string tenantRef, CancellationToken cancellationToken);
}
