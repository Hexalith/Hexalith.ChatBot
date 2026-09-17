namespace Hexalith.ChatBot.Server.Lifecycle.Workflows;

internal sealed record CorrectionPropagationWorkflowRuntimeStatus(
    bool IsAvailable,
    string Status,
    string ReasonCode,
    DateTimeOffset CheckedAtUtc);
