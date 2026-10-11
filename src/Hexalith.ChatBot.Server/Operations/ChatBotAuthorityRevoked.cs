using Hexalith.EventStore.Contracts.Events;

namespace Hexalith.ChatBot.Server.Operations;

/// <summary>Persisted revocation of one exact issuance.</summary>
/// <param name="GrantId">The issuance revoked.</param>
public sealed record ChatBotAuthorityRevoked(string GrantId) : IEventPayload;
