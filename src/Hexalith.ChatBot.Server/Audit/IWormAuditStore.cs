namespace Hexalith.ChatBot.Server.Audit;

/// <summary>
/// The append-only, tamper-evident WORM audit store (Story 9.1, NFR49a). Its contract makes deletion and in-place
/// mutation impossible at the storage layer: there is deliberately <b>no</b> update, delete, or remove member — only
/// <see cref="AppendAsync"/> plus tenant-partitioned reads. Every read is scoped to a single tenant
/// (<see cref="EnumerateChain"/> / <see cref="EnumerateTenants"/>), so no operation can observe or link another
/// tenant's records (NFR9a — tenant isolation by construction). The store assigns each appended record its per-tenant
/// monotonic sequence and the predecessor hash linking it to the prior chain head; callers supply only the envelope.
/// </summary>
internal interface IWormAuditStore
{
    /// <summary>
    /// Appends an envelope to its tenant's hash chain, assigning the per-tenant sequence and predecessor hash and
    /// computing the record hash. Returns the appended record on success, or a fail-open
    /// <see cref="WormAuditAppendOutcome.Unavailable"/> so the post-commit path reconciles the gap from the event log
    /// rather than blocking the commit.
    /// </summary>
    ValueTask<WormAuditAppendOutcome> AppendAsync(AuditEnvelope envelope, CancellationToken cancellationToken);

    /// <summary>
    /// Returns the full hash chain for a single tenant in append order. A foreign or unknown tenant yields an empty
    /// chain, so the read never confirms existence across the tenant boundary.
    /// </summary>
    IReadOnlyList<WormAuditChainRecord> EnumerateChain(string tenantId);

    /// <summary>Returns the tenant refs that currently hold a chain, so the nightly verifier can sweep per tenant.</summary>
    IReadOnlyList<string> EnumerateTenants();
}
