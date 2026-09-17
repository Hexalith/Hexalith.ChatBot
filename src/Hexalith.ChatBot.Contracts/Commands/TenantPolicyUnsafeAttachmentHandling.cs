using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Commands;

public static class TenantPolicyUnsafeAttachmentHandling
{
    public const string Quarantine = "quarantine";
    public const string Block = "block";
    public const string RejectMessage = "reject-message";

    public static IReadOnlyList<string> All { get; } = [Quarantine, Block, RejectMessage];
}
