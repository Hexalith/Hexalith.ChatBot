using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Server.Gateway.Stages;

/// <summary>
/// Default <see cref="IServiceClientRateLimitProvider"/> that always reports no configured limit. The durable
/// projection feeding a budget is deferred per the Story 7.14/7.15/7.16 read-side deferral; the validator seam is
/// wired and unit-tested with a fake.
/// </summary>
internal sealed class AlwaysUnlimitedServiceClientRateLimitProvider : IServiceClientRateLimitProvider
{
    public ValueTask<ServiceClientRateLimitState?> GetRateLimitAsync(
        string tenantId,
        string serviceClientId,
        CancellationToken cancellationToken)
        => ValueTask.FromResult<ServiceClientRateLimitState?>(null);
}
