namespace Hexalith.ChatBot.Server.Lifecycle.Workflows;

/// <summary>Durable input for binding one accepted association to canonical Memories ingestion results.</summary>
internal sealed record IngestionBindingRequest(
    string TenantId,
    string AssociationId,
    string IntakeId,
    string AssociatedProjectId,
    long SourceVersion,
    string CorrelationId,
    string WorkflowInstanceId);
