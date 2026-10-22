using Hexalith.ChatBot.Server.Audit;

namespace Hexalith.ChatBot.IntegrationTests.Recovery;

/// <summary>Returns an exact injected failure through each recovery decorator's real driver contract.</summary>
/// <param name="failure">The failure whose identity and cancellation token must survive.</param>
internal sealed class FailingRecoveryDriver(Exception failure)
    : IContinuityDrillScenarioRunner, IProjectionRebuildDriver, IScopedOutageInjectionDriver
{
    /// <inheritdoc />
    public ValueTask<ContinuityDrillMeasurement> RunAsync(
        string scenario, string testTenantRef, string correlationId, CancellationToken cancellationToken)
        => ValueTask.FromException<ContinuityDrillMeasurement>(failure);

    /// <inheritdoc />
    public ValueTask<ProjectionRebuildMeasurement> RebuildAsync(
        string testTenantRef, string datasetRef, string correlationId, CancellationToken cancellationToken)
        => ValueTask.FromException<ProjectionRebuildMeasurement>(failure);

    /// <inheritdoc />
    public ValueTask<ScopedOutageDegradationMeasurement> InjectAndMeasureAsync(
        string dependency, string testTenantRef, string correlationId, CancellationToken cancellationToken)
        => ValueTask.FromException<ScopedOutageDegradationMeasurement>(failure);
}
