namespace Hexalith.ChatBot.Server.Notifications;

/// <summary>
/// A fired reviewer-backlog alert (Story 7.10, NFR46): the metadata-only <see cref="NotificationDelivery"/> resolved
/// through the FR72 routing engine (always the aggregate <see cref="NotificationContentVisibility.MetadataRedacted"/>
/// form — no item-specific context), plus the three reviewer-attention signals used for the per-event audit record
/// (FR75g): the reviewer's <see cref="ReviewerRef"/> (a metadata-safe ref), the <see cref="BacklogDepth"/> (count of
/// open items), and the <see cref="OldestItemAgeSeconds"/> (server-measured UTC age of the oldest open item). Stays
/// metadata-only — no project content, evidence, recipient PII, provider payloads, or secrets.
/// </summary>
/// <param name="Notification">The aggregate, redacted delivery resolved through <see cref="NotificationRoutingResolver"/>.</param>
/// <param name="ReviewerRef">The metadata-safe reviewer identity the backlog is attributed to (the subject of the alert).</param>
/// <param name="BacklogDepth">The count of the reviewer's open approval items.</param>
/// <param name="OldestItemAgeSeconds">The server-measured UTC age, in seconds, of the reviewer's oldest open item.</param>
/// <param name="Threshold">The effective backlog threshold the depth exceeded.</param>
internal sealed record ReviewerBacklogAlert(
    NotificationDelivery Notification,
    string ReviewerRef,
    int BacklogDepth,
    int OldestItemAgeSeconds,
    int Threshold);
