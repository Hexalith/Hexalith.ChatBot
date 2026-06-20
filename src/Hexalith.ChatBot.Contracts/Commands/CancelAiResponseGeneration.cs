namespace Hexalith.ChatBot.Contracts.Commands;

/// <summary>
/// Metadata-only request to stop an admitted AI response generation through the governed command spine.
/// </summary>
public sealed record CancelAiResponseGeneration(
    string ProjectId,
    string ConversationId,
    string ResponseId,
    string GenerationId,
    long ExpectedSourceVersion,
    string CorrelationId,
    string CancellationId,
    string RedactionState = "metadata_only",
    string SchemaVersion = "chatbot.ai-response-cancel.v1") : IChatBotCommand;
