using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Server.Association.Participants;
using Hexalith.ChatBot.Server.Operations;

namespace Hexalith.ChatBot.Server.Projections;

internal static class ParticipantResolutionProjectionTranslator
{
    public static readonly string ResolvedEventType = typeof(MailboxParticipantResolved).FullName!;
    public static readonly string UnresolvedEventType = typeof(MailboxParticipantUnresolved).FullName!;

    public static ParticipantResolutionNotification? TryCreateNotification(PublishedParticipantResolutionEvent? published)
    {
        if (published is null ||
            !string.Equals(published.Domain, ChatBotEventStore.DomainName, StringComparison.Ordinal) ||
            string.IsNullOrWhiteSpace(published.TenantId) ||
            string.IsNullOrWhiteSpace(published.AggregateId) ||
            string.IsNullOrWhiteSpace(published.SourceParticipantId) ||
            string.IsNullOrWhiteSpace(published.IntakeId) ||
            string.IsNullOrWhiteSpace(published.SourceMailboxId) ||
            string.IsNullOrWhiteSpace(published.EvidenceReference) ||
            string.IsNullOrWhiteSpace(published.EvidenceFingerprint) ||
            published.SequenceNumber <= 0)
        {
            return null;
        }

        if (string.Equals(published.EventTypeName, ResolvedEventType, StringComparison.Ordinal) &&
            !string.IsNullOrWhiteSpace(published.PartyId))
        {
            return new ParticipantResolutionNotification(
                published.TenantId,
                published.AggregateId,
                published.IntakeId,
                published.SourceMailboxId,
                published.SourceParticipantId,
                published.PartyId,
                ParticipantResolutionStatus.Resolved,
                null,
                [],
                published.EvidenceReference,
                published.EvidenceFingerprint,
                published.SequenceNumber,
                published.Timestamp,
                published.CorrelationId ?? string.Empty);
        }

        if (string.Equals(published.EventTypeName, UnresolvedEventType, StringComparison.Ordinal) &&
            published.Reason is not null)
        {
            return new ParticipantResolutionNotification(
                published.TenantId,
                published.AggregateId,
                published.IntakeId,
                published.SourceMailboxId,
                published.SourceParticipantId,
                null,
                ParticipantResolutionStatus.Unresolved,
                published.Reason,
                published.AllowedReviewActions is { Count: > 0 } ? published.AllowedReviewActions : [],
                published.EvidenceReference,
                published.EvidenceFingerprint,
                published.SequenceNumber,
                published.Timestamp,
                published.CorrelationId ?? string.Empty);
        }

        return null;
    }
}
