namespace Hexalith.ChatBot.IntegrationTests.Recovery;

/// <summary>Controlled-loss adapter over the authenticated Aspire recovery sandbox operations.</summary>
internal sealed class AspireControlledLossPathOperations(AspireRecoverySandboxOperations operations)
    : IControlledLossPathOperations
{
    /// <inheritdoc />
    public DateTimeOffset UtcNow => operations.UtcNow;

    /// <inheritdoc />
    public ValueTask<DurableCommitObservation> WitnessPreFaultCommitAsync(
        string tenantRef,
        CancellationToken cancellationToken)
        => operations.WitnessControlledLossCommitAsync(tenantRef, preFault: true, cancellationToken);

    /// <inheritdoc />
    public ValueTask InjectSubscriptionFaultAsync(string tenantRef, CancellationToken cancellationToken)
        => operations.ExpireSubscriptionAsync(tenantRef, cancellationToken);

    /// <inheritdoc />
    public ValueTask<ControlledLossCandidateObservation> RejectFaultWindowCandidateAsync(
        string tenantRef,
        CancellationToken cancellationToken)
        => operations.RejectControlledLossCandidateAsync(tenantRef, cancellationToken);

    /// <inheritdoc />
    public ValueTask RestoreSubscriptionAsync(string tenantRef, CancellationToken cancellationToken)
        => operations.RestoreSubscriptionAsync(tenantRef, cancellationToken);

    /// <inheritdoc />
    public ValueTask<DurableCommitObservation> WitnessPostRecoveryCommitAsync(
        string tenantRef,
        CancellationToken cancellationToken)
        => operations.WitnessControlledLossCommitAsync(tenantRef, preFault: false, cancellationToken);

    /// <inheritdoc />
    public ValueTask<ControlledLossPathSafetyObservation> ReadSafetyObservationAsync(
        string tenantRef,
        CancellationToken cancellationToken)
        => operations.ReadControlledLossSafetyAsync(tenantRef, cancellationToken);

    /// <inheritdoc />
    public ValueTask<bool> CleanupAsync(string tenantRef, CancellationToken cancellationToken)
        => operations.CleanupSubscriptionScenarioAsync(tenantRef, cancellationToken);
}
