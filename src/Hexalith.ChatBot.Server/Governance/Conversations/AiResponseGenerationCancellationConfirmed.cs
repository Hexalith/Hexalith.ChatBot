using Hexalith.EventStore.Contracts.Events;

namespace Hexalith.ChatBot.Server.Governance.Conversations;

/// <summary>Terminal confirmation emitted only after the execution coordinator has stopped the exact generation.</summary>
public sealed record AiResponseGenerationCancellationConfirmed(
    string TenantId,
    string ProjectId,
    string ConversationId,
    string ResponseId,
    string GenerationId,
    string CancellationId,
    string CorrelationId,
    DateTimeOffset ConfirmedAtUtc,
    string RedactionState,
    string SchemaVersion,
    string SafeNextAction) : IEventPayload;
