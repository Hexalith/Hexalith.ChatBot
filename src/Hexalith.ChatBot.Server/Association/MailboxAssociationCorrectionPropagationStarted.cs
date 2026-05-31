using Hexalith.EventStore.Contracts.Events;

namespace Hexalith.ChatBot.Server.Association;

public sealed record MailboxAssociationCorrectionPropagationStarted(
    string AssociationId,
    string IntakeId,
    string TenantId,
    string SourceMailboxId,
    string SourceConversationId,
    string? SourceThreadId,
    string CorrectionId,
    string WorkflowInstanceId,
    string PriorProjectId,
    string CorrectedProjectId,
    IReadOnlyList<string> RequiredStoreKeys,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset EstimatedCompletionAtUtc,
    string ResponsibleOwnerRole,
    string NextSafeAction,
    string RedactionState,
    string RetentionClass,
    long SourceVersion,
    string SchemaVersion,
    string CorrelationId) : IEventPayload;
