namespace Hexalith.ChatBot.Server.Audit;

/// <summary>Safe non-persistent product default for the independent retention-failure side channel.</summary>
internal sealed class DiscardingRecoveryValidationEvidenceRetentionFailureSink :
    IRecoveryValidationEvidenceRetentionFailureSink
{
    /// <summary>Gets the process-wide inert marker sink.</summary>
    public static DiscardingRecoveryValidationEvidenceRetentionFailureSink Instance { get; } = new();

    private DiscardingRecoveryValidationEvidenceRetentionFailureSink()
    {
    }

    /// <inheritdoc />
    public ValueTask RecordAsync(
        RecoveryValidationEvidenceRetentionFailureMarker marker,
        CancellationToken cancellationToken)
        => ValueTask.CompletedTask;
}
