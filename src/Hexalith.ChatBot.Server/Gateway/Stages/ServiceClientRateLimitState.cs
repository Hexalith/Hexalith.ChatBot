using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Server.Gateway.Stages;

/// <summary>
/// The per-service-client command rate-limit budget observed by the admission seam (Story 7.17). A
/// <see langword="null"/> provider result means the client has no configured limit. The budget is bounded by
/// <see cref="ServiceClientRateLimitBounds"/>; an out-of-bounds budget falls back to the safe default at the
/// enforcement seam (never raises the cap). Mirrors the Story 7.14 <c>MailboxRateLimitState</c> shape, scoped to a
/// single service client and measured over admitted commands.
/// </summary>
/// <param name="Budget">The configured per-window command budget for the service client.</param>
/// <param name="Window">The trailing rolling window the budget is measured over.</param>
internal sealed record ServiceClientRateLimitState(int Budget, ServiceClientRateLimitWindow Window)
{
    /// <summary>
    /// Gets the effective in-bounds budget: an out-of-bounds configured budget falls back to
    /// <see cref="ServiceClientRateLimitBounds.SafeDefaults"/> — never silently raising the cap above the declared maximum.
    /// </summary>
    public int EffectiveBudget
        => new ServiceClientRateLimitBounds(Budget).IsWithinBounds
            ? Budget
            : ServiceClientRateLimitBounds.SafeDefaults.HourlyCommandBudget;

    /// <summary>Gets the trailing rolling window duration for <see cref="Window"/>.</summary>
    public TimeSpan WindowDuration
        => Window switch
        {
            ServiceClientRateLimitWindow.RollingHour => TimeSpan.FromHours(1),
            _ => TimeSpan.FromHours(1),
        };
}
