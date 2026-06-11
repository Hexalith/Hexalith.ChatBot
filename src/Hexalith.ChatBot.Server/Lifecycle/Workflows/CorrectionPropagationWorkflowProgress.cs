namespace Hexalith.ChatBot.Server.Lifecycle.Workflows;

internal sealed record CorrectionPropagationWorkflowProgress(
    string Status,
    string WorkflowInstanceId,
    string TenantId,
    string CorrectionId,
    long SourceVersion,
    int RetryCount,
    string LastFailureCode,
    string CorrelationId);
