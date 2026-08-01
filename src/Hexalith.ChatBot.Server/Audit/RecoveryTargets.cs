namespace Hexalith.ChatBot.Server.Audit;

/// <summary>
/// The single, canonical home for the ChatBot's recovery-point / recovery-time objectives (Story 9.11, NFR56 / A10).
/// These are the default-MVP commitments for source records, attachments, approval history, command history, policy
/// snapshots, and audit records. Story 12.15 recorded RPO ≤ 15 min / RTO ≤ 4 hr as <b>provisional</b> against the named
/// Aspire/DAPR sandbox, pending a retained hosted run locator; the A10 assumption is not yet discharged. Future drills
/// continue to produce recalibration evidence through <see cref="ContinuityDrillReport"/>.
/// <para>
/// This class is the <b>single source of truth</b> for the two targets (mirroring
/// <see cref="AuditCompletenessMeasurement.CompletenessTargetFraction"/> / <c>RollingWindow</c>): the pure
/// <see cref="ContinuityDrillEvaluator"/> compares measured RPO/RTO against these values, and they are never re-typed as
/// a hard-coded <c>15</c>/<c>4</c> anywhere else. Recalibration edits this one file (and the architecture/PRD A10
/// marker), not a value the drill code mutates at runtime.
/// </para>
/// </summary>
internal static class RecoveryTargets
{
    /// <summary>
    /// The provisional default-MVP recovery-point objective: the maximum tolerable data loss is 15 minutes (NFR56/A10).
    /// The drill compares the measured RPO against this value. Provisional pending a retained hosted run locator.
    /// </summary>
    public static readonly TimeSpan MaxRpo = TimeSpan.FromMinutes(15);

    /// <summary>
    /// The provisional default-MVP recovery-time objective: the maximum tolerable time to restore service is 4 hours
    /// (NFR56/A10). The drill compares the measured RTO against this value. Provisional pending a retained hosted run
    /// locator. Note that a sandbox lane whose restoration budget is shorter than this value can only ever report
    /// <c>unmeasurable</c> for a slower recovery — it cannot demonstrate a miss of this target.
    /// <para>
    /// Story 9.12 (NFR57) also consumes this value as the canonical <b>projection-rebuild duration target</b>: NFR57's
    /// "rebuild within the 4-hr target" bound is the same 4-hr recovery time, so the
    /// <see cref="ProjectionRebuildValidationCoordinator"/> compares a measured rebuild duration against
    /// <see cref="MaxRto"/> — it is never re-typed as a second hard-coded <c>4</c>/<c>FromHours(4)</c>.
    /// </para>
    /// </summary>
    public static readonly TimeSpan MaxRto = TimeSpan.FromHours(4);

    /// <summary>
    /// The maximum tolerable detection→scope-recording latency for a scoped degradation: incident status must state the
    /// affected scope + dependency within 5 minutes of detection when monitoring is available (Story 9.13, NFR41). This
    /// is the <b>single source</b> for the 5-min NFR41 budget — the
    /// <see cref="ScopedOutageDegradationValidationCoordinator"/> compares a measured scope-recording latency against this
    /// value and it is never re-typed as a hard-coded <c>FromMinutes(5)</c> for this concept.
    /// <para>
    /// It is a <b>deliberately separate</b> constant from <see cref="WormAuditChainVerifier.DetectionToAlertBudget"/>
    /// (also 5 min, but the NFR49a chain-break detection-to-alert budget) — different NFRs that happen to share the
    /// default value and recalibrate independently; do not couple them.
    /// </para>
    /// </summary>
    public static readonly TimeSpan MaxScopeRecordingLatency = TimeSpan.FromMinutes(5);
}
