namespace Hexalith.ChatBot.UI.Design;

/// <summary>
/// Reduced-motion metadata for governed UI primitives.
/// </summary>
/// <param name="SuppressedMotionHooks">CSS hooks whose non-essential motion is suppressed.</param>
/// <param name="StableStatusLabels">Text labels used instead of motion-only progress cues.</param>
/// <param name="PreservesFocusVisibility">Whether focus visibility remains intact.</param>
/// <param name="PreservesForcedColors">Whether forced-colors status cues remain intact.</param>
/// <param name="PreservesNonColorCues">Whether non-color labels/cues remain visible.</param>
/// <param name="SuppressesNonEssentialMotion">Whether non-essential animation and transitions are disabled.</param>
public sealed record ChatBotReducedMotionContract(
    IReadOnlyList<string> SuppressedMotionHooks,
    IReadOnlyList<string> StableStatusLabels,
    bool PreservesFocusVisibility,
    bool PreservesForcedColors,
    bool PreservesNonColorCues,
    bool SuppressesNonEssentialMotion)
{
    public static ChatBotReducedMotionContract GovernedFoundation { get; } = new(
        SuppressedMotionHooks:
        [
            ".chatbot-shimmer",
            ".chatbot-skeleton",
            ".chatbot-row-motion",
            ".chatbot-streaming-text",
            ".chatbot-panel-transition",
        ],
        StableStatusLabels:
        [
            "Scanning attachment",
            "Projection pending",
            "Submitting governed note",
            "New updates",
        ],
        PreservesFocusVisibility: true,
        PreservesForcedColors: true,
        PreservesNonColorCues: true,
        SuppressesNonEssentialMotion: true);

    /// <summary>Gets a value indicating whether the reduced-motion policy is complete.</summary>
    public bool IsComplete
        => SuppressedMotionHooks is { Count: >= 5 }
            && StableStatusLabels is { Count: >= 3 }
            && SuppressedMotionHooks.All(static hook => !string.IsNullOrWhiteSpace(hook))
            && StableStatusLabels.All(static label => !string.IsNullOrWhiteSpace(label))
            && PreservesFocusVisibility
            && PreservesForcedColors
            && PreservesNonColorCues
            && SuppressesNonEssentialMotion;
}
