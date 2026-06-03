using Hexalith.ChatBot.Server.Audit;

namespace Hexalith.ChatBot.Server.Adapters.Mailbox;

/// <summary>
/// A metadata-only "would-have-sent" envelope recorded by the test-mode outbound adapter (Story 9.4, FR95/FR95a). It
/// carries ONLY the safe identity tokens already present on <see cref="OutboundMailboxSendRequest"/> plus the replay run
/// id and a server UTC <see cref="RecordedAtUtc"/> — <b>never</b> recipient addresses, subject, or body content
/// (NFR2/NFR42 no-leak floor). Every string field is reduced to an <see cref="AuditMetadata"/>-safe bounded token on
/// construction via <see cref="FromRequest"/>, so a malformed token can never smuggle content into the trace store.
/// </summary>
internal sealed record OutboundTraceRecord(
    string TenantId,
    string ProjectId,
    string DraftId,
    string ApprovalId,
    string SendId,
    string RequesterId,
    string SendActorId,
    string SenderAuthorityClass,
    string AdapterMode,
    string CorrelationId,
    string? ReplayRunId,
    DateTimeOffset RecordedAtUtc)
{
    private const string SafeFallback = "redacted-ref";

    /// <summary>
    /// Builds the would-have-sent record from a send request, sanitizing every field to a safe bounded token. The
    /// replay run id is the only nullable field — it stays null for a production send and carries the run id for a
    /// replay send, mirroring the audit envelope's marker.
    /// </summary>
    public static OutboundTraceRecord FromRequest(OutboundMailboxSendRequest request, DateTimeOffset recordedAtUtc)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new OutboundTraceRecord(
            Safe(request.TenantId),
            Safe(request.ProjectId),
            Safe(request.DraftId),
            Safe(request.ApprovalId),
            Safe(request.SendId),
            Safe(request.RequesterId),
            Safe(request.SendActorId),
            Safe(request.SenderAuthorityClass.ToString()),
            Safe(request.AdapterMode),
            Safe(request.CorrelationId),
            AuditMetadata.SafeOptionalToken(request.ReplayRunId),
            recordedAtUtc.ToUniversalTime());
    }

    private static string Safe(string? value) => AuditMetadata.SafeOptionalToken(value) ?? SafeFallback;
}

/// <summary>
/// Tenant-partitioned store of the test-mode adapter's would-have-sent records (Story 9.4, AC1). It mirrors the
/// <see cref="Gateway.Stages.IOutboundChannelSendHistory"/> store shape and the WORM store's tenant partitioning
/// (NFR9a — cross-tenant access impossible at the store-access layer): a read for one tenant can never observe another's
/// records. <see cref="EnumerateTenants"/> lets the nightly isolation probe sweep every partition. There is no update or
/// delete path — a trace record is appended once and never mutated.
/// </summary>
internal interface IOutboundTraceStore
{
    /// <summary>Appends a would-have-sent record to its tenant's partition.</summary>
    ValueTask RecordAsync(OutboundTraceRecord record, CancellationToken cancellationToken);

    /// <summary>Returns every trace record for a single tenant in record order. A foreign/unknown tenant yields empty.</summary>
    IReadOnlyList<OutboundTraceRecord> EnumerateForTenant(string tenantId);

    /// <summary>Returns the tenant refs that currently hold any trace record, so the isolation probe can sweep per tenant.</summary>
    IReadOnlyList<string> EnumerateTenants();
}

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
