using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Observability;

namespace Hexalith.ChatBot.Server.Lifecycle.Retry;

internal sealed class RetryFailureAlertEmitter(IOperatorAlertSink alertSink, ISystemClock clock, IChatBotMetrics? metrics = null)
{
    private readonly IChatBotMetrics _metrics = metrics ?? NullChatBotMetrics.Instance;

    public async ValueTask EmitIfRequiredAsync(
        RetryPolicyDecision decision,
        string tenantId,
        string operationClass,
        string correlationId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(decision);
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(operationClass);
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);

        // Story 8.2: a workflow item that reached the retry-exhausted terminal state (operation-class `retry`).
        if (decision.IsExhausted)
        {
            _metrics.RecordRetryExhausted(tenantId);
        }

        OperatorAlertKind? kind = decision switch
        {
            { IsExhausted: true } => OperatorAlertKind.RetryExhausted,
            { IsRetryable: true } => OperatorAlertKind.DependencyDegraded,
            _ => null,
        };

        if (kind is null)
        {
            return;
        }

        await alertSink
            .EmitAsync(
                new OperatorAlert(
                    kind.Value,
                    decision.ReasonCode,
                    tenantId,
                    operationClass,
                    correlationId,
                    clock.UtcNow),
                cancellationToken)
            .ConfigureAwait(false);
    }
}
