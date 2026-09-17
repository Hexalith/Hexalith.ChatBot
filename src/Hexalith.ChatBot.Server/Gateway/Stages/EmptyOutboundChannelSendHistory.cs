using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Server.Gateway.Stages;

/// <summary>
/// Default <see cref="IOutboundChannelSendHistory"/> that always reports an empty admitted-send history (so the seam
/// sends by default). The durable per-(tenant × outbound-channel) history is deferred per the sanctioned read-side
/// deferral.
/// </summary>
internal sealed class EmptyOutboundChannelSendHistory : IOutboundChannelSendHistory
{
    public ValueTask<IReadOnlyList<DateTimeOffset>> GetRecentSendsAsync(
        string tenantId,
        string outboundChannelRef,
        CancellationToken cancellationToken)
        => ValueTask.FromResult<IReadOnlyList<DateTimeOffset>>([]);
}
