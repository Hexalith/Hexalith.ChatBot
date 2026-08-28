namespace Hexalith.ChatBot.Server.Audit;

/// <summary>
/// Retains every canonical Story 12.15 report before a coordinator reduces it to aggregate counts. Live Tier-3
/// composition replaces the safe discarding product default with a durable metadata-only artifact writer.
/// </summary>
internal interface IRecoveryValidationEvidenceSink
{
    /// <summary>Retains one distinct controlled-loss RPO report.</summary>
    ValueTask RecordAsync(ControlledLossPathReport report, CancellationToken cancellationToken);

    /// <summary>Retains one continuity-drill report.</summary>
    ValueTask RecordAsync(ContinuityDrillReport report, CancellationToken cancellationToken);

    /// <summary>Retains one projection-rebuild report.</summary>
    ValueTask RecordAsync(ProjectionRebuildReport report, CancellationToken cancellationToken);

    /// <summary>Retains one scoped-outage report.</summary>
    ValueTask RecordAsync(ScopedOutageDegradationReport report, CancellationToken cancellationToken);
}
