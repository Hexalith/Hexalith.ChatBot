using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Server.Adapters.Mailbox;

internal interface IOutboundMailboxSender
{
    ValueTask<OutboundMailboxSendResult> SendAsync(
        OutboundMailboxSendRequest request,
        CancellationToken cancellationToken = default);
}

internal sealed record OutboundMailboxSendRequest(
    string TenantId,
    string ProjectId,
    string DraftId,
    string ApprovalId,
    string SendId,
    string RequesterId,
    string SendActorId,
    SenderAuthorityClass SenderAuthorityClass,
    string AdapterMode,
    string CorrelationId,
    // Story 9.4 (FR95a): the replay/simulation run that issued this send, or null for a production send. Populated at the
    // dispatcher send seam from the immutable ChatBotCommandSubmission.ReplayRunId so the outbound-trace record carries
    // the same structurally-un-rewritable marker as the audit envelope. Production sends leave it null by omission.
    string? ReplayRunId = null);

internal enum OutboundMailboxSendResultKind
{
    Sent,
    Rejected,
    Unavailable,
}

internal sealed record OutboundMailboxSendResult(
    OutboundMailboxSendResultKind Kind,
    string AdapterStatus,
    string AdapterRef,
    string ReasonCode)
{
    public static OutboundMailboxSendResult Sent(string adapterRef)
        => new(OutboundMailboxSendResultKind.Sent, "sent", SafeToken(adapterRef), "sent");

    public static OutboundMailboxSendResult Rejected(string reasonCode)
        => new(OutboundMailboxSendResultKind.Rejected, "rejected", "adapter:mailbox-outbound", SafeToken(reasonCode));

    public static OutboundMailboxSendResult Unavailable(string reasonCode)
        => new(OutboundMailboxSendResultKind.Unavailable, "unavailable", "adapter:mailbox-outbound", SafeToken(reasonCode));

    private static string SafeToken(string? value)
        => string.IsNullOrWhiteSpace(value) ||
            value.Any(static character => char.IsControl(character) || char.IsWhiteSpace(character) ||
                !(char.IsAsciiLetterOrDigit(character) || character is '_' or '-' or '.' or ':'))
            ? "outbound_adapter_unavailable"
            : value;
}

