namespace Hexalith.ChatBot.Server.Governance.AiMediation;

internal interface IApprovedAiActionCommandAllowlist
{
    string CurrentVersion { get; }

    bool IsAllowed(string? commandName, string? allowlistVersion);
}
