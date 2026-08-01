namespace Hexalith.ChatBot.Server.Audit;

/// <summary>
/// Non-persistent product default used while live drivers remain Tier-3-only. It performs no IO and grants no
/// fault-injection or artifact authority; the opted-in recovery harness must replace it with a retaining sink.
/// </summary>
internal sealed class DiscardingRecoveryValidationEvidenceSink : IRecoveryValidationEvidenceSink
{
    /// <summary>Gets the process-wide inert sink.</summary>
    public static DiscardingRecoveryValidationEvidenceSink Instance { get; } = new();

    private DiscardingRecoveryValidationEvidenceSink()
    {
    }

    /// <inheritdoc />
    public ValueTask RecordAsync(ContinuityDrillReport report, CancellationToken cancellationToken)
        => ValueTask.CompletedTask;

    /// <inheritdoc />
    public ValueTask RecordAsync(ProjectionRebuildReport report, CancellationToken cancellationToken)
        => ValueTask.CompletedTask;

    /// <inheritdoc />
    public ValueTask RecordAsync(ScopedOutageDegradationReport report, CancellationToken cancellationToken)
        => ValueTask.CompletedTask;
}
