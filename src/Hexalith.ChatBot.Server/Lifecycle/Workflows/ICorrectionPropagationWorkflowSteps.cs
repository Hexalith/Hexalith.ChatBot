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

    Task CallStartAsync(CorrectionPropagationStartInput input);

    Task<CorrectionPropagationActivityResult> CallStoreAsync(CorrectionPropagationStoreActivityInput input);

    Task CreateTimerAsync(TimeSpan delay);

    Task CallCompleteAsync(CorrectionPropagationRequest request);

    Task<bool> CallDelayAsync(CorrectionPropagationDelayInput input);

    DateTimeOffset CurrentUtc { get; }
}
