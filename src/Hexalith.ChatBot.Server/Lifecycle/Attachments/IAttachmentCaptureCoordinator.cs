using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Server.Adapters.Folders;
using Hexalith.ChatBot.Server.Adapters.Mailbox;
using Hexalith.ChatBot.Server.Projections;

namespace Hexalith.ChatBot.Server.Lifecycle.Attachments;

internal interface IAttachmentCaptureCoordinator
{
    Task<AttachmentCaptureCoordinatorResult> CaptureAsync(
        AttachmentCaptureCoordinatorRequest request,
        CancellationToken cancellationToken = default);
}
