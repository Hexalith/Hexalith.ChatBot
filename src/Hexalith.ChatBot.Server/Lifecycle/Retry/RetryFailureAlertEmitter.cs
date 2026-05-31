using Hexalith.ChatBot.Server.Audit;

namespace Hexalith.ChatBot.Server.Lifecycle.Retry;

internal sealed class RetryFailureAlertEmitter(IOperatorAlertSink alertSink, ISystemClock clock)
{
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
