using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Server.Projections;

namespace Hexalith.ChatBot.Server.Lifecycle.Attachments;

internal enum AttachmentAuthorizationState
{
    Authorized,
    Redacted,
    Unavailable,
    Retryable,
}
