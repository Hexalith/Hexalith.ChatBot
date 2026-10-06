using Hexalith.ChatBot.Server.Adapters.Conversations;
using Hexalith.ChatBot.Server.Adapters.Mailbox;

namespace Hexalith.ChatBot.Server.Tests.Gateway;

/// <summary>Models an external writer that may have committed before losing its acknowledgement.</summary>
internal sealed class UncertainExternalWriters : IConversationWriter, IOutboundMailboxSender
{
    public int Attempts { get; private set; }

    /// <summary>Gets a value indicating whether writes are acknowledged as committed instead of losing their acknowledgement.</summary>
    public bool AcknowledgeWrites { get; init; }

    public ValueTask<ConversationAppendResult> PrepareAppendConversationMessageAsync(
        ApprovedAiConversationAppendRequest request, CancellationToken cancellationToken)
    {
        Attempts++;
        return AcknowledgeWrites
            ? ValueTask.FromResult(new ConversationAppendResult("appended", "committed", "metadata_only", "none"))
            : throw new IOException("Injected uncertain conversation write.");
    }

    public ValueTask<OutboundMailboxSendResult> SendAsync(OutboundMailboxSendRequest request,
        CancellationToken cancellationToken = default)
    {
        Attempts++;
        return AcknowledgeWrites
            ? ValueTask.FromResult(OutboundMailboxSendResult.Sent("adapter:mailbox-outbound:sent"))
            : throw new IOException("Injected uncertain mailbox write.");
    }
}
