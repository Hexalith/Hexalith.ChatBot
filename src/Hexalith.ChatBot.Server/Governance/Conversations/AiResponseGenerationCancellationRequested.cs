using Hexalith.EventStore.Contracts.Events;

namespace Hexalith.ChatBot.Server.Governance.Conversations;

public sealed record AiResponseGenerationCancellationRequested(
    string TenantId,
    string ProjectId,
    string ConversationId,
    string ResponseId,
    string GenerationId,
    string ActorId,
    long ExpectedSourceVersion,
    string CorrelationId,
    string CancellationId,
    DateTimeOffset RequestedAtUtc,
    string RedactionState,
    string SchemaVersion,
    long SourceVersion,
    string SafeNextAction) : IEventPayload;
