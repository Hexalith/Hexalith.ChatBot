using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Server.Adapters.Mailbox;
using Hexalith.ChatBot.Server.Projections;

namespace Hexalith.ChatBot.Server.Lifecycle.Attachments;

internal sealed record AttachmentScanResult(
    AttachmentScanResultKind Kind,
    string ReasonCode)
{
    public static AttachmentScanResult Clean()
        => new(AttachmentScanResultKind.Clean, "attachment_scan_clean");

    public static AttachmentScanResult Unsafe(string reasonCode)
        => new(AttachmentScanResultKind.Unsafe, SafeReason(reasonCode, "attachment_scan_unsafe"));

    public static AttachmentScanResult Unavailable(string reasonCode)
        => new(AttachmentScanResultKind.Unavailable, SafeReason(reasonCode, "attachment_scan_unavailable"));

    public static AttachmentScanResult Retryable(string reasonCode)
        => new(AttachmentScanResultKind.Retryable, SafeReason(reasonCode, "attachment_scan_retryable"));

    public static AttachmentScanResult Failed(string reasonCode)
        => new(AttachmentScanResultKind.Failed, SafeReason(reasonCode, "attachment_scan_failed"));

    public static AttachmentScanResult Indeterminate(string reasonCode)
        => new(AttachmentScanResultKind.Indeterminate, SafeReason(reasonCode, "attachment_scan_indeterminate"));

    internal static string SafeReason(string? value, string fallback)
        => string.IsNullOrWhiteSpace(value) ||
            value.Any(static character => char.IsControl(character) || char.IsWhiteSpace(character) ||
                !(char.IsLetterOrDigit(character) || character is '_' or '-'))
            ? fallback
            : value;
}
