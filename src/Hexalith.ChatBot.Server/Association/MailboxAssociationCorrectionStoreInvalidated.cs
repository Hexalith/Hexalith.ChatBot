using System.Text.Json.Serialization;

using Hexalith.EventStore.Contracts.Events;

namespace Hexalith.ChatBot.Server.Association;

public sealed record MailboxAssociationCorrectionStoreInvalidated(
    string AssociationId,
    string IntakeId,
    string TenantId,
    string SourceMailboxId,
    string SourceConversationId,
    string? SourceThreadId,
    string CorrectionId,
    string StoreKey,
    string WorkflowInstanceId,
    long SourceVersion,
    string PriorProjectId,
    string CorrectedProjectId,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset CompletedAtUtc,
    [property: JsonPropertyName("storeOutcome")] string Outcome,
    string? FailureReasonCode,
    string RedactionState,
    string RetentionClass,
    string SchemaVersion,
    string CorrelationId) : IEventPayload;
