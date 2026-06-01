namespace Hexalith.ChatBot.Server.Gateway.Stages;

internal sealed record ChatBotApprovalResult(
    ChatBotApprovalResultKind Kind,
    string ReasonCode,
    string SafeNextAction,
    string? PolicySnapshotId = null)
{
    public static ChatBotApprovalResult Approved { get; } = new(
        ChatBotApprovalResultKind.Approved,
        "pass_through",
        "none");

    public static ChatBotApprovalResult AllowedLowRiskExecution(string policySnapshotId, string reasonCode)
        => new(ChatBotApprovalResultKind.AllowedLowRiskExecution, reasonCode, "none", policySnapshotId);

    public static ChatBotApprovalResult RoutedToApproval(string policySnapshotId, string reasonCode)
        => new(ChatBotApprovalResultKind.RoutedToApproval, reasonCode, "review-ai-action", policySnapshotId);

    public static ChatBotApprovalResult ApprovalDecisionAllowed(string reasonCode)
        => new(ChatBotApprovalResultKind.ApprovalDecisionAllowed, reasonCode, "none");

    public static ChatBotApprovalResult Blocked(string reasonCode)
        => new(ChatBotApprovalResultKind.Blocked, reasonCode, "none");
}

internal enum ChatBotApprovalResultKind
{
    Approved,
    AllowedLowRiskExecution,
    RoutedToApproval,
    ApprovalDecisionAllowed,
    Blocked,
}
