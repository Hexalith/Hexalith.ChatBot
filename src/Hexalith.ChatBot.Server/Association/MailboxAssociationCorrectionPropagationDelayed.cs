using Hexalith.EventStore.Contracts.Events;

namespace Hexalith.ChatBot.Server.Association;

public sealed record MailboxAssociationCorrectionPropagationDelayed(
    string AssociationId,
    string IntakeId,
    string TenantId,
    string SourceMailboxId,
    string SourceConversationId,
    string? SourceThreadId,
    string CorrectionId,
    string WorkflowInstanceId,
    long SourceVersion,
    string PriorProjectId,
    string CorrectedProjectId,
    DateTimeOffset DelayedAtUtc,
    string ResponsibleOwnerRole,
    string NextSafeAction,
    string ReasonCode,
    string RedactionState,
    string RetentionClass,
    string SchemaVersion,
    string CorrelationId) : IEventPayload;
