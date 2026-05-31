namespace Hexalith.ChatBot.Server.Gateway;

/// <summary>
/// Outcome of dispatching an admitted command into EventStore. <see cref="AcceptedAt"/> is the UTC instant
/// the dispatch was accepted; <see cref="ResourceId"/> carries the resulting aggregate/resource identity so
/// the post-commit audit and operation status can reference it. Metadata-only (identifiers, no payload).
/// </summary>
/// <param name="AcceptedAt">The UTC instant the EventStore dispatch was accepted.</param>
/// <param name="ResourceId">The resulting aggregate/resource ULID, or <see langword="null"/> when not resolved.</param>
internal sealed record ChatBotDispatchResult(DateTimeOffset AcceptedAt, string? ResourceId = null);
