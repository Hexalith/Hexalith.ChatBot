namespace Hexalith.ChatBot.Server.Lifecycle.Workflows;

internal sealed record CorrectionPropagationActivityRequest(
    string TenantId,
    string AssociationId,
    string IntakeId,
    string CorrectionId,
    string WorkflowInstanceId,
    string StoreKey,
    long SourceVersion,
    string PriorProjectId,
    string CorrectedProjectId,
    string CorrectedCaseId,
    DateTimeOffset StartedAtUtc,
    string CorrelationId,
    string? RemoteOperationId = null);
