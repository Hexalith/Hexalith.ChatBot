namespace Hexalith.ChatBot.UI.Design;

/// <summary>
/// Stable governed UI labels and compact icon text for shared primitives.
/// </summary>
public static class ChatBotGovernedUiText
{
    public static string GetActorCategoryLabel(ChatBotActorCategory category)
        => category switch
        {
            ChatBotActorCategory.HumanUser => "Human user",
            ChatBotActorCategory.ExternalParty => "External party",
            ChatBotActorCategory.ServiceClient => "Service client",
            ChatBotActorCategory.AiActor => "AI actor",
            ChatBotActorCategory.BackgroundWorker => "Background worker",
            ChatBotActorCategory.Cli => "CLI",
            ChatBotActorCategory.Mcp => "MCP",
            ChatBotActorCategory.MailboxEvent => "Mailbox event",
            _ => throw new ArgumentOutOfRangeException(nameof(category), category, null),
        };

    public static string GetActorCategoryIconText(ChatBotActorCategory category)
        => category switch
        {
            ChatBotActorCategory.HumanUser => "HU",
            ChatBotActorCategory.ExternalParty => "EP",
            ChatBotActorCategory.ServiceClient => "SC",
            ChatBotActorCategory.AiActor => "AI",
            ChatBotActorCategory.BackgroundWorker => "BW",
            ChatBotActorCategory.Cli => "CL",
            ChatBotActorCategory.Mcp => "MP",
            ChatBotActorCategory.MailboxEvent => "ME",
            _ => throw new ArgumentOutOfRangeException(nameof(category), category, null),
        };

    public static string GetEvidenceStateLabel(ChatBotEvidenceState state)
        => state switch
        {
            ChatBotEvidenceState.Available => "Available evidence",
            ChatBotEvidenceState.Unavailable => "Evidence unavailable",
            ChatBotEvidenceState.Redacted => "Evidence redacted",
            ChatBotEvidenceState.Unauthorized => "Evidence restricted",
            _ => throw new ArgumentOutOfRangeException(nameof(state), state, null),
        };

    public static string GetRiskActionClassLabel(ChatBotRiskActionClass riskClass)
        => riskClass switch
        {
            ChatBotRiskActionClass.ExternallyVisible => "Externally visible",
            ChatBotRiskActionClass.FileExposing => "File-exposing",
            ChatBotRiskActionClass.ProjectMutating => "Project-mutating",
            ChatBotRiskActionClass.ToolInvoking => "Tool-invoking",
            ChatBotRiskActionClass.TaskCreating => "Task-creating",
            ChatBotRiskActionClass.ParticipantRepresenting => "Participant-representing",
            _ => throw new ArgumentOutOfRangeException(nameof(riskClass), riskClass, null),
        };

    public static string GetFeedbackKindLabel(ChatBotFeedbackKind kind)
        => kind switch
        {
            ChatBotFeedbackKind.Info => "Info",
            ChatBotFeedbackKind.Warning => "Warning",
            ChatBotFeedbackKind.Danger => "Danger",
            ChatBotFeedbackKind.Success => "Success",
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
        };

    public static string GetFeedbackKindSlot(ChatBotFeedbackKind kind)
        => kind switch
        {
            ChatBotFeedbackKind.Info => "info",
            ChatBotFeedbackKind.Warning => "warning",
            ChatBotFeedbackKind.Danger => "danger",
            ChatBotFeedbackKind.Success => "success",
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
        };

    public static string GetBlockedReasonLabel(ChatBotBlockedReason reason)
        => reason switch
        {
            ChatBotBlockedReason.Denial => "Denied",
            ChatBotBlockedReason.UnresolvedAssociation => "Unresolved association",
            ChatBotBlockedReason.Quarantine => "Quarantined",
            ChatBotBlockedReason.FailedDependency => "Dependency failed",
            ChatBotBlockedReason.UnsafeContext => "Unsafe context",
            _ => throw new ArgumentOutOfRangeException(nameof(reason), reason, null),
        };

    public static string GetInteractionGuardrailLabel(ChatBotInteractionGuardrail guardrail)
        => guardrail switch
        {
            ChatBotInteractionGuardrail.NoHiddenAutoAssociationWhenAmbiguous => "No hidden auto-association when ambiguous",
            ChatBotInteractionGuardrail.NoRiskyAiExecutionFromPlainSend => "No risky AI execution from plain send",
            ChatBotInteractionGuardrail.NoHoverOnlyCriticalActions => "No hover-only critical actions",
            ChatBotInteractionGuardrail.NoStackedActiveDialogsOrSheets => "No stacked active dialogs or sheets",
            ChatBotInteractionGuardrail.NoInfiniteScrollQueues => "No infinite-scroll queues",
            ChatBotInteractionGuardrail.NoCliMcpAdminAuthorizationBypassAffordance => "No CLI/MCP/admin authorization bypass affordance",
            _ => throw new ArgumentOutOfRangeException(nameof(guardrail), guardrail, null),
        };
}
