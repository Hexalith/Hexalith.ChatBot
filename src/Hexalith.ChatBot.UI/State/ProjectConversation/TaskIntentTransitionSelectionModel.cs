namespace Hexalith.ChatBot.UI.State.ProjectConversation;

public sealed record TaskIntentTransitionSelectionModel(
    string Transition,
    string? PredecessorTaskIntentId);
