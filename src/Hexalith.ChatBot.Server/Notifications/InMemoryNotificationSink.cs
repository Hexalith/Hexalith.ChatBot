namespace Hexalith.ChatBot.Server.Notifications;

internal sealed class InMemoryNotificationSink : INotificationSink
{
    private readonly Lock _gate = new();
    private readonly List<NotificationDelivery> _deliveries = [];

    public IReadOnlyList<NotificationDelivery> Deliveries
    {
        get
        {
            lock (_gate)
            {
                return [.. _deliveries];
            }
        }
    }

    public ValueTask DeliverAsync(NotificationDelivery delivery, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(delivery);
        lock (_gate)
        {
            _deliveries.Add(delivery);
        }

        return ValueTask.CompletedTask;
    }
}
