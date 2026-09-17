using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Server.Gateway.Stages;

/// <summary>
/// Read-side seam exposing the per-(tenant × outbound-channel) recent admitted-send timestamps to the outbound send
/// seam (Story 7.26). The durable history (incremented only on a successful send) is deferred per the same read-side
/// deferral; the default implementation reports an empty history, and tests pre-seed N timestamps to simulate N
/// admitted sends in the trailing window. Each outbound channel's history is independent (NFR30 isolation) and kept
/// separate from the per-actor / command-capability histories (subject-class separation).
/// </summary>
internal interface IOutboundChannelSendHistory
{
    /// <summary>
    /// Resolves the recent admitted-send timestamps for the given outbound channel (safe <c>AdapterRef</c> token)
    /// within the authenticated tenant. Defaults to an empty history when none has been projected. The trailing-window
    /// count is measured server-side in UTC against the injected clock — never against client/item-supplied time.
    /// </summary>
    ValueTask<IReadOnlyList<DateTimeOffset>> GetRecentSendsAsync(
        string tenantId,
        string outboundChannelRef,
        CancellationToken cancellationToken);

    ValueTask RecordSendAsync(
        string tenantId,
        string outboundChannelRef,
        DateTimeOffset sentAtUtc,
        CancellationToken cancellationToken)
        => ValueTask.CompletedTask;
}
