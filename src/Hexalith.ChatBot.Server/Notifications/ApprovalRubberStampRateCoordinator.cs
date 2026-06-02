using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Gateway.Stages;

namespace Hexalith.ChatBot.Server.Notifications;

/// <summary>The result of a rubber-stamp-rate evaluation pass: how many tenant snapshots were evaluated, how many fired
/// the FR41 tuning revisit (and were durably recorded), and how many fired but were suppressed fail-closed.</summary>
internal sealed record ApprovalRubberStampRateOutcome(int Evaluated, int Triggered, int AuditUnavailable);

/// <summary>
/// Injectable rubber-stamp-rate coordinator (Story 7.11, NFR46/FR41/NFR15a) following the Story 7.7
/// <see cref="EscalationEvaluationCoordinator"/> / Story 7.10 <see cref="ReviewerBacklogAlertCoordinator"/> discipline
/// exactly: evaluate → <strong>if</strong> the tenant-level FR41 condition fires, write the metadata-only pre-commit
/// audit envelope → only on audit success record the durable revisit side effect → count outcome.
/// <para>
/// Fail-closed (NFR15a): if the pre-commit audit for a fired revisit is unavailable, no durable/observable side effect
/// occurs and the <c>AuditUnavailable</c> count is incremented. Exactly one metadata-only
/// <see cref="AuditEnvelopeFactory.ApprovalTuningRevisitTriggered"/> envelope is written per fired tenant-level
/// condition per evaluation pass. No always-on <c>BackgroundService</c> is introduced; the periodic runtime/Dapr-timer
/// caller that materializes the decision snapshot from <c>ApprovalEventView</c> is deferred (consistent with the 7.6
/// delivery caller, the 7.7 escalation runtime, the 7.9 throttle runtime, and the 7.10 backlog runtime).
/// </para>
/// </summary>
internal sealed class ApprovalRubberStampRateCoordinator(
    IAuditWriter auditWriter,
    ISystemClock clock)
{
    public async ValueTask<ApprovalRubberStampRateOutcome> EvaluateAndRecordAsync(
        IReadOnlyList<ApprovalDecisionSample> decisions,
        string tenantRef,
        string correlationId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(decisions);
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantRef);
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);

        ApprovalRubberStampRateObservation observation = ApprovalRubberStampRateEvaluator.Evaluate(
            decisions, tenantRef, correlationId, clock);

        // No tenant-level crossing → nothing recorded (AC4: a 0/0 / empty / sub-threshold window never triggers).
        if (!observation.TuningRevisitTriggered)
        {
            return new ApprovalRubberStampRateOutcome(Evaluated: 1, Triggered: 0, AuditUnavailable: 0);
        }

        // Fail closed: the fired revisit writes its metadata-only audit record (FR75g) before any durable/observable
        // side effect. If the pre-commit audit is unavailable, no revisit is recorded — no side effect (NFR15a).
        AuditEnvelope envelope = AuditEnvelopeFactory.ApprovalTuningRevisitTriggered(observation, tenantRef, clock.UtcNow);
        AuditWriteResult auditResult = await auditWriter
            .RecordPreCommitAsync(envelope, cancellationToken)
            .ConfigureAwait(false);
        if (!auditResult.Succeeded)
        {
            return new ApprovalRubberStampRateOutcome(Evaluated: 1, Triggered: 0, AuditUnavailable: 1);
        }

        return new ApprovalRubberStampRateOutcome(Evaluated: 1, Triggered: 1, AuditUnavailable: 0);
    }
}
