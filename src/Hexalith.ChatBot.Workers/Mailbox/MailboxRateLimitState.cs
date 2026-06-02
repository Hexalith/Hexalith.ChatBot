using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Workers.Mailbox;

/// <summary>
/// The per-mailbox-source intake rate-limit budget observed by the worker (Story 7.14). Carried on
/// <see cref="ControlledMailboxPattern"/> as an optional, append-only field — <see langword="null"/> means the source
/// has no configured limit. The budget is bounded by <see cref="MailboxRateLimitBounds"/>; an out-of-bounds budget
/// falls back to the safe default at the enforcement seam (never raises the cap). Mirrors the Story 7.9 closed-bounds
/// throttle discipline, scoped to a single mailbox source.
/// </summary>
/// <param name="Budget">The configured per-window message budget for the source.</param>
/// <param name="Window">The trailing rolling window the budget is measured over.</param>
public sealed record MailboxRateLimitState(int Budget, MailboxRateLimitWindow Window)
{
    /// <summary>
    /// Gets the effective in-bounds budget: an out-of-bounds configured budget falls back to
    /// <see cref="MailboxRateLimitBounds.SafeDefaults"/> — never silently raising the cap above the declared maximum.
    /// </summary>
    public int EffectiveBudget
        => new MailboxRateLimitBounds(Budget).IsWithinBounds
            ? Budget
            : MailboxRateLimitBounds.SafeDefaults.HourlyMessageBudget;

    /// <summary>Gets the trailing rolling window duration for <see cref="Window"/>.</summary>
    public TimeSpan WindowDuration
        => Window switch
        {
            MailboxRateLimitWindow.RollingHour => TimeSpan.FromHours(1),
            _ => TimeSpan.FromHours(1),
        };

    /// <summary>
    /// Counts intake captures whose server-measured age (<c>now − timestamp</c>, UTC) falls strictly inside the
    /// trailing window. Mirrors <c>NotificationThrottleEvaluator.CountInTrailingWindow</c>: a capture exactly
    /// <c>window</c> old has aged out; future timestamps are ignored; never trusts client/item-supplied time.
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

/// <summary>
/// Safe, finite queue-impact observation emitted by the worker when a rate-limit budget applies to a mailbox source
/// (Story 7.14, AC6). Carries integer-only tokens — the effective budget, the observed trailing-window count, and
/// whether this message was deferred — mirroring the Story 7.9 <c>NotificationThrottleOutcome</c> shape. This is the
/// audit/observable seam only; full Epic-8 operational-dashboard wiring is out of scope.
/// </summary>
/// <param name="Budget">The effective in-bounds per-window budget for the source.</param>
/// <param name="ObservedWindowCount">The source's intake count in the trailing window at decision time.</param>
/// <param name="Deferred">Whether this message was deferred (throttled) because the budget was reached.</param>
public sealed record MailboxRateLimitObservation(int Budget, int ObservedWindowCount, bool Deferred);
