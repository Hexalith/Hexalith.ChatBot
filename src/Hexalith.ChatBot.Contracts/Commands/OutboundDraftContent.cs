namespace Hexalith.ChatBot.Contracts.Commands;

public sealed record OutboundDraftContent(
    string Subject,
    string ContentText,
    string ContentFormat,
    string ContentRedactionState = "governed_content");
