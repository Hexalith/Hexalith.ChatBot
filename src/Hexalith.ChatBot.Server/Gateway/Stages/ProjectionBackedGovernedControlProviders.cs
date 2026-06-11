using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Projections;

namespace Hexalith.ChatBot.Server.Gateway.Stages;

internal sealed class ProjectionBackedServiceClientControlStateProvider(
    IGovernedControlStateProjectionStore store,
    ISystemClock clock) : IServiceClientControlStateProvider
{
    public async ValueTask<ServiceClientControlState> GetControlStateAsync(string tenantId, string serviceClientId, CancellationToken cancellationToken)
    {
        GovernedControlStateView? view = await store.GetAsync(tenantId, GovernedControlSubjectClasses.ServiceClient, serviceClientId, cancellationToken).ConfigureAwait(false);
        return GovernedProjectionProviderHelpers.ServiceClientState(view, clock.UtcNow);
    }
}

internal sealed class ProjectionBackedAiActorControlStateProvider(
    IGovernedControlStateProjectionStore store,
    ISystemClock clock) : IAiActorControlStateProvider
{
    public async ValueTask<AiActorControlState> GetControlStateAsync(string tenantId, string aiActorId, CancellationToken cancellationToken)
    {
        GovernedControlStateView? view = await store.GetAsync(tenantId, GovernedControlSubjectClasses.AiActor, aiActorId, cancellationToken).ConfigureAwait(false);
        return GovernedProjectionProviderHelpers.AiActorState(view, clock.UtcNow);
    }
}

internal sealed class ProjectionBackedCommandCapabilityControlStateProvider(
    IGovernedControlStateProjectionStore store,
    ISystemClock clock) : ICommandCapabilityControlStateProvider
{
    public async ValueTask<CommandCapabilityControlState> GetControlStateAsync(string tenantId, string commandCapabilityRef, CancellationToken cancellationToken)
    {
        GovernedControlStateView? view = await store.GetAsync(tenantId, GovernedControlSubjectClasses.CommandCapability, commandCapabilityRef, cancellationToken).ConfigureAwait(false);
        return GovernedProjectionProviderHelpers.CommandCapabilityState(view, clock.UtcNow);
    }
}

internal sealed class ProjectionBackedOutboundChannelControlStateProvider(
    IGovernedControlStateProjectionStore store,
    ISystemClock clock) : IOutboundChannelControlStateProvider
{
    public async ValueTask<OutboundChannelControlState> GetControlStateAsync(string tenantId, string outboundChannelRef, CancellationToken cancellationToken)
    {
        GovernedControlStateView? view = await store.GetAsync(tenantId, GovernedControlSubjectClasses.OutboundChannel, outboundChannelRef, cancellationToken).ConfigureAwait(false);
        return GovernedProjectionProviderHelpers.OutboundChannelState(view, clock.UtcNow);
    }
}

internal sealed class ProjectionBackedServiceClientRateLimitProvider(
    IGovernedControlStateProjectionStore store,
    ISystemClock clock) : IServiceClientRateLimitProvider
{
    public async ValueTask<ServiceClientRateLimitState?> GetRateLimitAsync(string tenantId, string serviceClientId, CancellationToken cancellationToken)
    {
        GovernedControlStateView? view = await store.GetAsync(tenantId, GovernedControlSubjectClasses.ServiceClient, serviceClientId, cancellationToken).ConfigureAwait(false);
        return GovernedProjectionProviderHelpers.IsStale(view, clock.UtcNow)
            ? new ServiceClientRateLimitState(1, ServiceClientRateLimitWindow.RollingHour)
            : view?.RateLimitBudget is null ? null : new ServiceClientRateLimitState(view.RateLimitBudget.Value, ServiceClientRateLimitWindow.RollingHour);
    }
}

internal sealed class ProjectionBackedAiActorRateLimitProvider(
    IGovernedControlStateProjectionStore store,
    ISystemClock clock) : IAiActorRateLimitProvider
{
    public async ValueTask<AiActorRateLimitState?> GetRateLimitAsync(string tenantId, string aiActorId, CancellationToken cancellationToken)
    {
        GovernedControlStateView? view = await store.GetAsync(tenantId, GovernedControlSubjectClasses.AiActor, aiActorId, cancellationToken).ConfigureAwait(false);
        return GovernedProjectionProviderHelpers.IsStale(view, clock.UtcNow)
            ? new AiActorRateLimitState(1, AiActorRateLimitWindow.RollingHour)
            : view?.RateLimitBudget is null ? null : new AiActorRateLimitState(view.RateLimitBudget.Value, AiActorRateLimitWindow.RollingHour);
    }
}

internal sealed class ProjectionBackedCommandCapabilityRateLimitProvider(
    IGovernedControlStateProjectionStore store,
    ISystemClock clock) : ICommandCapabilityRateLimitProvider
{
    public async ValueTask<CommandCapabilityRateLimitState?> GetRateLimitAsync(string tenantId, string commandCapabilityRef, CancellationToken cancellationToken)
    {
        GovernedControlStateView? view = await store.GetAsync(tenantId, GovernedControlSubjectClasses.CommandCapability, commandCapabilityRef, cancellationToken).ConfigureAwait(false);
        return GovernedProjectionProviderHelpers.IsStale(view, clock.UtcNow)
            ? new CommandCapabilityRateLimitState(1, CommandCapabilityRateLimitWindow.RollingHour)
            : view?.RateLimitBudget is null ? null : new CommandCapabilityRateLimitState(view.RateLimitBudget.Value, CommandCapabilityRateLimitWindow.RollingHour);
    }
}

