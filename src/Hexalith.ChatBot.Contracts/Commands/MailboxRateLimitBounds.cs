namespace Hexalith.ChatBot.Contracts.Commands;

/// <summary>
/// The closed, schema-bounded per-mailbox-source intake rate-limit budget (Story 7.14, FR75). Exactly one declared
/// window dimension — a trailing rolling 60-minute message budget (<see cref="Enums.MailboxRateLimitWindow.RollingHour"/>)
/// — carried as a bounded non-negative integer. This is deliberately NOT a free-form map or expression: a tenant
/// cannot introduce a new window dimension or a custom formula, and an out-of-range / wrong-type / undeclared value is
/// rejected by the Tenant Policy Schema and the enforcement seam falls back to <see cref="SafeDefaults"/> — never
/// silently raising the cap. Mirrors the closed <c>NotificationThrottleCeilings</c> bounds discipline from Story 7.9
/// (<see cref="Minimum"/>/<see cref="Maximum"/>/<see cref="SafeDefaults"/>/<see cref="IsWithinBounds"/>). This is a
/// Standard (non-security-sensitive) policy knob — so no two-person rule applies.
/// </summary>
/// <param name="HourlyMessageBudget">Maximum messages admitted from one mailbox source per rolling 60 minutes.</param>
public sealed record MailboxRateLimitBounds(int HourlyMessageBudget)
{
    /// <summary>The inclusive lower bound for the budget (non-negative; a tenant may lower to 0 to defer all intake).</summary>
    public const int Minimum = 0;

    /// <summary>
    /// The inclusive upper bound for the rolling-hour message budget — the hard governance cap for a single mailbox
    /// source's intake. Chosen at 1000 messages/hour: high enough not to throttle a healthy source, finite enough to
    /// bound a noisy one. Integer arithmetic only (never float).
    /// </summary>
    public const int Maximum = 1000;

    /// <summary>
    /// The declared safe default applied when the tenant has not set the knob or set an out-of-bounds value. It equals
    /// the governance <see cref="Maximum"/> (the least-restrictive in-bounds budget): falling back to it can never raise
    /// the effective cap above the declared maximum.
    /// </summary>
    public static MailboxRateLimitBounds SafeDefaults { get; } = new(Maximum);

    /// <summary>Gets a value indicating whether the declared budget is within its non-negative, at-or-below-cap range.</summary>
    public bool IsWithinBounds
        => HourlyMessageBudget >= Minimum && HourlyMessageBudget <= Maximum;
}
