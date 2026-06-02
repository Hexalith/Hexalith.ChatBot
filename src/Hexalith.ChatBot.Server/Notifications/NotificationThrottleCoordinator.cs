using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Gateway.Stages;

namespace Hexalith.ChatBot.Server.Notifications;

/// <summary>The result of a throttle/digest pass: how many deliveries were evaluated, immediately delivered, rolled into a digest, or suppressed fail-closed.</summary>
internal sealed record NotificationThrottleOutcome(int Evaluated, int Delivered, int Throttled, int AuditUnavailable);

/// <summary>
/// Injectable throttle/digest coordinator (Story 7.9, NFR46) following the Story 7.7
/// <see cref="EscalationEvaluationCoordinator"/> discipline exactly: for each already-resolved
/// <see cref="NotificationDelivery"/> it runs evaluate → per-delivery pre-commit audit → (on audit success) side effect
/// + counter advance. The throttle decision sits <em>between</em> routing resolution and the sink, never bypassing it.
/// <para>
/// Fail-closed (NFR15a): if the pre-commit audit is unavailable, no durable/observable side effect occurs — neither the
/// immediate push nor the digest append — AND the recipient's immediate-push counter is NOT advanced, so an unaudited
/// delivery can never silently exhaust the ceiling. The immediate-push counter advances only for an audited immediate
/// push; a throttle-to-digest decision appends a digest entry and never advances the push counter (otherwise a
/// throttled recipient could never recover within the rolling window). No always-on <c>BackgroundService</c> is
/// introduced; the runtime delivery/digest-send binding is deferred to the runtime caller.
/// </para>
/// </summary>
internal sealed class NotificationThrottleCoordinator(
    INotificationSink notificationSink,
    INotificationDeliveryHistoryStore historyStore,
    INotificationDigestStore digestStore,
    IAuditWriter auditWriter,
    ISystemClock clock)
{
    public async ValueTask<NotificationThrottleOutcome> EvaluateAndDeliverAsync(
        IReadOnlyList<NotificationDelivery> deliveries,
        NotificationThrottleCeilings ceilings,
        string tenantRef,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(deliveries);
        ArgumentNullException.ThrowIfNull(ceilings);
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantRef);

        int delivered = 0;
        int throttled = 0;
        int auditUnavailable = 0;

        foreach (NotificationDelivery delivery in deliveries)
        {
            DateTimeOffset now = clock.UtcNow;
            IReadOnlyList<DateTimeOffset> priorPushes = historyStore.GetImmediatePushTimestamps(delivery.TenantRef, delivery.RecipientRef);
            int hourCount = NotificationThrottleEvaluator.CountInTrailingWindow(priorPushes, now, NotificationThrottleEvaluator.HourWindow);
            int dayCount = NotificationThrottleEvaluator.CountInTrailingWindow(priorPushes, now, NotificationThrottleEvaluator.DayWindow);
            NotificationThrottleDecision decision = NotificationThrottleEvaluator.Decide(hourCount, dayCount, ceilings);

            // Snapshot the resulting pending-digest size for the audit envelope (the +1 reflects this overflow entry).
            int rolledUpCount = decision == NotificationThrottleDecision.ThrottleToDigest
                ? digestStore.GetPendingEntries(delivery.TenantRef, delivery.RecipientRef).Count + 1
                : digestStore.GetPendingEntries(delivery.TenantRef, delivery.RecipientRef).Count;

            // Fail closed: each delivery decision writes its metadata-only audit record before any side effect. If the
            // pre-commit audit is unavailable, nothing is delivered or rolled up and no counter advances for this event.
            AuditEnvelope envelope = AuditEnvelopeFactory.NotificationDelivered(
                delivery, decision, hourCount, dayCount, rolledUpCount, tenantRef, now);
            AuditWriteResult auditResult = await auditWriter
                .RecordPreCommitAsync(envelope, cancellationToken)
                .ConfigureAwait(false);
            if (!auditResult.Succeeded)
            {
                auditUnavailable++;
                continue;
            }

            if (decision == NotificationThrottleDecision.Deliver)
            {
                await notificationSink.DeliverAsync(delivery, cancellationToken).ConfigureAwait(false);
                historyStore.RecordImmediatePush(delivery.TenantRef, delivery.RecipientRef, now);
                delivered++;
            }
            else
            {
                // A throttled notification is never dropped — it always becomes a digest entry, re-applying the
                // resolver's NFR2 redaction (a MetadataRedacted delivery yields an entry with no item ref).
                digestStore.Append(NotificationDigestEntry.FromDelivery(delivery));
                throttled++;
            }
        }

        return new NotificationThrottleOutcome(deliveries.Count, delivered, throttled, auditUnavailable);
    }
}
