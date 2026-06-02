using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Server.Notifications;

/// <summary>
/// A notify-worthy state event the routing engine evaluates (FR72). Carries only metadata-safe fields. The tenant
/// reference comes from the authenticated gateway binding — never from item refs, channel config, or correlation ids.
/// </summary>
/// <param name="TenantRef">Tenant reference from authenticated gateway binding.</param>
/// <param name="StateClass">One of the six declared notify-worthy state classes.</param>
/// <param name="ItemRef">Metadata-safe reference to the affected workflow item.</param>
/// <param name="QueueRef">Metadata-safe queue/workflow reference.</param>
/// <param name="ReasonCode">Safe reason code.</param>
/// <param name="CorrelationId">Correlation id.</param>
/// <param name="RaisedAtUtc">Server-side UTC timestamp the state was raised.</param>
/// <param name="ItemProjectRef">
/// Per-resource authority key for item-specific delivery, or <see langword="null"/> for an aggregate/see-only event.
/// </param>
internal sealed record NotificationStateEvent(
    string TenantRef,
    NotificationStateClass StateClass,
    string ItemRef,
    string QueueRef,
    string ReasonCode,
    string CorrelationId,
    DateTimeOffset RaisedAtUtc,
    string? ItemProjectRef = null);
