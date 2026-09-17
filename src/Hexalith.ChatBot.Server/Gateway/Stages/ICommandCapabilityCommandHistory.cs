using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Server.Gateway.Stages;

/// <summary>
/// Read-side seam exposing the per-(tenant × command-type) recent admitted-command timestamps to the actor-agnostic
/// admission pipeline (Story 7.23). The durable history (incremented only on a successful admission) is deferred per
/// the same read-side deferral; the default implementation reports an empty history, and tests pre-seed N timestamps
/// to simulate N admitted commands in the trailing window. Each command type's history is independent (NFR30
/// isolation) and kept separate from the per-actor command/proposal histories (subject-class separation).
/// </summary>
internal interface ICommandCapabilityCommandHistory
{
    /// <summary>
    /// Resolves the recent admitted-command timestamps for the given command capability (command type name) within
    /// the authenticated tenant. Defaults to an empty history when none has been projected. The trailing-window count
    /// is measured server-side in UTC against the injected clock — never against client/item-supplied time.
    /// </summary>
    ValueTask<IReadOnlyList<DateTimeOffset>> GetRecentAdmittedAsync(
        string tenantId,
        string commandCapabilityRef,
        CancellationToken cancellationToken);

    ValueTask RecordAdmittedAsync(
        string tenantId,
        string commandCapabilityRef,
        DateTimeOffset admittedAtUtc,
        CancellationToken cancellationToken)
        => ValueTask.CompletedTask;
}
