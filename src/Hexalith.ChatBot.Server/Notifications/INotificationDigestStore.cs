namespace Hexalith.ChatBot.Server.Notifications;

/// <summary>
/// Metadata-only pending-digest seam, keyed strictly by <c>(tenant-ref × recipient-ref)</c>. A throttled notification
/// is appended here as a <see cref="NotificationDigestEntry"/> rather than discarded, so no attention-needed signal is
/// silently lost. Entries are content-free and isolated per pair — a redacted-from-<c>B</c> item never appears in
/// <c>A</c>'s digest. The durable/Dapr-state binding and the runtime digest-send are deferred to the runtime caller.
/// </summary>
internal interface INotificationDigestStore
{
    /// <summary>Appends a throttled overflow notification to the recipient's pending digest.</summary>
    void Append(NotificationDigestEntry entry);

    /// <summary>Gets the pending digest entries for the <c>(tenant × recipient)</c> pair.</summary>
    IReadOnlyList<NotificationDigestEntry> GetPendingEntries(string tenantRef, string recipientRef);

    /// <summary>Builds — and removes — the pending digest for the <c>(tenant × recipient)</c> pair, ready to send.</summary>
    NotificationDigest DrainPendingDigest(string tenantRef, string recipientRef);
}
