using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Server.Audit;

namespace Hexalith.ChatBot.Server.Projections;

internal sealed class ParticipantResolutionProjectionHandler(
    IParticipantResolutionProjectionStore store,
    ISystemClock clock)
{
    public enum ProjectionOutcome
    {
        Applied,
        Ignored,
    }

    public async Task<ProjectionOutcome> HandleAsync(
        ParticipantResolutionNotification notification,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(notification);

        ParticipantResolutionView? existing = await store
            .GetAsync(notification.TenantId, notification.ResolutionId, notification.SourceParticipantId, cancellationToken)
            .ConfigureAwait(false);
        if (existing is not null && existing.SourceVersion >= notification.SourceVersion)
        {
            return ProjectionOutcome.Ignored;
        }

        ParticipantResolutionView view = new(
            notification.TenantId,
            notification.ResolutionId,
            notification.IntakeId,
            notification.SourceMailboxId,
            notification.SourceParticipantId,
            notification.PartyId,
            notification.Status,
            notification.Reason,
            notification.EvidenceReference,
            notification.EvidenceFingerprint,
            ParticipantResolutionView.CurrentSchemaVersion,
            ParticipantResolutionView.MailboxSourceProvenance,
            ParticipantResolutionView.CurrentDerivationKernelVersion,
            ParticipantResolutionView.MetadataOnlyRedactionState,
            ParticipantResolutionView.CollaborationRetentionClass,
            notification.SourceVersion,
            notification.CorrelationId,
            existing?.RecordedAt ?? notification.RecordedAt,
            clock.UtcNow);

        await store.SaveAsync(view, cancellationToken).ConfigureAwait(false);
        return ProjectionOutcome.Applied;
    }
}
