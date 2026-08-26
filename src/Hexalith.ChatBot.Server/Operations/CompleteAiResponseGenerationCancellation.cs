namespace Hexalith.ChatBot.Server.Operations;

/// <summary>Internal executor acknowledgement for a persisted AI-response cancellation request.</summary>
public sealed record CompleteAiResponseGenerationCancellation(
    string ProjectId,
    string ConversationId,
    string ResponseId,
    string GenerationId,
    string CancellationId,
    string CorrelationId,
    bool Confirmed,
    string? FailureReasonCode,
    string CompletionId,
    string SchemaVersion = "chatbot.ai-response-cancellation-completion.v1");
