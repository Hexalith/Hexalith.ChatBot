namespace Hexalith.ChatBot.UI.Design;

/// <summary>
/// Busy/loading focus metadata for governed UI regions.
/// </summary>
/// <param name="RegionId">Stable busy region id.</param>
/// <param name="AccessibleLabel">Accessible region label.</param>
/// <param name="BusyStateElementId">Element id that receives and clears aria-busy.</param>
/// <param name="FocusPreservationTargetId">Existing focus target that should remain focused after loading.</param>
/// <param name="LoadedContentLandingTargetId">Labelled landing target used when focus must move after loading.</param>
/// <param name="ClearsAriaBusyOnSameRegion">Whether aria-busy is cleared on the same node where it was set.</param>
/// <param name="AnnouncesHistoricalContent">Whether historical loaded content is announced as new content.</param>
public sealed record ChatBotBusyRegionContract(
    string RegionId,
    string AccessibleLabel,
    string BusyStateElementId,
    string FocusPreservationTargetId,
    string LoadedContentLandingTargetId,
    bool ClearsAriaBusyOnSameRegion,
    bool AnnouncesHistoricalContent)
{
    /// <summary>Gets a value indicating whether busy-region focus behavior is fully specified.</summary>
    public bool IsComplete
        => !string.IsNullOrWhiteSpace(RegionId)
            && !string.IsNullOrWhiteSpace(AccessibleLabel)
            && BusyStateElementId == RegionId
            && (!string.IsNullOrWhiteSpace(FocusPreservationTargetId) || !string.IsNullOrWhiteSpace(LoadedContentLandingTargetId))
            && ClearsAriaBusyOnSameRegion
            && !AnnouncesHistoricalContent;
}
