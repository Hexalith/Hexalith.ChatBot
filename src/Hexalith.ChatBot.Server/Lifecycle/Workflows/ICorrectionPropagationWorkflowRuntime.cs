namespace Hexalith.ChatBot.Server.Lifecycle.Workflows;

internal interface ICorrectionPropagationWorkflowRuntime
{
    bool IsAvailable { get; }

    ValueTask ScheduleAsync(CorrectionPropagationRequest request, CancellationToken cancellationToken);

    ValueTask<CorrectionPropagationWorkflowRuntimeStatus> CheckAsync(CancellationToken cancellationToken);
}

internal sealed record CorrectionPropagationWorkflowRuntimeStatus(
    bool IsAvailable,
    string Status,
    string ReasonCode,
    DateTimeOffset CheckedAtUtc);
