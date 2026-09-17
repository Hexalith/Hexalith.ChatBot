using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Server.Gateway.Stages;

/// <summary>
/// Read-side seam exposing the FR74/FR75 per-(tenant × outbound-channel) send rate-limit budget to the outbound send
/// seam. The durable projection of the <c>OutboundChannelRateLimitConfigured</c> event into this provider is deferred
/// (mirroring the Story 7.12/7.15/7.18/7.20/7.21/7.23/7.24/7.25 sanctioned read-side deferral): the default
/// implementation always reports no limit (<see langword="null"/>), and tests inject a fake reporting a configured
/// budget to exercise the dispatcher send-seam rate-limit gate in isolation. A dedicated outbound-channel seam —
/// never reusing the per-actor/command-capability history/provider or the control-state provider — keeps the FR74
/// subject classes independent (NFR30 isolation).
/// </summary>
internal interface IOutboundChannelRateLimitProvider
{
    /// <summary>
    /// Resolves the configured rate-limit budget for the given outbound channel (safe <c>AdapterRef</c> token) within
    /// the authenticated tenant. Returns <see langword="null"/> when no rate-limit has been configured/projected.
    /// </summary>
    ValueTask<OutboundChannelRateLimitState?> GetRateLimitAsync(
        string tenantId,
        string outboundChannelRef,
        CancellationToken cancellationToken);
}
