namespace Hexalith.ChatBot.Server.Audit;

/// <summary>In-process, tenant-partitioned implementation of <see cref="IRedactionProjectionStore"/> (Story 9.1, AC3).</summary>
internal sealed class InMemoryRedactionProjectionStore : IRedactionProjectionStore
{
    private readonly Lock _gate = new();
    private readonly HashSet<(string Tenant, string Subject)> _tombstones = [];

    public void Tombstone(string tenantRef, string subjectRef)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantRef);
        ArgumentException.ThrowIfNullOrWhiteSpace(subjectRef);
        lock (_gate)
        {
            _tombstones.Add((tenantRef, subjectRef));
        }
    }

    public bool IsTombstoned(string tenantRef, string subjectRef)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantRef);
        ArgumentException.ThrowIfNullOrWhiteSpace(subjectRef);
        lock (_gate)
        {
            return _tombstones.Contains((tenantRef, subjectRef));
        }
    }
}
