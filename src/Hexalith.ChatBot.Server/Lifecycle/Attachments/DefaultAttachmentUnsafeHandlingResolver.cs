using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Server.Adapters.Mailbox;
using Hexalith.ChatBot.Server.Projections;

namespace Hexalith.ChatBot.Server.Lifecycle.Attachments;

internal sealed class DefaultAttachmentUnsafeHandlingResolver : IAttachmentUnsafeHandlingResolver
{
    public ValueTask<string> ResolveUnsafeHandlingAsync(
        AttachmentUnsafeHandlingResolutionRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(AttachmentUnsafeHandling.Quarantine);
    }
}
