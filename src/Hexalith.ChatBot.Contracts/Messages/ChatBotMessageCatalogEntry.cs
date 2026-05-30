namespace Hexalith.ChatBot.Contracts.Messages;

public sealed record ChatBotMessageCatalogEntry(
    string Code,
    string Headline,
    string Reason,
    string NextAction,
    string? DisabledActionReason,
    string DetailVisibility);
