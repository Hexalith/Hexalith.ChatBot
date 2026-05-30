namespace Hexalith.ChatBot.Server.Audit;

internal static class ChatBotStateWritingPathInventory
{
    public const string RequiredAuditCommitSeam = "IAuditWriter.RecordPreCommitAsync";

    public static IReadOnlyList<ChatBotStateWritingPath> Paths { get; } =
    [
        Path("m365-mailbox-intake", "M365 mailbox intake"),
        Path("deterministic-association", "Deterministic association"),
        Path("ambiguous-user-association", "Ambiguous/user association"),
        Path("correction", "Correction"),
        Path("ai-action-proposal", "AI action proposal"),
        Path("approval-decision", "Approval decision"),
        Path("command-execution", "Command execution"),
        Path("outbound-send", "Outbound send"),
        Path("tenant-policy-mutation", "Tenant policy mutation"),
        Path("allowlist-mutation", "Allowlist mutation"),
    ];

    private static ChatBotStateWritingPath Path(string code, string displayName)
        => new(code, displayName, RequiredAuditCommitSeam);
}
