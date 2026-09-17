namespace Hexalith.ChatBot.UI.State.ProjectConversation;

public sealed record ProjectConversationAiResponseNudgeModel(
    string ProjectId,
    string ConversationId,
    string ResponseId,
    string GenerationId,
    string CorrelationId,
    long SourceVersion,
    long Sequence,
    string State,
    string RedactionState,
    string VisibilityState);
