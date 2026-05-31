namespace Hexalith.ChatBot.Server.Association;

public sealed record AcknowledgeMailboxAssociationCorrectionStoreInvalidated(
    string AssociationId,
    string CorrectionId,
    string WorkflowInstanceId,
    string StoreKey,
    long SourceVersion,
    string PriorProjectId,
    string CorrectedProjectId,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset CompletedAtUtc,
    string Outcome,
    string? FailureReasonCode,
    string RedactionState,
    string RetentionClass,
    string SchemaVersion);
