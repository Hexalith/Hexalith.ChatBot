using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Contracts.Identities;
using Hexalith.EventStore.Contracts.Events;

namespace Hexalith.ChatBot.Server.Operations;

/// <summary>Persisted exact grant, separate from the authenticated caller's token claims.</summary>
/// <param name="GrantId">The immutable issuance id.</param>
/// <param name="TenantId">The exact owning tenant.</param>
/// <param name="PrincipalId">The exact grantee.</param>
/// <param name="ResourceId">The exact resource or create parent.</param>
/// <param name="Operation">The exact operation.</param>
/// <param name="Authority">The exact authority.</param>
/// <param name="ActorClass">The exact actor class.</param>
/// <param name="Origin">The exact origin.</param>
/// <param name="ExpiresAt">The grant expiry.</param>
/// <param name="ServiceGrant">The bounded machine grant, if any.</param>
public sealed record ChatBotAuthorityGranted(string GrantId, string TenantId, string PrincipalId, string ResourceId, string Operation, string Authority, string ActorClass, ChatBotSurfaceOrigin Origin, DateTimeOffset ExpiresAt, ServiceClientGrant? ServiceGrant) : IEventPayload;
