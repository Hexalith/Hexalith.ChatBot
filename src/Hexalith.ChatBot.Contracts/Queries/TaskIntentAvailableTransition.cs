namespace Hexalith.ChatBot.Contracts.Queries;

public sealed record TaskIntentAvailableTransition(
    string Transition,
    string Label,
    bool Enabled,
    string? DisabledReasonCode = null,
    bool RequiresPredecessorTaskIntentId = false);
