using System.Globalization;

using Hexalith.ChatBot.Server.Adapters.Mailbox;

namespace Hexalith.ChatBot.Server.Audit;

/// <summary>
/// Pure, deterministic verifier for a production tenant's replay isolation (Story 9.4, AC3, FR95a). Given a production
/// tenant's enumerated outbound-trace records and WORM-chain envelopes it asserts <b>two complementary invariants</b>
/// (both required):
/// <list type="number">
/// <item>no outbound-trace record carries a non-null <see cref="OutboundTraceRecord.ReplayRunId"/> (the primary AC3
/// assertion: no replay run has ever produced a record in any production tenant's outbound-trace store);</item>
/// <item>no WORM-chain envelope is a replay envelope (<see cref="AuditReplayExclusion.IsReplayEnvelope"/>) — defense in
/// depth, reusing the Story 9.2 predicate rather than re-deriving the marker test.</item>
/// </list>
/// It returns a metadata-only <see cref="ReplayIsolationVerificationResult"/> — the status, a bounded reason code, and a
/// safe first-offender locator token, never record content. Mirrors <see cref="WormAuditChainVerifier"/>.
/// <para>
/// The verifier itself never returns <see cref="ReplayIsolationStatus.Unknown"/>; the coordinator treats an enumeration
/// that cannot complete as <c>Unknown</c> — a breach signal — rather than silent success (the same fail-closed split as
/// the chain verifier/coordinator). The trace-store assertion is checked first so its locator is preferred when both
/// invariants are violated.
/// </para>
/// </summary>
internal static class ReplayIsolationVerifier
{
    public static ReplayIsolationVerificationResult Verify(
        string tenantRef,
        IReadOnlyList<OutboundTraceRecord> productionTraceRecords,
        IReadOnlyList<AuditEnvelope> productionChainEnvelopes)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantRef);
        ArgumentNullException.ThrowIfNull(productionTraceRecords);
        ArgumentNullException.ThrowIfNull(productionChainEnvelopes);

        for (int index = 0; index < productionTraceRecords.Count; index++)
        {
            if (productionTraceRecords[index].ReplayRunId is not null)
            {
                return Breach(
                    tenantRef,
                    ReplayIsolationVerificationResult.TraceBreachReasonCode,
                    TraceLocator(productionTraceRecords[index], index));
            }
        }

        for (int index = 0; index < productionChainEnvelopes.Count; index++)
        {
            if (AuditReplayExclusion.IsReplayEnvelope(productionChainEnvelopes[index]))
            {
                return Breach(
                    tenantRef,
                    ReplayIsolationVerificationResult.ChainBreachReasonCode,
                    $"chain-seq:{index.ToString(CultureInfo.InvariantCulture)}");
            }
        }

        return new ReplayIsolationVerificationResult(
            tenantRef,
            ReplayIsolationStatus.Clean,
            ReplayIsolationVerificationResult.CleanReasonCode,
            FirstOffenderLocator: null);
    }

    // A safe, bounded locator for the first offending trace record: its safe SendId when available, else the index.
    private static string TraceLocator(OutboundTraceRecord record, int index)
        => AuditMetadata.SafeOptionalToken(record.SendId) is { } safeSendId
            ? $"trace-send:{safeSendId}"
            : $"trace-index:{index.ToString(CultureInfo.InvariantCulture)}";

    private static ReplayIsolationVerificationResult Breach(string tenantRef, string reasonCode, string locator)
        => new(tenantRef, ReplayIsolationStatus.Breach, reasonCode, locator);
}