internal sealed class ProjectionBackedOutboundChannelRateLimitProvider(
    IGovernedControlStateProjectionStore store,
    ISystemClock clock) : IOutboundChannelRateLimitProvider
{
    public async ValueTask<OutboundChannelRateLimitState?> GetRateLimitAsync(string tenantId, string outboundChannelRef, CancellationToken cancellationToken)
    {
        GovernedControlStateView? view = await store.GetAsync(tenantId, GovernedControlSubjectClasses.OutboundChannel, outboundChannelRef, cancellationToken).ConfigureAwait(false);
        return GovernedProjectionProviderHelpers.IsStale(view, clock.UtcNow)
            ? new OutboundChannelRateLimitState(1, OutboundChannelRateLimitWindow.RollingHour)
            : view?.RateLimitBudget is null ? null : new OutboundChannelRateLimitState(view.RateLimitBudget.Value, OutboundChannelRateLimitWindow.RollingHour);
    }
}

internal sealed class ProjectionBackedServiceClientCommandHistory(IGovernedControlStateProjectionStore store, ISystemClock clock)
    : ProjectionBackedAdmittedHistory(store, clock, GovernedControlSubjectClasses.ServiceClient), IServiceClientCommandHistory
{
    public ValueTask<IReadOnlyList<DateTimeOffset>> GetRecentAdmittedAsync(string tenantId, string serviceClientId, CancellationToken cancellationToken)
        => ReadAsync(tenantId, serviceClientId, cancellationToken);

    public ValueTask RecordAdmittedAsync(string tenantId, string serviceClientId, DateTimeOffset admittedAtUtc, CancellationToken cancellationToken)
        => RecordAsync(tenantId, serviceClientId, admittedAtUtc, cancellationToken);
}

internal sealed class ProjectionBackedAiActorProposalHistory(IGovernedControlStateProjectionStore store, ISystemClock clock)
    : ProjectionBackedAdmittedHistory(store, clock, GovernedControlSubjectClasses.AiActor), IAiActorProposalHistory
{
    public ValueTask<IReadOnlyList<DateTimeOffset>> GetRecentAdmittedAsync(string tenantId, string aiActorId, CancellationToken cancellationToken)
        => ReadAsync(tenantId, aiActorId, cancellationToken);

    public ValueTask RecordAdmittedAsync(string tenantId, string aiActorId, DateTimeOffset admittedAtUtc, CancellationToken cancellationToken)
        => RecordAsync(tenantId, aiActorId, admittedAtUtc, cancellationToken);
}

internal sealed class ProjectionBackedCommandCapabilityCommandHistory(IGovernedControlStateProjectionStore store, ISystemClock clock)
    : ProjectionBackedAdmittedHistory(store, clock, GovernedControlSubjectClasses.CommandCapability), ICommandCapabilityCommandHistory
{
    public ValueTask<IReadOnlyList<DateTimeOffset>> GetRecentAdmittedAsync(string tenantId, string commandCapabilityRef, CancellationToken cancellationToken)
        => ReadAsync(tenantId, commandCapabilityRef, cancellationToken);

    public ValueTask RecordAdmittedAsync(string tenantId, string commandCapabilityRef, DateTimeOffset admittedAtUtc, CancellationToken cancellationToken)
        => RecordAsync(tenantId, commandCapabilityRef, admittedAtUtc, cancellationToken);
}

internal sealed class ProjectionBackedOutboundChannelSendHistory(IGovernedControlStateProjectionStore store, ISystemClock clock)
    : ProjectionBackedAdmittedHistory(store, clock, GovernedControlSubjectClasses.OutboundChannel), IOutboundChannelSendHistory
{
    public ValueTask<IReadOnlyList<DateTimeOffset>> GetRecentSendsAsync(string tenantId, string outboundChannelRef, CancellationToken cancellationToken)
        => ReadAsync(tenantId, outboundChannelRef, cancellationToken);

    public ValueTask RecordSendAsync(string tenantId, string outboundChannelRef, DateTimeOffset sentAtUtc, CancellationToken cancellationToken)
        => RecordAsync(tenantId, outboundChannelRef, sentAtUtc, cancellationToken);
}

internal abstract class ProjectionBackedAdmittedHistory(
    IGovernedControlStateProjectionStore store,
    ISystemClock clock,
    string subjectClass)
{
    protected async ValueTask<IReadOnlyList<DateTimeOffset>> ReadAsync(string tenantId, string subjectRef, CancellationToken cancellationToken)
    {
        GovernedControlStateView? view = await store.GetAsync(tenantId, subjectClass, subjectRef, cancellationToken).ConfigureAwait(false);
        return GovernedProjectionProviderHelpers.IsStale(view, clock.UtcNow) ? [clock.UtcNow] : view?.RecentAdmittedAtUtc ?? [];
    }

    protected async ValueTask RecordAsync(string tenantId, string subjectRef, DateTimeOffset admittedAtUtc, CancellationToken cancellationToken)
    {
        GovernedControlStateView? view = await store.GetAsync(tenantId, subjectClass, subjectRef, cancellationToken).ConfigureAwait(false);
        if (view is null)
        {
            return;
        }

        DateTimeOffset cutoff = admittedAtUtc.ToUniversalTime().AddHours(-1);
        DateTimeOffset[] updated = view.RecentAdmittedAtUtc
            .Where(timestamp => timestamp >= cutoff)
            .Append(admittedAtUtc.ToUniversalTime())
            .ToArray();
        await store.SaveAsync(view with { AdmittedAtUtc = updated, LastUpdatedAtUtc = clock.UtcNow }, cancellationToken).ConfigureAwait(false);
    }
}

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
