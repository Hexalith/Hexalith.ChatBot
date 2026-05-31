namespace Hexalith.ChatBot.UI.Design;

/// <summary>
/// Visible-order focus sequence metadata for a governed UI surface.
/// </summary>
/// <param name="SkipLinkTargetId">Skip-link target element id.</param>
/// <param name="MainRegionId">Main content element id.</param>
/// <param name="SurfaceHeadingSelector">Navigation focus selector for the current surface heading.</param>
/// <param name="OrderedLandmarkNames">Landmark names in expected reading and action order.</param>
public sealed record ChatBotFocusSequenceContract(
    string SkipLinkTargetId,
    string MainRegionId,
    string SurfaceHeadingSelector,
    IReadOnlyList<string> OrderedLandmarkNames)
{
    /// <summary>Gets a value indicating whether required focus sequence metadata is complete.</summary>
    public bool IsComplete
        => !string.IsNullOrWhiteSpace(SkipLinkTargetId)
            && SkipLinkTargetId == MainRegionId
            && !string.IsNullOrWhiteSpace(SurfaceHeadingSelector)
            && SurfaceHeadingSelector.Contains("h1", StringComparison.OrdinalIgnoreCase)
            && OrderedLandmarkNames is { Count: > 0 }
            && OrderedLandmarkNames.All(static landmark => !string.IsNullOrWhiteSpace(landmark));
}
