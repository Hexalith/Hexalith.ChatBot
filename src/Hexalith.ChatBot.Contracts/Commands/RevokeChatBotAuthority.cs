namespace Hexalith.ChatBot.Contracts.Commands;

/// <summary>Revokes one current grant on its exact EventStore stream.</summary>
/// <param name="AuthorityKey">The deterministic grant stream id.</param>
/// <param name="GrantId">The issuance to revoke, preventing revocation of a later replacement.</param>
public sealed record RevokeChatBotAuthority(string AuthorityKey, string GrantId) : IChatBotCommand;
