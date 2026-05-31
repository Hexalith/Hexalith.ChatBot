namespace Hexalith.ChatBot.Server.Lifecycle.Workflows;

internal sealed record CorrectionPropagationActivityRequest(
    string TenantId,
    string AssociationId,
    string CorrectionId,
    string WorkflowInstanceId,
    string StoreKey,
    long SourceVersion,
    string PriorProjectId,
    string CorrectedProjectId,
    DateTimeOffset StartedAtUtc,
    string CorrelationId);
