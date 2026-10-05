using Hexalith.ChatBot.Server.Adapters.Conversations;
using Hexalith.ChatBot.Server.Adapters.Mailbox;

namespace Hexalith.ChatBot.Server.Tests.Gateway;

/// <summary>Models an external writer that may have committed before losing its acknowledgement.</summary>
internal sealed class UncertainExternalWriters : IConversationWriter, IOutboundMailboxSender
{
    public int Attempts { get; private set; }

    public ValueTask<ConversationAppendResult> PrepareAppendConversationMessageAsync(
        ApprovedAiConversationAppendRequest request, CancellationToken cancellationToken)
    {
        Attempts++;
        throw new IOException("Injected uncertain conversation write.");
    }

    public ValueTask<OutboundMailboxSendResult> SendAsync(OutboundMailboxSendRequest request,
        CancellationToken cancellationToken = default)
    {
        Attempts++;
        throw new IOException("Injected uncertain mailbox write.");
    }
}
