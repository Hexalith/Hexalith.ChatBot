using Hexalith.EventStore.Contracts.Events;

namespace Hexalith.ChatBot.Server.Operations;

/// <summary>
/// Structured, metadata-only rejection for malformed workflow retry requests.
/// </summary>
/// <param name="RetryId">The requested retry ULID when available.</param>
/// <param name="ReasonCode">The finite validation reason code.</param>
public sealed record WorkflowRetryInvalidRejection(string? RetryId, string ReasonCode) : IRejectionEvent;
