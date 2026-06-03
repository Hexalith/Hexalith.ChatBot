namespace Hexalith.ChatBot.Server.Audit;

/// <summary>
/// In-process, append-only implementation of <see cref="IWormAuditStore"/> (Story 9.1) — the seam-first test/dev
/// default, mirroring <see cref="Gateway.Stages.InMemoryAuditWriter"/>. It holds one ordered, lock-guarded chain per
/// tenant; the chains are partitioned by an ordinal tenant key so a read for one tenant can never observe another's
/// records (NFR9a). The production swap is an immutable/WORM object store, but the chaining contract — append-only,
/// per-tenant monotonic sequence, predecessor-hash linkage — is identical and lives here behind the interface.
/// <para>
/// There is no update/delete path: the only mutation is appending to the tail. M0 is single-tenant but the per-tenant
/// dictionary makes a second tenant additive, not a rewrite.
/// </para>
/// </summary>
internal sealed class InMemoryWormAuditStore : IWormAuditStore
{
    private readonly Lock _gate = new();
    private readonly Dictionary<string, List<WormAuditChainRecord>> _chains = new(StringComparer.Ordinal);

    public ValueTask<WormAuditAppendOutcome> AppendAsync(AuditEnvelope envelope, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(envelope);
        ArgumentException.ThrowIfNullOrWhiteSpace(envelope.TenantId);

        lock (_gate)
        {
            if (!_chains.TryGetValue(envelope.TenantId, out List<WormAuditChainRecord>? chain))
            {
                chain = [];
                _chains[envelope.TenantId] = chain;
            }

            long sequence = chain.Count;
            string predecessorHash = chain.Count == 0
                ? WormAuditChainHasher.GenesisPredecessorHash
                : chain[^1].RecordHash;

            // The store — not the factory — assigns the predecessor hash at append time, making the envelope's
            // already-existing PredecessorHash field real for chained appends (it is always null off-chain).
            AuditEnvelope chainedEnvelope = envelope with { PredecessorHash = predecessorHash };
            string recordHash = WormAuditChainHasher.ComputeRecordHash(chainedEnvelope, predecessorHash, sequence);

            WormAuditChainRecord record = new(
                chainedEnvelope,
                sequence,
                predecessorHash,
                recordHash,
                WormAuditChainHasher.CanonicalSerializationVersion);
            chain.Add(record);

            return ValueTask.FromResult(WormAuditAppendOutcome.Success(record));
        }
    }

    public IReadOnlyList<WormAuditChainRecord> EnumerateChain(string tenantId)
    {
        ArgumentNullException.ThrowIfNull(tenantId);
        lock (_gate)
        {
            return _chains.TryGetValue(tenantId, out List<WormAuditChainRecord>? chain) ? [.. chain] : [];
        }
    }

    public IReadOnlyList<string> EnumerateTenants()
    {
        lock (_gate)
        {
            return [.. _chains.Keys];
        }
    }
}
