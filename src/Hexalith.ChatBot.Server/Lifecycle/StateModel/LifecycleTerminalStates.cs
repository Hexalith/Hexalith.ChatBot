namespace Hexalith.ChatBot.Server.Lifecycle.StateModel;

internal static class LifecycleTerminalStates
{
    private static readonly HashSet<string> TerminalStates =
    [
        LifecycleStates.Rejected,
        LifecycleStates.Failed,
        LifecycleStates.Skipped,
    ];

    public static bool IsTerminal(string state)
        => TerminalStates.Contains(state);
}
