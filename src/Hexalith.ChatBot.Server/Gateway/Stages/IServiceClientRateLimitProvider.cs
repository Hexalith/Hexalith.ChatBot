using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Server.Gateway.Stages;

/// <summary>
/// Read-side seam exposing the FR74/FR75 per-service-client command rate-limit budget to the admission pipeline.
/// The durable projection of the <c>ServiceClientRateLimitConfigured</c> event into this provider is deferred
/// (mirroring the Story 7.14/7.15/7.16 sanctioned read-side deferral): the default implementation always reports
/// no limit (<see langword="null"/>), and tests inject a fake reporting a configured budget to exercise the
/// <see cref="ServiceClientGrantValidator"/> final-gate rate-limit branch in isolation.
/// </summary>
internal interface IServiceClientRateLimitProvider
{
    /// <summary>
    /// Resolves the configured rate-limit budget for the given service client within the authenticated tenant.
    /// Returns <see langword="null"/> when no rate-limit has been configured/projected.
    /// </summary>
    ValueTask<ServiceClientRateLimitState?> GetRateLimitAsync(
        string tenantId,
        string serviceClientId,
        CancellationToken cancellationToken);
}
