namespace Hexalith.ChatBot.Server.Audit;

/// <summary>
/// Fail-safe product default. The live rebuild driver is available only when the opted-in Tier-3 recovery harness
/// constructs it with the isolated validation dataset and evidence dependencies.
/// </summary>
internal sealed class DeferredProjectionRebuildDriver : IProjectionRebuildDriver
{
    /// <inheritdoc />
    public ValueTask<ProjectionRebuildMeasurement> RebuildAsync(
        string testTenantRef,
        string datasetRef,
        string correlationId,
        CancellationToken cancellationToken)
        => throw new NotSupportedException(
            "projection rebuild validation requires the opted-in Tier-3 recovery harness");
}
