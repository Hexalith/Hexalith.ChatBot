namespace Hexalith.ChatBot.Server.Notifications;

/// <summary>
/// Whether a delivered notification carries item-specific context or a safe metadata-only/redacted form.
/// A <see cref="MetadataRedacted"/> notification must be indistinguishable from safe-not-found (NFR2).
/// </summary>
internal enum NotificationContentVisibility
{
    /// <summary>Item-specific context (item ref) is included; recipient holds per-resource authority.</summary>
    ItemContext,

    /// <summary>Metadata-only/redacted form with no resource-existence leakage.</summary>
    MetadataRedacted,
}
