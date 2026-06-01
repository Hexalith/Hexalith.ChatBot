using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Server.Adapters.Parties;
using Hexalith.ChatBot.Server.Audit;

namespace Hexalith.ChatBot.Server.Projections;

internal sealed class ParticipantResolutionProjectionHandler(
    IParticipantResolutionProjectionStore store,
    ISystemClock clock,
    IParticipantDisplayDirectory displayDirectory,
    IProjectConversationProjectionStore? conversationStore = null)
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

        ParticipantDisplaySnapshot display = await ResolveDisplayAsync(notification, cancellationToken).ConfigureAwait(false);
        ParticipantResolutionView view = new(
            notification.TenantId,
            notification.ResolutionId,
            notification.IntakeId,
            notification.SourceMailboxId,
            notification.SourceParticipantId,
            notification.PartyId,
            notification.Status,
            notification.Reason,
            notification.AllowedReviewActions,
            display.DisplayKind,
            SafeDisplayLabel(display),
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
        if (conversationStore is not null)
        {
            await conversationStore.UpsertParticipantResolutionAsync(view, cancellationToken).ConfigureAwait(false);
        }

        return ProjectionOutcome.Applied;
    }

    private async Task<ParticipantDisplaySnapshot> ResolveDisplayAsync(
        ParticipantResolutionNotification notification,
        CancellationToken cancellationToken)
    {
        if (notification.Status != ParticipantResolutionStatus.Resolved ||
            string.IsNullOrWhiteSpace(notification.PartyId))
        {
            return new ParticipantDisplaySnapshot(
                ProjectConversationParticipantDisplayKind.UnresolvedParticipant,
                null);
        }

        ParticipantDisplaySnapshot display = await displayDirectory
            .GetSafeDisplayAsync(notification.TenantId, notification.PartyId, cancellationToken)
            .ConfigureAwait(false);

        return display.DisplayKind is ProjectConversationParticipantDisplayKind.InternalParticipant
            or ProjectConversationParticipantDisplayKind.ExternalParticipant
            ? display
            : new ParticipantDisplaySnapshot(ProjectConversationParticipantDisplayKind.RestrictedParticipant, null);
    }

    private static string SafeDisplayLabel(ParticipantDisplaySnapshot display)
        => display.DisplayKind switch
        {
            ProjectConversationParticipantDisplayKind.InternalParticipant => display.SafeDisplayLabel ?? string.Empty,
            ProjectConversationParticipantDisplayKind.ExternalParticipant => display.SafeDisplayLabel ?? string.Empty,
            _ => string.Empty,
        };
}
