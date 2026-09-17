using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Server.Adapters.Mailbox;

namespace Hexalith.ChatBot.Server.Adapters.Folders;

internal sealed record StoredMailboxAttachmentReference(
    string FolderId,
    string FileId,
    string DuplicateState,
    string RetryState,
    string AiContextEligibility,
    IReadOnlyList<string> AllowedActions,
    string StorageOperationId,
    string IdempotencyKey);
