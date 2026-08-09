namespace Hexalith.ChatBot.Server.Lifecycle.Workflows;

internal sealed class NullCorrectionPropagationWorkflowStatusSink : ICorrectionPropagationWorkflowStatusSink
{
    public static NullCorrectionPropagationWorkflowStatusSink Instance { get; } = new();

    public ValueTask ReportAsync(
        CorrectionPropagationRequest request,
        string workflowStatus,
        int workflowRetryCount,
        string? workflowLastFailureCode,
        CancellationToken cancellationToken)
        => ValueTask.CompletedTask;
}
