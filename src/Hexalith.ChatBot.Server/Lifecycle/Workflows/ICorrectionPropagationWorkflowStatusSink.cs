namespace Hexalith.ChatBot.Server.Lifecycle.Workflows;

internal interface ICorrectionPropagationWorkflowStatusSink
{
    ValueTask ReportAsync(
        CorrectionPropagationRequest request,
        string workflowStatus,
        int workflowRetryCount,
        string? workflowLastFailureCode,
        CancellationToken cancellationToken);
}
