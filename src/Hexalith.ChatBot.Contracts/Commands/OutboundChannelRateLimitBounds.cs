namespace Hexalith.ChatBot.Contracts.Commands;

/// <summary>
/// The closed, schema-bounded per-(tenant × outbound-channel) send rate-limit budget (Story 7.26, FR75). Exactly
/// one declared window dimension — a trailing rolling 60-minute send budget
/// (<see cref="Enums.OutboundChannelRateLimitWindow.RollingHour"/>) — carried as a bounded non-negative integer.
/// This is deliberately NOT a free-form map or expression: a tenant cannot introduce a new window dimension or a
/// custom formula, and an out-of-range / wrong-type / undeclared value is rejected by the Tenant Policy Schema and
/// the enforcement seam falls back to <see cref="SafeDefaults"/> — never silently raising the cap. Mirrors the
/// closed <c>MailboxRateLimitBounds</c>/<c>CommandCapabilityRateLimitBounds</c> discipline
/// (<see cref="Minimum"/>/<see cref="Maximum"/>/<see cref="SafeDefaults"/>/<see cref="IsWithinBounds"/>). This is a
/// Standard (non-security-sensitive) policy knob — so no two-person rule applies.
/// </summary>
/// <param name="HourlySendBudget">Maximum approved sends through one outbound channel per rolling 60 minutes.</param>
public sealed record OutboundChannelRateLimitBounds(int HourlySendBudget)
{
    /// <summary>The inclusive lower bound for the budget (non-negative; a tenant may lower to 0 to defer all sends through the channel).</summary>
    public const int Minimum = 0;

    /// <summary>
    /// The inclusive upper bound for the rolling-hour send budget — the hard governance cap for a single outbound
    /// channel's external sends. Chosen at 1000 sends/hour, aligned with the <c>MailboxRateLimitBounds</c> 1000 cap
    /// (Story 7.14 — the other external-communication-channel cell) and the approval-gated AI-actor 1000, and
    /// deliberately NOT the service-client/command-capability raw-command-admission 10000 cap: an outbound channel
    /// carries approval-gated external messages leaving the boundary, so the cap is bounded by external-send
    /// throughput, not by raw command-admission throughput. Integer arithmetic only (never float).
    /// </summary>
    public const int Maximum = 1000;

    /// <summary>
    /// The declared safe default applied when the tenant has not set the knob or set an out-of-bounds value. It
    /// equals the governance <see cref="Maximum"/> (the least-restrictive in-bounds budget): falling back to it can
    /// never raise the effective cap above the declared maximum.
    /// </summary>
    public static OutboundChannelRateLimitBounds SafeDefaults { get; } = new(Maximum);

    /// <summary>Gets a value indicating whether the declared budget is within its non-negative, at-or-below-cap range.</summary>
    public bool IsWithinBounds
        => HourlySendBudget >= Minimum && HourlySendBudget <= Maximum;
}
