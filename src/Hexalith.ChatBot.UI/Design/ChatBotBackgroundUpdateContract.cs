namespace Hexalith.ChatBot.UI.Design;

/// <summary>
/// Foundation metadata for background updates while the user is reading older content.
/// </summary>
/// <param name="AffordanceLabel">Keyboard-reachable affordance label.</param>
/// <param name="IsKeyboardReachable">Whether the affordance is reachable by keyboard.</param>
/// <param name="PreventsForcedScroll">Whether updates avoid forced scroll.</param>
/// <param name="PreservesFocusAndSelection">Whether existing focus and selection are preserved.</param>
/// <param name="ObservedForOthersUpdatesAreInlineOnly">Whether updates belonging to other users avoid live-region announcements.</param>
/// <param name="UsesMotionOnlyCue">Whether motion is the only update cue.</param>
public sealed record ChatBotBackgroundUpdateContract(
    string AffordanceLabel,
    bool IsKeyboardReachable,
    bool PreventsForcedScroll,
    bool PreservesFocusAndSelection,
    bool ObservedForOthersUpdatesAreInlineOnly,
    bool UsesMotionOnlyCue)
{
    /// <summary>Gets the governed foundation default for future stream, queue, and timeline surfaces.</summary>
    public static ChatBotBackgroundUpdateContract GovernedFoundation { get; } = new(
        AffordanceLabel: "New updates",
        IsKeyboardReachable: true,
        PreventsForcedScroll: true,
        PreservesFocusAndSelection: true,
        ObservedForOthersUpdatesAreInlineOnly: true,
        UsesMotionOnlyCue: false);

    /// <summary>Gets a value indicating whether background-update behavior is completely specified.</summary>
    public bool IsComplete
        => !string.IsNullOrWhiteSpace(AffordanceLabel)
            && IsKeyboardReachable
            && PreventsForcedScroll
            && PreservesFocusAndSelection
            && ObservedForOthersUpdatesAreInlineOnly
            && !UsesMotionOnlyCue;
}
