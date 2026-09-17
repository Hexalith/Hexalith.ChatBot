using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Server.Adapters.Mailbox;

namespace Hexalith.ChatBot.Server.Adapters.Folders;

internal sealed record AttachmentStorageFailure(
    ProjectConversationAttachmentStatus Status,
    string DuplicateState,
    string RetryState,
    string AiContextEligibility,
    string ReasonCode);
