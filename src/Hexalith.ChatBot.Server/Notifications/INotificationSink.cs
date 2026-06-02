namespace Hexalith.ChatBot.Server.Notifications;

/// <summary>
/// Metadata-only delivery seam for notifications, parallel to <c>IOperatorAlertSink</c>. Implementations must not
/// carry restricted content. Per-event (no rollup) so Story 7.9 can layer digest/throttle on top.
/// </summary>
internal interface INotificationSink
{
    ValueTask DeliverAsync(NotificationDelivery delivery, CancellationToken cancellationToken);
}
