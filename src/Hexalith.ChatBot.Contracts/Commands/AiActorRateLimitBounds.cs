namespace Hexalith.ChatBot.Contracts.Commands;

/// <summary>
/// The closed, schema-bounded per-AI-actor proposal rate-limit budget (Story 7.20, FR75). Exactly one declared
/// window dimension — a trailing rolling 60-minute proposal budget (<see cref="Enums.AiActorRateLimitWindow.RollingHour"/>)
/// — carried as a bounded non-negative integer. This is deliberately NOT a free-form map or expression: a tenant
/// cannot introduce a new window dimension or a custom formula, and an out-of-range / wrong-type / undeclared value is
/// rejected by the Tenant Policy Schema and the enforcement seam falls back to <see cref="SafeDefaults"/> — never
/// silently raising the cap. Mirrors the closed <c>ServiceClientRateLimitBounds</c> discipline from Story 7.17
/// (<see cref="Minimum"/>/<see cref="Maximum"/>/<see cref="SafeDefaults"/>/<see cref="IsWithinBounds"/>). This is a
/// Standard (non-security-sensitive) policy knob — so no two-person rule applies.
/// </summary>
/// <param name="HourlyProposalBudget">Maximum proposals admitted from one AI actor per rolling 60 minutes.</param>
public sealed record AiActorRateLimitBounds(int HourlyProposalBudget)
{
    /// <summary>The inclusive lower bound for the budget (non-negative; a tenant may lower to 0 to defer all proposals).</summary>
    public const int Minimum = 0;

    /// <summary>
    /// The inclusive upper bound for the rolling-hour proposal budget — the hard governance cap for a single AI actor's
    /// proposal admission. Chosen at 1000 proposals/hour: deliberately LOWER than the service-client 10000 command cap
    /// because each AI proposal can require human review, so the cap is bounded by reviewer throughput / approval-fatigue
    /// (the epic's "proposal volume does not overwhelm reviewers or queues") rather than raw automation throughput.
    /// Integer arithmetic only (never float).
    /// </summary>
    public const int Maximum = 1000;

    /// <summary>
    /// The declared safe default applied when the tenant has not set the knob or set an out-of-bounds value. It equals
    /// the governance <see cref="Maximum"/> (the least-restrictive in-bounds budget): falling back to it can never raise
    /// the effective cap above the declared maximum.
    /// </summary>
    public static AiActorRateLimitBounds SafeDefaults { get; } = new(Maximum);

    /// <summary>Gets a value indicating whether the declared budget is within its non-negative, at-or-below-cap range.</summary>
    public bool IsWithinBounds
        => HourlyProposalBudget >= Minimum && HourlyProposalBudget <= Maximum;
}
