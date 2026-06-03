namespace Hexalith.ChatBot.Server.Audit;

/// <summary>
/// The read-model projection over redaction state (Story 9.1, AC3). Erasure marks a subject's projection
/// <see cref="Tombstone"/>d so reads collapse to safe-not-found — without ever touching the immutable hash chain. The
/// projection is tenant-partitioned so one tenant's tombstones can never be observed or linked from another (NFR9a).
/// </summary>
internal interface IRedactionProjectionStore
{
    /// <summary>Tombstones the subject's projection for the tenant; subsequent reads must return safe-not-found.</summary>
    void Tombstone(string tenantRef, string subjectRef);

    /// <summary>Whether the subject's projection has been tombstoned within the tenant (a tombstone reads as not-found).</summary>
    bool IsTombstoned(string tenantRef, string subjectRef);
}
