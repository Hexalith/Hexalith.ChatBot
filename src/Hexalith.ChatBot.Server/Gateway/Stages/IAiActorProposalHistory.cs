using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Server.Gateway.Stages;

/// <summary>
/// Read-side seam exposing the per-(tenant × AI-actor) recent admitted-proposal timestamps to the admission
/// pipeline (Story 7.20). The durable history (incremented only on a successful admission) is deferred per the same
/// read-side deferral; the default implementation reports an empty history, and tests pre-seed N timestamps to
/// simulate N admitted proposals in the trailing window. Each AI actor's history is independent (NFR30 isolation) and
/// kept separate from the service-client command history (subject-class separation).
/// </summary>
internal interface IAiActorProposalHistory
{
    /// <summary>
    /// Resolves the recent admitted-proposal timestamps for the given AI actor within the authenticated tenant.
    /// Defaults to an empty history when none has been projected. The trailing-window count is measured server-side in
    /// UTC against the injected clock — never against client/item-supplied time.
    /// </summary>
    ValueTask<IReadOnlyList<DateTimeOffset>> GetRecentAdmittedAsync(
        string tenantId,
        string aiActorId,
        CancellationToken cancellationToken);

    ValueTask RecordAdmittedAsync(
        string tenantId,
        string aiActorId,
        DateTimeOffset admittedAtUtc,
        CancellationToken cancellationToken)
        => ValueTask.CompletedTask;
}
