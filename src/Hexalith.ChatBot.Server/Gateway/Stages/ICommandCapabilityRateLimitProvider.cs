using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Server.Gateway.Stages;

/// <summary>
/// Read-side seam exposing the FR74/FR75 per-(tenant × command-type) command rate-limit budget to the actor-agnostic
/// admission pipeline. The durable projection of the <c>CommandCapabilityRateLimitConfigured</c> event into this
/// provider is deferred (mirroring the Story 7.20–7.22 sanctioned read-side deferral): the default implementation
/// always reports no limit (<see langword="null"/>), and tests inject a fake reporting a configured budget to exercise
/// the <see cref="ParticipantAuthorizationStage"/> final-gate rate-limit branch in isolation. A dedicated
/// command-capability seam — never reusing the per-actor history/provider — keeps the FR74 subject classes
/// independent (NFR30 isolation).
/// </summary>
internal interface ICommandCapabilityRateLimitProvider
{
    /// <summary>
    /// Resolves the configured rate-limit budget for the given command capability (command type name) within the
    /// authenticated tenant. Returns <see langword="null"/> when no rate-limit has been configured/projected.
    /// </summary>
    ValueTask<CommandCapabilityRateLimitState?> GetRateLimitAsync(
        string tenantId,
        string commandCapabilityRef,
        CancellationToken cancellationToken);
}
