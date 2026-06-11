using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Gateway.Stages;
using Hexalith.ChatBot.Server.Projections;

namespace Hexalith.ChatBot.Server.Notifications;

/// <summary>The result of a reviewer-backlog evaluation pass: how many alerts fired, were delivered, or were suppressed fail-closed.</summary>
internal sealed record ReviewerBacklogAlertOutcome(int Fired, int Delivered, int AuditUnavailable);

/// <summary>
/// Injectable reviewer-backlog alert coordinator (Story 7.10, NFR46) following the Story 7.7
/// <see cref="EscalationEvaluationCoordinator"/> discipline exactly: evaluate → per-event pre-commit audit → (on audit
/// success) deliver via <see cref="INotificationSink"/> → count. The alert decision sits between aggregation and the
/// sink, never bypassing the routing resolver or the audit.
/// <para>
/// Fail-closed (NFR15a): if the pre-commit audit for a fired alert is unavailable, no durable/observable delivery side
/// effect occurs and the <c>AuditUnavailable</c> count is incremented. Exactly one metadata-only envelope is written per
/// fired alert. Story 8.7b's periodic enforcement runtime owns the scheduler caller for this coordinator.
/// </para>
/// </summary>
internal sealed class ReviewerBacklogAlertCoordinator(
    INotificationSink notificationSink,
    IAuditWriter auditWriter,
    ISystemClock clock)
{
    public async ValueTask<ReviewerBacklogAlertOutcome> EvaluateAndDeliverAsync(
        IReadOnlyList<AdminQueueSummaryProjectionItem> items,
        IReadOnlyList<NotificationRecipientCandidate> candidates,
        string tenantRef,
        string correlationId,
        ReviewerBacklogThreshold threshold,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(candidates);
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantRef);
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);
        ArgumentNullException.ThrowIfNull(threshold);

        IReadOnlyList<ReviewerBacklogAlert> alerts = ReviewerBacklogEvaluator.Evaluate(
            items, candidates, tenantRef, correlationId, threshold, clock);

        int delivered = 0;
        int auditUnavailable = 0;
        foreach (ReviewerBacklogAlert alert in alerts)
        {
            // Fail closed: each fired alert writes its metadata-only audit record (FR75g) before delivery. If the
            // pre-commit audit is unavailable, no alert is delivered for that event — no durable/observable side effect.
            AuditEnvelope envelope = AuditEnvelopeFactory.ReviewerBacklogAlertFired(alert, tenantRef, clock.UtcNow);
            AuditWriteResult auditResult = await auditWriter
                .RecordPreCommitAsync(envelope, cancellationToken)
                .ConfigureAwait(false);
            if (!auditResult.Succeeded)
            {
                auditUnavailable++;
                continue;
            }

            await notificationSink.DeliverAsync(alert.Notification, cancellationToken).ConfigureAwait(false);
            delivered++;
        }

        return new ReviewerBacklogAlertOutcome(alerts.Count, delivered, auditUnavailable);
    }
}
