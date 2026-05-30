namespace Hexalith.ChatBot.Server.Lifecycle.StateModel;

internal static class LifecycleSubStates
{
    public static IReadOnlyList<string> All { get; } =
    [
        LifecycleStates.Correcting,
        LifecycleStates.CorrectionDelayed,
    ];
}
