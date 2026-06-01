namespace Hexalith.ChatBot.Contracts.Queries;

public sealed record TaskIntentReviewSourceMessage(
    string SourceMessageId,
    string Content,
    string ContentType,
    string RedactionState,
    string SourceVersion,
    IReadOnlyList<string> EvidenceReferences);
