using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Server.Gateway.Stages;

/// <summary>
/// Default <see cref="ICommandCapabilityRateLimitProvider"/> that always reports no configured limit. The durable
/// projection feeding a budget is deferred per the Story 7.20–7.22 read-side deferral; the enforcement seam is wired
/// and unit-tested with a fake.
/// </summary>
internal sealed class AlwaysUnlimitedCommandCapabilityRateLimitProvider : ICommandCapabilityRateLimitProvider
{
    public ValueTask<CommandCapabilityRateLimitState?> GetRateLimitAsync(
        string tenantId,
        string commandCapabilityRef,
        CancellationToken cancellationToken)
        => ValueTask.FromResult<CommandCapabilityRateLimitState?>(null);
}
