using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Server.Gateway.Stages;

/// <summary>
/// Read-side seam exposing the per-(tenant × service-client) recent admitted-command timestamps to the admission
/// pipeline (Story 7.17). The durable history (incremented only on a successful admission) is deferred per the same
/// read-side deferral; the default implementation reports an empty history, and tests pre-seed N timestamps to
/// simulate N admitted commands in the trailing window. Each client's history is independent (NFR30 isolation).
/// </summary>
internal interface IServiceClientCommandHistory
{
    /// <summary>
    /// Resolves the recent admitted-command timestamps for the given service client within the authenticated tenant.
    /// Defaults to an empty history when none has been projected. The trailing-window count is measured server-side in
    /// UTC against the injected clock — never against client/item-supplied time.
    /// </summary>
    ValueTask<IReadOnlyList<DateTimeOffset>> GetRecentAdmittedAsync(
        string tenantId,
        string serviceClientId,
        CancellationToken cancellationToken);

    ValueTask RecordAdmittedAsync(
        string tenantId,
        string serviceClientId,
        DateTimeOffset admittedAtUtc,
        CancellationToken cancellationToken)
        => ValueTask.CompletedTask;
}
