namespace Hexalith.ChatBot.Server.Lifecycle.Workflows;

internal interface ICorrectionPropagationWorkflowRuntime
{
    bool IsAvailable { get; }

    ValueTask ScheduleAsync(CorrectionPropagationRequest request, CancellationToken cancellationToken);

    ValueTask<CorrectionPropagationWorkflowRuntimeStatus> CheckAsync(CancellationToken cancellationToken);

    /// <summary>Whether this runtime can read durable orchestration progress independently of status publication.</summary>
    bool HasAuthoritativeProgress => false;

    /// <summary>Reads the workflow's durable progress so stale projections cannot authorize retry during an active attempt.</summary>
    ValueTask<CorrectionPropagationWorkflowProgress?> ReadProgressAsync(string instanceId, CancellationToken cancellationToken)
        => ValueTask.FromResult<CorrectionPropagationWorkflowProgress?>(null);
}
