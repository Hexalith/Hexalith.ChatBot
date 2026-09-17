using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Server.Gateway.Stages;

/// <summary>
/// The per-AI-actor proposal rate-limit budget observed by the admission seam (Story 7.20). A
/// <see langword="null"/> provider result means the AI actor has no configured limit. The budget is bounded by
/// <see cref="AiActorRateLimitBounds"/>; an out-of-bounds budget falls back to the safe default at the enforcement
/// seam (never raises the cap). Mirrors the Story 7.17 <c>ServiceClientRateLimitState</c> shape, scoped to a single
/// AI actor and measured over admitted proposals — a DEDICATED AI-actor plane, kept independent from the
/// service-client rate-limit state (subject-class separation, NFR30).
/// </summary>
/// <param name="Budget">The configured per-window proposal budget for the AI actor.</param>
/// <param name="Window">The trailing rolling window the budget is measured over.</param>
internal sealed record AiActorRateLimitState(int Budget, AiActorRateLimitWindow Window)
{
    /// <summary>
    /// Gets the effective in-bounds budget: an out-of-bounds configured budget falls back to
    /// <see cref="AiActorRateLimitBounds.SafeDefaults"/> — never silently raising the cap above the declared maximum.
    /// </summary>
    public int EffectiveBudget
        => new AiActorRateLimitBounds(Budget).IsWithinBounds
            ? Budget
            : AiActorRateLimitBounds.SafeDefaults.HourlyProposalBudget;

    /// <summary>Gets the trailing rolling window duration for <see cref="Window"/>.</summary>
    public TimeSpan WindowDuration
        => Window switch
        {
            AiActorRateLimitWindow.RollingHour => TimeSpan.FromHours(1),
            _ => TimeSpan.FromHours(1),
        };
}
