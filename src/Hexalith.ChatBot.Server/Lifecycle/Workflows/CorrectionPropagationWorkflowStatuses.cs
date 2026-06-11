namespace Hexalith.ChatBot.Server.Lifecycle.Workflows;

internal static class CorrectionPropagationWorkflowStatuses
{
    public const string Started = "started";
    public const string Retrying = "retrying";
    public const string Completed = "completed";
    public const string Delayed = "delayed";
    public const string Failed = "failed";
    public const string RuntimeUnavailable = "runtime-unavailable";
}
