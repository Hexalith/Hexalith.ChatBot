namespace Hexalith.ChatBot.UI.Design;

/// <summary>
/// UX-DR33 banned interaction guardrails enforced by the ChatBot UI foundation.
/// </summary>
public enum ChatBotInteractionGuardrail
{
    NoHiddenAutoAssociationWhenAmbiguous,
    NoRiskyAiExecutionFromPlainSend,
    NoHoverOnlyCriticalActions,
    NoStackedActiveDialogsOrSheets,
    NoInfiniteScrollQueues,
    NoCliMcpAdminAuthorizationBypassAffordance,
}
