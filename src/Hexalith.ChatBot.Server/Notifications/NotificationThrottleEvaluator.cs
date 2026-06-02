using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Server.Audit;

namespace Hexalith.ChatBot.Server.Notifications;

/// <summary>
/// Deterministic, clock-injected per-recipient throttle engine (NFR46). Given a recipient's prior <em>immediate-push</em>
/// delivery timestamps, the configured ceilings, and an injected "now", it decides whether the next notification is
/// delivered as an immediate push or rolled into the digest. Mirrors the <see cref="EscalationPolicyEvaluator"/>
/// discipline exactly: static, pure, no wall-clock access — the rolling windows are measured server-side in UTC against
/// the injected clock, never against item/client-supplied time or a per-process wall clock.
/// <para>
/// Both windows are enforced simultaneously: a recipient at the hourly ceiling is throttled even when under the daily
/// ceiling, and vice-versa. An out-of-bounds ceiling set fails closed to the NFR46 governance maximums
/// (<see cref="NotificationThrottleCeilings.SafeDefaults"/>).
/// </para>
/// </summary>
internal static class NotificationThrottleEvaluator
{
    /// <summary>The trailing rolling window for the hourly ceiling (NFR46: ≤ 8 immediate pushes per rolling 60 minutes).</summary>
    public static readonly TimeSpan HourWindow = TimeSpan.FromHours(1);

    /// <summary>The trailing rolling window for the daily ceiling (NFR46: ≤ 30 immediate pushes per rolling 24 hours).</summary>
    public static readonly TimeSpan DayWindow = TimeSpan.FromHours(24);

    public static NotificationThrottleDecision Decide(
        IReadOnlyList<DateTimeOffset> priorImmediatePushTimestamps,
        NotificationThrottleCeilings ceilings,
        ISystemClock clock)
    {
        ArgumentNullException.ThrowIfNull(priorImmediatePushTimestamps);
        ArgumentNullException.ThrowIfNull(ceilings);
        ArgumentNullException.ThrowIfNull(clock);

        DateTimeOffset now = clock.UtcNow;
        return Decide(
            CountInTrailingWindow(priorImmediatePushTimestamps, now, HourWindow),
            CountInTrailingWindow(priorImmediatePushTimestamps, now, DayWindow),
            ceilings);
    }

    /// <summary>
    /// Pure decision over already-computed trailing-window counts: deliver only when strictly under <em>both</em>
    /// ceilings (so the Nth delivery that brings a window to its ceiling still delivers, and the next one throttles).
    /// </summary>
    public static NotificationThrottleDecision Decide(int hourWindowCount, int dayWindowCount, NotificationThrottleCeilings ceilings)
    {
        ArgumentNullException.ThrowIfNull(ceilings);

        // Trust boundary: an out-of-bounds ceiling set never raises the cap — fall back to the NFR46 maximums.
        NotificationThrottleCeilings effective = ceilings.IsWithinBounds ? ceilings : NotificationThrottleCeilings.SafeDefaults;

        return hourWindowCount < effective.HourlyCeiling && dayWindowCount < effective.DailyCeiling
            ? NotificationThrottleDecision.Deliver
            : NotificationThrottleDecision.ThrottleToDigest;
    }

    /// <summary>
    /// Counts deliveries whose server-measured age (<c>now − timestamp</c>, UTC) falls strictly inside the trailing
    /// window. A delivery exactly <c>window</c> old is outside (e.g. a delivery exactly 3600s old has aged out of the
    /// hourly window); future timestamps are ignored. Never trusts item/client-supplied time.
    /// </summary>
    public static int CountInTrailingWindow(IReadOnlyList<DateTimeOffset> timestamps, DateTimeOffset now, TimeSpan window)
    {
        ArgumentNullException.ThrowIfNull(timestamps);

        int count = 0;
        foreach (DateTimeOffset timestamp in timestamps)
        {
            TimeSpan age = now - timestamp.ToUniversalTime();
            if (age >= TimeSpan.Zero && age < window)
            {
                count++;
            }
        }

        return count;
    }
}
