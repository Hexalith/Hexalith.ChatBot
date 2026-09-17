using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Server.Gateway.Stages;

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
