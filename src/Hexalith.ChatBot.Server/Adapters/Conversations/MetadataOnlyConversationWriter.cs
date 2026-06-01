namespace Hexalith.ChatBot.Server.Adapters.Conversations;

internal sealed class MetadataOnlyConversationWriter : IConversationWriter
{
    public ValueTask<ConversationAppendResult> PrepareAppendConversationMessageAsync(
        ApprovedAiConversationAppendRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        return ValueTask.FromResult(new ConversationAppendResult(
            "success",
            "available",
            "metadata_only",
            "none"));
    }
}
