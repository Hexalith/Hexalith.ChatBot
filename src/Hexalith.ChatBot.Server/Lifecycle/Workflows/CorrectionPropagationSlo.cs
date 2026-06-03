namespace Hexalith.ChatBot.Server.Lifecycle.Workflows;

/// <summary>The correction-propagation scope a deadline is computed for — the M0/M1 metadata-only path versus the M2 path that includes vector reindex.</summary>
internal enum CorrectionPropagationScope
{
    /// <summary>Metadata-only correction propagation (no vector index) — the existing four M0 activities.</summary>
    M0M1,

    /// <summary>Correction propagation that includes the vector-reindex activity (the slower derived-store rebuild).</summary>
    M2,
}

/// <summary>
/// The single authoritative correction-propagation SLO contract (Story 9.6, AC2, NFR17a, epics.md:209,
/// cross-cutting define-once). Holds <b>both</b> p95 latency targets in one place — the existing
/// <see cref="DaprCorrectionPropagationCoordinator.M0M1P95Target"/> (10 min, M0/M1, no vector index) and the net-new
/// <see cref="M2P95Target"/> (60 min, M2, incl. vector reindex) — so neither the coordinator nor the reindex activity
/// ever inlines a second <c>FromMinutes(60)</c> (the Story 9.4 <c>ReplayTenantPolicy</c> / Story 9.5
/// <c>DerivedStorePartition</c> define-once lesson). Items beyond their scope's deadline surface
/// <c>correction-delayed</c> with an owner role + next safe action, and an SLO breach is a P2 incident.
/// </summary>
internal static class CorrectionPropagationSlo
{
    /// <summary>The M0/M1 p95 latency target (no vector index): 10 minutes. Aliases the coordinator's existing constant so there is one source.</summary>
    public static TimeSpan M0M1P95Target => DaprCorrectionPropagationCoordinator.M0M1P95Target;

    /// <summary>The M2 p95 latency target (incl. vector reindex): 60 minutes (NFR17a).</summary>
    public static readonly TimeSpan M2P95Target = TimeSpan.FromMinutes(60);

    /// <summary>Returns the p95 latency target for a scope — 10 min for M0/M1, 60 min for M2.</summary>
    /// <param name="scope">The correction-propagation scope.</param>
    /// <returns>The scope's p95 target.</returns>
    public static TimeSpan TargetFor(CorrectionPropagationScope scope)
        => scope == CorrectionPropagationScope.M2 ? M2P95Target : M0M1P95Target;

    /// <summary>
    /// Computes the absolute deadline a correction must complete by ⇒ <paramref name="startedAtUtc"/> + the scope's
    /// p95 target. A correction whose scope includes vector reindex (<see cref="CorrectionPropagationScope.M2"/>) gets
    /// the 60-minute target.
    /// </summary>
    /// <param name="scope">The correction-propagation scope.</param>
    /// <param name="startedAtUtc">When the correction propagation started.</param>
    /// <returns>The absolute completion deadline.</returns>
    public static DateTimeOffset DeadlineFor(CorrectionPropagationScope scope, DateTimeOffset startedAtUtc)
        => startedAtUtc + TargetFor(scope);

    /// <summary>
    /// Returns whether a correction has breached its SLO ⇒ <paramref name="nowUtc"/> is strictly after
    /// <paramref name="deadlineUtc"/>. The boundary (now == deadline) is <b>not</b> breached.
    /// </summary>
    /// <param name="deadlineUtc">The absolute completion deadline.</param>
    /// <param name="nowUtc">The current time.</param>
    /// <returns><see langword="true"/> iff now is past the deadline.</returns>
    public static bool IsBreached(DateTimeOffset deadlineUtc, DateTimeOffset nowUtc)
        => nowUtc > deadlineUtc;
}
