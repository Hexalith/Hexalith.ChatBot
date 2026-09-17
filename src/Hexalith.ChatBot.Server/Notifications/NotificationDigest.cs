using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Server.Notifications;

/// <summary>
/// A metadata-only digest of throttled overflow notifications for a single <c>(tenant-ref × recipient-ref)</c> pair.
/// Delivered as a metadata-only notification (not a new content-bearing channel); Story 8.7b's periodic enforcement
/// runtime is the single caller that advances throttle/digest evaluation.
/// </summary>
internal sealed record NotificationDigest(
    string TenantRef,
    string RecipientRef,
    IReadOnlyList<NotificationDigestEntry> Entries)
{
    /// <summary>The number of overflow notifications rolled up in this digest (never silently dropped).</summary>
    public int RolledUpCount => Entries.Count;
}
