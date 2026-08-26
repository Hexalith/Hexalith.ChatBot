using Hexalith.EventStore.Contracts.Events;

namespace Hexalith.ChatBot.Server.Governance.Conversations;

/// <summary>Terminal, metadata-only outcome when the executor cannot confirm that the exact generation stopped.</summary>
public sealed record AiResponseGenerationCancellationFailed(
    string TenantId,
    string ProjectId,
    string ConversationId,
    string ResponseId,
    string GenerationId,
    string CancellationId,
    string CorrelationId,
    DateTimeOffset FailedAtUtc,
    string FailureReasonCode,
    string RedactionState,
    string SchemaVersion,
    string SafeNextAction) : IEventPayload;
