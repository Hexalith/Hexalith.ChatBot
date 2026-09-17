using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Server.Gateway.Stages;

/// <summary>
/// The per-(tenant × outbound-channel) send rate-limit budget observed at the outbound send seam in
/// <see cref="AcceptedCommandDispatcher"/>'s <c>ExecuteApprovedOutboundDraft</c> branch (Story 7.26). A
/// <see langword="null"/> provider result means the outbound channel has no configured limit. The budget is bounded by
/// <see cref="OutboundChannelRateLimitBounds"/>; an out-of-bounds budget falls back to the safe default at the
/// enforcement seam (never raises the cap). Mirrors the Story 7.23 <c>CommandCapabilityRateLimitState</c> shape,
/// scoped to a single outbound channel (the safe <c>AdapterRef</c> token) and measured over admitted sends — a
/// DEDICATED outbound-channel plane, kept independent from the per-actor / command-capability rate-limit planes
/// (subject-class separation, NFR30) and from the <c>IOutboundChannelControlStateProvider</c> control plane.
/// </summary>
/// <param name="Budget">The configured per-window send budget for the outbound channel.</param>
/// <param name="Window">The trailing rolling window the budget is measured over.</param>
internal sealed record OutboundChannelRateLimitState(int Budget, OutboundChannelRateLimitWindow Window)
{
    /// <summary>
    /// Gets the effective in-bounds budget: an out-of-bounds configured budget falls back to
    /// <see cref="OutboundChannelRateLimitBounds.SafeDefaults"/> — never silently raising the cap above the declared
    /// maximum.
    /// </summary>
    public int EffectiveBudget
        => new OutboundChannelRateLimitBounds(Budget).IsWithinBounds
            ? Budget
            : OutboundChannelRateLimitBounds.SafeDefaults.HourlySendBudget;

    /// <summary>Gets the trailing rolling window duration for <see cref="Window"/>.</summary>
    public TimeSpan WindowDuration
        => Window switch
        {
            OutboundChannelRateLimitWindow.RollingHour => TimeSpan.FromHours(1),
            _ => TimeSpan.FromHours(1),
        };
}
