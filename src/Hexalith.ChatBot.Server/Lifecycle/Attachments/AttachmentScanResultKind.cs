using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Server.Adapters.Mailbox;
using Hexalith.ChatBot.Server.Projections;

namespace Hexalith.ChatBot.Server.Lifecycle.Attachments;

internal enum AttachmentScanResultKind
{
    Clean,
    Unsafe,
    Unavailable,
    Retryable,
    Failed,
    Indeterminate,
}
