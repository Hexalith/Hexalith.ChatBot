namespace Hexalith.ChatBot.Server.Audit;

/// <summary>
/// Independent best-effort side channel for bounded proof that both canonical evidence writes failed.
/// </summary>
internal interface IRecoveryValidationEvidenceRetentionFailureSink
{
    /// <summary>Attempts to retain one metadata-only evidence-retention failure marker.</summary>
    ValueTask RecordAsync(
        RecoveryValidationEvidenceRetentionFailureMarker marker,
        CancellationToken cancellationToken);
}
