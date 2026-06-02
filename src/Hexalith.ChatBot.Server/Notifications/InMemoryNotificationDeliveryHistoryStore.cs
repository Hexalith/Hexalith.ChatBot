namespace Hexalith.ChatBot.Server.Notifications;

/// <summary>
/// In-memory <see cref="INotificationDeliveryHistoryStore"/> parallel to <c>InMemoryNotificationSink</c>. Holds only
/// server-measured UTC timestamps of audited immediate-push deliveries, partitioned by <c>(tenant-ref × recipient-ref)</c>
/// so one recipient's volume can never throttle, advance, or leak into another's window counts.
/// </summary>
internal sealed class InMemoryNotificationDeliveryHistoryStore : INotificationDeliveryHistoryStore
{
    private readonly Lock _gate = new();
    private readonly Dictionary<string, List<DateTimeOffset>> _timestamps = new(StringComparer.Ordinal);

    public IReadOnlyList<DateTimeOffset> GetImmediatePushTimestamps(string tenantRef, string recipientRef)
    {
        string key = Key(tenantRef, recipientRef);
        lock (_gate)
        {
            return _timestamps.TryGetValue(key, out List<DateTimeOffset>? values) ? [.. values] : [];
        }
    }

    public void RecordImmediatePush(string tenantRef, string recipientRef, DateTimeOffset deliveredAtUtc)
    {
        string key = Key(tenantRef, recipientRef);
        lock (_gate)
        {
            if (!_timestamps.TryGetValue(key, out List<DateTimeOffset>? values))
            {
                values = [];
                _timestamps[key] = values;
            }

            values.Add(deliveredAtUtc.ToUniversalTime());
        }
    }

    private static string Key(string tenantRef, string recipientRef)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantRef);
        ArgumentException.ThrowIfNullOrWhiteSpace(recipientRef);

        // Length-prefix the tenant ref so distinct (tenant × recipient) pairs can never collide on a shared separator.
        return $"{tenantRef.Length}:{tenantRef}{recipientRef}";
    }
}
