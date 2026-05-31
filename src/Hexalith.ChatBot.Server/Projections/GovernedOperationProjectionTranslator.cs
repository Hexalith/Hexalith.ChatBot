using Hexalith.ChatBot.Server.Operations;

namespace Hexalith.ChatBot.Server.Projections;

/// <summary>
/// Translates an EventStore-published envelope into a <see cref="GovernedNoteRecordedNotification"/>, deriving
/// the tenant (M2) and the order-tolerant source version (M1) from the <b>verified</b> envelope metadata only.
/// The original command submitter never controls these values: tenant comes from the EventStore-stamped
/// <c>tenantId</c> (not a request body field), and the source version comes from the aggregate sequence number.
/// Any event that is not a chatbot-domain governed-note-recorded event, or whose envelope is malformed, yields
/// <see langword="null"/> so unrelated events delivered on the topic are ignored idempotently.
/// </summary>
internal static class GovernedOperationProjectionTranslator
{
    /// <summary>The fully-qualified event type name EventStore stamps for a recorded governed note.</summary>
    public static readonly string GovernedNoteRecordedEventType = typeof(GovernedNoteRecorded).FullName!;

    /// <summary>
    /// Builds a projection notification from a published envelope, or returns <see langword="null"/> when the
    /// envelope is not a chatbot governed-note-recorded event or is missing required verified metadata.
    /// </summary>
    /// <param name="published">The published EventStore envelope (CloudEvent data).</param>
    /// <returns>The projection notification, or <see langword="null"/> to ignore the event.</returns>
    public static GovernedNoteRecordedNotification? TryCreateNotification(PublishedGovernedOperationEvent? published)
    {
        if (published is null
            || !string.Equals(published.Domain, ChatBotEventStore.DomainName, StringComparison.Ordinal)
            || !string.Equals(published.EventTypeName, GovernedNoteRecordedEventType, StringComparison.Ordinal)
            || string.IsNullOrWhiteSpace(published.TenantId)
            || string.IsNullOrWhiteSpace(published.AggregateId)
            || published.SequenceNumber <= 0)
        {
            return null;
        }

        return new GovernedNoteRecordedNotification(
            published.TenantId,
            published.AggregateId,
            string.IsNullOrWhiteSpace(published.MessageId) ? published.AggregateId : published.MessageId,
            published.SequenceNumber,
            published.Timestamp,
            published.CorrelationId ?? string.Empty);
    }
}
