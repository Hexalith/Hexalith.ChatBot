using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Commands;

/// <summary>
/// Metadata-only selected internet header inspection result.
/// </summary>
public sealed record MailboxHeaderInspectionSnapshot(
    IReadOnlyList<MailboxSelectedHeaderSnapshot> ReceivedHeaders,
    IReadOnlyList<MailboxSelectedHeaderSnapshot> AuthenticationResultsHeaders,
    MailboxHeaderValueState From,
    MailboxHeaderValueState ReplyTo,
    MailboxHeaderValueState Sender,
    MailboxHeaderValueState XOriginalSender,
    IReadOnlyList<MailboxHeaderDiscrepancyKind> Discrepancies);
