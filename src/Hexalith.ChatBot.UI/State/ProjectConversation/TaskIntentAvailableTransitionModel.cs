namespace Hexalith.ChatBot.UI.State.ProjectConversation;

public sealed record TaskIntentAvailableTransitionModel(
    string Transition,
    string Label,
    bool Enabled,
    string? DisabledReasonCode,
    bool RequiresPredecessorTaskIntentId);
