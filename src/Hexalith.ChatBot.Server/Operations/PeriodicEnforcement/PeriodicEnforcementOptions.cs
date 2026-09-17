using System.Collections.Concurrent;

using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Contracts.Queries;
using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Notifications;
using Hexalith.ChatBot.Server.Observability;
using Hexalith.ChatBot.Server.Projections;

using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Hexalith.ChatBot.Server.Operations.PeriodicEnforcement;

internal sealed class PeriodicEnforcementOptions
{
    public static readonly TimeSpan DefaultM2SweepCadence = TimeSpan.FromDays(1);

    public bool UsePeriodicEnforcementRuntime { get; set; }

    public bool RunM2AuditRecoverySweeps { get; set; }

    public TimeSpan Cadence { get; set; } = TimeSpan.FromMinutes(1);

    public TimeSpan MissedCadenceAlertAfter { get; set; } = TimeSpan.FromMinutes(5);

    public TimeSpan ControlStateHeartbeatBeforeStale { get; set; } = TimeSpan.FromMinutes(4);

    public TimeSpan M2SweepCadence { get; set; } = DefaultM2SweepCadence;

    public TimeSpan M2SweepDayAnchorUtc { get; set; }

    /// <summary>
    /// The shortest interval between two attempts of the same M2 sweep within one cadence partition. A sweep that
    /// throws does NOT consume its partition (the partition is committed only on success), so it is retried — but
    /// bounded by this interval rather than on every tick, because these sweeps are expensive (the derived-store
    /// probe is O(tenants²) store round-trips).
    /// </summary>
    public TimeSpan M2SweepRetryAfter { get; set; } = TimeSpan.FromMinutes(15);

    /// <summary>
    /// The suppression window for a repeated scheduler alert whose condition cannot self-clear on the next tick — the
    /// M2 missed-cadence alerts, the per-sweep failure alerts, and the whole-pass failure alert. Without it, one
    /// persistent fault re-alerts on every tick into an unbounded in-memory sink.
    /// </summary>
    public TimeSpan M2MissedCadenceAlertResuppressAfter { get; set; } = TimeSpan.FromHours(1);

    /// <summary>
    /// The deadline for a single M2 sweep. Without it a sweep that hangs (rather than throws) blocks the enforcement
    /// pass forever, and because <c>CheckHealthAsync</c> runs after the pass returns, it also blocks the very detector
    /// meant to report the stall. The derived-store probe is the realistic trigger: it is O(tenants²) store
    /// round-trips against a store that may be unresponsive rather than fast-failing.
    /// </summary>
    public TimeSpan M2SweepTimeout { get; set; } = TimeSpan.FromMinutes(10);

    public int RunbookSampleSize { get; set; } = 100;

    /// <summary>
    /// <see cref="System.Threading.PeriodicTimer"/> rejects a period above <c>Timer.MaxSupportedTimeout</c>
    /// (<c>uint.MaxValue - 1</c> ms, ≈49.71 days). The timer is constructed outside every guarded block in
    /// <c>ExecuteAsync</c>, so an out-of-range value there escapes the background loop and stops the whole host.
    /// </summary>
    private static readonly TimeSpan _maxTimerPeriod = TimeSpan.FromMilliseconds(uint.MaxValue - 1);

    /// <summary>
    /// Validates the operator-supplied configuration. Bound from <c>ChatBot:PeriodicEnforcement</c> and enforced at
    /// startup via <c>ValidateOnStart</c>, so a bad value fails the host deterministically at boot instead of throwing
    /// out of the background loop's first tick (which the default <see cref="BackgroundServiceExceptionBehavior"/>
    /// turns into a whole-host shutdown, potentially minutes after a successful start).
    /// </summary>
    public string? Validate()
    {
        if (Cadence <= TimeSpan.Zero)
        {
            return $"{nameof(Cadence)} must be positive (was {Cadence}).";
        }

        if (Cadence > _maxTimerPeriod)
        {
            return $"{nameof(Cadence)} must not exceed {_maxTimerPeriod} (was {Cadence}); PeriodicTimer rejects a longer period.";
        }

        if (MissedCadenceAlertAfter < TimeSpan.Zero)
        {
            return $"{nameof(MissedCadenceAlertAfter)} must be non-negative (was {MissedCadenceAlertAfter}).";
        }

        if (ControlStateHeartbeatBeforeStale < TimeSpan.Zero)
        {
            return $"{nameof(ControlStateHeartbeatBeforeStale)} must be non-negative (was {ControlStateHeartbeatBeforeStale}).";
        }

        if (M2SweepCadence <= TimeSpan.Zero)
        {
            return $"{nameof(M2SweepCadence)} must be positive (was {M2SweepCadence}).";
        }

        if (RunM2AuditRecoverySweeps && Cadence > M2SweepCadence)
        {
            return $"{nameof(Cadence)} must not exceed {nameof(M2SweepCadence)} when M2 sweeps are enabled (tick {Cadence}, M2 cadence {M2SweepCadence}); the background service cannot run a sweep more often than it ticks.";
        }

        if (M2SweepDayAnchorUtc < TimeSpan.Zero || M2SweepDayAnchorUtc >= M2SweepCadence)
        {
            return $"{nameof(M2SweepDayAnchorUtc)} must be non-negative and less than {nameof(M2SweepCadence)} (anchor {M2SweepDayAnchorUtc}, cadence {M2SweepCadence}).";
        }

        // Both windows are throttles. Accepting zero disabled the guard rather than tightening it: a zero retry
        // interval retries a permanently failing sweep on every tick (~1,440 alerts/day/job), and a zero re-suppress
        // window defeats the alert de-duplication entirely. Neither is a meaningful operator intent.
        if (M2SweepRetryAfter <= TimeSpan.Zero)
        {
            return $"{nameof(M2SweepRetryAfter)} must be positive (was {M2SweepRetryAfter}).";
        }

        if (M2SweepRetryAfter >= M2SweepCadence)
        {
            return $"{nameof(M2SweepRetryAfter)} must be shorter than {nameof(M2SweepCadence)} (retry {M2SweepRetryAfter}, cadence {M2SweepCadence}); otherwise the backoff outlives the period it retries within and a correctly-running scheduler alerts as overdue.";
        }

        if (M2MissedCadenceAlertResuppressAfter <= TimeSpan.Zero)
        {
            return $"{nameof(M2MissedCadenceAlertResuppressAfter)} must be positive (was {M2MissedCadenceAlertResuppressAfter}).";
        }

        if (M2SweepTimeout <= TimeSpan.Zero)
        {
            return $"{nameof(M2SweepTimeout)} must be positive (was {M2SweepTimeout}).";
        }

        if (M2SweepTimeout > M2SweepCadence)
        {
            return $"{nameof(M2SweepTimeout)} must not exceed {nameof(M2SweepCadence)} (timeout {M2SweepTimeout}, cadence {M2SweepCadence}).";
        }

        // The missed-cadence budget is M2SweepCadence + MissedCadenceAlertAfter; reject a pair that would overflow
        // when added rather than throwing OverflowException out of the health check every tick.
        if (M2SweepCadence.Ticks > TimeSpan.MaxValue.Ticks - MissedCadenceAlertAfter.Ticks)
        {
            return $"{nameof(M2SweepCadence)} + {nameof(MissedCadenceAlertAfter)} overflows TimeSpan (cadence {M2SweepCadence}, budget {MissedCadenceAlertAfter}).";
        }

        return RunbookSampleSize < 0
            ? $"{nameof(RunbookSampleSize)} must be non-negative (was {RunbookSampleSize})."
            : null;
    }
}
