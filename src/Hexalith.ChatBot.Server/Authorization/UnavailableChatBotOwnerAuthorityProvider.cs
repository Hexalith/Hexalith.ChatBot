namespace Hexalith.ChatBot.Server.Authorization;

/// <summary>Fails closed until accepted owner mappings exist; operator directory credentials prove no requester grant.</summary>
internal sealed class UnavailableChatBotOwnerAuthorityProvider : IChatBotOwnerAuthorityProvider
{
    /// <inheritdoc/>
    public ValueTask<ChatBotOwnerAuthorityEvidence?> GetAuthorityAsync(ChatBotOwnerAuthorityRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult<ChatBotOwnerAuthorityEvidence?>(null);
    }
}
