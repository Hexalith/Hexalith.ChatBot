using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Gateway;
using Hexalith.ChatBot.Server.Gateway.Stages;
using Hexalith.ChatBot.Server.Observability;
using Hexalith.ChatBot.Server.Projections;

namespace Hexalith.ChatBot.Server.Notifications;

/// <summary>
/// Injectable coordinator that wires the five NFR43 default alert thresholds (Story 8.4) to a deterministic,
/// fail-closed evaluate → pre-commit-audit → deliver pipeline, following the <see cref="ReviewerBacklogAlertCoordinator"/>
/// discipline exactly. For one tenant evaluation pass it runs all five pure evaluators over already-available
/// in-process signals (audit-projection-lag reading, retry-exhaustion hook, approval-queue / subscription-expiry
/// queue snapshot, authorization-failure rolling count), collects the fired metadata-only
/// <see cref="OperationalAlertPayload"/> items, and for each writes the pre-commit audit envelope via
/// <see cref="AuditEnvelopeFactory.OperationalAlertFired"/> and delivers through <see cref="INotificationSink"/> only
/// when the audit succeeds (NFR15a). The alert decision sits between the evaluators and the sink, never bypassing the
/// routing resolver or the audit. No always-on <c>BackgroundService</c> is introduced; the periodic runtime caller is
/// deferred (consistent with <see cref="ReviewerBacklogAlertCoordinator"/>).
/// </summary>
internal sealed class OperationalAlertWiringCoordinator(
    INotificationSink notificationSink,
    IAuditWriter auditWriter,
    IAuditProjectionLagSource lagSource,
    IRetryExhaustionAlertSource retrySource,
    IAuthorizationFailureCounter authFailureCounter,
    ISystemClock clock)
{
    private const string OperationalAlertQueueRef = "queue:operational-alerts";

    public async ValueTask<OperationalAlertOutcome> EvaluateAndDeliverAsync(
        IReadOnlyList<AdminQueueSummaryProjectionItem> queueItems,
        IReadOnlyList<NotificationRecipientCandidate> candidates,
        string tenantRef,
        string correlationId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(queueItems);
        ArgumentNullException.ThrowIfNull(candidates);
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantRef);
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);

        DateTimeOffset now = clock.UtcNow;
        IReadOnlyList<OperationalAlertPayload> alerts = CollectAlerts(queueItems, tenantRef, correlationId, now);

        int delivered = 0;
        int auditUnavailable = 0;
        foreach (OperationalAlertPayload alert in alerts)
        {
            // Fail closed (NFR15a): each fired alert writes its metadata-only pre-commit audit record before any
            // delivery. If the pre-commit audit is unavailable, no alert is delivered — no durable/observable side
            // effect — and the AuditUnavailable count is incremented.
            AuditEnvelope envelope = AuditEnvelopeFactory.OperationalAlertFired(alert, now);
            AuditWriteResult auditResult = await auditWriter
                .RecordPreCommitAsync(envelope, cancellationToken)
                .ConfigureAwait(false);
            if (!auditResult.Succeeded)
            {
                auditUnavailable++;
                continue;
            }

            foreach (NotificationDelivery delivery in ResolveDeliveries(alert, candidates))
            {
                await notificationSink.DeliverAsync(delivery, cancellationToken).ConfigureAwait(false);
                delivered++;
            }
        }

        return new OperationalAlertOutcome(alerts.Count, delivered, auditUnavailable);
    }

    private IReadOnlyList<OperationalAlertPayload> CollectAlerts(
        IReadOnlyList<AdminQueueSummaryProjectionItem> queueItems,
        string tenantRef,
        string correlationId,
        DateTimeOffset now)
    {
        List<OperationalAlertPayload> alerts = [];

        // Audit-projection lag: fire at most one alert per tenant per pass when any trustworthy reading is
        // Degraded/Failed (no-data never fabricates an alert).
        foreach (AuditProjectionLagReading reading in lagSource.ReadCurrent())
        {
            if (!string.Equals(reading.TenantId, tenantRef, StringComparison.Ordinal))
            {
                continue;
            }

            AuditProjectionLagStatus status = AuditProjectionLagEvaluator.Evaluate(
                reading.LastProjectedPosition,
                reading.LatestCommittedPosition,
                reading.SnapshotUtc,
                now);
            if (AuditProjectionLagAlertEvaluator.Evaluate(status, tenantRef, correlationId, now) is { } lagAlert)
            {
                alerts.Add(lagAlert);
                break;
            }
        }

        // Retry exhaustion: the hook flag for this tenant is read-and-cleared each pass.
        if (RetryExhaustionAlertEvaluator.Evaluate(retrySource.ReadAndClear(tenantRef), tenantRef, correlationId, now)
            is { } retryAlert)
        {
            alerts.Add(retryAlert);
        }

        // Approval-queue age: a single aggregate alert per tenant when any open item exceeds the threshold.
        if (ApprovalQueueAgeAlertEvaluator.Evaluate(queueItems, tenantRef, correlationId, now) is { } approvalAlert)
        {
            alerts.Add(approvalAlert);
        }

        // Mailbox subscription expiry: one alert per affected mailbox.
        alerts.AddRange(SubscriptionExpiryAlertEvaluator.Evaluate(queueItems, tenantRef, correlationId, now));

        // Authorization-failure spike: one alert per tenant exceeding the baseline (scoped to this tenant).
        IReadOnlyList<AuthorizationFailureReading> authReadings = authFailureCounter.ReadAndReset()
            .Where(reading => string.Equals(reading.TenantId, tenantRef, StringComparison.Ordinal))
            .ToList();
        alerts.AddRange(AuthorizationFailureSpikeEvaluator.Evaluate(authReadings, correlationId, now));

        return alerts;
    }

    private static IReadOnlyList<NotificationDelivery> ResolveDeliveries(
        OperationalAlertPayload alert,
        IReadOnlyList<NotificationRecipientCandidate> candidates)
    {
        if (!AdminRoles.TryFromWireValue(alert.OwnerRole, out AdminRole ownerRole))
        {
            return [];
        }

        NotificationStateClass stateClass = StateClassFor(alert.AlertKind);

        // Aggregate, redacted event: ItemProjectRef = null → the resolver yields MetadataRedacted (no per-project
        // leakage), identical to the reviewer-backlog aggregate alert. The tenant ref is the authenticated binding.
        NotificationStateEvent stateEvent = new(
            alert.TenantRef,
            stateClass,
            $"operational-alert:{OperationalAlertPayload.AlertKindWireValue(alert.AlertKind)}",
            OperationalAlertQueueRef,
            alert.ReasonCode,
            alert.CorrelationId,
            alert.FiredAtUtc,
            ItemProjectRef: null);

        // Each alert kind routes to exactly its owner role via a single see-only in-app routing entry; the resolver
        // resolves audience + redaction through the existing authority path.
        NotificationRoutingChangeSet routing = new(
        [
            new NotificationRoutingEntry(stateClass, AdminScope.SeeOnly, ownerRole, NotificationChannel.InApp),
        ]);

        return NotificationRoutingResolver.Resolve(stateEvent, routing, candidates);
    }

    private static NotificationStateClass StateClassFor(OperatorAlertKind kind)
        => kind switch
        {
            OperatorAlertKind.RetryExhausted => NotificationStateClass.Retry,
            OperatorAlertKind.ApprovalQueueAgeBreached => NotificationStateClass.ApprovalPending,
            _ => NotificationStateClass.Degraded,
        };
}
