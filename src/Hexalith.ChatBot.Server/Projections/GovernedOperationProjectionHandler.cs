using Hexalith.ChatBot.Server.Audit;

namespace Hexalith.ChatBot.Server.Projections;

/// <summary>
/// Applies a published <c>GovernedNoteRecorded</c> notification to the tenant-partitioned read model. DAPR
/// pub/sub is at-least-once and unordered, so this handler is idempotent and order-tolerant: it is
/// version-stamped and last-writer-wins by source version. A duplicate or stale (lower-or-equal version)
/// notification is a no-op, so exactly one durable read-model effect remains for a repeated submission.
/// </summary>
internal sealed class GovernedOperationProjectionHandler(
    IGovernedOperationProjectionStore store,
    ISystemClock clock)
{
    /// <summary>The outcome of applying a projection notification.</summary>
    public enum ProjectionOutcome
    {
        /// <summary>The notification advanced the read model.</summary>
        Applied,

        /// <summary>The notification was a duplicate or arrived out of order and was ignored.</summary>
        Ignored,
    }

    /// <summary>
    /// Projects a published notification into the read model.
    /// </summary>
    /// <param name="notification">The metadata-only published-event notification.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>Whether the read model advanced or the notification was ignored as duplicate/stale.</returns>
    public async Task<ProjectionOutcome> HandleAsync(
        GovernedNoteRecordedNotification notification,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(notification);

        GovernedOperationView? existing = await store
            .GetAsync(notification.TenantId, notification.NoteId, cancellationToken)
            .ConfigureAwait(false);

        // Idempotent + order-tolerant: drop a duplicate or out-of-order (lower-or-equal source version) event.
        if (existing is not null && existing.SourceVersion >= notification.SourceVersion)
        {
            return ProjectionOutcome.Ignored;
        }

        GovernedOperationView view = new(
            notification.TenantId,
            notification.NoteId,
            GovernedOperationView.CurrentSchemaVersion,
            GovernedOperationView.GovernedCommandProvenance,
            GovernedOperationView.CurrentDerivationKernelVersion,
            GovernedOperationView.MetadataOnlyRedactionState,
            GovernedOperationView.GovernedOperationalRetentionClass,
            notification.SourceVersion,
            existing?.RecordedAt ?? notification.RecordedAt,
            clock.UtcNow);

        await store.SaveAsync(view, cancellationToken).ConfigureAwait(false);
        return ProjectionOutcome.Applied;
    }
}
