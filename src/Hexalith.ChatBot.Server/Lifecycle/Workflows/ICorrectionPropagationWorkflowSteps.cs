namespace Hexalith.ChatBot.Server.Lifecycle.Workflows;

/// <summary>
/// Testable orchestration steps for correction-propagation. The Dapr Workflow type delegates here so
/// Complete vs Delay branching is covered without a parallel test-only orchestrator.
/// </summary>
internal interface ICorrectionPropagationWorkflowSteps
{
    void SetStatus(CorrectionPropagationWorkflowProgress progress);

    Task<IReadOnlyList<string>> CallScopeAsync(CorrectionPropagationRequest request);

    Task<string> CallResolveCorrectedCaseAsync(CorrectionPropagationRequest request);

    Task CallRetryStatusAsync(CorrectionPropagationRetryStatusInput input);

    Task CallStartAsync(CorrectionPropagationStartInput input);

    Task<CorrectionPropagationActivityResult> CallStoreAsync(CorrectionPropagationStoreActivityInput input);

    Task CreateTimerAsync(TimeSpan delay);

    /// <summary>Obtains an activity-recorded snapshot of the shared observable UTC authority.</summary>
    Task<DateTimeOffset> ReadUtcAsync() => Task.FromResult(CurrentUtc);

    /// <summary>Schedules the same absolute UTC instant published as retry eligibility.</summary>
    Task CreateTimerAtAsync(DateTimeOffset dueAt) => CreateTimerAsync(dueAt - CurrentUtc);

    Task CallCompleteAsync(CorrectionPropagationRequest request);

    Task<bool> CallDelayAsync(CorrectionPropagationDelayInput input);

    DateTimeOffset CurrentUtc { get; }
}
