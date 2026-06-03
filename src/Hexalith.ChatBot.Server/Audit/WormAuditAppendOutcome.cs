namespace Hexalith.ChatBot.Server.Audit;

/// <summary>
/// The result of appending an envelope to the WORM audit chain (Story 9.1, NFR49a). Mirrors the fail-open
/// <see cref="AuditWriteResult"/> shape so the post-commit path can map a chain-append failure straight onto the
/// gateway's existing reconcile-from-event-log machinery: on success the appended <see cref="Record"/> is carried for
/// inspection; on failure <see cref="Record"/> is <c>null</c> and the reason code drives the reconcile/alert path.
/// </summary>
internal sealed record WormAuditAppendOutcome(bool Succeeded, string ReasonCode, WormAuditChainRecord? Record)
{
    public static WormAuditAppendOutcome Success(WormAuditChainRecord record)
        => new(true, "worm_chain_appended", record);

    public static WormAuditAppendOutcome Unavailable(string reasonCode = AuditFailureReasonCodes.AuditUnavailable)
        => new(false, reasonCode, Record: null);
}
