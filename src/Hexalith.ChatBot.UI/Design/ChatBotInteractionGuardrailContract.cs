namespace Hexalith.ChatBot.UI.Design;

/// <summary>
/// Exact list of banned interaction guardrails future governed surfaces must preserve.
/// </summary>
public static class ChatBotInteractionGuardrailContract
{
    /// <summary>Gets the UX-DR33 banned interactions in stable review order.</summary>
    public static IReadOnlyList<ChatBotInteractionGuardrail> BannedInteractions { get; } =
    [
        ChatBotInteractionGuardrail.NoHiddenAutoAssociationWhenAmbiguous,
        ChatBotInteractionGuardrail.NoRiskyAiExecutionFromPlainSend,
        ChatBotInteractionGuardrail.NoHoverOnlyCriticalActions,
        ChatBotInteractionGuardrail.NoStackedActiveDialogsOrSheets,
        ChatBotInteractionGuardrail.NoInfiniteScrollQueues,
        ChatBotInteractionGuardrail.NoCliMcpAdminAuthorizationBypassAffordance,
    ];
}
