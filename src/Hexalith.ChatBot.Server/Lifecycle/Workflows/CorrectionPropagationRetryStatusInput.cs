namespace Hexalith.ChatBot.Server.Lifecycle.Workflows;

internal sealed record CorrectionPropagationRetryStatusInput(
    CorrectionPropagationRequest Request,
    int RetryCount,
    string FailureCode,
    string WorkflowStatus = CorrectionPropagationWorkflowStatuses.Retrying,
    DateTimeOffset? RetryDueAt = null);
