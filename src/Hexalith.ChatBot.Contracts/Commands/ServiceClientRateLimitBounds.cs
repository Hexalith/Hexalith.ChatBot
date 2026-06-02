namespace Hexalith.ChatBot.Contracts.Commands;

/// <summary>
/// The closed, schema-bounded per-service-client command rate-limit budget (Story 7.17, FR75). Exactly one declared
/// window dimension — a trailing rolling 60-minute command budget (<see cref="Enums.ServiceClientRateLimitWindow.RollingHour"/>)
/// — carried as a bounded non-negative integer. This is deliberately NOT a free-form map or expression: a tenant
/// cannot introduce a new window dimension or a custom formula, and an out-of-range / wrong-type / undeclared value is
/// rejected by the Tenant Policy Schema and the enforcement seam falls back to <see cref="SafeDefaults"/> — never
/// silently raising the cap. Mirrors the closed <c>MailboxRateLimitBounds</c> discipline from Story 7.14
/// (<see cref="Minimum"/>/<see cref="Maximum"/>/<see cref="SafeDefaults"/>/<see cref="IsWithinBounds"/>). This is a
/// Standard (non-security-sensitive) policy knob — so no two-person rule applies.
/// </summary>
/// <param name="HourlyCommandBudget">Maximum commands admitted from one service client per rolling 60 minutes.</param>
public sealed record ServiceClientRateLimitBounds(int HourlyCommandBudget)
{
    /// <summary>The inclusive lower bound for the budget (non-negative; a tenant may lower to 0 to defer all commands).</summary>
    public const int Minimum = 0;

    /// <summary>
    /// The inclusive upper bound for the rolling-hour command budget — the hard governance cap for a single service
    /// client's command admission. Chosen at 10000 commands/hour: higher than the mailbox-source 1000 intake cap
    /// because command automation is legitimately higher-volume than mailbox intake, yet finite enough to bound a
    /// runaway client. Integer arithmetic only (never float).
    /// </summary>
    public const int Maximum = 10000;

    /// <summary>
    /// The declared safe default applied when the tenant has not set the knob or set an out-of-bounds value. It equals
    /// the governance <see cref="Maximum"/> (the least-restrictive in-bounds budget): falling back to it can never raise
    /// the effective cap above the declared maximum.
    /// </summary>
    public static ServiceClientRateLimitBounds SafeDefaults { get; } = new(Maximum);

    /// <summary>Gets a value indicating whether the declared budget is within its non-negative, at-or-below-cap range.</summary>
    public bool IsWithinBounds
        => HourlyCommandBudget >= Minimum && HourlyCommandBudget <= Maximum;
}
