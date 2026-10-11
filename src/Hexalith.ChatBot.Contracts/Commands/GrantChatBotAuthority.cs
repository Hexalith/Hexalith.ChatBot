using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Contracts.Identities;

namespace Hexalith.ChatBot.Contracts.Commands;

/// <summary>Issues one exact, expiring ChatBot owner grant through the governed EventStore path.</summary>
/// <param name="AuthorityKey">The deterministic grant stream id.</param>
/// <param name="GrantId">An opaque issuance id.</param>
/// <param name="PrincipalId">The exact grantee principal.</param>
/// <param name="ResourceId">The exact resource or tenant parent for a create grant.</param>
/// <param name="Operation">The exact ChatBot operation.</param>
/// <param name="Authority">The exact authority class.</param>
/// <param name="ActorClass">The exact actor class.</param>
/// <param name="Origin">The exact declared origin.</param>
/// <param name="ExpiresAt">The grant expiry in UTC.</param>
/// <param name="ServiceGrant">The machine grant when authority is service-grant.</param>
public sealed record GrantChatBotAuthority(string AuthorityKey, string GrantId, string PrincipalId, string ResourceId, string Operation, string Authority, string ActorClass, ChatBotSurfaceOrigin Origin, DateTimeOffset ExpiresAt, ServiceClientGrant? ServiceGrant = null) : IChatBotCommand;
