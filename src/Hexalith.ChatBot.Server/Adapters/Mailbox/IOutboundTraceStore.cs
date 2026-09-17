using Hexalith.ChatBot.Server.Audit;

namespace Hexalith.ChatBot.Server.Adapters.Mailbox;

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
