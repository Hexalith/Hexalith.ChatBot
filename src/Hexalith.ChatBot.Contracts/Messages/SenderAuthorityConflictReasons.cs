namespace Hexalith.ChatBot.Contracts.Messages;

public static class SenderAuthorityConflictReasons
{
    public const string DelegationMismatch = "delegation-mismatch";
    public const string MembershipRevoked = "membership-revoked";
    public const string ApprovalMissing = "approval-missing";

    public static IReadOnlyList<string> All { get; } =
    [
        ChatBotDisabledActionReasons.PolicyBlocked,
        DelegationMismatch,
        MembershipRevoked,
        ApprovalMissing,
    ];
}
