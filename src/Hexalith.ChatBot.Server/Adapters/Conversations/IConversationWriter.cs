namespace Hexalith.ChatBot.Server.Adapters.Conversations;

internal interface IConversationWriter
{
    ValueTask<ConversationAppendResult> PrepareAppendConversationMessageAsync(
        ApprovedAiConversationAppendRequest request,
        CancellationToken cancellationToken);
}
