using Hexalith.EventStore.Contracts.Events;

namespace Hexalith.ChatBot.Server.Operations;

/// <summary>Metadata-only rejection for an invalid grant lifecycle command.</summary>
/// <param name="AuthorityKey">The attempted authority stream.</param>
/// <param name="Reason">The safe rejection code.</param>
public sealed record ChatBotAuthorityChangeRejected(string AuthorityKey, string Reason) : IRejectionEvent;
