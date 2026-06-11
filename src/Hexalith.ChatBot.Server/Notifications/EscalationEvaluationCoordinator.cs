using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Gateway.Stages;

namespace Hexalith.ChatBot.Server.Notifications;

/// <summary>The result of an escalation evaluation pass: how many escalations fired, were delivered, or were suppressed fail-closed.</summary>
internal sealed record EscalationEvaluationOutcome(int Fired, int Delivered, int AuditUnavailable);

/// <summary>
/// Injectable escalation coordinator following the project's established firing-source pattern. It drives evaluate →
/// per-event fail-closed audit → deliver. Story 8.7b's periodic enforcement runtime owns the hosted trigger and calls
/// this coordinator as one evaluator in a non-overlapping pass.
/// </summary>
internal sealed class EscalationEvaluationCoordinator(
    INotificationSink notificationSink,
    IAuditWriter auditWriter,
    ISystemClock clock)
{
    public async ValueTask<EscalationEvaluationOutcome> EvaluateAndDeliverAsync(
        IReadOnlyList<EscalationQueueItem> items,
        EscalationPolicyChangeSet policy,
        IReadOnlyList<NotificationRecipientCandidate> candidates,
        string tenantRef,
        string correlationId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(policy);
        ArgumentNullException.ThrowIfNull(candidates);
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantRef);
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);

        IReadOnlyList<EscalationDelivery> escalations = EscalationPolicyEvaluator.Evaluate(
            items, policy, candidates, tenantRef, correlationId, clock);

        int delivered = 0;
        int auditUnavailable = 0;
        foreach (EscalationDelivery escalation in escalations)
        {
            // Fail closed: each fired escalation writes its metadata-only audit record (FR59) before delivery. If the
            // pre-commit audit is unavailable, no escalation is delivered for that event — no durable/observable side effect.
            AuditEnvelope envelope = AuditEnvelopeFactory.EscalationFired(escalation, tenantRef, clock.UtcNow);
            AuditWriteResult auditResult = await auditWriter
                .RecordPreCommitAsync(envelope, cancellationToken)
                .ConfigureAwait(false);
            if (!auditResult.Succeeded)
            {
                auditUnavailable++;
                continue;
            }

            await notificationSink.DeliverAsync(escalation.Notification, cancellationToken).ConfigureAwait(false);
            delivered++;
        }

        return new EscalationEvaluationOutcome(escalations.Count, delivered, auditUnavailable);
    }
}
