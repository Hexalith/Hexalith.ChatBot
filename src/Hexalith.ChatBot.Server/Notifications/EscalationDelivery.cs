using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Server.Notifications;

/// <summary>
/// A fired escalation: the metadata-only <see cref="NotificationDelivery"/> resolved through the FR73 routing engine,
/// plus the structured breach context used for the per-event audit record (FR59). Distinguishable from an ordinary
/// 7.6 notification by both this distinct type and the escalation reason code carried on the inner delivery. Stays
/// metadata-only — no project content, recipient addresses, or secrets.
/// </summary>
internal sealed record EscalationDelivery(
    NotificationDelivery Notification,
    EscalationBreachReason BreachReason,
    EscalationSeverity Severity,
    int AgeSeconds,
    int AgeThresholdSeconds);
