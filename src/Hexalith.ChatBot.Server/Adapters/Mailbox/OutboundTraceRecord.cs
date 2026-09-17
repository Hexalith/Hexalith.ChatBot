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
