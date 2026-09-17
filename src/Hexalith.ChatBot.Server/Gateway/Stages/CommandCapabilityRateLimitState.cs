using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Server.Gateway.Stages;

/// <summary>
/// The per-(tenant × command-type) command rate-limit budget observed by the actor-agnostic admission seam
/// (Story 7.23). A <see langword="null"/> provider result means the command capability has no configured limit. The
/// budget is bounded by <see cref="CommandCapabilityRateLimitBounds"/>; an out-of-bounds budget falls back to the
/// safe default at the enforcement seam (never raises the cap). Mirrors the Story 7.20 <c>AiActorRateLimitState</c>
/// shape, scoped to a single command TYPE and measured over admitted commands — a DEDICATED command-capability plane,
/// kept independent from the per-actor rate-limit planes (subject-class separation, NFR30).
/// </summary>
/// <param name="Budget">The configured per-window command budget for the command type.</param>
/// <param name="Window">The trailing rolling window the budget is measured over.</param>
internal sealed record CommandCapabilityRateLimitState(int Budget, CommandCapabilityRateLimitWindow Window)
{
    /// <summary>
    /// Gets the effective in-bounds budget: an out-of-bounds configured budget falls back to
    /// <see cref="CommandCapabilityRateLimitBounds.SafeDefaults"/> — never silently raising the cap above the
    /// declared maximum.
    /// </summary>
    public int EffectiveBudget
        => new CommandCapabilityRateLimitBounds(Budget).IsWithinBounds
            ? Budget
            : CommandCapabilityRateLimitBounds.SafeDefaults.HourlyCommandBudget;

    /// <summary>Gets the trailing rolling window duration for <see cref="Window"/>.</summary>
    public TimeSpan WindowDuration
        => Window switch
        {
            CommandCapabilityRateLimitWindow.RollingHour => TimeSpan.FromHours(1),
            _ => TimeSpan.FromHours(1),
        };
}
