using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Commands;

/// <summary>
/// Safe selected-header evidence reference. Values are intentionally excluded.
/// </summary>
public sealed record MailboxSelectedHeaderSnapshot(
    string Name,
    int Ordinal,
    MailboxHeaderValueState ValueState);
