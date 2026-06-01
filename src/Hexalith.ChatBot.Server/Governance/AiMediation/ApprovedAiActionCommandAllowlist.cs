namespace Hexalith.ChatBot.Server.Governance.AiMediation;

internal sealed class ApprovedAiActionCommandAllowlist : IApprovedAiActionCommandAllowlist
{
    private static readonly HashSet<string> M0Commands =
        new(StringComparer.Ordinal)
        {
            AiActionCommandMetadataProvider.AppendConversationMessageCommandName,
        };

    public string CurrentVersion => AiActionCommandMetadataProvider.M0AllowlistVersion;

    public bool IsAllowed(string? commandName, string? allowlistVersion)
        => string.Equals(allowlistVersion, CurrentVersion, StringComparison.Ordinal) &&
            !string.IsNullOrWhiteSpace(commandName) &&
            M0Commands.Contains(commandName);
}
