using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Server.Adapters.Mailbox;

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
