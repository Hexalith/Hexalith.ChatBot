using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Server.Adapters.Mailbox;
using Hexalith.ChatBot.Server.Projections;

namespace Hexalith.ChatBot.Server.Lifecycle.Attachments;

internal interface IAttachmentUnsafeHandlingResolver
{
    ValueTask<string> ResolveUnsafeHandlingAsync(
        AttachmentUnsafeHandlingResolutionRequest request,
        CancellationToken cancellationToken = default);
}
