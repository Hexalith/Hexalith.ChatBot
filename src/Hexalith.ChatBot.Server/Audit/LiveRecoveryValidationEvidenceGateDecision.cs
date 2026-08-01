namespace Hexalith.ChatBot.Server.Audit;

/// <summary>
/// Fail-closed release decision that preserves target deviations separately from structural breach reasons.
/// <para>
/// <see cref="ClaimLimitationReasons"/> is a third, non-blocking channel: it records where the evidence is structurally
/// unable to support the target it was judged against — for example an allowed RTO of four hours measured by a lane
/// whose restoration budget is three minutes. Such a run is not a breach and must not stop-ship, but a reader must not
/// be able to mistake its pass for evidence of the wider target either.
/// </para>
/// </summary>
internal sealed record LiveRecoveryValidationEvidenceGateDecision(
    bool IsStopShip,
    IReadOnlyList<string> StopShipReasons,
    IReadOnlyList<string> TargetDeviationReasons,
    IReadOnlyList<string> ClaimLimitationReasons,
    IReadOnlyDictionary<string, int> EvidenceCounts);
