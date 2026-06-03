using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Server.Gateway.Stages;

/// <summary>
/// Read-side seam exposing the FR74 governance control state for an outbound channel (the governed external-send
/// path, identified by its safe <c>AdapterRef</c> token) to the outbound send seam in
/// <see cref="AcceptedCommandDispatcher"/>'s <c>ExecuteApprovedOutboundDraft</c> branch. The durable projection of
/// the <c>OutboundChannelDisabled</c> control-state event into this provider is deferred (mirroring the Story
/// 7.12/7.15/7.18/7.21 sanctioned read-side deferral): the default implementation always reports
/// <see cref="OutboundChannelControlState.Active"/>, and tests inject a fake reporting
/// <see cref="OutboundChannelControlState.Disabled"/> to exercise the send-seam fail-closed branch in isolation.
/// This is a dedicated outbound-channel control plane — distinct from the per-actor providers
/// (<see cref="IServiceClientControlStateProvider"/>/<see cref="IAiActorControlStateProvider"/>), the
/// command-capability provider (<see cref="ICommandCapabilityControlStateProvider"/>), and the global static
/// <c>ChatBotSpineCommandAllowlist</c> — keyed by the safe outbound-channel ref. Each tenant's disabled set is
/// independent (isolation).
/// </summary>
internal interface IOutboundChannelControlStateProvider
{
    /// <summary>
    /// Resolves the FR74 control state for the given outbound channel (safe <c>AdapterRef</c> token) within the
    /// authenticated tenant. Defaults to <see cref="OutboundChannelControlState.Active"/> when no disable has been
    /// projected.
    /// </summary>
    ValueTask<OutboundChannelControlState> GetControlStateAsync(
        string tenantId,
        string outboundChannelRef,
        CancellationToken cancellationToken);
}

/// <summary>
/// Default <see cref="IOutboundChannelControlStateProvider"/> that always reports
/// <see cref="OutboundChannelControlState.Active"/>. The durable projection feeding a disabled state is deferred per
/// the Story 7.12/7.15/7.18/7.21 read-side deferral; the enforcement seam is wired and unit-tested with a fake.
/// </summary>
internal sealed class AlwaysActiveOutboundChannelControlStateProvider : IOutboundChannelControlStateProvider
{
    public ValueTask<OutboundChannelControlState> GetControlStateAsync(
        string tenantId,
        string outboundChannelRef,
        CancellationToken cancellationToken)
        => ValueTask.FromResult(OutboundChannelControlState.Active);
}
