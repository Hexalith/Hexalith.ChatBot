using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Server.Notifications;

/// <summary>
/// A metadata-only notification delivery record. Like <c>OperatorAlert</c>, this seam is intentionally
/// content-free: it carries only state-class/channel/reason/refs, never project names, mailbox content, evidence,
/// provider payloads, recipient addresses, or secrets. <see cref="ItemRef"/> is populated only when the recipient
/// holds per-resource authority (<see cref="NotificationContentVisibility.ItemContext"/>).
/// </summary>
internal sealed record NotificationDelivery(
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
    DateTimeOffset RaisedAtUtc);
