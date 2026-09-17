using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Server.Gateway.Stages;

/// <summary>
/// Default <see cref="IOutboundChannelRateLimitProvider"/> that always reports no configured limit. The durable
/// projection feeding a budget is deferred per the sanctioned read-side deferral; the enforcement seam is wired and
/// unit-tested with a fake.
/// </summary>
internal sealed class AlwaysUnlimitedOutboundChannelRateLimitProvider : IOutboundChannelRateLimitProvider
{
    public ValueTask<OutboundChannelRateLimitState?> GetRateLimitAsync(
        string tenantId,
        string outboundChannelRef,
        CancellationToken cancellationToken)
        => ValueTask.FromResult<OutboundChannelRateLimitState?>(null);
}
