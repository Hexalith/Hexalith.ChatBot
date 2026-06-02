using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Server.Notifications;

/// <summary>
/// A single rolled-up overflow notification in a recipient's pending digest (Story 7.9, NFR46/NFR2). Mirrors the
/// <see cref="NotificationDelivery"/> content-free invariant: it carries only state-class/channel/scope/reason/refs and
/// the recipient's resolved <see cref="NotificationContentVisibility"/> — never project names, evidence, mailbox
/// content, recipient PII beyond safe refs, provider payloads, prompts, command bodies, claims, headers, tokens, or
/// secrets. <see cref="ItemRef"/> survives only for an <see cref="NotificationContentVisibility.ItemContext"/> entry;
/// a <see cref="NotificationContentVisibility.MetadataRedacted"/> entry omits it and is indistinguishable from
/// safe-not-found. The next-action affordance is the safe <see cref="ReasonCode"/> + state class the recipient already
/// holds authority to act on.
/// </summary>
internal sealed record NotificationDigestEntry(
    NotificationStateClass StateClass,
    NotificationChannel Channel,
    AdminRole RecipientRole,
    AdminScope Scope,
    string RecipientRef,
    string TenantRef,
    string? ItemRef,
    string QueueRef,
    string ReasonCode,
    string CorrelationId,
    NotificationContentVisibility Visibility,
    DateTimeOffset RaisedAtUtc)
{
    /// <summary>
    /// Rolls a throttled delivery into a digest entry, re-applying the resolver's NFR2 redaction: because the
    /// <paramref name="delivery"/> already came through <see cref="NotificationRoutingResolver"/>, its visibility is
    /// authoritative — the item ref is preserved only for an <see cref="NotificationContentVisibility.ItemContext"/>
    /// delivery and dropped for the redacted form. The digest therefore never reveals more than the immediate push would.
    /// </summary>
    public static NotificationDigestEntry FromDelivery(NotificationDelivery delivery)
    {
        ArgumentNullException.ThrowIfNull(delivery);

        return new NotificationDigestEntry(
            delivery.StateClass,
            delivery.Channel,
            delivery.RecipientRole,
            delivery.Scope,
            delivery.RecipientRef,
            delivery.TenantRef,
            delivery.Visibility is NotificationContentVisibility.ItemContext ? delivery.ItemRef : null,
            delivery.QueueRef,
            delivery.ReasonCode,
            delivery.CorrelationId,
            delivery.Visibility,
            delivery.RaisedAtUtc);
    }
}

/// <summary>
/// A metadata-only digest of throttled overflow notifications for a single <c>(tenant-ref × recipient-ref)</c> pair.
/// Delivered as a metadata-only notification (not a new content-bearing channel); the runtime digest-send binding is
/// deferred to the runtime caller (consistent with the Story 7.6/7.7 deferred delivery runtime).
/// </summary>
internal sealed record NotificationDigest(
    string TenantRef,
    string RecipientRef,
    IReadOnlyList<NotificationDigestEntry> Entries)
{
    /// <summary>The number of overflow notifications rolled up in this digest (never silently dropped).</summary>
    public int RolledUpCount => Entries.Count;
}
