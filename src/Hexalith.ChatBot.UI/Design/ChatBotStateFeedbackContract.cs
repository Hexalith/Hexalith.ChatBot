namespace Hexalith.ChatBot.UI.Design;

/// <summary>
/// Metadata contract for a governed workflow feedback state.
/// </summary>
/// <param name="StateFamily">Governed state family.</param>
/// <param name="Primitive">UI primitive used for feedback.</param>
/// <param name="Politeness">Live-region politeness.</param>
/// <param name="FocusBehavior">Required focus behavior.</param>
/// <param name="DedupRule">Announcement repeat policy.</param>
/// <param name="AnnouncementKeySource">Stable key source used for announcement deduplication.</param>
/// <param name="RequiresInlineStatus">Whether visible inline status/reason text is required.</param>
/// <param name="RequiresBackgroundUpdateAffordance">Whether a keyboard-reachable new-updates affordance is required.</param>
/// <param name="RequiredExistingContracts">Existing foundation contracts this matrix entry composes.</param>
public sealed record ChatBotStateFeedbackContract(
    ChatBotFeedbackStateFamily StateFamily,
    ChatBotFeedbackPrimitive Primitive,
    ChatBotLiveRegionPoliteness Politeness,
    ChatBotFeedbackFocusBehavior FocusBehavior,
    ChatBotAnnouncementDedupRule DedupRule,
    string AnnouncementKeySource,
    bool RequiresInlineStatus,
    bool RequiresBackgroundUpdateAffordance,
    IReadOnlyList<string> RequiredExistingContracts)
{
    /// <summary>Gets a value indicating whether this entry intentionally skips live-region output.</summary>
    public bool IsInlineOnly => Politeness is ChatBotLiveRegionPoliteness.None && DedupRule is ChatBotAnnouncementDedupRule.NoLiveAnnouncement;

    /// <summary>Gets the ARIA role for the live primitive, or <see langword="null" /> when inline-only.</summary>
    public string? AriaRole
        => Politeness switch
        {
            ChatBotLiveRegionPoliteness.Polite => "status",
            ChatBotLiveRegionPoliteness.Assertive => "alert",
            ChatBotLiveRegionPoliteness.None => null,
            _ => throw new ArgumentOutOfRangeException(nameof(Politeness), Politeness, null),
        };

    /// <summary>Gets the aria-live value for the primitive.</summary>
    public string AriaLive
        => Politeness switch
        {
            ChatBotLiveRegionPoliteness.Polite => "polite",
            ChatBotLiveRegionPoliteness.Assertive => "assertive",
            ChatBotLiveRegionPoliteness.None => "off",
            _ => throw new ArgumentOutOfRangeException(nameof(Politeness), Politeness, null),
        };

    /// <summary>Gets a value indicating whether announcement deduplication has a stable source.</summary>
    public bool HasStableAnnouncementKey
        => DedupRule is ChatBotAnnouncementDedupRule.NoLiveAnnouncement
            || !string.IsNullOrWhiteSpace(AnnouncementKeySource);

    /// <summary>Gets a value indicating whether required metadata is complete.</summary>
    public bool IsComplete
        => Primitive is not ChatBotFeedbackPrimitive.None
            && HasStableAnnouncementKey
            && RequiredExistingContracts is not null
            && RequiredExistingContracts.All(static contract => !string.IsNullOrWhiteSpace(contract))
            && (!RequiresBackgroundUpdateAffordance || Primitive is ChatBotFeedbackPrimitive.NewUpdatesAffordance);
}
