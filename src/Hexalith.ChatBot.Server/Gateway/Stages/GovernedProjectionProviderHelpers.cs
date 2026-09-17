using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Projections;

namespace Hexalith.ChatBot.Server.Gateway.Stages;

internal static class GovernedProjectionProviderHelpers
{
    private static readonly TimeSpan OrdinaryFreshness = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan RevocationFreshness = TimeSpan.FromSeconds(60);

    public static bool IsStale(GovernedControlStateView? view, DateTimeOffset now)
    {
        if (view is null)
        {
            return false;
        }

        TimeSpan maxAge = view.RevocationSensitive ? RevocationFreshness : OrdinaryFreshness;
        return now.ToUniversalTime() - view.LastUpdatedAtUtc.ToUniversalTime() > maxAge;
    }

    public static ServiceClientControlState ServiceClientState(GovernedControlStateView? view, DateTimeOffset now)
        => IsStale(view, now) ? ServiceClientControlState.Disabled : view?.ControlState switch
        {
            GovernedControlStateView.Disabled => ServiceClientControlState.Disabled,
            GovernedControlStateView.Quarantined => ServiceClientControlState.Quarantined,
            _ => ServiceClientControlState.Active,
        };

    public static AiActorControlState AiActorState(GovernedControlStateView? view, DateTimeOffset now)
        => IsStale(view, now) ? AiActorControlState.Disabled : view?.ControlState switch
        {
            GovernedControlStateView.Disabled => AiActorControlState.Disabled,
            GovernedControlStateView.Quarantined => AiActorControlState.Quarantined,
            _ => AiActorControlState.Active,
        };

    public static CommandCapabilityControlState CommandCapabilityState(GovernedControlStateView? view, DateTimeOffset now)
        => IsStale(view, now) ? CommandCapabilityControlState.Disabled : view?.ControlState switch
        {
            GovernedControlStateView.Disabled => CommandCapabilityControlState.Disabled,
            GovernedControlStateView.Quarantined => CommandCapabilityControlState.Quarantined,
            _ => CommandCapabilityControlState.Active,
        };

    public static OutboundChannelControlState OutboundChannelState(GovernedControlStateView? view, DateTimeOffset now)
        => IsStale(view, now) ? OutboundChannelControlState.Disabled : view?.ControlState switch
        {
            GovernedControlStateView.Disabled => OutboundChannelControlState.Disabled,
            GovernedControlStateView.Quarantined => OutboundChannelControlState.Quarantined,
            _ => OutboundChannelControlState.Active,
        };
}
