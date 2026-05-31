namespace Hexalith.ChatBot.UI.Design;

/// <summary>
/// Metadata a dense governed workflow must preserve when represented as phone-limited content.
/// </summary>
/// <param name="ReadOnlySummary">Read-only summary visible on the small screen.</param>
/// <param name="CurrentStatus">Current governed state or completion status.</param>
/// <param name="SafeActions">Safe actions that remain available when applicable.</param>
/// <param name="HandoffLinkLabel">Copy/share handoff link label or metadata.</param>
/// <param name="LargerScreenGuidance">Guidance for opening the full workflow on a larger screen.</param>
/// <param name="PreservedStateMarker">Marker that draft or filter state must be preserved for handoff.</param>
/// <param name="ReachableExplanation">Non-tooltip explanation for unavailable dense controls.</param>
public sealed record ChatBotSmallScreenFallbackContract(
    string ReadOnlySummary,
    string CurrentStatus,
    IReadOnlyList<string> SafeActions,
    string HandoffLinkLabel,
    string LargerScreenGuidance,
    string PreservedStateMarker,
    string ReachableExplanation)
{
    /// <summary>Gets a value indicating whether required phone fallback metadata is complete.</summary>
    public bool IsComplete
        => !string.IsNullOrWhiteSpace(ReadOnlySummary)
            && !string.IsNullOrWhiteSpace(CurrentStatus)
            && SafeActions is { Count: > 0 }
            && SafeActions.All(static action => !string.IsNullOrWhiteSpace(action))
            && !string.IsNullOrWhiteSpace(HandoffLinkLabel)
            && !string.IsNullOrWhiteSpace(LargerScreenGuidance)
            && !string.IsNullOrWhiteSpace(PreservedStateMarker)
            && !string.IsNullOrWhiteSpace(ReachableExplanation)
            && !ReachableExplanation.Contains("tooltip", StringComparison.OrdinalIgnoreCase);

    /// <summary>Creates the required fallback metadata for a phone-limited governed workflow.</summary>
    /// <param name="ReadOnlySummary">Read-only summary visible on the small screen.</param>
    /// <param name="CurrentStatus">Current governed state or completion status.</param>
    /// <param name="SafeActions">Safe actions that remain available when applicable.</param>
    /// <param name="HandoffLinkLabel">Copy/share handoff link label or metadata.</param>
    /// <param name="LargerScreenGuidance">Guidance for opening the full workflow on a larger screen.</param>
    /// <param name="PreservedStateMarker">Marker that draft or filter state must be preserved for handoff.</param>
    /// <param name="ReachableExplanation">Non-tooltip explanation for unavailable dense controls.</param>
    /// <returns>A phone-limited fallback contract.</returns>
    public static ChatBotSmallScreenFallbackContract CreatePhoneLimited(
        string ReadOnlySummary,
        string CurrentStatus,
        IReadOnlyList<string> SafeActions,
        string HandoffLinkLabel,
        string LargerScreenGuidance,
        string PreservedStateMarker,
        string ReachableExplanation)
        => new(
            ReadOnlySummary,
            CurrentStatus,
            SafeActions,
            HandoffLinkLabel,
            LargerScreenGuidance,
            PreservedStateMarker,
            ReachableExplanation);
}
