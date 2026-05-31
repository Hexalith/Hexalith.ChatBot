using System.Globalization;

using Hexalith.ChatBot.UI.Localization;

namespace Hexalith.ChatBot.UI.Design;

/// <summary>
/// Stable governed UI labels and compact icon text for shared primitives.
/// </summary>
public static class ChatBotGovernedUiText
{
    public static string GetActorCategoryLabel(ChatBotActorCategory category)
        => GetString(GetActorCategoryResourceKey(category));

    public static string GetActorCategoryResourceKey(ChatBotActorCategory category)
        => category switch
        {
            ChatBotActorCategory.HumanUser => ChatBotUiTextKey.ActorCategoryHumanUser,
            ChatBotActorCategory.ExternalParty => ChatBotUiTextKey.ActorCategoryExternalParty,
            ChatBotActorCategory.ServiceClient => ChatBotUiTextKey.ActorCategoryServiceClient,
            ChatBotActorCategory.AiActor => ChatBotUiTextKey.ActorCategoryAiActor,
            ChatBotActorCategory.BackgroundWorker => ChatBotUiTextKey.ActorCategoryBackgroundWorker,
            ChatBotActorCategory.Cli => ChatBotUiTextKey.ActorCategoryCli,
            ChatBotActorCategory.Mcp => ChatBotUiTextKey.ActorCategoryMcp,
            ChatBotActorCategory.MailboxEvent => ChatBotUiTextKey.ActorCategoryMailboxEvent,
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
        => GetString(GetEvidenceStateResourceKey(state));

    public static string GetEvidenceStateResourceKey(ChatBotEvidenceState state)
        => state switch
        {
            ChatBotEvidenceState.Available => ChatBotUiTextKey.EvidenceStateAvailable,
            ChatBotEvidenceState.Unavailable => ChatBotUiTextKey.EvidenceStateUnavailable,
            ChatBotEvidenceState.Redacted => ChatBotUiTextKey.EvidenceStateRedacted,
            ChatBotEvidenceState.Unauthorized => ChatBotUiTextKey.EvidenceStateUnauthorized,
            _ => throw new ArgumentOutOfRangeException(nameof(state), state, null),
        };

    public static string GetRiskActionClassLabel(ChatBotRiskActionClass riskClass)
        => GetString(GetRiskActionClassResourceKey(riskClass));

    public static string GetRiskActionClassResourceKey(ChatBotRiskActionClass riskClass)
        => riskClass switch
        {
            ChatBotRiskActionClass.ExternallyVisible => ChatBotUiTextKey.RiskClassExternallyVisible,
            ChatBotRiskActionClass.FileExposing => ChatBotUiTextKey.RiskClassFileExposing,
            ChatBotRiskActionClass.ProjectMutating => ChatBotUiTextKey.RiskClassProjectMutating,
            ChatBotRiskActionClass.ToolInvoking => ChatBotUiTextKey.RiskClassToolInvoking,
            ChatBotRiskActionClass.TaskCreating => ChatBotUiTextKey.RiskClassTaskCreating,
            ChatBotRiskActionClass.ParticipantRepresenting => ChatBotUiTextKey.RiskClassParticipantRepresenting,
            _ => throw new ArgumentOutOfRangeException(nameof(riskClass), riskClass, null),
        };

    public static string GetFeedbackKindLabel(ChatBotFeedbackKind kind)
        => GetString(GetFeedbackKindResourceKey(kind));

    public static string GetFeedbackKindResourceKey(ChatBotFeedbackKind kind)
        => kind switch
        {
            ChatBotFeedbackKind.Info => ChatBotUiTextKey.FeedbackKindInfo,
            ChatBotFeedbackKind.Warning => ChatBotUiTextKey.FeedbackKindWarning,
            ChatBotFeedbackKind.Danger => ChatBotUiTextKey.FeedbackKindDanger,
            ChatBotFeedbackKind.Success => ChatBotUiTextKey.FeedbackKindSuccess,
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
        => GetString(GetBlockedReasonResourceKey(reason));

    public static string GetBlockedReasonResourceKey(ChatBotBlockedReason reason)
        => reason switch
        {
            ChatBotBlockedReason.Denial => ChatBotUiTextKey.BlockedReasonDenial,
            ChatBotBlockedReason.UnresolvedAssociation => ChatBotUiTextKey.BlockedReasonUnresolvedAssociation,
            ChatBotBlockedReason.Quarantine => ChatBotUiTextKey.BlockedReasonQuarantine,
            ChatBotBlockedReason.FailedDependency => ChatBotUiTextKey.BlockedReasonFailedDependency,
            ChatBotBlockedReason.UnsafeContext => ChatBotUiTextKey.BlockedReasonUnsafeContext,
            _ => throw new ArgumentOutOfRangeException(nameof(reason), reason, null),
        };

    public static string GetInteractionGuardrailLabel(ChatBotInteractionGuardrail guardrail)
        => GetString(GetInteractionGuardrailResourceKey(guardrail));

    public static string GetInteractionGuardrailResourceKey(ChatBotInteractionGuardrail guardrail)
        => guardrail switch
        {
            ChatBotInteractionGuardrail.NoHiddenAutoAssociationWhenAmbiguous => ChatBotUiTextKey.GuardrailNoHiddenAutoAssociationWhenAmbiguous,
            ChatBotInteractionGuardrail.NoRiskyAiExecutionFromPlainSend => ChatBotUiTextKey.GuardrailNoRiskyAiExecutionFromPlainSend,
            ChatBotInteractionGuardrail.NoHoverOnlyCriticalActions => ChatBotUiTextKey.GuardrailNoHoverOnlyCriticalActions,
            ChatBotInteractionGuardrail.NoStackedActiveDialogsOrSheets => ChatBotUiTextKey.GuardrailNoStackedActiveDialogsOrSheets,
            ChatBotInteractionGuardrail.NoInfiniteScrollQueues => ChatBotUiTextKey.GuardrailNoInfiniteScrollQueues,
            ChatBotInteractionGuardrail.NoCliMcpAdminAuthorizationBypassAffordance => ChatBotUiTextKey.GuardrailNoCliMcpAdminAuthorizationBypassAffordance,
            _ => throw new ArgumentOutOfRangeException(nameof(guardrail), guardrail, null),
        };

    private static string GetString(string key)
        => SharedResource.ResourceManager.GetString(key, CultureInfo.CurrentUICulture)
            ?? throw new InvalidOperationException($"Missing ChatBot UI localization resource '{key}'.");
}
