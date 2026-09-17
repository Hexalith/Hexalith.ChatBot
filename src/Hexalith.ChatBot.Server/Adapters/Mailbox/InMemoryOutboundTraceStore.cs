using Hexalith.ChatBot.Server.Audit;

namespace Hexalith.ChatBot.Server.Adapters.Mailbox;

/// <summary>
/// In-process, append-only <see cref="IOutboundTraceStore"/> — the seam-first test/dev default, mirroring
/// <see cref="InMemoryWormAuditStore"/>. One lock-guarded list per tenant, partitioned by an ordinal tenant key so a
/// read for one tenant can never observe another's records. The production swap is a durable tenant-partitioned store
/// behind the same interface.
/// </summary>
internal sealed class InMemoryOutboundTraceStore : IOutboundTraceStore
{
    private readonly Lock _gate = new();
    private readonly Dictionary<string, List<OutboundTraceRecord>> _records = new(StringComparer.Ordinal);

    public ValueTask RecordAsync(OutboundTraceRecord record, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(record);
        ArgumentException.ThrowIfNullOrWhiteSpace(record.TenantId);
        cancellationToken.ThrowIfCancellationRequested();

        lock (_gate)
        {
            if (!_records.TryGetValue(record.TenantId, out List<OutboundTraceRecord>? partition))
            {
                partition = [];
                _records[record.TenantId] = partition;
            }

            partition.Add(record);
        }

        return ValueTask.CompletedTask;
    }

    public IReadOnlyList<OutboundTraceRecord> EnumerateForTenant(string tenantId)
    {
        ArgumentNullException.ThrowIfNull(tenantId);
        lock (_gate)
        {
            return _records.TryGetValue(tenantId, out List<OutboundTraceRecord>? partition) ? [.. partition] : [];
        }
    }

    public IReadOnlyList<string> EnumerateTenants()
    {
        lock (_gate)
        {
            return [.. _records.Keys];
        }
    }
}
