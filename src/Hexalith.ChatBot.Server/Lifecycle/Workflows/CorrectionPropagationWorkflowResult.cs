namespace Hexalith.ChatBot.Server.Lifecycle.Workflows;

internal sealed record CorrectionPropagationWorkflowResult(
    string Status,
    int RetryCount,
    string? FailureReasonCode,
    IReadOnlyList<string> StoreKeys);
