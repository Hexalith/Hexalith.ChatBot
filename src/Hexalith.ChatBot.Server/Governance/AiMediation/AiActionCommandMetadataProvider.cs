using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Server.Governance.AiMediation;

internal static class AiActionCommandMetadataProvider
{
    public const string AppendConversationMessageCommandName = "Project.AppendConversationMessage";
    public const string ExecuteLowRiskAssistanceCommandName = "ChatBot.ExecuteLowRiskAssistance";
    public const string M0AllowlistVersion = "ai-action-command-allowlist.m0";

    public static AiActionCommandMetadata? TryGet(string commandName)
        => string.Equals(commandName, AppendConversationMessageCommandName, StringComparison.Ordinal)
            ? new AiActionCommandMetadata(
                AppendConversationMessageCommandName,
                [AiActionRiskActionClass.ModifiesState],
                "project-conversation",
                "approval-required",
                M0AllowlistVersion,
                AiActionRiskClass.ApprovalRequired,
                true)
            : string.Equals(commandName, ExecuteLowRiskAssistanceCommandName, StringComparison.Ordinal)
                ? new AiActionCommandMetadata(
                    ExecuteLowRiskAssistanceCommandName,
                    [],
                    "read-only",
                    "low-risk",
                    M0AllowlistVersion,
                    AiActionRiskClass.LowRisk,
                    true)
                : null;
}

internal sealed record AiActionCommandMetadata(
    string CommandName,
    IReadOnlyList<AiActionRiskActionClass> ActionClasses,
    string EffectSurface,
    string TenantPolicyClassification,
    string CommandAllowlistVersion,
    AiActionRiskClass CommandDefaultRisk,
    bool Supported);
