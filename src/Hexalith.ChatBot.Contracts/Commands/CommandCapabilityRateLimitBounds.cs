namespace Hexalith.ChatBot.Contracts.Commands;

/// <summary>
/// The closed, schema-bounded per-(tenant × command-type) command rate-limit budget (Story 7.23, FR75). Exactly
/// one declared window dimension — a trailing rolling 60-minute command budget
/// (<see cref="Enums.CommandCapabilityRateLimitWindow.RollingHour"/>) — carried as a bounded non-negative integer.
/// This is deliberately NOT a free-form map or expression: a tenant cannot introduce a new window dimension or a
/// custom formula, and an out-of-range / wrong-type / undeclared value is rejected by the Tenant Policy Schema and
/// the enforcement seam falls back to <see cref="SafeDefaults"/> — never silently raising the cap. Mirrors the
/// closed <c>AiActorRateLimitBounds</c>/<c>ServiceClientRateLimitBounds</c> discipline
/// (<see cref="Minimum"/>/<see cref="Maximum"/>/<see cref="SafeDefaults"/>/<see cref="IsWithinBounds"/>). This is a
/// Standard (non-security-sensitive) policy knob — so no two-person rule applies.
/// </summary>
/// <param name="HourlyCommandBudget">Maximum commands of this type admitted for the tenant per rolling 60 minutes.</param>
public sealed record CommandCapabilityRateLimitBounds(int HourlyCommandBudget)
{
    /// <summary>The inclusive lower bound for the budget (non-negative; a tenant may lower to 0 to defer all submissions of the type).</summary>
    public const int Minimum = 0;

    /// <summary>
    /// The inclusive upper bound for the rolling-hour command budget — the hard governance cap for a single
    /// command type's admission. Chosen at 10000 commands/hour, aligned with the service-client 10000 command cap
    /// (Story 7.17) and deliberately NOT the AI-actor reviewer-bounded 1000: a command capability is a command
    /// <em>type</em> submitted by ANY actor (human/service/AI), so the cap is bounded by raw command-admission
    /// throughput, not by reviewer/approval-fatigue throughput. Integer arithmetic only (never float).
    /// </summary>
    public const int Maximum = 10000;

    /// <summary>
    /// The declared safe default applied when the tenant has not set the knob or set an out-of-bounds value. It
    /// equals the governance <see cref="Maximum"/> (the least-restrictive in-bounds budget): falling back to it can
    /// never raise the effective cap above the declared maximum.
    /// </summary>
    public static CommandCapabilityRateLimitBounds SafeDefaults { get; } = new(Maximum);

    /// <summary>Gets a value indicating whether the declared budget is within its non-negative, at-or-below-cap range.</summary>
    public bool IsWithinBounds
        => HourlyCommandBudget >= Minimum && HourlyCommandBudget <= Maximum;
}
