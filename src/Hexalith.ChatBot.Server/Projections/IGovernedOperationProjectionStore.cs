namespace Hexalith.ChatBot.Server.Projections;

/// <summary>
/// Tenant-partitioned read-model store for the governed operation projection. Reads and writes are keyed by
/// <c>(tenantId, noteId)</c> so no record is shared across tenants. Implementations must not leak across the
/// tenant boundary: a read for the wrong tenant returns <see langword="null"/> (safe-not-found), never another
/// tenant's record.
/// </summary>
internal interface IGovernedOperationProjectionStore
{
    /// <summary>Gets the projected view for a tenant-scoped note, or <see langword="null"/> when absent.</summary>
    /// <param name="tenantId">The owning tenant.</param>
    /// <param name="noteId">The governed note aggregate id.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The projected view, or <see langword="null"/>.</returns>
    Task<GovernedOperationView?> GetAsync(string tenantId, string noteId, CancellationToken cancellationToken = default);

    /// <summary>Upserts the projected view for its tenant-scoped key.</summary>
    /// <param name="view">The view to persist.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    Task SaveAsync(GovernedOperationView view, CancellationToken cancellationToken = default);
}
