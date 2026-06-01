namespace Hexalith.ChatBot.Server.Governance.AiMediation;

internal sealed record MailboxMessageContentResult(
    bool Available,
    string ReasonCode,
    string? Content = null,
    string ContentType = "text/plain",
    string RedactionState = "metadata_only");
