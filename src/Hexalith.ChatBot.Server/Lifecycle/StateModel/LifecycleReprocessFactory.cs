namespace Hexalith.ChatBot.Server.Lifecycle.StateModel;

internal static class LifecycleReprocessFactory
{
    public const string SupersededByWorkflowLink = "superseded_by";
    public const string SupersedesWorkflowLink = "supersedes";

    public static LifecycleReprocessPlan Create(string terminalState, string supersededWorkflowId, string newWorkflowId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(terminalState);
        ArgumentException.ThrowIfNullOrWhiteSpace(supersededWorkflowId);
        ArgumentException.ThrowIfNullOrWhiteSpace(newWorkflowId);

        if (!LifecycleTerminalStates.IsTerminal(terminalState))
        {
            throw new ArgumentException("Reprocessing requires a terminal lifecycle state.", nameof(terminalState));
        }

        if (string.Equals(supersededWorkflowId, newWorkflowId, StringComparison.Ordinal))
        {
            throw new ArgumentException("Reprocessing requires a new workflow instance ID.", nameof(newWorkflowId));
        }

        return new LifecycleReprocessPlan(
            supersededWorkflowId,
            newWorkflowId,
            SupersededByWorkflowLink,
            SupersedesWorkflowLink);
    }
}
