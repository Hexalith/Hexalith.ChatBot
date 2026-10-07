namespace Hexalith.ChatBot.Server.Authorization;

/// <summary>Retrieves current owner evidence through accepted owner mappings only.</summary>
internal interface IChatBotOwnerAuthorityProvider
{
    /// <summary>Returns exact metadata-only evidence, or null when mapping or evidence is unavailable.</summary>
    ValueTask<ChatBotOwnerAuthorityEvidence?> GetAuthorityAsync(ChatBotOwnerAuthorityRequest request, CancellationToken cancellationToken);
}
