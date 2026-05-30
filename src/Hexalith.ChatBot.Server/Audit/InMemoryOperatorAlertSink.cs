namespace Hexalith.ChatBot.Server.Audit;

internal sealed class InMemoryOperatorAlertSink : IOperatorAlertSink
{
    private readonly Lock _gate = new();
    private readonly List<OperatorAlert> _alerts = [];

    public IReadOnlyList<OperatorAlert> Alerts
    {
        get
        {
            lock (_gate)
            {
                return [.. _alerts];
            }
        }
    }

    public ValueTask EmitAsync(OperatorAlert alert, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(alert);
        lock (_gate)
        {
            _alerts.Add(alert);
        }

        return ValueTask.CompletedTask;
    }
}
