using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Server.Gateway.Stages;

/// <summary>
/// Default <see cref="IAiActorRateLimitProvider"/> that always reports no configured limit. The durable
/// projection feeding a budget is deferred per the Story 7.14–7.19 read-side deferral; the validator seam is
/// wired and unit-tested with a fake.
/// </summary>
internal sealed class AlwaysUnlimitedAiActorRateLimitProvider : IAiActorRateLimitProvider
{
    public ValueTask<AiActorRateLimitState?> GetRateLimitAsync(
        string tenantId,
        string aiActorId,
        CancellationToken cancellationToken)
        => ValueTask.FromResult<AiActorRateLimitState?>(null);
}
