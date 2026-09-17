using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Server.Projections;

namespace Hexalith.ChatBot.Server.Lifecycle.Attachments;

internal interface IAttachmentAuthorizationService
{
    AttachmentAuthorizationResult Authorize(ProjectConversationAttachmentStorageCandidate candidate);
}
