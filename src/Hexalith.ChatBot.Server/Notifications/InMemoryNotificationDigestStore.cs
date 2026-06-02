namespace Hexalith.ChatBot.Server.Notifications;

/// <summary>
/// In-memory <see cref="INotificationDigestStore"/> parallel to <c>InMemoryNotificationSink</c>. Pending digest entries
/// are partitioned by <c>(tenant-ref × recipient-ref)</c> so one recipient's overflow can never leak into another's
/// digest. Entries are the content-free <see cref="NotificationDigestEntry"/> form only.
/// </summary>
internal sealed class InMemoryNotificationDigestStore : INotificationDigestStore
{
    private readonly Lock _gate = new();
    private readonly Dictionary<string, List<NotificationDigestEntry>> _entries = new(StringComparer.Ordinal);

    public void Append(NotificationDigestEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        string key = Key(entry.TenantRef, entry.RecipientRef);
        lock (_gate)
        {
            if (!_entries.TryGetValue(key, out List<NotificationDigestEntry>? entries))
            {
                entries = [];
                _entries[key] = entries;
            }

            entries.Add(entry);
        }
    }

    public IReadOnlyList<NotificationDigestEntry> GetPendingEntries(string tenantRef, string recipientRef)
    {
        string key = Key(tenantRef, recipientRef);
        lock (_gate)
        {
            return _entries.TryGetValue(key, out List<NotificationDigestEntry>? entries) ? [.. entries] : [];
        }
    }

    public NotificationDigest DrainPendingDigest(string tenantRef, string recipientRef)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantRef);
        ArgumentException.ThrowIfNullOrWhiteSpace(recipientRef);
        string key = Key(tenantRef, recipientRef);
        lock (_gate)
        {
            IReadOnlyList<NotificationDigestEntry> entries =
                _entries.Remove(key, out List<NotificationDigestEntry>? pending) ? [.. pending] : [];
            return new NotificationDigest(tenantRef, recipientRef, entries);
        }
    }

    private static string Key(string tenantRef, string recipientRef)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantRef);
        ArgumentException.ThrowIfNullOrWhiteSpace(recipientRef);

        // Length-prefix the tenant ref so distinct (tenant × recipient) pairs can never collide on a shared separator.
        return $"{tenantRef.Length}:{tenantRef}{recipientRef}";
    }
}
