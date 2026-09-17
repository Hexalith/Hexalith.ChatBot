using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Server.Adapters.Mailbox;
using Hexalith.ChatBot.Server.Projections;

namespace Hexalith.ChatBot.Server.Lifecycle.Attachments;

internal static class AttachmentUnsafeHandling
{
    public const string Quarantine = "quarantine";
    public const string Block = "block";
    public const string RejectMessage = "reject-message";

    public static string Normalize(string? value)
        => value is Quarantine or Block or RejectMessage ? value : Quarantine;
}
