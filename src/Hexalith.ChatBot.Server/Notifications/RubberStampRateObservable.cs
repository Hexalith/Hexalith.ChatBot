namespace Hexalith.ChatBot.Server.Notifications;

/// <summary>
/// Single-sourced NFR46/FR41 governance constants for the rubber-stamp-rate observable (Story 7.11). These are the
/// <em>definition</em> of approval fatigue under FR41/NFR46 — the share of <c>approval-required</c> approvals decided
/// within <see cref="RubberStampLatencySeconds"/> over a <see cref="RollingWindowDays"/>-day rolling window that, when it
/// exceeds <see cref="FatigueFractionPercent"/> at the tenant level, triggers the FR41 approval-tuning revisit.
/// <para>
/// They are deliberately <strong>fixed governance constants, not tenant-tunable knobs</strong>: a tenant lowering the
/// rubber-stamp definition is meaningless and a tenant raising it would <em>hide</em> fatigue, defeating the FR41 guard.
/// Each value is defined exactly once here; the evaluator and the audit envelope reference these constants, never
/// literals. (Contrast Story 7.10's <c>ReviewerBacklogThreshold</c>, which is a tunable triage threshold and therefore a
/// closed-bounded knob.)
/// </para>
/// </summary>
internal static class RubberStampRateObservable
{
    /// <summary>The rubber-stamp latency ceiling in seconds (NFR46): a decision is rubber-stamp when latency is
    /// <strong>strictly less than</strong> this value (exactly 5.000 s is not rubber-stamp).</summary>
    public const int RubberStampLatencySeconds = 5;

    /// <summary>The tenant-level approval-fatigue fraction in percent (NFR46): the FR41 revisit fires when the
    /// rubber-stamp fraction <strong>strictly exceeds</strong> this percentage (exactly 15.000 % does not trigger).</summary>
    public const int FatigueFractionPercent = 15;

    /// <summary>The rolling observation window in days (NFR46): a decision is in-window when
    /// <c>now − DecidedAtUtc ∈ [0, RollingWindowDays)</c> (a decision exactly 7 days old is outside).</summary>
    public const int RollingWindowDays = 7;

    /// <summary>The rolling observation window as a <see cref="TimeSpan"/>, derived from <see cref="RollingWindowDays"/>.</summary>
    public static readonly TimeSpan RollingWindow = TimeSpan.FromDays(RollingWindowDays);

    /// <summary>The rubber-stamp latency ceiling as a <see cref="TimeSpan"/>, derived from <see cref="RubberStampLatencySeconds"/>.</summary>
    public static readonly TimeSpan RubberStampLatency = TimeSpan.FromSeconds(RubberStampLatencySeconds);
}
