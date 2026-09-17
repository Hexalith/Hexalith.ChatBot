using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Server.Gateway.Stages;

/// <summary>
/// Read-side seam exposing the FR74/FR75 per-AI-actor proposal rate-limit budget to the admission pipeline.
/// The durable projection of the <c>AiActorRateLimitConfigured</c> event into this provider is deferred
/// (mirroring the Story 7.14–7.19 sanctioned read-side deferral): the default implementation always reports
/// no limit (<see langword="null"/>), and tests inject a fake reporting a configured budget to exercise the
/// <see cref="ServiceClientGrantValidator"/> final-gate AI-actor rate-limit branch in isolation. A dedicated
/// AI-actor seam — never reusing the service-client history/provider — keeps the two FR74 subject classes
/// independent (NFR30 isolation).
/// </summary>
internal interface IAiActorRateLimitProvider
{
    /// <summary>
    /// Resolves the configured rate-limit budget for the given AI actor within the authenticated tenant.
    /// Returns <see langword="null"/> when no rate-limit has been configured/projected.
    /// </summary>
    ValueTask<AiActorRateLimitState?> GetRateLimitAsync(
        string tenantId,
        string aiActorId,
        CancellationToken cancellationToken);
}
