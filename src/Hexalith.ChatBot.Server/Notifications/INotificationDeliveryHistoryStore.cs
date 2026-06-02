namespace Hexalith.ChatBot.Server.Notifications;

/// <summary>
/// Metadata-only per-recipient immediate-push history seam, keyed strictly by <c>(tenant-ref × recipient-ref)</c>. It
/// records only the server-measured UTC timestamps of <em>audited immediate-push</em> deliveries — never restricted
/// content — so the <see cref="NotificationThrottleEvaluator"/> can measure the rolling windows. Tenant/recipient refs
/// come from the authenticated-binding-sourced <c>NotificationDelivery</c>; one recipient's history can never be read
/// or advanced from another's. The durable/Dapr-state binding is deferred to the runtime caller (consistent with the
/// Story 7.6/7.7 deferred delivery runtime); the in-memory implementation parallels <c>InMemoryNotificationSink</c>.
/// </summary>
internal interface INotificationDeliveryHistoryStore
{
    /// <summary>Gets the recorded immediate-push UTC timestamps for the <c>(tenant × recipient)</c> pair.</summary>
    IReadOnlyList<DateTimeOffset> GetImmediatePushTimestamps(string tenantRef, string recipientRef);

    /// <summary>Records an audited immediate-push delivery's server-measured UTC timestamp for the pair.</summary>
    void RecordImmediatePush(string tenantRef, string recipientRef, DateTimeOffset deliveredAtUtc);
}
