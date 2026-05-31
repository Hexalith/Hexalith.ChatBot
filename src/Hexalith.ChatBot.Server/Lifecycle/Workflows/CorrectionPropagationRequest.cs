namespace Hexalith.ChatBot.Server.Lifecycle.Workflows;

internal sealed record CorrectionPropagationRequest(
    string TenantId,
    string ActorId,
    string AssociationId,
    string IntakeId,
    string CorrectionId,
    string WorkflowInstanceId,
    string PriorProjectId,
    string CorrectedProjectId,
    long SourceVersion,
    string CorrelationId,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset EstimatedCompletionAtUtc);
