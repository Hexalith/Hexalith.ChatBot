using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Server.Governance.AiMediation;

internal static class AiActionCommandMetadataProvider
{
    public const string AppendConversationMessageCommandName = "Project.AppendConversationMessage";
    public const string ExecuteLowRiskAssistanceCommandName = "ChatBot.ExecuteLowRiskAssistance";
    public const string M0AllowlistVersion = "ai-action-command-allowlist.m0";
    public const string V1AllowlistVersion = "ai-action-command-allowlist.v1";

    public static AiActionCommandMetadata? TryGet(string commandName)
        => string.Equals(commandName, AppendConversationMessageCommandName, StringComparison.Ordinal)
            ? new AiActionCommandMetadata(
                AppendConversationMessageCommandName,
                [AiActionRiskActionClass.ModifiesState],
                "project-conversation",
                "approval-required",
                M0AllowlistVersion,
                AiActionRiskClass.ApprovalRequired,
                true,
                AiActionAuthorityClass.DelegatedProjectContributor,
                AiActionIdempotencyContract.CommandExecution)
            : string.Equals(commandName, ExecuteLowRiskAssistanceCommandName, StringComparison.Ordinal)
                ? new AiActionCommandMetadata(
                    ExecuteLowRiskAssistanceCommandName,
                    [],
                    "read-only",
                    "low-risk",
                    M0AllowlistVersion,
                    AiActionRiskClass.LowRisk,
                    true,
                    AiActionAuthorityClass.ReadOnlyAssistant,
                    AiActionIdempotencyContract.CommandExecution)
                : null;
}
