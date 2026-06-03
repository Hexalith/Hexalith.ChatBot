using System.Security.Claims;
using System.Text.Json;

using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Server.Gateway;
using Hexalith.ChatBot.Server.Gateway.Redaction;
using Hexalith.ChatBot.Server.Gateway.Stages;
using Hexalith.ChatBot.Server.Lifecycle.StateModel;
using Hexalith.ChatBot.Server.Notifications;
using Hexalith.ChatBot.Server.Observability;

namespace Hexalith.ChatBot.Server.Audit;

internal static class AuditEnvelopeFactory
{
    private const string EnvelopeSchemaVersion = "chatbot.audit-envelope.v1";
    private const string NoPayloadPolicySnapshotId = "chatbot.gateway.policy-snapshot.v1";

    public static AuditEnvelope PreCommit(
        ChatBotGatewayContext context,
        LifecycleTransitionDefinition transition,
        DateTimeOffset timestamp)
        => Create(
            context,
            timestamp,
            AuditCommitPhase.PreCommit,
            decision: "allow",
            reasonCode: "pre_commit_gate",
            stateTransition: transition.ToString(),
            outcome: "gate_passed");

    public static AuditEnvelope PostCommit(
        ChatBotGatewayContext context,
        ChatBotDispatchResult dispatchResult,
        LifecycleTransitionDefinition transition,
        DateTimeOffset timestamp)
    {
        ArgumentNullException.ThrowIfNull(dispatchResult);

        return Create(
            context,
            timestamp,
            AuditCommitPhase.PostCommit,
            decision: "allow",
            reasonCode: "eventstore_dispatch_accepted",
            stateTransition: transition.ToString(),
            outcome: "proposed",
            resourceId: dispatchResult.ResourceId);
    }

    public static AuditEnvelope DuplicateMailboxIntakeSuppressed(
        ChatBotGatewayContext context,
        LifecycleTransitionDefinition skipTransition,
        DateTimeOffset timestamp)
    {
        ArgumentNullException.ThrowIfNull(skipTransition);

        return Create(
            context,
            timestamp,
            AuditCommitPhase.PostCommit,
            decision: "suppress",
            reasonCode: "duplicate_provider_message",
            stateTransition: skipTransition.ToString(),
            outcome: "duplicate_suppressed");
    }

    public static AuditEnvelope RejectedLifecycleTransition(
        ChatBotGatewayContext context,
        LifecycleTransitionValidation transition,
        DateTimeOffset timestamp)
    {
        ArgumentNullException.ThrowIfNull(transition);

        return Create(
            context,
            timestamp,
            AuditCommitPhase.PreCommit,
            decision: "reject",
            reasonCode: transition.ReasonCode,
            stateTransition: transition.Transition.ToString(),
            outcome: "rejected");
    }

    /// <summary>
    /// Builds the metadata-only audit record for a single fired escalation (FR59, NFR15a). Carries the affected
    /// item's correlation context and safe refs only — never raw item content, recipient addresses, or secrets. The
    /// item ref is included only when the recipient holds per-resource authority (NFR2 redaction discipline).
    /// </summary>
    public static AuditEnvelope EscalationFired(
        EscalationDelivery escalation,
        string tenantRef,
        DateTimeOffset timestamp)
    {
        ArgumentNullException.ThrowIfNull(escalation);
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantRef);

        NotificationDelivery delivery = escalation.Notification;
        string breachToken = escalation.BreachReason == EscalationBreachReason.AgeThreshold
            ? "age-threshold"
            : "severity-threshold";

        List<string> refs =
        [
            $"correlation:{delivery.CorrelationId}",
            "admin-operation:escalation-fired",
            $"escalation-state-class:{NotificationStateClasses.ToWireValue(delivery.StateClass)}",
            $"escalation-scope:{AdminScopes.ToWireValue(delivery.Scope)}",
            $"escalation-target-role:{AdminRoles.ToWireValue(delivery.RecipientRole)}",
            $"escalation-channel:{NotificationChannels.ToWireValue(delivery.Channel)}",
            $"escalation-severity:{EscalationSeverities.ToWireValue(escalation.Severity)}",
            $"escalation-breach:{breachToken}",
            $"escalation-age-seconds:{escalation.AgeSeconds.ToString(System.Globalization.CultureInfo.InvariantCulture)}",
            $"escalation-age-threshold-seconds:{escalation.AgeThresholdSeconds.ToString(System.Globalization.CultureInfo.InvariantCulture)}",
        ];

        if (AuditMetadata.SafeOptionalToken(delivery.QueueRef) is { } safeQueue)
        {
            refs.Add($"escalation-queue:{safeQueue}");
        }

        // Item-specific ref only when the recipient holds per-resource authority (NFR2): a redacted escalation must be
        // indistinguishable from safe-not-found, so item refs never leak into the metadata-redacted form.
        if (delivery.Visibility == NotificationContentVisibility.ItemContext &&
            AuditMetadata.SafeOptionalToken(delivery.ItemRef) is { } safeItem)
        {
            refs.Add($"escalation-item:{safeItem}");
        }

        return new AuditEnvelope(
            tenantRef,
            "escalation-evaluator",
            "system",
            "EscalationFired",
            AuditMetadata.IsSafeStableIdentifier(delivery.QueueRef) ? delivery.QueueRef : "escalation",
            Decision: "escalate",
            ReasonCode: delivery.ReasonCode,
            CorrelationId: delivery.CorrelationId,
            timestamp,
            NoPayloadPolicySnapshotId,
            refs,
            IdempotencyKey: null,
            StateTransition: "Unresolved->Escalated",
            CoarseUserFacingRedactionStage.MetadataOnlyDecision,
            Outcome: "escalated",
            AuditCommitPhase.PostCommit,
            EnvelopeSchemaVersion,
            PredecessorHash: null,
            ChatBotSurfaceOrigins.ToWireValue(ChatBotSurfaceOrigin.Worker));
    }

    /// <summary>
    /// Builds the metadata-only audit record for a single notification-delivery decision — immediate push or
    /// throttle-to-digest (Story 7.9, NFR46/NFR15a/FR75g). Written pre-commit so the delivery fails closed if audit is
    /// unavailable. Carries safe refs only — tenant/recipient refs, state-class/channel/scope tokens, the throttle
    /// decision token, the rolling-window counter snapshot, and the rolled-up count — never raw item content, recipient
    /// addresses, provider payloads, or secrets. The item ref is included only when the recipient holds per-resource
    /// authority (<see cref="NotificationContentVisibility.ItemContext"/>), mirroring the escalation redaction discipline.
    /// </summary>
    public static AuditEnvelope NotificationDelivered(
        NotificationDelivery delivery,
        NotificationThrottleDecision decision,
        int hourWindowCount,
        int dayWindowCount,
        int rolledUpCount,
        string tenantRef,
        DateTimeOffset timestamp)
    {
        ArgumentNullException.ThrowIfNull(delivery);
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantRef);

        string decisionToken = decision == NotificationThrottleDecision.Deliver ? "delivered" : "digest";

        List<string> refs =
        [
            $"correlation:{delivery.CorrelationId}",
            "admin-operation:notification-delivery",
            $"notification-state-class:{NotificationStateClasses.ToWireValue(delivery.StateClass)}",
            $"notification-channel:{NotificationChannels.ToWireValue(delivery.Channel)}",
            $"admin-scope:{AdminScopes.ToWireValue(delivery.Scope)}",
            $"recipient-role:{AdminRoles.ToWireValue(delivery.RecipientRole)}",
            $"throttle-decision:{decisionToken}",
            $"throttle-window-hour:{hourWindowCount.ToString(System.Globalization.CultureInfo.InvariantCulture)}",
            $"throttle-window-day:{dayWindowCount.ToString(System.Globalization.CultureInfo.InvariantCulture)}",
            $"digest-rolled-up-count:{rolledUpCount.ToString(System.Globalization.CultureInfo.InvariantCulture)}",
        ];

        if (AuditMetadata.SafeOptionalToken(delivery.RecipientRef) is { } safeRecipient)
        {
            refs.Add($"recipient:{safeRecipient}");
        }

        if (AuditMetadata.SafeOptionalToken(delivery.QueueRef) is { } safeQueue)
        {
            refs.Add($"notification-queue:{safeQueue}");
        }

        // Item-specific ref only when the recipient holds per-resource authority (NFR2): a redacted delivery must be
        // indistinguishable from safe-not-found, so item refs never leak into the metadata-redacted form.
        if (delivery.Visibility == NotificationContentVisibility.ItemContext &&
            AuditMetadata.SafeOptionalToken(delivery.ItemRef) is { } safeItem)
        {
            refs.Add($"notification-item:{safeItem}");
        }

        string outcome = decision == NotificationThrottleDecision.Deliver ? "delivered" : "throttled";
        string stateTransition = decision == NotificationThrottleDecision.Deliver ? "Pending->Delivered" : "Pending->Digest";

        return new AuditEnvelope(
            tenantRef,
            "notification-throttle-evaluator",
            "system",
            "NotificationDelivered",
            AuditMetadata.IsSafeStableIdentifier(delivery.QueueRef) ? delivery.QueueRef : "notification",
            Decision: decisionToken,
            ReasonCode: delivery.ReasonCode,
            CorrelationId: delivery.CorrelationId,
            timestamp,
            NoPayloadPolicySnapshotId,
            refs,
            IdempotencyKey: null,
            StateTransition: stateTransition,
            CoarseUserFacingRedactionStage.MetadataOnlyDecision,
            Outcome: outcome,
            AuditCommitPhase.PostCommit,
            EnvelopeSchemaVersion,
            PredecessorHash: null,
            ChatBotSurfaceOrigins.ToWireValue(ChatBotSurfaceOrigin.Worker));
    }

    /// <summary>
    /// Builds the metadata-only audit record for a single fired reviewer-backlog alert (Story 7.10, NFR46/NFR15a/FR75g).
    /// Written pre-commit so the alert fails closed if audit is unavailable. Carries safe refs only — tenant ref, the
    /// reviewer's safe ref, the backlog-depth / oldest-age / threshold counters, the state-class/channel/scope/role
    /// tokens, the reason code, the correlation id, and the UTC timestamp — never raw item content, project names,
    /// evidence, recipient PII, provider payloads, or secrets. The alert is the aggregate redacted form, so it never
    /// carries a per-resource item ref (NFR2 — indistinguishable from safe-not-found).
    /// </summary>
    public static AuditEnvelope ReviewerBacklogAlertFired(
        ReviewerBacklogAlert alert,
        string tenantRef,
        DateTimeOffset timestamp)
    {
        ArgumentNullException.ThrowIfNull(alert);
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantRef);

        NotificationDelivery delivery = alert.Notification;

        List<string> refs =
        [
            $"correlation:{delivery.CorrelationId}",
            "admin-operation:reviewer-backlog-alert-fired",
            $"notification-state-class:{NotificationStateClasses.ToWireValue(delivery.StateClass)}",
            $"notification-channel:{NotificationChannels.ToWireValue(delivery.Channel)}",
            $"admin-scope:{AdminScopes.ToWireValue(delivery.Scope)}",
            $"recipient-role:{AdminRoles.ToWireValue(delivery.RecipientRole)}",
            $"backlog-depth:{alert.BacklogDepth.ToString(System.Globalization.CultureInfo.InvariantCulture)}",
            $"backlog-oldest-age-seconds:{alert.OldestItemAgeSeconds.ToString(System.Globalization.CultureInfo.InvariantCulture)}",
            $"backlog-threshold:{alert.Threshold.ToString(System.Globalization.CultureInfo.InvariantCulture)}",
        ];

        // The reviewer identity is a metadata-safe ref (the same safe-token class as queue-assignee/queue-reviewer) — the
        // subject of the alert, never a leak of an item the recipient lacks authority over.
        if (AuditMetadata.SafeOptionalToken(alert.ReviewerRef) is { } safeReviewer)
        {
            refs.Add($"reviewer:{safeReviewer}");
        }

        if (AuditMetadata.SafeOptionalToken(delivery.RecipientRef) is { } safeRecipient)
        {
            refs.Add($"recipient:{safeRecipient}");
        }

        // The alert is always the aggregate MetadataRedacted form, so no per-resource item ref is ever emitted — the
        // audit trail stays indistinguishable from safe-not-found (NFR2) and never becomes a covert channel.
        return new AuditEnvelope(
            tenantRef,
            "reviewer-backlog-evaluator",
            "system",
            "ReviewerBacklogAlertFired",
            AuditMetadata.IsSafeStableIdentifier(delivery.QueueRef) ? delivery.QueueRef : "reviewer-backlog",
            Decision: "alert",
            ReasonCode: delivery.ReasonCode,
            CorrelationId: delivery.CorrelationId,
            timestamp,
            NoPayloadPolicySnapshotId,
            refs,
            IdempotencyKey: null,
            StateTransition: "Open->BacklogAlerted",
            CoarseUserFacingRedactionStage.MetadataOnlyDecision,
            Outcome: "alerted",
            AuditCommitPhase.PostCommit,
            EnvelopeSchemaVersion,
            PredecessorHash: null,
            ChatBotSurfaceOrigins.ToWireValue(ChatBotSurfaceOrigin.Worker));
    }

    /// <summary>
    /// Builds the metadata-only, pre-commit audit record for a single fired NFR43 operational alert (Story 8.4,
    /// AC7/NFR15a/NFR42/NFR2). Written pre-commit so the alert fails closed if audit is unavailable. Carries safe
    /// bounded tokens only — the tenant ref, the alert-kind/reason-code/owner-role/next-action/affected-scope tokens,
    /// and the correlation id — never project names, evidence, file metadata, actor PII, authorization-claim detail,
    /// or secrets. One envelope per fired alert.
    /// </summary>
    public static AuditEnvelope OperationalAlertFired(
        OperationalAlertPayload alert,
        DateTimeOffset timestamp)
    {
        ArgumentNullException.ThrowIfNull(alert);

        // The affected scope can carry a single space separating safe components (e.g. "tenant:t mailbox:m"); audit
        // refs are colon-delimited tokens, so the separator is folded to the safe '|' token character.
        string safeScope = alert.AffectedScope.Replace(' ', '|');

        List<string> refs =
        [
            $"correlation:{alert.CorrelationId}",
            "admin-operation:operational-alert-fired",
            $"operational-alert-kind:{OperationalAlertPayload.AlertKindWireValue(alert.AlertKind)}",
            $"operational-alert-reason:{alert.ReasonCode}",
            $"operational-alert-owner-role:{alert.OwnerRole}",
            $"operational-alert-next-action:{alert.NextSafeAction}",
            $"operational-alert-scope:{safeScope}",
        ];

        return new AuditEnvelope(
            alert.TenantRef,
            "operational-alert-evaluator",
            "system",
            "OperationalAlertFired",
            OperationalAlertPayload.AlertKindWireValue(alert.AlertKind),
            Decision: "alert",
            ReasonCode: alert.ReasonCode,
            CorrelationId: alert.CorrelationId,
            timestamp,
            NoPayloadPolicySnapshotId,
            refs,
            IdempotencyKey: null,
            StateTransition: "Open->Alerted",
            CoarseUserFacingRedactionStage.MetadataOnlyDecision,
            Outcome: "alerted",
            AuditCommitPhase.PreCommit,
            EnvelopeSchemaVersion,
            PredecessorHash: null,
            ChatBotSurfaceOrigins.ToWireValue(ChatBotSurfaceOrigin.Worker));
    }

    /// <summary>
    /// Builds the metadata-only audit record for a fired FR41 approval-tuning revisit (Story 7.11, NFR46/NFR15a/FR75g).
    /// Written pre-commit so the revisit fails closed if audit is unavailable. Carries safe refs only — tenant ref, the
    /// exact rubber-stamp-count / approval-total pair (plus the integer-floor permille for convenience), the three fixed
    /// governance constants (rubber-stamp latency 5 s, fatigue fraction 15 %, rolling window 7 d), the
    /// <c>approval-required</c> risk-class scope, the per-reviewer diagnosis breakdown (safe reviewer refs + counts), the
    /// reason code, the correlation id, and the UTC timestamp — never project/proposal content, command bodies, evidence,
    /// recipient PII, prompts, provider payloads, claims, headers, tokens, or secrets. The rate is exact (count + total),
    /// never a lossy float. Mirrors <see cref="ReviewerBacklogAlertFired"/> / <see cref="NotificationDelivered"/> /
    /// <see cref="EscalationFired"/>.
    /// </summary>
    public static AuditEnvelope ApprovalTuningRevisitTriggered(
        ApprovalRubberStampRateObservation observation,
        string tenantRef,
        DateTimeOffset timestamp)
    {
        ArgumentNullException.ThrowIfNull(observation);
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantRef);

        List<string> refs =
        [
            $"correlation:{observation.CorrelationId}",
            "admin-operation:approval-tuning-revisit-triggered",
            "risk-class:approval-required",
            $"rubber-stamp-count:{observation.RubberStampCount.ToString(System.Globalization.CultureInfo.InvariantCulture)}",
            $"approval-total:{observation.ApprovalTotal.ToString(System.Globalization.CultureInfo.InvariantCulture)}",
            $"rubber-stamp-rate-permille:{observation.RubberStampRatePermille.ToString(System.Globalization.CultureInfo.InvariantCulture)}",
            $"rubber-stamp-latency-seconds:{RubberStampRateObservable.RubberStampLatencySeconds.ToString(System.Globalization.CultureInfo.InvariantCulture)}",
            $"fatigue-fraction-percent:{RubberStampRateObservable.FatigueFractionPercent.ToString(System.Globalization.CultureInfo.InvariantCulture)}",
            $"rolling-window-days:{RubberStampRateObservable.RollingWindowDays.ToString(System.Globalization.CultureInfo.InvariantCulture)}",
        ];

        // Per-reviewer diagnosis breakdown: the reviewer identity is a metadata-safe ref (the same safe-token class as
        // queue-reviewer), carried with its exact rubber-stamp / qualifying-approval counts — never an item/project leak.
        foreach (ReviewerRubberStampRate reviewer in observation.PerReviewer)
        {
            if (AuditMetadata.SafeOptionalToken(reviewer.ReviewerRef) is { } safeReviewer)
            {
                refs.Add($"reviewer-rubber-stamp:{safeReviewer}:{reviewer.RubberStampCount.ToString(System.Globalization.CultureInfo.InvariantCulture)}:{reviewer.ApprovalTotal.ToString(System.Globalization.CultureInfo.InvariantCulture)}");
            }
        }

        return new AuditEnvelope(
            tenantRef,
            "rubber-stamp-rate-evaluator",
            "system",
            "ApprovalTuningRevisitTriggered",
            "approval-tuning-revisit",
            Decision: "revisit-triggered",
            ReasonCode: ApprovalRubberStampRateEvaluator.TuningRevisitReasonCode,
            CorrelationId: observation.CorrelationId,
            timestamp,
            NoPayloadPolicySnapshotId,
            refs,
            IdempotencyKey: null,
            StateTransition: "Observed->TuningRevisitTriggered",
            CoarseUserFacingRedactionStage.MetadataOnlyDecision,
            Outcome: "revisit-triggered",
            AuditCommitPhase.PostCommit,
            EnvelopeSchemaVersion,
            PredecessorHash: null,
            ChatBotSurfaceOrigins.ToWireValue(ChatBotSurfaceOrigin.Worker));
    }

    /// <summary>
    /// Builds the metadata-only, pre-commit audit record for a detected WORM-chain breach (Story 9.1, AC2/NFR49a). It
    /// is written pre-commit so the broken-chain alert fails closed if audit is unavailable (audit-then-deliver). Carries
    /// safe bounded tokens only — the tenant ref, the chain status, the reason code, the first-break locator token, and
    /// the correlation id — never envelope content, hash bytes, or secrets. One envelope per detected breach.
    /// </summary>
    public static AuditEnvelope AuditChainBroken(
        WormAuditChainVerificationResult result,
        string correlationId,
        DateTimeOffset timestamp)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);

        string statusToken = result.Status == WormChainVerificationStatus.Unknown ? "unknown" : "broken";

        List<string> refs =
        [
            $"correlation:{correlationId}",
            "admin-operation:audit-chain-verification",
            $"worm-chain-status:{statusToken}",
            $"worm-chain-reason:{result.ReasonCode}",
        ];

        if (AuditMetadata.SafeOptionalToken(result.FirstBreakLocator) is { } safeLocator)
        {
            refs.Add($"worm-chain-first-break:{safeLocator}");
        }

        return new AuditEnvelope(
            result.TenantRef,
            "worm-audit-chain-verifier",
            "system",
            "AuditChainBroken",
            "worm-audit-chain",
            Decision: "alert",
            ReasonCode: result.ReasonCode,
            CorrelationId: correlationId,
            timestamp,
            NoPayloadPolicySnapshotId,
            refs,
            IdempotencyKey: null,
            StateTransition: "Verified->Broken",
            CoarseUserFacingRedactionStage.MetadataOnlyDecision,
            Outcome: "chain_broken",
            AuditCommitPhase.PreCommit,
            EnvelopeSchemaVersion,
            PredecessorHash: null,
            ChatBotSurfaceOrigins.ToWireValue(ChatBotSurfaceOrigin.Worker));
    }

    /// <summary>
    /// Builds the metadata-only, pre-commit audit record for an audit-completeness budget breach (Story 9.2,
    /// AC2/NFR50a). Written pre-commit so the breach alert fails closed if audit is unavailable (audit-then-deliver).
    /// Carries safe bounded tokens only — the tenant ref, the explicit <b>P1</b> severity, the coarse budget state, the
    /// measurement status (measured/unmeasurable), the fraction in permille (only when measurable — never a fabricated
    /// value for an unmeasurable run), the rolling-window length, the safe first-diverging-operation locator, and the
    /// correlation id — never operation ids, prompts, recipient PII, or payloads. One envelope per breaching tenant.
    /// </summary>
    public static AuditEnvelope AuditCompletenessBudgetBreached(
        AuditCompletenessMeasurement measurement,
        ErrorBudgetBurnState budgetState,
        string correlationId,
        DateTimeOffset timestamp)
    {
        ArgumentNullException.ThrowIfNull(measurement);
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);

        string budgetToken = budgetState switch
        {
            ErrorBudgetBurnState.Exhausted => "exhausted",
            ErrorBudgetBurnState.Unknown => "unknown",
            ErrorBudgetBurnState.Approaching => "approaching",
            _ => "within-budget",
        };

        List<string> refs =
        [
            $"correlation:{correlationId}",
            "admin-operation:audit-completeness-measurement",
            "audit-completeness-severity:p1",
            $"audit-completeness-budget:{budgetToken}",
            $"audit-completeness-status:{(measurement.IsMeasurable ? "measured" : "unmeasurable")}",
            $"audit-completeness-window-days:{((int)AuditCompletenessMeasurement.RollingWindow.TotalDays).ToString(System.Globalization.CultureInfo.InvariantCulture)}",
        ];

        // The fraction is emitted only when the run actually measured one — an unmeasurable run carries NO fabricated
        // value (Epic 8 no-fabrication). Coarse permille keeps it a bounded safe token, never a high-precision float.
        if (measurement.IsMeasurable)
        {
            int permille = (int)Math.Round(measurement.Fraction * 1000, MidpointRounding.AwayFromZero);
            refs.Add($"audit-completeness-fraction-permille:{permille.ToString(System.Globalization.CultureInfo.InvariantCulture)}");
        }

        if (AuditMetadata.SafeOptionalToken(measurement.FirstDivergingOperationLocator) is { } safeLocator)
        {
            refs.Add($"audit-completeness-first-diverging:{safeLocator}");
        }

        return new AuditEnvelope(
            measurement.TenantRef,
            "audit-completeness-measurer",
            "system",
            "AuditCompletenessBudgetBreached",
            "audit-completeness",
            Decision: "alert",
            ReasonCode: measurement.ReasonCode,
            CorrelationId: correlationId,
            timestamp,
            NoPayloadPolicySnapshotId,
            refs,
            IdempotencyKey: null,
            StateTransition: "WithinBudget->Breached",
            CoarseUserFacingRedactionStage.MetadataOnlyDecision,
            Outcome: "completeness_budget_breached",
            AuditCommitPhase.PreCommit,
            EnvelopeSchemaVersion,
            PredecessorHash: null,
            ChatBotSurfaceOrigins.ToWireValue(ChatBotSurfaceOrigin.Worker));
    }

    /// <summary>
    /// Builds the metadata-only, pre-commit audit record for a detected replay-isolation breach (Story 9.4, AC3/FR95a).
    /// It is written pre-commit so the breach alert fails closed if audit is unavailable (audit-then-deliver). Carries
    /// safe bounded tokens only — the production tenant ref, the breach status, the reason code (trace-side vs chain-side),
    /// the first-offender locator token, and the correlation id — never trace/envelope content. One envelope per breached
    /// production tenant. The envelope itself is a system record and stays production (its own ReplayRunId is null).
    /// </summary>
    public static AuditEnvelope ReplayIsolationBreach(
        ReplayIsolationVerificationResult result,
        string correlationId,
        DateTimeOffset timestamp)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);

        string statusToken = result.Status == ReplayIsolationStatus.Unknown ? "unknown" : "breach";

        List<string> refs =
        [
            $"correlation:{correlationId}",
            "admin-operation:replay-isolation-probe",
            "replay-isolation-severity:stop-ship",
            $"replay-isolation-status:{statusToken}",
            $"replay-isolation-reason:{result.ReasonCode}",
        ];

        if (AuditMetadata.SafeOptionalToken(result.FirstOffenderLocator) is { } safeLocator)
        {
            refs.Add($"replay-isolation-first-offender:{safeLocator}");
        }

        return new AuditEnvelope(
            result.TenantRef,
            "replay-isolation-probe",
            "system",
            "ReplayIsolationBreach",
            "replay-isolation",
            Decision: "alert",
            ReasonCode: result.ReasonCode,
            CorrelationId: correlationId,
            timestamp,
            NoPayloadPolicySnapshotId,
            refs,
            IdempotencyKey: null,
            StateTransition: "Clean->Breach",
            CoarseUserFacingRedactionStage.MetadataOnlyDecision,
            Outcome: "replay_isolation_breach",
            AuditCommitPhase.PreCommit,
            EnvelopeSchemaVersion,
            PredecessorHash: null,
            ChatBotSurfaceOrigins.ToWireValue(ChatBotSurfaceOrigin.Worker));
    }

    /// <summary>
    /// Builds the metadata-only, pre-commit audit record for a detected derived-store cross-tenant isolation breach
    /// (Story 9.5, AC2/FR55a/NFR9a). Written pre-commit so the breach alert fails closed if audit is unavailable
    /// (audit-then-deliver). Carries safe bounded tokens only — the owner tenant ref, the intruder tenant ref, the breach
    /// status, the reason code, the safe first-offender (leaked-sentinel) locator, and the correlation id — never
    /// derived-store content (vectors, embeddings, prompt text, candidate payloads). One envelope per breached ordered
    /// tenant pair. The envelope itself is a system record and stays production (its own ReplayRunId is null).
    /// </summary>
    public static AuditEnvelope DerivedStoreIsolationBreach(
        DerivedStoreIsolationVerificationResult result,
        string correlationId,
        DateTimeOffset timestamp)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);

        string statusToken = result.Status == DerivedStoreIsolationStatus.Unknown ? "unknown" : "breach";

        List<string> refs =
        [
            $"correlation:{correlationId}",
            "admin-operation:derived-store-isolation-probe",
            "derived-store-isolation-severity:stop-ship",
            $"derived-store-isolation-status:{statusToken}",
            $"derived-store-isolation-reason:{result.ReasonCode}",
        ];

        if (AuditMetadata.SafeOptionalToken(result.IntruderTenantRef) is { } safeIntruder)
        {
            refs.Add($"derived-store-isolation-intruder:{safeIntruder}");
        }

        if (AuditMetadata.SafeOptionalToken(result.FirstOffenderLocator) is { } safeLocator)
        {
            refs.Add($"derived-store-isolation-first-offender:{safeLocator}");
        }

        return new AuditEnvelope(
            result.OwnerTenantRef,
            "derived-store-isolation-probe",
            "system",
            "DerivedStoreIsolationBreach",
            "derived-store-isolation",
            Decision: "alert",
            ReasonCode: result.ReasonCode,
            CorrelationId: correlationId,
            timestamp,
            NoPayloadPolicySnapshotId,
            refs,
            IdempotencyKey: null,
            StateTransition: "Isolated->Breach",
            CoarseUserFacingRedactionStage.MetadataOnlyDecision,
            Outcome: "derived_store_isolation_breach",
            AuditCommitPhase.PreCommit,
            EnvelopeSchemaVersion,
            PredecessorHash: null,
            ChatBotSurfaceOrigins.ToWireValue(ChatBotSurfaceOrigin.Worker));
    }

    /// <summary>
    /// Builds the metadata-only, pre-commit audit record for a continuity drill that missed an RPO/RTO target or could
    /// not complete (Story 9.11, AC4/NFR56/A10). Written pre-commit so the breach alert fails closed if audit is
    /// unavailable (audit-then-deliver), exactly like <see cref="DerivedStoreIsolationBreach"/>. Carries safe bounded
    /// tokens only — the test-tenant ref, the scenario/verdict/reason tokens, integer-second durations, boolean flags,
    /// the bounded deviation tokens, and the safe follow-up locator — never raw item content, recipient PII, prompts, or
    /// payloads. One envelope per breached drill (a miss or an unmeasurable drill). The envelope is a system record.
    /// </summary>
    public static AuditEnvelope ContinuityDrillTargetMissed(
        ContinuityDrillReport report,
        string correlationId,
        DateTimeOffset timestamp)
    {
        ArgumentNullException.ThrowIfNull(report);
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);

        List<string> refs =
        [
            $"correlation:{correlationId}",
            "admin-operation:continuity-drill",
            $"continuity-drill-scenario:{report.Scenario}",
            $"continuity-drill-verdict:{report.Verdict}",
            $"continuity-drill-reason:{report.ReasonCode}",
            $"continuity-drill-rpo-seconds:{(long)report.MeasuredRpo.TotalSeconds}",
            $"continuity-drill-rto-seconds:{(long)report.MeasuredRto.TotalSeconds}",
            $"continuity-drill-data-loss:{report.DataLossDetected}",
            $"continuity-drill-recalibration:{report.RecalibrationFlag}",
        ];

        foreach (string deviation in report.Deviations)
        {
            refs.Add($"continuity-drill-deviation:{deviation}");
        }

        if (AuditMetadata.SafeOptionalToken(report.FollowUpActionRef) is { } safeFollowUp)
        {
            refs.Add($"continuity-drill-follow-up:{safeFollowUp}");
        }

        return new AuditEnvelope(
            report.TenantRef,
            "continuity-drill",
            "system",
            "ContinuityDrillTargetMissed",
            "continuity-drill",
            Decision: "alert",
            ReasonCode: report.ReasonCode,
            CorrelationId: correlationId,
            timestamp,
            NoPayloadPolicySnapshotId,
            refs,
            IdempotencyKey: null,
            StateTransition: "Recovered->TargetMissed",
            CoarseUserFacingRedactionStage.MetadataOnlyDecision,
            Outcome: "continuity_drill_target_missed",
            AuditCommitPhase.PreCommit,
            EnvelopeSchemaVersion,
            PredecessorHash: null,
            ChatBotSurfaceOrigins.ToWireValue(ChatBotSurfaceOrigin.Worker));
    }

    /// <summary>
    /// Builds the metadata-only, pre-commit audit record for a projection-rebuild validation that diverged, missed the
    /// 4-hr rebuild target, or could not complete (Story 9.12, AC4/NFR57/NFR49a). Written pre-commit so the breach alert
    /// fails closed if audit is unavailable (audit-then-deliver), exactly like <see cref="ContinuityDrillTargetMissed"/>.
    /// Carries safe bounded tokens only — the test-tenant ref, the dataset/verdict/reason tokens, the integer-second
    /// rebuild duration, the within-target/resources-compared/schema-version values, the bounded deviation tokens, and
    /// the safe first-diverging locator — never raw item content, recipient PII, prompts, or payloads. One envelope per
    /// breached validation (a divergence, a duration overrun, or an unmeasurable validation). The envelope is a system record.
    /// </summary>
    public static AuditEnvelope ProjectionRebuildValidationFailed(
        ProjectionRebuildReport report,
        string correlationId,
        DateTimeOffset timestamp)
    {
        ArgumentNullException.ThrowIfNull(report);
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);

        List<string> refs =
        [
            $"correlation:{correlationId}",
            "admin-operation:projection-rebuild-validation",
            $"projection-rebuild-dataset:{report.DatasetRef}",
            $"projection-rebuild-verdict:{report.Verdict}",
            $"projection-rebuild-reason:{report.ReasonCode}",
            $"projection-rebuild-duration-seconds:{(long)report.MeasuredRebuildDuration.TotalSeconds}",
            $"projection-rebuild-within-target:{report.DurationWithinTarget}",
            $"projection-rebuild-resources-compared:{report.ResourcesCompared}",
            $"projection-rebuild-schema-version:{report.ProjectionSchemaVersion}",
        ];

        foreach (string deviation in report.Deviations)
        {
            refs.Add($"projection-rebuild-deviation:{deviation}");
        }

        if (AuditMetadata.SafeOptionalToken(report.FirstDivergingResourceLocator) is { } safeFirstDiverging)
        {
            refs.Add($"projection-rebuild-first-diverging:{safeFirstDiverging}");
        }

        return new AuditEnvelope(
            report.TenantRef,
            "projection-rebuild-validation",
            "system",
            "ProjectionRebuildValidationFailed",
            "projection-rebuild",
            Decision: "alert",
            ReasonCode: report.ReasonCode,
            CorrelationId: correlationId,
            timestamp,
            NoPayloadPolicySnapshotId,
            refs,
            IdempotencyKey: null,
            StateTransition: "Rebuilt->ValidationFailed",
            CoarseUserFacingRedactionStage.MetadataOnlyDecision,
            Outcome: "projection_rebuild_validation_failed",
            AuditCommitPhase.PreCommit,
            EnvelopeSchemaVersion,
            PredecessorHash: null,
            ChatBotSurfaceOrigins.ToWireValue(ChatBotSurfaceOrigin.Worker));
    }

    /// <summary>
    /// Builds the metadata-only, pre-commit audit record for a correction-propagation SLO delay (Story 9.6, AC2,
    /// NFR17a). Written pre-commit so the delay alert fails closed if audit is unavailable (audit-then-deliver), exactly
    /// like <see cref="DerivedStoreIsolationBreach"/>. Carries safe bounded tokens only — the tenant ref, the explicit
    /// <b>P2</b> severity marker (the P2 incident linkage, mirroring Story 9.2's <c>audit-completeness-severity:p1</c>),
    /// the responsible owner role, the next safe action, the reason code (e.g. <c>vector_reindex_slo_exceeded</c> /
    /// <c>vector_reindex_failed</c> / <c>m0_store_invalidation_failed</c>), the safe association/correction locators, and
    /// the correlation id — never operation ids, prompts, recipient PII, vectors, embeddings, prompt text, or candidate
    /// payloads. One envelope per delayed correction.
    /// </summary>
    public static AuditEnvelope CorrectionPropagationDelayed(
        string tenantRef,
        string associationLocator,
        string correctionLocator,
        string reasonCode,
        string ownerRole,
        string nextSafeAction,
        string correlationId,
        DateTimeOffset timestamp)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantRef);
        ArgumentException.ThrowIfNullOrWhiteSpace(reasonCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerRole);
        ArgumentException.ThrowIfNullOrWhiteSpace(nextSafeAction);
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);

        List<string> refs =
        [
            $"correlation:{correlationId}",
            "admin-operation:correction-propagation",
            "correction-propagation-severity:p2",
            $"correction-propagation-owner:{AuditMetadata.SafeOptionalToken(ownerRole) ?? "operations"}",
            $"correction-propagation-next-action:{AuditMetadata.SafeOptionalToken(nextSafeAction) ?? "escalate-to-operations"}",
            $"correction-propagation-reason:{AuditMetadata.SafeOptionalToken(reasonCode) ?? "correction_delayed"}",
        ];

        if (AuditMetadata.SafeOptionalToken(associationLocator) is { } safeAssociation)
        {
            refs.Add($"correction-propagation-association:{safeAssociation}");
        }

        if (AuditMetadata.SafeOptionalToken(correctionLocator) is { } safeCorrection)
        {
            refs.Add($"correction-propagation-correction:{safeCorrection}");
        }

        return new AuditEnvelope(
            tenantRef,
            "correction-propagation-coordinator",
            "system",
            "CorrectionPropagationDelayed",
            "correction-propagation",
            Decision: "alert",
            ReasonCode: reasonCode,
            CorrelationId: correlationId,
            timestamp,
            NoPayloadPolicySnapshotId,
            refs,
            IdempotencyKey: null,
            StateTransition: "Propagating->Delayed",
            CoarseUserFacingRedactionStage.MetadataOnlyDecision,
            Outcome: "correction_propagation_delayed",
            AuditCommitPhase.PreCommit,
            EnvelopeSchemaVersion,
            PredecessorHash: null,
            ChatBotSurfaceOrigins.ToWireValue(ChatBotSurfaceOrigin.Worker));
    }

    /// <summary>
    /// Builds the metadata-only audit record for an appended GDPR redaction record (Story 9.1, AC3/NFR49a). The
    /// redaction is itself a normal chained append: it advances the chain and references the redacted record by safe
    /// locator token, the redaction reason code, and the redaction-key handle — never the original content (which lives
    /// only as opaque ciphertext under a separate KMS key). One envelope per redaction.
    /// </summary>
    public static AuditEnvelope AuditRecordRedacted(
        string tenantRef,
        string redactedRecordLocator,
        string subjectRef,
        string redactionKeyHandle,
        string reasonCode,
        string correlationId,
        DateTimeOffset timestamp)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantRef);
        ArgumentException.ThrowIfNullOrWhiteSpace(reasonCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);

        List<string> refs =
        [
            $"correlation:{correlationId}",
            "admin-operation:audit-record-redacted",
            $"redaction-reason:{reasonCode}",
        ];

        if (AuditMetadata.SafeOptionalToken(redactedRecordLocator) is { } safeLocator)
        {
            refs.Add($"redacted-record:{safeLocator}");
        }

        if (AuditMetadata.SafeOptionalToken(subjectRef) is { } safeSubject)
        {
            refs.Add($"redaction-subject:{safeSubject}");
        }

        if (AuditMetadata.SafeOptionalToken(redactionKeyHandle) is { } safeHandle)
        {
            refs.Add($"redaction-key:{safeHandle}");
        }

        return new AuditEnvelope(
            tenantRef,
            "audit-redaction-service",
            "system",
            "AuditRecordRedacted",
            "audit-redaction",
            Decision: "redact",
            ReasonCode: reasonCode,
            CorrelationId: correlationId,
            timestamp,
            NoPayloadPolicySnapshotId,
            refs,
            IdempotencyKey: null,
            StateTransition: "Recorded->Redacted",
            CoarseUserFacingRedactionStage.MetadataOnlyDecision,
            Outcome: "redacted",
            AuditCommitPhase.PostCommit,
            EnvelopeSchemaVersion,
            PredecessorHash: null,
            ChatBotSurfaceOrigins.ToWireValue(ChatBotSurfaceOrigin.Worker));
    }

    private static AuditEnvelope Create(
        ChatBotGatewayContext context,
        DateTimeOffset timestamp,
        AuditCommitPhase phase,
        string decision,
        string reasonCode,
        string stateTransition,
        string outcome,
        string? resourceId = null)
    {
        ArgumentNullException.ThrowIfNull(context);

        string commandName = CommandName(context);

        // The post-commit envelope references the durable aggregate identity (the dispatched NoteId) when the
        // dispatcher resolved one; pre-commit / rejection envelopes have no aggregate yet and fall back to the
        // command id. The value is still a safe, metadata-only ULID token (no payload).
        string auditedResourceId = AuditMetadata.IsSafeStableIdentifier(resourceId)
            ? resourceId!
            : context.Submission.Request.CommandId;

        return new AuditEnvelope(
            context.TenantBinding.TenantId,
            context.Actor.ActorId,
            ActorType(context),
            commandName,
            auditedResourceId,
            decision,
            reasonCode,
            context.Submission.CorrelationId,
            timestamp,
            NoPayloadPolicySnapshotId,
            SourceEvidenceRefs(context, phase),
            AuditMetadata.SafeOptionalToken(IdempotencyKey(context)),
            stateTransition,
            CoarseUserFacingRedactionStage.MetadataOnlyDecision,
            outcome,
            phase,
            EnvelopeSchemaVersion,
            PredecessorHash: null,
            ChatBotSurfaceOrigins.ToWireValue(context.Submission.Origin),
            // Story 9.4 (FR95a): this single line populates the replay marker for the ENTIRE command path. Because every
            // public command-path factory method (pre-commit, post-commit, duplicate-suppression, rejection,
            // escalation) funnels through Create, a replay run's submission marks all of its envelopes here in one place;
            // a production submission leaves Submission.ReplayRunId null, so the marker stays null by omission. The
            // marker is the same AuditMetadata-safe bounded token discipline as every other Epic 9 field, and it is
            // covered by the v2 canonical hash (Story 9.2) so a replay record is tamper-evidently distinct from a
            // production one. The non-Create system/operator factories (AuditChainBroken, AuditCompletenessBudgetBreached,
            // AuditRecordRedacted, ReplayIsolationBreach, the operational-alert factories) deliberately leave it null —
            // they are out-of-band system envelopes, not command-path records of a replay run.
            ReplayRunId: AuditMetadata.SafeOptionalToken(context.Submission.ReplayRunId));
    }

    private static string CommandName(ChatBotGatewayContext context)
    {
        string? runtimeTypeName = context.Submission.Request.Command?.GetType().Name;
        if (!string.IsNullOrWhiteSpace(runtimeTypeName) &&
            !string.Equals(runtimeTypeName, "JsonElement", StringComparison.Ordinal))
        {
            return AuditMetadata.SafeCommandName(runtimeTypeName);
        }

        return AuditMetadata.SafeCommandName(context.Submission.Request.CommandType);
    }

    private static string ActorType(ChatBotGatewayContext context)
    {
        string? actorType = context.Actor.Principal.Claims
            .FirstOrDefault(static claim => string.Equals(claim.Type, ParticipantAuthorizationStage.ActorTypeClaim, StringComparison.Ordinal))?
            .Value;

        actorType ??= context.Actor.Principal.Claims
            .FirstOrDefault(static claim => string.Equals(claim.Type, "actor_type", StringComparison.Ordinal))?
            .Value;

        return AuditMetadata.SafeActorType(actorType ?? context.Actor.ActorType);
    }

    private static IReadOnlyList<string> SourceEvidenceRefs(ChatBotGatewayContext context, AuditCommitPhase phase)
    {
        List<string> refs =
        [
            $"command:{context.Submission.Request.CommandId}",
            $"correlation:{context.Submission.CorrelationId}",
            $"phase:{PhaseName(phase)}",
        ];

        refs.AddRange(AssociationDecisionEvidenceRefs(context));
        refs.AddRange(AssociationScoringEvidenceRefs(context));
        refs.AddRange(AssociationCorrectionEvidenceRefs(context));
        refs.AddRange(AiActionClassificationEvidenceRefs(context));
        refs.AddRange(LowRiskAiAssistanceEvidenceRefs(context));
        refs.AddRange(ApprovalDecisionEvidenceRefs(context));
        refs.AddRange(ApprovedAiActionExecutionEvidenceRefs(context));
        refs.AddRange(OutboundDraftEvidenceRefs(context));
        refs.AddRange(OutboundApprovalEvidenceRefs(context));
        refs.AddRange(OutboundSendEvidenceRefs(context));
        refs.AddRange(MailboxIntakeEvidenceRefs(context));
        refs.AddRange(AdminEvidenceRefs(context));
        refs.AddRange(ServiceClientGrantEvidenceRefs(context));
        return refs;
    }

    private static string PhaseName(AuditCommitPhase phase)
        => phase switch
        {
            AuditCommitPhase.PreCommit => "pre-commit",
            AuditCommitPhase.PostCommit => "post-commit",
            _ => throw new ArgumentOutOfRangeException(nameof(phase), phase, "Unsupported audit phase."),
        };

    private static string? IdempotencyKey(ChatBotGatewayContext context)
        => context.Idempotency?.CoarseKeyHash;

    private static IEnumerable<string> AssociationDecisionEvidenceRefs(ChatBotGatewayContext context)
    {
        string commandType = context.Submission.Request.CommandType ?? string.Empty;
        if (commandType is not (nameof(AssociateEmailToProject)
            or nameof(RejectEmailProjectAssociation)
            or nameof(DeferEmailProjectAssociation)
            or nameof(MarkEmailAssociationNeedsReview)))
        {
            yield break;
        }

        JsonElement element = context.Submission.Request.Command is JsonElement json
            ? json
            : JsonSerializer.SerializeToElement(context.Submission.Request.Command, new JsonSerializerOptions(JsonSerializerDefaults.Web));

        if (TryReadString(element, "decisionKind", out string? decisionKind))
        {
            yield return $"decision-kind:{AuditMetadata.SafeOptionalToken(decisionKind)}";
        }

        if (TryReadString(element, "candidateEvidenceFingerprint", out string? fingerprint))
        {
            yield return $"evidence-fingerprint:{AuditMetadata.SafeOptionalToken(fingerprint)}";
        }

        if (TryReadString(element, "associationId", out string? associationId))
        {
            yield return $"association:{AuditMetadata.SafeOptionalToken(associationId)}";
        }
    }

    private static IEnumerable<string> AssociationCorrectionEvidenceRefs(ChatBotGatewayContext context)
    {
        string commandType = context.Submission.Request.CommandType ?? string.Empty;
        if (!string.Equals(commandType, nameof(CorrectEmailProjectAssociation), StringComparison.Ordinal))
        {
            yield break;
        }

        JsonElement element = context.Submission.Request.Command is JsonElement json
            ? json
            : JsonSerializer.SerializeToElement(context.Submission.Request.Command, new JsonSerializerOptions(JsonSerializerDefaults.Web));

        if (TryReadString(element, "correctionKind", out string? correctionKind))
        {
            yield return $"correction-kind:{AuditMetadata.SafeOptionalToken(correctionKind)}";
        }

        if (TryReadString(element, "candidateEvidenceFingerprint", out string? fingerprint))
        {
            yield return $"evidence-fingerprint:{AuditMetadata.SafeOptionalToken(fingerprint)}";
        }

        if (TryReadString(element, "associationId", out string? associationId))
        {
            yield return $"association:{AuditMetadata.SafeOptionalToken(associationId)}";
        }

        if (TryReadString(element, "predecessorAssociationId", out string? predecessorAssociationId))
        {
            yield return $"predecessor-association:{AuditMetadata.SafeOptionalToken(predecessorAssociationId)}";
        }

        if (TryReadString(element, "priorProjectId", out string? priorProjectId))
        {
            yield return $"prior-project:{AuditMetadata.SafeOptionalToken(priorProjectId)}";
        }

        if (TryReadString(element, "targetProjectId", out string? targetProjectId))
        {
            yield return $"corrected-project:{AuditMetadata.SafeOptionalToken(targetProjectId)}";
        }

        if (TryReadInt64(element, "sourceVersion", out long sourceVersion))
        {
            long propagationSourceVersion = sourceVersion + 1;
            yield return $"correction-source-version:{propagationSourceVersion.ToString(System.Globalization.CultureInfo.InvariantCulture)}";
            if (TryReadString(element, "associationId", out string? correctionAssociationId))
            {
                yield return $"correction-id:{AuditMetadata.SafeOptionalToken($"{correctionAssociationId}:correction:{propagationSourceVersion.ToString(System.Globalization.CultureInfo.InvariantCulture)}")}";
            }
        }
    }

    private static IEnumerable<string> AssociationScoringEvidenceRefs(ChatBotGatewayContext context)
    {
        string commandType = context.Submission.Request.CommandType ?? string.Empty;
        if (!string.Equals(commandType, nameof(ScoreMailboxMessageAssociation), StringComparison.Ordinal))
        {
            yield break;
        }

        JsonElement element = context.Submission.Request.Command is JsonElement json
            ? json
            : JsonSerializer.SerializeToElement(context.Submission.Request.Command, new JsonSerializerOptions(JsonSerializerDefaults.Web));

        if (element.TryGetProperty("externalSender", out JsonElement externalSender) &&
            externalSender.ValueKind == JsonValueKind.Object)
        {
            if (TryReadBool(externalSender, "externalSender", out bool isExternal))
            {
                yield return $"external-sender:{isExternal.ToString().ToLowerInvariant()}";
            }

            if (TryReadString(externalSender, "partyResolutionState", out string? state))
            {
                yield return $"party-resolution:{AuditMetadata.SafeOptionalToken(state)}";
            }
        }

        if (element.TryGetProperty("strictnessPolicy", out JsonElement strictnessPolicy) &&
            strictnessPolicy.ValueKind == JsonValueKind.Object)
        {
            if (TryReadString(strictnessPolicy, "strictness", out string? strictness))
            {
                yield return $"authenticity-strictness:{AuditMetadata.SafeOptionalToken(strictness)}";
            }

            if (TryReadString(strictnessPolicy, "reasonCode", out string? reason))
            {
                yield return $"authenticity-strictness-reason:{AuditMetadata.SafeOptionalToken(reason)}";
            }
        }

        if (element.TryGetProperty("result", out JsonElement result) &&
            result.ValueKind == JsonValueKind.Object &&
            TryReadString(result, "routingReason", out string? routing))
        {
            yield return $"routing:{AuditMetadata.SafeOptionalToken(routing)}";
        }
    }

    private static IEnumerable<string> AiActionClassificationEvidenceRefs(ChatBotGatewayContext context)
    {
        if (context.RiskClassification?.Record is not { } classification)
        {
            yield break;
        }

        yield return $"classifier:{AuditMetadata.SafeOptionalToken(classification.ClassifierVersion)}";
        yield return $"risk-class:{AuditMetadata.SafeOptionalToken(RiskClassToken(classification.RiskClass))}";
        yield return $"reason:{AuditMetadata.SafeOptionalToken(classification.ReasonCode)}";

        if (!string.IsNullOrWhiteSpace(classification.PolicySnapshotId))
        {
            yield return $"policy-snapshot:{AuditMetadata.SafeOptionalToken(classification.PolicySnapshotId)}";
        }

        foreach (string actionClass in classification.RiskActionClasses.Select(RiskActionClassToken))
        {
            yield return $"risk-action:{AuditMetadata.SafeOptionalToken(actionClass)}";
        }
    }

    private static IEnumerable<string> LowRiskAiAssistanceEvidenceRefs(ChatBotGatewayContext context)
    {
        string commandType = context.Submission.Request.CommandType ?? string.Empty;
        if (!string.Equals(commandType, nameof(ExecuteLowRiskAIAssistance), StringComparison.Ordinal))
        {
            yield break;
        }

        JsonElement element = context.Submission.Request.Command is JsonElement json
            ? json
            : JsonSerializer.SerializeToElement(context.Submission.Request.Command, new JsonSerializerOptions(JsonSerializerDefaults.Web));

        if (context.ApprovalResult is { } approval)
        {
            yield return $"low-risk-policy-decision:{AuditMetadata.SafeOptionalToken(approval.Kind.ToString())}";
            yield return $"low-risk-policy-reason:{AuditMetadata.SafeOptionalToken(approval.ReasonCode)}";
            if (!string.IsNullOrWhiteSpace(approval.PolicySnapshotId))
            {
                yield return $"policy-snapshot:{AuditMetadata.SafeOptionalToken(approval.PolicySnapshotId)}";
            }
        }

        if (TryReadString(element, "contextPackageId", out string? contextPackageId))
        {
            yield return $"context-package:{AuditMetadata.SafeOptionalToken(contextPackageId)}";
        }

        if (TryReadString(element, "contextPackageVersion", out string? contextPackageVersion))
        {
            yield return $"context-package-version:{AuditMetadata.SafeOptionalToken(contextPackageVersion)}";
        }

        if (TryReadString(element, "executionId", out string? executionId))
        {
            yield return $"execution:{AuditMetadata.SafeOptionalToken(executionId)}";
        }

        if (TryReadString(element, "proposalId", out string? proposalId))
        {
            yield return $"proposal:{AuditMetadata.SafeOptionalToken(proposalId)}";
        }
    }

    private static IEnumerable<string> ApprovalDecisionEvidenceRefs(ChatBotGatewayContext context)
    {
        string commandType = context.Submission.Request.CommandType ?? string.Empty;
        if (!string.Equals(commandType, nameof(DecideAiActionApproval), StringComparison.Ordinal))
        {
            yield break;
        }

        JsonElement element = context.Submission.Request.Command is JsonElement json
            ? json
            : JsonSerializer.SerializeToElement(context.Submission.Request.Command, new JsonSerializerOptions(JsonSerializerDefaults.Web));

        if (TryReadString(element, "approvalId", out string? approvalId))
        {
            yield return $"approval:{AuditMetadata.SafeOptionalToken(approvalId)}";
        }

        if (TryReadString(element, "proposalId", out string? proposalId))
        {
            yield return $"proposal:{AuditMetadata.SafeOptionalToken(proposalId)}";
        }

        if (TryReadString(element, "decision", out string? decision))
        {
            yield return $"approval-decision:{AuditMetadata.SafeOptionalToken(decision)}";
        }

        if (context.ApprovalResult is { } approval)
        {
            yield return $"approval-authority:{AuditMetadata.SafeOptionalToken(approval.ReasonCode)}";
        }

        foreach (string batchRef in BatchDecisionEvidenceRefs(element))
        {
            yield return batchRef;
        }
    }

    /// <summary>
    /// Story 7.8: safe, metadata-only batch context for a grouped decision (one envelope per item, never one per batch).
    /// The group fingerprint, risk-class token, and authority-rank token are emitted only when the submitted command
    /// element exposes the matching safe fields; single-item decisions and the current typed
    /// <see cref="DecideAiActionApproval"/>/<see cref="DecideOutboundApproval"/> fan-out commands omit them.
    /// <para>
    /// Wiring note: the public decision command records intentionally do NOT carry these fields (adding them would force
    /// an OpenAPI/generated-client change the story scopes out — AC8). This extractor is therefore a forward-looking,
    /// defensively-tested seam: when batch-dispatch wiring lands it must enrich the command element with these safe refs
    /// server-side. Until then no real fan-out populates them — they appear only when an element already carries them.
    /// </para>
    /// Refs only — never project content, recipient PII, or command bodies.
    /// </summary>
    private static IEnumerable<string> BatchDecisionEvidenceRefs(JsonElement element)
    {
        if (TryReadString(element, "groupKeyFingerprint", out string? groupKeyFingerprint))
        {
            yield return $"approval-group:{AuditMetadata.SafeOptionalToken(groupKeyFingerprint)}";
        }

        if (TryReadString(element, "riskClass", out string? riskClass))
        {
            yield return $"approval-risk-class:{AuditMetadata.SafeOptionalToken(riskClass)}";
        }

        if (TryReadString(element, "authorityRank", out string? authorityRank))
        {
            yield return $"approval-authority-rank:{AuditMetadata.SafeOptionalToken(authorityRank)}";
        }
    }

    private static IEnumerable<string> ApprovedAiActionExecutionEvidenceRefs(ChatBotGatewayContext context)
    {
        string commandType = context.Submission.Request.CommandType ?? string.Empty;
        if (!string.Equals(commandType, nameof(ExecuteApprovedAIAction), StringComparison.Ordinal))
        {
            yield break;
        }

        JsonElement element = context.Submission.Request.Command is JsonElement json
            ? json
            : JsonSerializer.SerializeToElement(context.Submission.Request.Command, new JsonSerializerOptions(JsonSerializerDefaults.Web));

        if (TryReadString(element, "executionId", out string? executionId))
        {
            yield return $"execution:{AuditMetadata.SafeOptionalToken(executionId)}";
        }

        if (TryReadString(element, "proposalId", out string? proposalId))
        {
            yield return $"proposal:{AuditMetadata.SafeOptionalToken(proposalId)}";
        }

        if (TryReadString(element, "approvalId", out string? approvalId))
        {
            yield return $"approval:{AuditMetadata.SafeOptionalToken(approvalId)}";
        }

        if (TryReadString(element, "commandName", out string? commandName))
        {
            yield return $"approved-ai-command:{AuditMetadata.SafeOptionalToken(commandName)}";
        }

        if (TryReadString(element, "commandAllowlistVersion", out string? version))
        {
            yield return $"ai-action-command-allowlist:{AuditMetadata.SafeOptionalToken(version)}";
        }
    }

    private static IEnumerable<string> OutboundDraftEvidenceRefs(ChatBotGatewayContext context)
    {
        string commandType = context.Submission.Request.CommandType ?? string.Empty;
        if (!string.Equals(commandType, nameof(CreateOutboundDraft), StringComparison.Ordinal))
        {
            yield break;
        }

        JsonElement element = context.Submission.Request.Command is JsonElement json
            ? json
            : JsonSerializer.SerializeToElement(context.Submission.Request.Command, new JsonSerializerOptions(JsonSerializerDefaults.Web));

        if (TryReadString(element, "draftId", out string? draftId))
        {
            yield return $"outbound-draft:{AuditMetadata.SafeOptionalToken(draftId)}";
        }

        yield return "sender-authority:draft-only";

        if (TryReadString(element, "requesterId", out string? requesterId))
        {
            yield return $"requester:{AuditMetadata.SafeOptionalToken(requesterId)}";
        }

        if (TryReadString(element, "projectId", out string? projectId))
        {
            yield return $"project:{AuditMetadata.SafeOptionalToken(projectId)}";
        }

        if (TryReadString(element, "policySnapshotId", out string? policySnapshotId))
        {
            yield return $"policy-snapshot:{AuditMetadata.SafeOptionalToken(policySnapshotId)}";
        }

        foreach (string safeRef in SafeRefArray(element, "contextRefs"))
        {
            yield return safeRef;
        }

        foreach (string safeRef in SafeRefArray(element, "recipientRefs"))
        {
            yield return safeRef;
        }

        if (TryReadString(element, "sourceConversationId", out string? sourceConversationId))
        {
            yield return $"conversation:{AuditMetadata.SafeOptionalToken(sourceConversationId)}";
        }

        if (TryReadString(element, "sourceMessageId", out string? sourceMessageId))
        {
            yield return $"source-message:{AuditMetadata.SafeOptionalToken(sourceMessageId)}";
        }
    }

    private static IEnumerable<string> OutboundApprovalEvidenceRefs(ChatBotGatewayContext context)
    {
        string commandType = context.Submission.Request.CommandType ?? string.Empty;
        if (commandType is not (nameof(RequestOutboundSendApproval) or nameof(DecideOutboundApproval)))
        {
            yield break;
        }

        JsonElement element = context.Submission.Request.Command is JsonElement json
            ? json
            : JsonSerializer.SerializeToElement(context.Submission.Request.Command, new JsonSerializerOptions(JsonSerializerDefaults.Web));

        if (TryReadString(element, "approvalId", out string? approvalId))
        {
            yield return $"approval:{AuditMetadata.SafeOptionalToken(approvalId)}";
        }

        if (TryReadString(element, "draftId", out string? draftId))
        {
            yield return $"outbound-draft:{AuditMetadata.SafeOptionalToken(draftId)}";
        }

        if (TryReadString(element, "requesterId", out string? requesterId))
        {
            yield return $"requester:{AuditMetadata.SafeOptionalToken(requesterId)}";
        }

        if (TryReadString(element, "projectId", out string? projectId))
        {
            yield return $"project:{AuditMetadata.SafeOptionalToken(projectId)}";
        }

        if (TryReadString(element, "policySnapshotId", out string? policySnapshotId))
        {
            yield return $"policy-snapshot:{AuditMetadata.SafeOptionalToken(policySnapshotId)}";
        }

        if (TryReadString(element, "decision", out string? decision))
        {
            yield return $"approval-decision:{AuditMetadata.SafeOptionalToken(decision)}";
        }

        if (TryReadString(element, "senderAuthorityClass", out string? authorityClass))
        {
            yield return $"sender-authority:{AuditMetadata.SafeOptionalToken(authorityClass!.Replace(" ", "-", StringComparison.Ordinal))}";
        }

        foreach (string safeRef in SafeRefArray(element, "contextRefs"))
        {
            yield return safeRef;
        }

        foreach (string safeRef in SafeRefArray(element, "recipientRefs"))
        {
            yield return safeRef;
        }

        foreach (string batchRef in BatchDecisionEvidenceRefs(element))
        {
            yield return batchRef;
        }
    }

    private static IEnumerable<string> OutboundSendEvidenceRefs(ChatBotGatewayContext context)
    {
        string commandType = context.Submission.Request.CommandType ?? string.Empty;
        if (!string.Equals(commandType, nameof(ExecuteApprovedOutboundDraft), StringComparison.Ordinal))
        {
            yield break;
        }

        JsonElement element = context.Submission.Request.Command is JsonElement json
            ? json
            : JsonSerializer.SerializeToElement(context.Submission.Request.Command, new JsonSerializerOptions(JsonSerializerDefaults.Web));

        if (TryReadString(element, "sendId", out string? sendId))
        {
            yield return $"outbound-send:{AuditMetadata.SafeOptionalToken(sendId)}";
        }

        if (TryReadString(element, "approvalId", out string? approvalId))
        {
            yield return $"approval:{AuditMetadata.SafeOptionalToken(approvalId)}";
        }

        if (TryReadString(element, "draftId", out string? draftId))
        {
            yield return $"outbound-draft:{AuditMetadata.SafeOptionalToken(draftId)}";
        }

        if (TryReadString(element, "senderAuthorityClass", out string? authorityClass))
        {
            yield return $"sender-authority:{AuditMetadata.SafeOptionalToken(authorityClass!.Replace(" ", "-", StringComparison.Ordinal))}";
        }

        if (TryReadString(element, "requesterId", out string? requesterId))
        {
            yield return $"requester:{AuditMetadata.SafeOptionalToken(requesterId)}";
        }

        if (TryReadString(element, "sendActorId", out string? sendActorId))
        {
            yield return $"send-actor:{AuditMetadata.SafeOptionalToken(sendActorId)}";
        }

        if (TryReadString(element, "projectId", out string? projectId))
        {
            yield return $"project:{AuditMetadata.SafeOptionalToken(projectId)}";
        }

        if (TryReadString(element, "policySnapshotId", out string? policySnapshotId))
        {
            yield return $"policy-snapshot:{AuditMetadata.SafeOptionalToken(policySnapshotId)}";
        }

        if (TryReadString(element, "adapterMode", out string? adapterMode))
        {
            yield return $"adapter-mode:{AuditMetadata.SafeOptionalToken(adapterMode)}";
        }

        foreach (string safeRef in SafeRefArray(element, "contextRefs"))
        {
            yield return safeRef;
        }

        foreach (string safeRef in SafeRefArray(element, "recipientRefs"))
        {
            yield return safeRef;
        }
    }

    private static IEnumerable<string> MailboxIntakeEvidenceRefs(ChatBotGatewayContext context)
    {
        string commandType = context.Submission.Request.CommandType ?? string.Empty;
        if (!string.Equals(commandType, nameof(CaptureMailboxMessageIntake), StringComparison.Ordinal))
        {
            yield break;
        }

        JsonElement element = context.Submission.Request.Command is JsonElement json
            ? json
            : JsonSerializer.SerializeToElement(context.Submission.Request.Command, new JsonSerializerOptions(JsonSerializerDefaults.Web));

        if (element.TryGetProperty("source", out JsonElement source))
        {
            if (TryReadString(source, "mailboxId", out string? mailboxId))
            {
                yield return $"mailbox:{AuditMetadata.SafeOptionalToken(mailboxId)}";
            }

            if (TryReadString(source, "providerMessageId", out string? providerMessageId))
            {
                yield return $"provider-message:{AuditMetadata.SafeOptionalToken(providerMessageId)}";
            }

            if (source.TryGetProperty("delegatedSender", out JsonElement delegatedSender) &&
                delegatedSender.ValueKind == JsonValueKind.Object)
            {
                if (TryReadString(delegatedSender, "state", out string? state))
                {
                    yield return $"delegated-send:{AuditMetadata.SafeOptionalToken(state)}";
                }

                if (delegatedSender.TryGetProperty("delegate", out JsonElement delegateIdentity) &&
                    TryReadString(delegateIdentity, "address", out string? delegateAddress))
                {
                    yield return $"delegate:{AuditMetadata.SafeOptionalToken(delegateAddress)}";
                }

                if (delegatedSender.TryGetProperty("principalFor", out JsonElement principalFor) &&
                    TryReadString(principalFor, "address", out string? principalAddress))
                {
                    yield return $"principal-for:{AuditMetadata.SafeOptionalToken(principalAddress)}";
                }
            }

            if (source.TryGetProperty("externalSender", out JsonElement externalSender) &&
                externalSender.ValueKind == JsonValueKind.Object)
            {
                if (TryReadBool(externalSender, "externalSender", out bool isExternal))
                {
                    yield return $"external-sender:{isExternal.ToString().ToLowerInvariant()}";
                }

                if (TryReadString(externalSender, "partyResolutionState", out string? state))
                {
                    yield return $"party-resolution:{AuditMetadata.SafeOptionalToken(state)}";
                }
            }
        }

        if (!element.TryGetProperty("authenticity", out JsonElement authenticity) ||
            authenticity.ValueKind != JsonValueKind.Object)
        {
            yield break;
        }

        if (authenticity.TryGetProperty("authenticationResults", out JsonElement authenticationResults))
        {
            foreach ((string property, string prefix) in new[]
                     {
                         ("spf", "auth-spf"),
                         ("dkim", "auth-dkim"),
                         ("dmarc", "auth-dmarc"),
                         ("compositeAuthentication", "auth-compauth"),
                     })
            {
                if (TryReadString(authenticationResults, property, out string? verdict))
                {
                    yield return $"{prefix}:{AuditMetadata.SafeOptionalToken(verdict)}";
                }
            }

            if (TryReadString(authenticationResults, "compositeAuthenticationReason", out string? reason))
            {
                yield return $"auth-compauth-reason:{AuditMetadata.SafeOptionalToken(reason)}";
            }
        }

        if (authenticity.TryGetProperty("headerInspection", out JsonElement headerInspection))
        {
            foreach (string discrepancy in SafeRefArray(headerInspection, "discrepancies"))
            {
                yield return $"header-discrepancy:{discrepancy}";
            }

            foreach (string headerName in SelectedHeaderNames(headerInspection, "receivedHeaders"))
            {
                yield return $"selected-header:{headerName}";
            }

            foreach (string headerName in SelectedHeaderNames(headerInspection, "authenticationResultsHeaders"))
            {
                yield return $"selected-header:{headerName}";
            }
        }

        if (authenticity.TryGetProperty("strictnessPolicy", out JsonElement strictnessPolicy) &&
            strictnessPolicy.ValueKind == JsonValueKind.Object)
        {
            if (TryReadString(strictnessPolicy, "strictness", out string? strictness))
            {
                yield return $"authenticity-strictness:{AuditMetadata.SafeOptionalToken(strictness)}";
            }

            if (TryReadString(strictnessPolicy, "reasonCode", out string? reason))
            {
                yield return $"authenticity-strictness-reason:{AuditMetadata.SafeOptionalToken(reason)}";
            }
        }
    }

    private static IEnumerable<string> SelectedHeaderNames(JsonElement element, string propertyName)
    {
        if (element.ValueKind != JsonValueKind.Object ||
            !element.TryGetProperty(propertyName, out JsonElement property) ||
            property.ValueKind != JsonValueKind.Array)
        {
            yield break;
        }

        foreach (JsonElement item in property.EnumerateArray())
        {
            if (TryReadString(item, "name", out string? value))
            {
                yield return AuditMetadata.SafeOptionalToken(value)!;
            }
        }
    }

    private static IEnumerable<string> ServiceClientGrantEvidenceRefs(ChatBotGatewayContext context)
    {
        if (context.ServiceClientGrantEvidence is not { } evidence)
        {
            yield break;
        }

        yield return $"service-client:{AuditMetadata.SafeOptionalToken(evidence.ServiceClientId)}";
        yield return $"actor-type:{AuditMetadata.SafeOptionalToken(context.Actor.ActorType)}";
        yield return $"grant:{AuditMetadata.SafeOptionalToken(evidence.GrantId)}";
        yield return $"grant-scope:{AuditMetadata.SafeOptionalToken(string.Join('|', evidence.Scopes))}";
        yield return $"grant-expiry:{AuditMetadata.SafeOptionalToken(evidence.ExpiresAt.UtcDateTime.ToString("yyyyMMddTHHmmssZ", System.Globalization.CultureInfo.InvariantCulture))}";
        yield return $"command-set:{AuditMetadata.SafeOptionalToken(evidence.CommandSetVersion)}";
        yield return $"service-surface:{AuditMetadata.SafeOptionalToken(ChatBotSurfaceOrigins.ToWireValue(evidence.SurfaceOrigin))}";
        yield return $"service-client-class:{AuditMetadata.SafeOptionalToken(ServiceClientClasses.ToWireValue(evidence.ClientClass))}";

        if (!string.IsNullOrWhiteSpace(evidence.DelegatedUserId))
        {
            yield return $"delegated-user:{AuditMetadata.SafeOptionalToken(evidence.DelegatedUserId)}";
        }

        if (!string.IsNullOrWhiteSpace(evidence.OAuthGrantEvidenceFingerprint))
        {
            yield return $"oauth-evidence:{AuditMetadata.SafeOptionalToken(evidence.OAuthGrantEvidenceFingerprint)}";
        }
    }

    private static IEnumerable<string> AdminEvidenceRefs(ChatBotGatewayContext context)
    {
        string commandType = context.Submission.Request.CommandType ?? string.Empty;
        if (commandType is not (nameof(AssignTenantAdminRole)
            or nameof(ExecuteAdminQueueOperation)
            or nameof(SubmitTenantPolicyChange)
            or nameof(ApproveTenantPolicyChange)
            or nameof(SubmitMailboxConfigurationChange)
            or nameof(SubmitMailboxSourceDisable)
            or nameof(ApproveMailboxSourceDisable)
            or nameof(SubmitMailboxSourceQuarantine)
            or nameof(ApproveMailboxSourceQuarantine)
            or nameof(SubmitMailboxSourceRateLimit)
            or nameof(SubmitServiceClientRateLimit)
            or nameof(SubmitAiActorRateLimit)
            or nameof(SubmitCommandCapabilityRateLimit)
            or nameof(SubmitServiceClientDisable)
            or nameof(ApproveServiceClientDisable)
            or nameof(SubmitAiActorDisable)
            or nameof(ApproveAiActorDisable)
            or nameof(SubmitCommandCapabilityDisable)
            or nameof(ApproveCommandCapabilityDisable)
            or nameof(SubmitOutboundChannelDisable)
            or nameof(ApproveOutboundChannelDisable)
            or nameof(SubmitOutboundChannelQuarantine)
            or nameof(ApproveOutboundChannelQuarantine)
            or nameof(SubmitOutboundChannelRateLimit)
            or nameof(SubmitCommandCapabilityQuarantine)
            or nameof(ApproveCommandCapabilityQuarantine)
            or nameof(SubmitAiActorQuarantine)
            or nameof(ApproveAiActorQuarantine)
            or nameof(SubmitServiceClientQuarantine)
            or nameof(ApproveServiceClientQuarantine)
            or nameof(SubmitNotificationRoutingChange)
            or nameof(SubmitEscalationPolicyChange)
            or nameof(RecordMailboxProviderConnection)
            or nameof(RequestComplianceInvestigation)
            or nameof(RequestComplianceEscalation)
            or nameof(SubmitRetentionConfigurationChange)
            or nameof(SubmitDataClassInventoryChange)
            or nameof(SubmitTenantExportRequest)
            or nameof(SubmitDeletionErasureRequest)
            or nameof(SubmitConsentLawfulBasisRecord)))
        {
            yield break;
        }

        JsonElement element = context.Submission.Request.Command is JsonElement json
            ? json
            : JsonSerializer.SerializeToElement(context.Submission.Request.Command, new JsonSerializerOptions(JsonSerializerDefaults.Web));

        foreach (Claim roleClaim in context.Actor.Principal.FindAll(ParticipantAuthorizationStage.TenantRoleClaim))
        {
            if (AdminRoles.TryFromWireValue(roleClaim.Value, out AdminRole role))
            {
                yield return $"admin-role:{AdminRoles.ToWireValue(role)}";
            }
        }

        if (string.Equals(commandType, nameof(AssignTenantAdminRole), StringComparison.Ordinal))
        {
            yield return "admin-operation:assign-role";

            if (TryReadString(element, "role", out string? role) &&
                AdminRoles.TryFromWireValue(role, out AdminRole assignedRole))
            {
                yield return $"admin-role:{AdminRoles.ToWireValue(assignedRole)}";
            }

            if (TryReadString(element, "targetActorId", out string? targetActorId) &&
                AuditMetadata.SafeOptionalToken(targetActorId) is { } safeTargetActor)
            {
                yield return $"admin-subject:{safeTargetActor}";
            }
        }

        if (string.Equals(commandType, nameof(ExecuteAdminQueueOperation), StringComparison.Ordinal))
        {
            if (TryReadString(element, "operation", out string? operation) &&
                AdminQueueOperations.TryFromWireValue(operation, out AdminQueueOperation parsedOperation))
            {
                yield return $"admin-operation:{AdminQueueOperations.ToWireValue(parsedOperation)}";
            }

            if (TryReadString(element, "scopeUsed", out string? scope) &&
                AdminScopes.TryFromWireValue(scope, out AdminScope parsedScope))
            {
                yield return $"admin-scope:{AdminScopes.ToWireValue(parsedScope)}";
            }

            if (TryReadString(element, "queueRef", out string? queueRef) &&
                AuditMetadata.SafeOptionalToken(queueRef) is { } safeQueue)
            {
                yield return $"admin-queue:{safeQueue}";
            }

            if (TryReadString(element, "queueFamily", out string? queueFamily) &&
                OperationalQueueFamilies.TryFromWireValue(queueFamily, out OperationalQueueFamily parsedFamily))
            {
                yield return $"queue-family:{OperationalQueueFamilies.ToWireValue(parsedFamily)}";
            }

            if (TryReadInt64(element, "itemCount", out long itemCount))
            {
                yield return $"admin-item-count:{itemCount.ToString(System.Globalization.CultureInfo.InvariantCulture)}";
            }

            foreach (string subjectRef in SafeAdminSubjectRefs(element, "itemRefs"))
            {
                yield return $"admin-subject:{subjectRef}";
            }

            if (TryReadString(element, "assigneeRef", out string? assigneeRef) &&
                AuditMetadata.SafeOptionalToken(assigneeRef) is { } safeAssignee)
            {
                yield return $"queue-assignee:{safeAssignee}";
            }

            if (TryReadString(element, "reviewerRef", out string? reviewerRef) &&
                AuditMetadata.SafeOptionalToken(reviewerRef) is { } safeReviewer)
            {
                yield return $"queue-reviewer:{safeReviewer}";
            }

            if (TryReadString(element, "previousAssigneeRef", out string? previousAssigneeRef) &&
                AuditMetadata.SafeOptionalToken(previousAssigneeRef) is { } safePreviousAssignee)
            {
                yield return $"queue-previous-assignee:{safePreviousAssignee}";
            }

            if (TryReadString(element, "policySnapshotId", out string? queuePolicySnapshotId) &&
                AuditMetadata.SafeOptionalToken(queuePolicySnapshotId) is { } safeQueuePolicySnapshot)
            {
                yield return $"policy-snapshot:{safeQueuePolicySnapshot}";
            }

            if (TryReadString(element, "reasonCode", out string? queueReasonCode) &&
                AuditMetadata.SafeOptionalToken(queueReasonCode) is { } safeQueueReason)
            {
                yield return $"reason:{safeQueueReason}";
            }

            if (TryReadString(element, "redactionState", out string? redactionState) &&
                AuditMetadata.SafeOptionalToken(redactionState) is { } safeRedaction)
            {
                yield return $"redaction:{safeRedaction}";
            }

            if (TryReadInt64(element, "sourceVersion", out long queueSourceVersion))
            {
                yield return $"queue-source-version:{queueSourceVersion.ToString(System.Globalization.CultureInfo.InvariantCulture)}";
            }
        }

        if (string.Equals(commandType, nameof(SubmitTenantPolicyChange), StringComparison.Ordinal))
        {
            yield return "admin-operation:submit-policy-change";
            yield return "admin-scope:policy";
            foreach (string policyRef in PolicyEvidenceRefs(element, "policyChangeId", "policy-change"))
            {
                yield return policyRef;
            }

            foreach (string policyRef in PolicyEvidenceRefs(element, "sourcePolicySnapshotId", "policy-snapshot"))
            {
                yield return policyRef;
            }

            foreach (string policyRef in PolicyEvidenceRefs(element, "proposedPolicySnapshotId", "policy-snapshot"))
            {
                yield return policyRef;
            }

            foreach (string fingerprint in PolicyEvidenceRefs(element, "oldValueFingerprint", "policy-old-fingerprint"))
            {
                yield return fingerprint;
            }

            foreach (string fingerprint in PolicyEvidenceRefs(element, "newValueFingerprint", "policy-new-fingerprint"))
            {
                yield return fingerprint;
            }

            foreach (string knob in SafeAdminSubjectRefs(element, "changedKnobIds"))
            {
                yield return $"policy-knob:{knob}";
            }

            if (TryReadInt64(element, "sourceVersion", out long sourceVersion))
            {
                yield return $"policy-source-version:{sourceVersion.ToString(System.Globalization.CultureInfo.InvariantCulture)}";
            }
        }

        if (string.Equals(commandType, nameof(ApproveTenantPolicyChange), StringComparison.Ordinal))
        {
            yield return "admin-operation:approve-policy-change";
            yield return "admin-scope:policy";
            foreach (string policyRef in PolicyEvidenceRefs(element, "policyChangeId", "policy-change"))
            {
                yield return policyRef;
            }

            foreach (string policyRef in PolicyEvidenceRefs(element, "pendingPolicySnapshotId", "policy-snapshot"))
            {
                yield return policyRef;
            }

            foreach (string policyRef in PolicyEvidenceRefs(element, "activatedPolicySnapshotId", "policy-snapshot"))
            {
                yield return policyRef;
            }

            foreach (string knob in SafeAdminSubjectRefs(element, "changedKnobIds"))
            {
                yield return $"policy-knob:{knob}";
            }

            if (TryReadString(element, "approverRef", out string? approverRef) &&
                AuditMetadata.SafeOptionalToken(approverRef) is { } safeApprover)
            {
                yield return $"admin-subject:{safeApprover}";
            }

            if (TryReadInt64(element, "sourceVersion", out long sourceVersion))
            {
                yield return $"policy-source-version:{sourceVersion.ToString(System.Globalization.CultureInfo.InvariantCulture)}";
            }
        }

        if (string.Equals(commandType, nameof(SubmitMailboxConfigurationChange), StringComparison.Ordinal))
        {
            yield return "admin-operation:mailbox-config-change";
            yield return "admin-scope:mailbox";
            foreach (string mailboxRef in PolicyEvidenceRefs(element, "configurationChangeId", "mailbox-change"))
            {
                yield return mailboxRef;
            }

            foreach (string mailboxRef in PolicyEvidenceRefs(element, "sourceConfigurationSnapshotId", "mailbox-config"))
            {
                yield return mailboxRef;
            }

            foreach (string mailboxRef in PolicyEvidenceRefs(element, "proposedConfigurationSnapshotId", "mailbox-config"))
            {
                yield return mailboxRef;
            }

            foreach (string fingerprint in PolicyEvidenceRefs(element, "oldConfigurationFingerprint", "mailbox-old-fingerprint"))
            {
                yield return fingerprint;
            }

            foreach (string fingerprint in PolicyEvidenceRefs(element, "newConfigurationFingerprint", "mailbox-new-fingerprint"))
            {
                yield return fingerprint;
            }

            if (TryReadInt64(element, "sourceVersion", out long sourceVersion))
            {
                yield return $"mailbox-config-source-version:{sourceVersion.ToString(System.Globalization.CultureInfo.InvariantCulture)}";
            }

            if (element.TryGetProperty("changeSet", out JsonElement changeSet) &&
                changeSet.ValueKind == JsonValueKind.Object)
            {
                foreach (string mailboxSource in SafeObjectArrayRefs(changeSet, "monitoredPatterns", "mailboxId"))
                {
                    yield return $"mailbox-source:{mailboxSource}";
                }

                foreach (string mailboxConfig in SafeObjectArrayRefs(changeSet, "monitoredPatterns", "patternRef"))
                {
                    yield return $"mailbox-config:{mailboxConfig}";
                }

                foreach (string routingRule in SafeObjectArrayRefs(changeSet, "routingRules", "routingRuleId"))
                {
                    yield return $"mailbox-routing-rule:{routingRule}";
                }

                foreach (string providerConnection in SafeObjectArrayRefs(changeSet, "providerConnections", "providerConnectionRef"))
                {
                    yield return $"provider-connection:{providerConnection}";
                }

                foreach (string permissionStatus in SafeObjectArrayRefs(changeSet, "permissionStatuses", "permissionStatusRef"))
                {
                    yield return $"permission-status:{permissionStatus}";
                }

                foreach (string permissionEvidence in SafeObjectArrayRefs(changeSet, "permissionStatuses", "permissionEvidenceRef"))
                {
                    yield return $"permission-evidence:{permissionEvidence}";
                }
            }
        }

        if (string.Equals(commandType, nameof(SubmitMailboxSourceDisable), StringComparison.Ordinal) ||
            string.Equals(commandType, nameof(ApproveMailboxSourceDisable), StringComparison.Ordinal))
        {
            bool isApproval = string.Equals(commandType, nameof(ApproveMailboxSourceDisable), StringComparison.Ordinal);
            yield return isApproval ? "admin-operation:mailbox-source-disable-approve" : "admin-operation:mailbox-source-disable";
            yield return "admin-scope:mailbox";
            foreach (string disableRef in PolicyEvidenceRefs(element, "disableChangeId", "mailbox-source-disable-change"))
            {
                yield return disableRef;
            }

            foreach (string subjectRef in PolicyEvidenceRefs(element, "mailboxSourceRef", "mailbox-source"))
            {
                yield return subjectRef;
            }

            foreach (string snapshotRef in PolicyEvidenceRefs(element, "policySnapshotId", "policy-snapshot"))
            {
                yield return snapshotRef;
            }

            foreach (string reasonRef in PolicyEvidenceRefs(element, "reasonCode", "reason"))
            {
                yield return reasonRef;
            }

            if (TryReadString(element, "oldState", out string? oldState) &&
                AuditMetadata.SafeOptionalToken(oldState) is { } safeOldState)
            {
                yield return $"mailbox-source-old-state:{safeOldState}";
            }

            if (TryReadString(element, "newState", out string? newState) &&
                AuditMetadata.SafeOptionalToken(newState) is { } safeNewState)
            {
                yield return $"mailbox-source-new-state:{safeNewState}";
            }

            if (isApproval &&
                TryReadString(element, "approverRef", out string? approverRef) &&
                AuditMetadata.SafeOptionalToken(approverRef) is { } safeApprover)
            {
                yield return $"admin-subject:{safeApprover}";
            }

            if (TryReadInt64(element, "sourceVersion", out long disableSourceVersion))
            {
                yield return $"mailbox-source-disable-source-version:{disableSourceVersion.ToString(System.Globalization.CultureInfo.InvariantCulture)}";
            }
        }

        if (string.Equals(commandType, nameof(SubmitServiceClientDisable), StringComparison.Ordinal) ||
            string.Equals(commandType, nameof(ApproveServiceClientDisable), StringComparison.Ordinal))
        {
            bool isApproval = string.Equals(commandType, nameof(ApproveServiceClientDisable), StringComparison.Ordinal);
            yield return isApproval ? "admin-operation:service-client-disable-approve" : "admin-operation:service-client-disable";
            yield return "admin-scope:tenant-admin";
            foreach (string disableRef in PolicyEvidenceRefs(element, "disableChangeId", "service-client-disable-change"))
            {
                yield return disableRef;
            }

            foreach (string subjectRef in PolicyEvidenceRefs(element, "serviceClientRef", "service-client"))
            {
                yield return subjectRef;
            }

            foreach (string snapshotRef in PolicyEvidenceRefs(element, "policySnapshotId", "policy-snapshot"))
            {
                yield return snapshotRef;
            }

            foreach (string reasonRef in PolicyEvidenceRefs(element, "reasonCode", "reason"))
            {
                yield return reasonRef;
            }

            if (TryReadString(element, "oldState", out string? oldState) &&
                AuditMetadata.SafeOptionalToken(oldState) is { } safeOldState)
            {
                yield return $"service-client-old-state:{safeOldState}";
            }

            if (TryReadString(element, "newState", out string? newState) &&
                AuditMetadata.SafeOptionalToken(newState) is { } safeNewState)
            {
                yield return $"service-client-new-state:{safeNewState}";
            }

            if (isApproval &&
                TryReadString(element, "approverRef", out string? approverRef) &&
                AuditMetadata.SafeOptionalToken(approverRef) is { } safeApprover)
            {
                yield return $"admin-subject:{safeApprover}";
            }

            if (TryReadInt64(element, "sourceVersion", out long disableSourceVersion))
            {
                yield return $"service-client-disable-source-version:{disableSourceVersion.ToString(System.Globalization.CultureInfo.InvariantCulture)}";
            }
        }

        if (string.Equals(commandType, nameof(SubmitAiActorDisable), StringComparison.Ordinal) ||
            string.Equals(commandType, nameof(ApproveAiActorDisable), StringComparison.Ordinal))
        {
            bool isApproval = string.Equals(commandType, nameof(ApproveAiActorDisable), StringComparison.Ordinal);
            yield return isApproval ? "admin-operation:ai-actor-disable-approve" : "admin-operation:ai-actor-disable";
            yield return "admin-scope:policy";
            foreach (string disableRef in PolicyEvidenceRefs(element, "disableChangeId", "ai-actor-disable-change"))
            {
                yield return disableRef;
            }

            foreach (string subjectRef in PolicyEvidenceRefs(element, "aiActorRef", "ai-actor"))
            {
                yield return subjectRef;
            }

            foreach (string snapshotRef in PolicyEvidenceRefs(element, "policySnapshotId", "policy-snapshot"))
            {
                yield return snapshotRef;
            }

            foreach (string reasonRef in PolicyEvidenceRefs(element, "reasonCode", "reason"))
            {
                yield return reasonRef;
            }

            if (TryReadString(element, "oldState", out string? oldState) &&
                AuditMetadata.SafeOptionalToken(oldState) is { } safeOldState)
            {
                yield return $"ai-actor-old-state:{safeOldState}";
            }

            if (TryReadString(element, "newState", out string? newState) &&
                AuditMetadata.SafeOptionalToken(newState) is { } safeNewState)
            {
                yield return $"ai-actor-new-state:{safeNewState}";
            }

            if (isApproval &&
                TryReadString(element, "approverRef", out string? approverRef) &&
                AuditMetadata.SafeOptionalToken(approverRef) is { } safeApprover)
            {
                yield return $"admin-subject:{safeApprover}";
            }

            if (TryReadInt64(element, "sourceVersion", out long disableSourceVersion))
            {
                yield return $"ai-actor-disable-source-version:{disableSourceVersion.ToString(System.Globalization.CultureInfo.InvariantCulture)}";
            }
        }

        if (string.Equals(commandType, nameof(SubmitCommandCapabilityDisable), StringComparison.Ordinal) ||
            string.Equals(commandType, nameof(ApproveCommandCapabilityDisable), StringComparison.Ordinal))
        {
            bool isApproval = string.Equals(commandType, nameof(ApproveCommandCapabilityDisable), StringComparison.Ordinal);
            yield return isApproval ? "admin-operation:command-capability-disable-approve" : "admin-operation:command-capability-disable";
            yield return "admin-scope:policy";
            foreach (string disableRef in PolicyEvidenceRefs(element, "disableChangeId", "command-capability-disable-change"))
            {
                yield return disableRef;
            }

            foreach (string subjectRef in PolicyEvidenceRefs(element, "commandCapabilityRef", "command-capability"))
            {
                yield return subjectRef;
            }

            foreach (string snapshotRef in PolicyEvidenceRefs(element, "policySnapshotId", "policy-snapshot"))
            {
                yield return snapshotRef;
            }

            foreach (string reasonRef in PolicyEvidenceRefs(element, "reasonCode", "reason"))
            {
                yield return reasonRef;
            }

            if (TryReadString(element, "oldState", out string? oldState) &&
                AuditMetadata.SafeOptionalToken(oldState) is { } safeOldState)
            {
                yield return $"command-capability-old-state:{safeOldState}";
            }

            if (TryReadString(element, "newState", out string? newState) &&
                AuditMetadata.SafeOptionalToken(newState) is { } safeNewState)
            {
                yield return $"command-capability-new-state:{safeNewState}";
            }

            if (isApproval &&
                TryReadString(element, "approverRef", out string? approverRef) &&
                AuditMetadata.SafeOptionalToken(approverRef) is { } safeApprover)
            {
                yield return $"admin-subject:{safeApprover}";
            }

            if (TryReadInt64(element, "sourceVersion", out long disableSourceVersion))
            {
                yield return $"command-capability-disable-source-version:{disableSourceVersion.ToString(System.Globalization.CultureInfo.InvariantCulture)}";
            }
        }

        if (string.Equals(commandType, nameof(SubmitOutboundChannelDisable), StringComparison.Ordinal) ||
            string.Equals(commandType, nameof(ApproveOutboundChannelDisable), StringComparison.Ordinal))
        {
            bool isApproval = string.Equals(commandType, nameof(ApproveOutboundChannelDisable), StringComparison.Ordinal);
            yield return isApproval ? "admin-operation:outbound-channel-disable-approve" : "admin-operation:outbound-channel-disable";
            yield return "admin-scope:policy";
            foreach (string disableRef in PolicyEvidenceRefs(element, "disableChangeId", "outbound-channel-disable-change"))
            {
                yield return disableRef;
            }

            foreach (string subjectRef in PolicyEvidenceRefs(element, "outboundChannelRef", "outbound-channel"))
            {
                yield return subjectRef;
            }

            foreach (string snapshotRef in PolicyEvidenceRefs(element, "policySnapshotId", "policy-snapshot"))
            {
                yield return snapshotRef;
            }

            foreach (string reasonRef in PolicyEvidenceRefs(element, "reasonCode", "reason"))
            {
                yield return reasonRef;
            }

            if (TryReadString(element, "oldState", out string? oldState) &&
                AuditMetadata.SafeOptionalToken(oldState) is { } safeOldState)
            {
                yield return $"outbound-channel-old-state:{safeOldState}";
            }

            if (TryReadString(element, "newState", out string? newState) &&
                AuditMetadata.SafeOptionalToken(newState) is { } safeNewState)
            {
                yield return $"outbound-channel-new-state:{safeNewState}";
            }

            if (isApproval &&
                TryReadString(element, "approverRef", out string? approverRef) &&
                AuditMetadata.SafeOptionalToken(approverRef) is { } safeApprover)
            {
                yield return $"admin-subject:{safeApprover}";
            }

            if (TryReadInt64(element, "sourceVersion", out long disableSourceVersion))
            {
                yield return $"outbound-channel-disable-source-version:{disableSourceVersion.ToString(System.Globalization.CultureInfo.InvariantCulture)}";
            }
        }

        if (string.Equals(commandType, nameof(SubmitOutboundChannelQuarantine), StringComparison.Ordinal) ||
            string.Equals(commandType, nameof(ApproveOutboundChannelQuarantine), StringComparison.Ordinal))
        {
            bool isApproval = string.Equals(commandType, nameof(ApproveOutboundChannelQuarantine), StringComparison.Ordinal);
            yield return isApproval ? "admin-operation:outbound-channel-quarantine-approve" : "admin-operation:outbound-channel-quarantine";
            yield return "admin-scope:policy";
            foreach (string quarantineRef in PolicyEvidenceRefs(element, "quarantineChangeId", "outbound-channel-quarantine-change"))
            {
                yield return quarantineRef;
            }

            foreach (string subjectRef in PolicyEvidenceRefs(element, "outboundChannelRef", "outbound-channel"))
            {
                yield return subjectRef;
            }

            foreach (string snapshotRef in PolicyEvidenceRefs(element, "policySnapshotId", "policy-snapshot"))
            {
                yield return snapshotRef;
            }

            foreach (string reasonRef in PolicyEvidenceRefs(element, "reasonCode", "reason"))
            {
                yield return reasonRef;
            }

            if (TryReadString(element, "oldState", out string? oldState) &&
                AuditMetadata.SafeOptionalToken(oldState) is { } safeOldState)
            {
                yield return $"outbound-channel-old-state:{safeOldState}";
            }

            if (TryReadString(element, "newState", out string? newState) &&
                AuditMetadata.SafeOptionalToken(newState) is { } safeNewState)
            {
                yield return $"outbound-channel-new-state:{safeNewState}";
            }

            if (isApproval &&
                TryReadString(element, "approverRef", out string? approverRef) &&
                AuditMetadata.SafeOptionalToken(approverRef) is { } safeApprover)
            {
                yield return $"admin-subject:{safeApprover}";
            }

            if (TryReadInt64(element, "sourceVersion", out long quarantineSourceVersion))
            {
                yield return $"outbound-channel-quarantine-source-version:{quarantineSourceVersion.ToString(System.Globalization.CultureInfo.InvariantCulture)}";
            }
        }

        if (string.Equals(commandType, nameof(SubmitCommandCapabilityQuarantine), StringComparison.Ordinal) ||
            string.Equals(commandType, nameof(ApproveCommandCapabilityQuarantine), StringComparison.Ordinal))
        {
            bool isApproval = string.Equals(commandType, nameof(ApproveCommandCapabilityQuarantine), StringComparison.Ordinal);
            yield return isApproval ? "admin-operation:command-capability-quarantine-approve" : "admin-operation:command-capability-quarantine";
            yield return "admin-scope:policy";
            foreach (string quarantineRef in PolicyEvidenceRefs(element, "quarantineChangeId", "command-capability-quarantine-change"))
            {
                yield return quarantineRef;
            }

            foreach (string subjectRef in PolicyEvidenceRefs(element, "commandCapabilityRef", "command-capability"))
            {
                yield return subjectRef;
            }

            foreach (string snapshotRef in PolicyEvidenceRefs(element, "policySnapshotId", "policy-snapshot"))
            {
                yield return snapshotRef;
            }

            foreach (string reasonRef in PolicyEvidenceRefs(element, "reasonCode", "reason"))
            {
                yield return reasonRef;
            }

            if (TryReadString(element, "oldState", out string? oldState) &&
                AuditMetadata.SafeOptionalToken(oldState) is { } safeOldState)
            {
                yield return $"command-capability-old-state:{safeOldState}";
            }

            if (TryReadString(element, "newState", out string? newState) &&
                AuditMetadata.SafeOptionalToken(newState) is { } safeNewState)
            {
                yield return $"command-capability-new-state:{safeNewState}";
            }

            if (isApproval &&
                TryReadString(element, "approverRef", out string? approverRef) &&
                AuditMetadata.SafeOptionalToken(approverRef) is { } safeApprover)
            {
                yield return $"admin-subject:{safeApprover}";
            }

            if (TryReadInt64(element, "sourceVersion", out long quarantineSourceVersion))
            {
                yield return $"command-capability-quarantine-source-version:{quarantineSourceVersion.ToString(System.Globalization.CultureInfo.InvariantCulture)}";
            }
        }

        if (string.Equals(commandType, nameof(SubmitAiActorQuarantine), StringComparison.Ordinal) ||
            string.Equals(commandType, nameof(ApproveAiActorQuarantine), StringComparison.Ordinal))
        {
            bool isApproval = string.Equals(commandType, nameof(ApproveAiActorQuarantine), StringComparison.Ordinal);
            yield return isApproval ? "admin-operation:ai-actor-quarantine-approve" : "admin-operation:ai-actor-quarantine";
            yield return "admin-scope:policy";
            foreach (string quarantineRef in PolicyEvidenceRefs(element, "quarantineChangeId", "ai-actor-quarantine-change"))
            {
                yield return quarantineRef;
            }

            foreach (string subjectRef in PolicyEvidenceRefs(element, "aiActorRef", "ai-actor"))
            {
                yield return subjectRef;
            }

            foreach (string snapshotRef in PolicyEvidenceRefs(element, "policySnapshotId", "policy-snapshot"))
            {
                yield return snapshotRef;
            }

            foreach (string reasonRef in PolicyEvidenceRefs(element, "reasonCode", "reason"))
            {
                yield return reasonRef;
            }

            if (TryReadString(element, "oldState", out string? oldState) &&
                AuditMetadata.SafeOptionalToken(oldState) is { } safeOldState)
            {
                yield return $"ai-actor-old-state:{safeOldState}";
            }

            if (TryReadString(element, "newState", out string? newState) &&
                AuditMetadata.SafeOptionalToken(newState) is { } safeNewState)
            {
                yield return $"ai-actor-new-state:{safeNewState}";
            }

            if (isApproval &&
                TryReadString(element, "approverRef", out string? approverRef) &&
                AuditMetadata.SafeOptionalToken(approverRef) is { } safeApprover)
            {
                yield return $"admin-subject:{safeApprover}";
            }

            if (TryReadInt64(element, "sourceVersion", out long quarantineSourceVersion))
            {
                yield return $"ai-actor-quarantine-source-version:{quarantineSourceVersion.ToString(System.Globalization.CultureInfo.InvariantCulture)}";
            }
        }

        if (string.Equals(commandType, nameof(SubmitServiceClientQuarantine), StringComparison.Ordinal) ||
            string.Equals(commandType, nameof(ApproveServiceClientQuarantine), StringComparison.Ordinal))
        {
            bool isApproval = string.Equals(commandType, nameof(ApproveServiceClientQuarantine), StringComparison.Ordinal);
            yield return isApproval ? "admin-operation:service-client-quarantine-approve" : "admin-operation:service-client-quarantine";
            yield return "admin-scope:tenant-admin";
            foreach (string quarantineRef in PolicyEvidenceRefs(element, "quarantineChangeId", "service-client-quarantine-change"))
            {
                yield return quarantineRef;
            }

            foreach (string subjectRef in PolicyEvidenceRefs(element, "serviceClientRef", "service-client"))
            {
                yield return subjectRef;
            }

            foreach (string snapshotRef in PolicyEvidenceRefs(element, "policySnapshotId", "policy-snapshot"))
            {
                yield return snapshotRef;
            }

            foreach (string reasonRef in PolicyEvidenceRefs(element, "reasonCode", "reason"))
            {
                yield return reasonRef;
            }

            if (TryReadString(element, "oldState", out string? oldState) &&
                AuditMetadata.SafeOptionalToken(oldState) is { } safeOldState)
            {
                yield return $"service-client-old-state:{safeOldState}";
            }

            if (TryReadString(element, "newState", out string? newState) &&
                AuditMetadata.SafeOptionalToken(newState) is { } safeNewState)
            {
                yield return $"service-client-new-state:{safeNewState}";
            }

            if (isApproval &&
                TryReadString(element, "approverRef", out string? approverRef) &&
                AuditMetadata.SafeOptionalToken(approverRef) is { } safeApprover)
            {
                yield return $"admin-subject:{safeApprover}";
            }

            if (TryReadInt64(element, "sourceVersion", out long quarantineSourceVersion))
            {
                yield return $"service-client-quarantine-source-version:{quarantineSourceVersion.ToString(System.Globalization.CultureInfo.InvariantCulture)}";
            }
        }

        if (string.Equals(commandType, nameof(SubmitMailboxSourceQuarantine), StringComparison.Ordinal) ||
            string.Equals(commandType, nameof(ApproveMailboxSourceQuarantine), StringComparison.Ordinal))
        {
            bool isApproval = string.Equals(commandType, nameof(ApproveMailboxSourceQuarantine), StringComparison.Ordinal);
            yield return isApproval ? "admin-operation:mailbox-source-quarantine-approve" : "admin-operation:mailbox-source-quarantine";
            yield return "admin-scope:mailbox";
            foreach (string quarantineRef in PolicyEvidenceRefs(element, "quarantineChangeId", "mailbox-source-quarantine-change"))
            {
                yield return quarantineRef;
            }

            foreach (string subjectRef in PolicyEvidenceRefs(element, "mailboxSourceRef", "mailbox-source"))
            {
                yield return subjectRef;
            }

            foreach (string snapshotRef in PolicyEvidenceRefs(element, "policySnapshotId", "policy-snapshot"))
            {
                yield return snapshotRef;
            }

            foreach (string reasonRef in PolicyEvidenceRefs(element, "reasonCode", "reason"))
            {
                yield return reasonRef;
            }

            if (TryReadString(element, "oldState", out string? oldState) &&
                AuditMetadata.SafeOptionalToken(oldState) is { } safeOldState)
            {
                yield return $"mailbox-source-old-state:{safeOldState}";
            }

            if (TryReadString(element, "newState", out string? newState) &&
                AuditMetadata.SafeOptionalToken(newState) is { } safeNewState)
            {
                yield return $"mailbox-source-new-state:{safeNewState}";
            }

            if (isApproval &&
                TryReadString(element, "approverRef", out string? approverRef) &&
                AuditMetadata.SafeOptionalToken(approverRef) is { } safeApprover)
            {
                yield return $"admin-subject:{safeApprover}";
            }

            if (TryReadInt64(element, "sourceVersion", out long quarantineSourceVersion))
            {
                yield return $"mailbox-source-quarantine-source-version:{quarantineSourceVersion.ToString(System.Globalization.CultureInfo.InvariantCulture)}";
            }
        }

        if (string.Equals(commandType, nameof(SubmitMailboxSourceRateLimit), StringComparison.Ordinal))
        {
            // Story 7.14: single-actor standard policy mutation — no StateTransition ref (rate-limit is a bounded
            // parameter, not a control-state lifecycle transition). "Old state"/"new state" are the per-window budgets.
            yield return "admin-operation:mailbox-source-rate-limit";
            yield return "admin-scope:mailbox";
            foreach (string rateLimitRef in PolicyEvidenceRefs(element, "rateLimitChangeId", "mailbox-source-rate-limit-change"))
            {
                yield return rateLimitRef;
            }

            foreach (string subjectRef in PolicyEvidenceRefs(element, "mailboxSourceRef", "mailbox-source"))
            {
                yield return subjectRef;
            }

            foreach (string snapshotRef in PolicyEvidenceRefs(element, "policySnapshotId", "policy-snapshot"))
            {
                yield return snapshotRef;
            }

            foreach (string reasonRef in PolicyEvidenceRefs(element, "reasonCode", "reason"))
            {
                yield return reasonRef;
            }

            if (TryReadInt64(element, "oldBudget", out long oldBudget))
            {
                yield return $"mailbox-source-rate-limit-old:{oldBudget.ToString(System.Globalization.CultureInfo.InvariantCulture)}";
            }

            if (TryReadInt64(element, "newBudget", out long newBudget))
            {
                yield return $"mailbox-source-rate-limit-new:{newBudget.ToString(System.Globalization.CultureInfo.InvariantCulture)}";
            }

            if (TryReadString(element, "window", out string? window) &&
                AuditMetadata.SafeOptionalToken(window) is { } safeWindow)
            {
                yield return $"mailbox-source-rate-limit-window:{safeWindow}";
            }

            if (TryReadInt64(element, "sourceVersion", out long rateLimitSourceVersion))
            {
                yield return $"mailbox-source-rate-limit-source-version:{rateLimitSourceVersion.ToString(System.Globalization.CultureInfo.InvariantCulture)}";
            }
        }

        if (string.Equals(commandType, nameof(SubmitServiceClientRateLimit), StringComparison.Ordinal))
        {
            // Story 7.17: single-actor standard policy mutation — no StateTransition ref (rate-limit is a bounded
            // parameter, not a control-state lifecycle transition). "Old state"/"new state" are the per-window budgets.
            yield return "admin-operation:service-client-rate-limit";
            yield return "admin-scope:tenant-admin";
            foreach (string rateLimitRef in PolicyEvidenceRefs(element, "rateLimitChangeId", "service-client-rate-limit-change"))
            {
                yield return rateLimitRef;
            }

            foreach (string subjectRef in PolicyEvidenceRefs(element, "serviceClientRef", "service-client"))
            {
                yield return subjectRef;
            }

            foreach (string snapshotRef in PolicyEvidenceRefs(element, "policySnapshotId", "policy-snapshot"))
            {
                yield return snapshotRef;
            }

            foreach (string reasonRef in PolicyEvidenceRefs(element, "reasonCode", "reason"))
            {
                yield return reasonRef;
            }

            if (TryReadInt64(element, "oldBudget", out long oldBudget))
            {
                yield return $"service-client-rate-limit-old:{oldBudget.ToString(System.Globalization.CultureInfo.InvariantCulture)}";
            }

            if (TryReadInt64(element, "newBudget", out long newBudget))
            {
                yield return $"service-client-rate-limit-new:{newBudget.ToString(System.Globalization.CultureInfo.InvariantCulture)}";
            }

            if (TryReadString(element, "window", out string? window) &&
                AuditMetadata.SafeOptionalToken(window) is { } safeWindow)
            {
                yield return $"service-client-rate-limit-window:{safeWindow}";
            }

            if (TryReadInt64(element, "sourceVersion", out long rateLimitSourceVersion))
            {
                yield return $"service-client-rate-limit-source-version:{rateLimitSourceVersion.ToString(System.Globalization.CultureInfo.InvariantCulture)}";
            }
        }

        if (string.Equals(commandType, nameof(SubmitAiActorRateLimit), StringComparison.Ordinal))
        {
            // Story 7.20: single-actor standard policy mutation — no StateTransition ref (rate-limit is a bounded
            // parameter, not a control-state lifecycle transition). "Old state"/"new state" are the per-window budgets.
            // admin-scope:policy (AI-action governance is the policy-admin's domain), not tenant-admin.
            yield return "admin-operation:ai-actor-rate-limit";
            yield return "admin-scope:policy";
            foreach (string rateLimitRef in PolicyEvidenceRefs(element, "rateLimitChangeId", "ai-actor-rate-limit-change"))
            {
                yield return rateLimitRef;
            }

            foreach (string subjectRef in PolicyEvidenceRefs(element, "aiActorRef", "ai-actor"))
            {
                yield return subjectRef;
            }

            foreach (string snapshotRef in PolicyEvidenceRefs(element, "policySnapshotId", "policy-snapshot"))
            {
                yield return snapshotRef;
            }

            foreach (string reasonRef in PolicyEvidenceRefs(element, "reasonCode", "reason"))
            {
                yield return reasonRef;
            }

            if (TryReadInt64(element, "oldBudget", out long oldBudget))
            {
                yield return $"ai-actor-rate-limit-old:{oldBudget.ToString(System.Globalization.CultureInfo.InvariantCulture)}";
            }

            if (TryReadInt64(element, "newBudget", out long newBudget))
            {
                yield return $"ai-actor-rate-limit-new:{newBudget.ToString(System.Globalization.CultureInfo.InvariantCulture)}";
            }

            if (TryReadString(element, "window", out string? window) &&
                AuditMetadata.SafeOptionalToken(window) is { } safeWindow)
            {
                yield return $"ai-actor-rate-limit-window:{safeWindow}";
            }

            if (TryReadInt64(element, "sourceVersion", out long aiRateLimitSourceVersion))
            {
                yield return $"ai-actor-rate-limit-source-version:{aiRateLimitSourceVersion.ToString(System.Globalization.CultureInfo.InvariantCulture)}";
            }
        }

        if (string.Equals(commandType, nameof(SubmitCommandCapabilityRateLimit), StringComparison.Ordinal))
        {
            // Story 7.23: single-actor standard policy mutation — no StateTransition ref (rate-limit is a bounded
            // parameter, not a control-state lifecycle transition). "Old state"/"new state" are the per-window command
            // budgets. admin-scope:policy (command-capability governance is a security-sensitive policy concern), and
            // the subject is the safe command TYPE name. The policy-snapshot ref is the allowlist/policy version in effect.
            yield return "admin-operation:command-capability-rate-limit";
            yield return "admin-scope:policy";
            foreach (string rateLimitRef in PolicyEvidenceRefs(element, "rateLimitChangeId", "command-capability-rate-limit-change"))
            {
                yield return rateLimitRef;
            }

            foreach (string subjectRef in PolicyEvidenceRefs(element, "commandCapabilityRef", "command-capability"))
            {
                yield return subjectRef;
            }

            foreach (string snapshotRef in PolicyEvidenceRefs(element, "policySnapshotId", "policy-snapshot"))
            {
                yield return snapshotRef;
            }

            foreach (string reasonRef in PolicyEvidenceRefs(element, "reasonCode", "reason"))
            {
                yield return reasonRef;
            }

            if (TryReadInt64(element, "oldBudget", out long oldCommandBudget))
            {
                yield return $"command-capability-rate-limit-old:{oldCommandBudget.ToString(System.Globalization.CultureInfo.InvariantCulture)}";
            }

            if (TryReadInt64(element, "newBudget", out long newCommandBudget))
            {
                yield return $"command-capability-rate-limit-new:{newCommandBudget.ToString(System.Globalization.CultureInfo.InvariantCulture)}";
            }

            if (TryReadString(element, "window", out string? commandWindow) &&
                AuditMetadata.SafeOptionalToken(commandWindow) is { } safeCommandWindow)
            {
                yield return $"command-capability-rate-limit-window:{safeCommandWindow}";
            }

            if (TryReadInt64(element, "sourceVersion", out long commandRateLimitSourceVersion))
            {
                yield return $"command-capability-rate-limit-source-version:{commandRateLimitSourceVersion.ToString(System.Globalization.CultureInfo.InvariantCulture)}";
            }
        }

        if (string.Equals(commandType, nameof(SubmitOutboundChannelRateLimit), StringComparison.Ordinal))
        {
            // Story 7.26: single-actor standard policy mutation — no StateTransition ref (rate-limit is a bounded
            // parameter, not a control-state lifecycle transition; the OutboundChannelControlState enum is unchanged).
            // "Old state"/"new state" are the per-window send budgets. admin-scope:policy (outbound-channel governance
            // is a security-sensitive policy concern), and the subject is the safe outbound-channel ref (the AdapterRef
            // token). The policy-snapshot ref is the policy version in effect.
            yield return "admin-operation:outbound-channel-rate-limit";
            yield return "admin-scope:policy";
            foreach (string rateLimitRef in PolicyEvidenceRefs(element, "rateLimitChangeId", "outbound-channel-rate-limit-change"))
            {
                yield return rateLimitRef;
            }

            foreach (string subjectRef in PolicyEvidenceRefs(element, "outboundChannelRef", "outbound-channel"))
            {
                yield return subjectRef;
            }

            foreach (string snapshotRef in PolicyEvidenceRefs(element, "policySnapshotId", "policy-snapshot"))
            {
                yield return snapshotRef;
            }

            foreach (string reasonRef in PolicyEvidenceRefs(element, "reasonCode", "reason"))
            {
                yield return reasonRef;
            }

            if (TryReadInt64(element, "oldBudget", out long oldSendBudget))
            {
                yield return $"outbound-channel-rate-limit-old:{oldSendBudget.ToString(System.Globalization.CultureInfo.InvariantCulture)}";
            }

            if (TryReadInt64(element, "newBudget", out long newSendBudget))
            {
                yield return $"outbound-channel-rate-limit-new:{newSendBudget.ToString(System.Globalization.CultureInfo.InvariantCulture)}";
            }

            if (TryReadString(element, "window", out string? sendWindow) &&
                AuditMetadata.SafeOptionalToken(sendWindow) is { } safeSendWindow)
            {
                yield return $"outbound-channel-rate-limit-window:{safeSendWindow}";
            }

            if (TryReadInt64(element, "sourceVersion", out long sendRateLimitSourceVersion))
            {
                yield return $"outbound-channel-rate-limit-source-version:{sendRateLimitSourceVersion.ToString(System.Globalization.CultureInfo.InvariantCulture)}";
            }
        }

        if (string.Equals(commandType, nameof(SubmitNotificationRoutingChange), StringComparison.Ordinal))
        {
            yield return "admin-operation:notification-routing-edit";
            yield return "admin-scope:policy";
            foreach (string routingRef in PolicyEvidenceRefs(element, "routingChangeId", "notification-routing-change"))
            {
                yield return routingRef;
            }

            foreach (string routingRef in PolicyEvidenceRefs(element, "sourceRoutingSnapshotId", "routing-snapshot"))
            {
                yield return routingRef;
            }

            foreach (string routingRef in PolicyEvidenceRefs(element, "proposedRoutingSnapshotId", "routing-snapshot"))
            {
                yield return routingRef;
            }

            foreach (string fingerprint in PolicyEvidenceRefs(element, "oldRoutingFingerprint", "routing-old-fingerprint"))
            {
                yield return fingerprint;
            }

            foreach (string fingerprint in PolicyEvidenceRefs(element, "newRoutingFingerprint", "routing-new-fingerprint"))
            {
                yield return fingerprint;
            }

            if (TryReadInt64(element, "sourceVersion", out long sourceVersion))
            {
                yield return $"notification-routing-source-version:{sourceVersion.ToString(System.Globalization.CultureInfo.InvariantCulture)}";
            }

            if (element.TryGetProperty("changeSet", out JsonElement changeSet) &&
                changeSet.ValueKind == JsonValueKind.Object &&
                changeSet.TryGetProperty("entries", out JsonElement entries) &&
                entries.ValueKind == JsonValueKind.Array)
            {
                foreach (JsonElement entry in entries.EnumerateArray())
                {
                    if (TryReadString(entry, "stateClass", out string? stateClass) &&
                        AuditMetadata.SafeOptionalToken(stateClass) is { } safeStateClass)
                    {
                        yield return $"notification-state-class:{safeStateClass}";
                    }

                    if (TryReadString(entry, "scope", out string? scope) &&
                        AuditMetadata.SafeOptionalToken(scope) is { } safeScope)
                    {
                        yield return $"notification-scope:{safeScope}";
                    }

                    if (TryReadString(entry, "recipientRole", out string? recipientRole) &&
                        AuditMetadata.SafeOptionalToken(recipientRole) is { } safeRecipientRole)
                    {
                        yield return $"recipient-role:{safeRecipientRole}";
                    }

                    if (TryReadString(entry, "channel", out string? channel) &&
                        AuditMetadata.SafeOptionalToken(channel) is { } safeChannel)
                    {
                        yield return $"notification-channel:{safeChannel}";
                    }
                }
            }
        }

        if (string.Equals(commandType, nameof(SubmitEscalationPolicyChange), StringComparison.Ordinal))
        {
            yield return "admin-operation:escalation-policy-edit";
            yield return "admin-scope:policy";
            foreach (string escalationRef in PolicyEvidenceRefs(element, "escalationPolicyChangeId", "escalation-policy-change"))
            {
                yield return escalationRef;
            }

            foreach (string escalationRef in PolicyEvidenceRefs(element, "sourceEscalationSnapshotId", "escalation-snapshot"))
            {
                yield return escalationRef;
            }

            foreach (string escalationRef in PolicyEvidenceRefs(element, "proposedEscalationSnapshotId", "escalation-snapshot"))
            {
                yield return escalationRef;
            }

            foreach (string fingerprint in PolicyEvidenceRefs(element, "oldEscalationFingerprint", "escalation-old-fingerprint"))
            {
                yield return fingerprint;
            }

            foreach (string fingerprint in PolicyEvidenceRefs(element, "newEscalationFingerprint", "escalation-new-fingerprint"))
            {
                yield return fingerprint;
            }

            if (TryReadInt64(element, "sourceVersion", out long escalationSourceVersion))
            {
                yield return $"escalation-policy-source-version:{escalationSourceVersion.ToString(System.Globalization.CultureInfo.InvariantCulture)}";
            }

            if (element.TryGetProperty("changeSet", out JsonElement escalationChangeSet) &&
                escalationChangeSet.ValueKind == JsonValueKind.Object &&
                escalationChangeSet.TryGetProperty("entries", out JsonElement escalationEntries) &&
                escalationEntries.ValueKind == JsonValueKind.Array)
            {
                foreach (JsonElement entry in escalationEntries.EnumerateArray())
                {
                    if (TryReadString(entry, "stateClass", out string? stateClass) &&
                        AuditMetadata.SafeOptionalToken(stateClass) is { } safeStateClass)
                    {
                        yield return $"escalation-state-class:{safeStateClass}";
                    }

                    if (TryReadString(entry, "scope", out string? scope) &&
                        AuditMetadata.SafeOptionalToken(scope) is { } safeScope)
                    {
                        yield return $"escalation-scope:{safeScope}";
                    }

                    if (TryReadString(entry, "severityThreshold", out string? severity) &&
                        AuditMetadata.SafeOptionalToken(severity) is { } safeSeverity)
                    {
                        yield return $"escalation-severity:{safeSeverity}";
                    }

                    if (TryReadString(entry, "escalationTargetRole", out string? targetRole) &&
                        AuditMetadata.SafeOptionalToken(targetRole) is { } safeTargetRole)
                    {
                        yield return $"escalation-target-role:{safeTargetRole}";
                    }

                    if (TryReadString(entry, "escalationChannel", out string? channel) &&
                        AuditMetadata.SafeOptionalToken(channel) is { } safeChannel)
                    {
                        yield return $"escalation-channel:{safeChannel}";
                    }

                    if (TryReadInt64(entry, "ageThresholdSeconds", out long ageThreshold))
                    {
                        yield return $"escalation-age-threshold-seconds:{ageThreshold.ToString(System.Globalization.CultureInfo.InvariantCulture)}";
                    }
                }
            }
        }

        if (string.Equals(commandType, nameof(RecordMailboxProviderConnection), StringComparison.Ordinal))
        {
            yield return "admin-operation:mailbox-provider-connection";
            yield return "admin-scope:mailbox";
            foreach (string providerRef in PolicyEvidenceRefs(element, "providerConnectionChangeId", "mailbox-provider-change"))
            {
                yield return providerRef;
            }

            foreach (string providerRef in PolicyEvidenceRefs(element, "providerConnectionRef", "provider-connection"))
            {
                yield return providerRef;
            }

            foreach (string fingerprint in PolicyEvidenceRefs(element, "credentialFingerprint", "provider-credential-fingerprint"))
            {
                yield return fingerprint;
            }

            foreach (string evidenceRef in PolicyEvidenceRefs(element, "permissionEvidenceRef", "permission-evidence"))
            {
                yield return evidenceRef;
            }

            if (TryReadString(element, "freshness", out string? freshness) &&
                AuditMetadata.SafeOptionalToken(freshness) is { } safeFreshness)
            {
                yield return $"permission-freshness:{safeFreshness}";
            }

            if (TryReadInt64(element, "sourceVersion", out long sourceVersion))
            {
                yield return $"mailbox-config-source-version:{sourceVersion.ToString(System.Globalization.CultureInfo.InvariantCulture)}";
            }
        }

        if (string.Equals(commandType, nameof(RequestComplianceInvestigation), StringComparison.Ordinal))
        {
            yield return "admin-operation:trigger-compliance-investigation";
            yield return "admin-scope:compliance";
            foreach (string investigationRef in PolicyEvidenceRefs(element, "investigationId", "investigation"))
            {
                yield return investigationRef;
            }

            foreach (string queryRef in PolicyEvidenceRefs(element, "queryRef", "audit-query"))
            {
                yield return queryRef;
            }

            foreach (string filterRef in SafeAdminSubjectRefs(element, "filterRefs"))
            {
                yield return $"audit-filter:{filterRef}";
            }

            if (TryReadString(element, "redactionState", out string? redactionState) &&
                AuditMetadata.SafeOptionalToken(redactionState) is { } safeRedaction)
            {
                yield return $"redaction:{safeRedaction}";
            }

            if (TryReadInt64(element, "sourceVersion", out long sourceVersion))
            {
                yield return $"compliance-source-version:{sourceVersion.ToString(System.Globalization.CultureInfo.InvariantCulture)}";
            }
        }

        if (string.Equals(commandType, nameof(RequestComplianceEscalation), StringComparison.Ordinal))
        {
            yield return "admin-operation:request-compliance-escalation";
            yield return "admin-scope:compliance";
            foreach (string escalationRef in PolicyEvidenceRefs(element, "escalationId", "escalation"))
            {
                yield return escalationRef;
            }

            foreach (string investigationRef in PolicyEvidenceRefs(element, "investigationId", "investigation"))
            {
                yield return investigationRef;
            }

            foreach (string auditRef in PolicyEvidenceRefs(element, "auditRecordRef", "audit-record"))
            {
                yield return auditRef;
            }

            if (TryReadString(element, "redactionState", out string? redactionState) &&
                AuditMetadata.SafeOptionalToken(redactionState) is { } safeRedaction)
            {
                yield return $"redaction:{safeRedaction}";
            }

            if (TryReadString(element, "escalationStatus", out string? escalationStatus) &&
                AuditMetadata.SafeOptionalToken(escalationStatus) is { } safeEscalation)
            {
                yield return $"escalation:{safeEscalation}";
            }

            if (TryReadInt64(element, "sourceVersion", out long sourceVersion))
            {
                yield return $"compliance-source-version:{sourceVersion.ToString(System.Globalization.CultureInfo.InvariantCulture)}";
            }
        }

        if (string.Equals(commandType, nameof(SubmitRetentionConfigurationChange), StringComparison.Ordinal))
        {
            yield return "admin-operation:submit-retention-change";
            yield return "admin-scope:compliance";
            foreach (string retentionRef in PolicyEvidenceRefs(element, "retentionChangeId", "retention-change"))
            {
                yield return retentionRef;
            }

            foreach (string snapshotRef in PolicyEvidenceRefs(element, "sourceRetentionSnapshotId", "retention-snapshot"))
            {
                yield return snapshotRef;
            }

            foreach (string snapshotRef in PolicyEvidenceRefs(element, "proposedRetentionSnapshotId", "retention-snapshot"))
            {
                yield return snapshotRef;
            }

            foreach (string fingerprint in PolicyEvidenceRefs(element, "oldRetentionSnapshotFingerprint", "retention-old-fingerprint"))
            {
                yield return fingerprint;
            }

            foreach (string fingerprint in PolicyEvidenceRefs(element, "newRetentionSnapshotFingerprint", "retention-new-fingerprint"))
            {
                yield return fingerprint;
            }

            if (element.TryGetProperty("changeSet", out JsonElement changeSet) &&
                changeSet.ValueKind == JsonValueKind.Object)
            {
                foreach (string retentionClass in SafeObjectArrayRefs(changeSet, "windows", "retentionClassId"))
                {
                    yield return $"retention-class:{retentionClass}";
                }

                foreach (string retentionWindow in SafeObjectArrayRefs(changeSet, "windows", "retentionWindowRef"))
                {
                    yield return $"retention-window:{retentionWindow}";
                }
            }
        }

        if (string.Equals(commandType, nameof(SubmitDataClassInventoryChange), StringComparison.Ordinal))
        {
            yield return "admin-operation:submit-data-class-inventory-change";
            yield return "admin-scope:compliance";
            foreach (string inventoryRef in PolicyEvidenceRefs(element, "inventoryChangeId", "inventory-change"))
            {
                yield return inventoryRef;
            }

            foreach (string snapshotRef in PolicyEvidenceRefs(element, "sourceInventorySnapshotId", "inventory-snapshot"))
            {
                yield return snapshotRef;
            }

            foreach (string snapshotRef in PolicyEvidenceRefs(element, "proposedInventorySnapshotId", "inventory-snapshot"))
            {
                yield return snapshotRef;
            }

            foreach (string fingerprint in PolicyEvidenceRefs(element, "oldInventorySnapshotFingerprint", "inventory-old-fingerprint"))
            {
                yield return fingerprint;
            }

            foreach (string fingerprint in PolicyEvidenceRefs(element, "newInventorySnapshotFingerprint", "inventory-new-fingerprint"))
            {
                yield return fingerprint;
            }

            if (element.TryGetProperty("changeSet", out JsonElement inventoryChangeSet) &&
                inventoryChangeSet.ValueKind == JsonValueKind.Object)
            {
                foreach (string dataClass in SafeObjectArrayRefs(inventoryChangeSet, "classifications", "dataClassId"))
                {
                    yield return $"data-class:{dataClass}";
                }

                foreach (string ownerRole in SafeObjectArrayRefs(inventoryChangeSet, "classifications", "ownerRole"))
                {
                    yield return $"owner-role:{ownerRole}";
                }

                foreach (string retentionClass in SafeObjectArrayRefs(inventoryChangeSet, "classifications", "retentionClassId"))
                {
                    yield return $"retention-class:{retentionClass}";
                }

                foreach (string redaction in SafeObjectArrayRefs(inventoryChangeSet, "classifications", "redactionSensitivity"))
                {
                    yield return $"redaction-sensitivity:{redaction}";
                }

                foreach (string deletion in SafeObjectArrayRefs(inventoryChangeSet, "classifications", "deletionBehavior"))
                {
                    yield return $"deletion-behavior:{deletion}";
                }

                foreach (string export in SafeObjectArrayRefs(inventoryChangeSet, "classifications", "exportEligibility"))
                {
                    yield return $"export-eligibility:{export}";
                }
            }
        }

        if (string.Equals(commandType, nameof(SubmitTenantExportRequest), StringComparison.Ordinal))
        {
            yield return "admin-operation:submit-tenant-export-request";
            yield return "admin-scope:compliance";
            foreach (string exportRef in PolicyEvidenceRefs(element, "exportRunId", "export-run"))
            {
                yield return exportRef;
            }

            foreach (string snapshotRef in PolicyEvidenceRefs(element, "inventorySnapshotId", "inventory-snapshot"))
            {
                yield return snapshotRef;
            }

            foreach (string fingerprint in PolicyEvidenceRefs(element, "manifestFingerprint", "export-manifest-fingerprint"))
            {
                yield return fingerprint;
            }

            if (element.TryGetProperty("requestSpec", out JsonElement requestSpec) &&
                requestSpec.ValueKind == JsonValueKind.Object)
            {
                foreach (string dataClass in SafeRefArray(requestSpec, "requestedDataClassIds"))
                {
                    yield return $"data-class:{dataClass}";
                }

                if (requestSpec.TryGetProperty("scope", out JsonElement scope) &&
                    scope.ValueKind == JsonValueKind.Object)
                {
                    foreach (string tenantRef in PolicyEvidenceRefs(scope, "tenantRef", "export-scope-tenant"))
                    {
                        yield return tenantRef;
                    }

                    // Only the AUTHORIZED project refs reach the committed command, so no unauthorized ref leaks (NFR2).
                    foreach (string projectRef in SafeRefArray(scope, "projectScopeRefs"))
                    {
                        yield return $"export-scope-project:{projectRef}";
                    }
                }
            }
        }

        if (string.Equals(commandType, nameof(SubmitDeletionErasureRequest), StringComparison.Ordinal))
        {
            yield return "admin-operation:submit-deletion-erasure-request";
            yield return "admin-scope:compliance";
            foreach (string runRef in PolicyEvidenceRefs(element, "deletionRunId", "deletion-run"))
            {
                yield return runRef;
            }

            foreach (string snapshotRef in PolicyEvidenceRefs(element, "inventorySnapshotId", "inventory-snapshot"))
            {
                yield return snapshotRef;
            }

            foreach (string proofRef in PolicyEvidenceRefs(element, "proofFingerprint", "deletion-proof"))
            {
                yield return proofRef;
            }

            if (element.TryGetProperty("requestSpec", out JsonElement deletionSpec) &&
                deletionSpec.ValueKind == JsonValueKind.Object)
            {
                foreach (string modeRef in PolicyEvidenceRefs(deletionSpec, "mode", "deletion-mode"))
                {
                    yield return modeRef;
                }

                foreach (string dataClass in SafeRefArray(deletionSpec, "requestedDataClassIds"))
                {
                    yield return $"data-class:{dataClass}";
                }

                if (deletionSpec.TryGetProperty("scope", out JsonElement deletionScope) &&
                    deletionScope.ValueKind == JsonValueKind.Object)
                {
                    foreach (string tenantRef in PolicyEvidenceRefs(deletionScope, "tenantRef", "deletion-scope-tenant"))
                    {
                        yield return tenantRef;
                    }

                    // Only the AUTHORIZED project refs reach the committed command, so no unauthorized ref leaks (NFR2).
                    foreach (string projectRef in SafeRefArray(deletionScope, "projectScopeRefs"))
                    {
                        yield return $"deletion-scope-project:{projectRef}";
                    }
                }
            }
        }

        if (string.Equals(commandType, nameof(SubmitConsentLawfulBasisRecord), StringComparison.Ordinal))
        {
            yield return "admin-operation:submit-consent-lawful-basis-record";
            yield return "admin-scope:compliance";

            // The opaque subjectLocator is NEVER emitted as a ref — only the record id + scope-project localize the
            // record (NFR2). The record id, subject kind, lawful basis, status, basis source, scope project, and the
            // record fingerprint are the bounded source-evidence refs (AC3).
            foreach (string recordRef in PolicyEvidenceRefs(element, "recordId", "consent-record"))
            {
                yield return recordRef;
            }

            foreach (string subjectKindRef in PolicyEvidenceRefs(element, "subjectKind", "consent-subject-kind"))
            {
                yield return subjectKindRef;
            }

            foreach (string lawfulBasisRef in PolicyEvidenceRefs(element, "lawfulBasis", "consent-lawful-basis"))
            {
                yield return lawfulBasisRef;
            }

            foreach (string recordStatusRef in PolicyEvidenceRefs(element, "recordStatus", "consent-record-status"))
            {
                yield return recordStatusRef;
            }

            foreach (string basisSourceRef in PolicyEvidenceRefs(element, "basisSource", "consent-basis-source"))
            {
                yield return basisSourceRef;
            }

            foreach (string scopeProjectRef in PolicyEvidenceRefs(element, "projectScopeRef", "consent-scope-project"))
            {
                yield return scopeProjectRef;
            }

            foreach (string fingerprintRef in PolicyEvidenceRefs(element, "recordFingerprint", "consent-fingerprint"))
            {
                yield return fingerprintRef;
            }
        }

        if (TryReadString(element, "policySnapshotId", out string? policySnapshotId) &&
            AuditMetadata.SafeOptionalToken(policySnapshotId) is { } safePolicySnapshot)
        {
            yield return $"policy-snapshot:{safePolicySnapshot}";
        }

        if (TryReadString(element, "reasonCode", out string? reasonCode) &&
            AuditMetadata.SafeOptionalToken(reasonCode) is { } safeReason)
        {
            yield return $"reason:{safeReason}";
        }
    }

    private static IEnumerable<string> SafeObjectArrayRefs(JsonElement element, string propertyName, string refPropertyName)
    {
        if (element.ValueKind != JsonValueKind.Object ||
            !element.TryGetProperty(propertyName, out JsonElement property) ||
            property.ValueKind != JsonValueKind.Array)
        {
            yield break;
        }

        foreach (JsonElement item in property.EnumerateArray())
        {
            if (TryReadString(item, refPropertyName, out string? value) &&
                AuditMetadata.SafeOptionalToken(value) is { } safeValue)
            {
                yield return safeValue;
            }
        }
    }

    private static string RiskClassToken(Hexalith.ChatBot.Contracts.Enums.AiActionRiskClass riskClass)
        => riskClass switch
        {
            Hexalith.ChatBot.Contracts.Enums.AiActionRiskClass.LowRisk => "low-risk",
            Hexalith.ChatBot.Contracts.Enums.AiActionRiskClass.ApprovalRequired => "approval-required",
            _ => "approval-required",
        };

    private static IEnumerable<string> PolicyEvidenceRefs(JsonElement element, string propertyName, string prefix)
    {
        if (TryReadString(element, propertyName, out string? value) &&
            AuditMetadata.SafeOptionalToken(value) is { } safeValue)
        {
            yield return $"{prefix}:{safeValue}";
        }
    }

    private static string RiskActionClassToken(Hexalith.ChatBot.Contracts.Enums.AiActionRiskActionClass actionClass)
        => actionClass switch
        {
            Hexalith.ChatBot.Contracts.Enums.AiActionRiskActionClass.ModifiesState => "modifies-state",
            Hexalith.ChatBot.Contracts.Enums.AiActionRiskActionClass.ExposesFiles => "exposes-files",
            Hexalith.ChatBot.Contracts.Enums.AiActionRiskActionClass.SendsExternal => "sends-external",
            Hexalith.ChatBot.Contracts.Enums.AiActionRiskActionClass.CreatesTasks => "creates-tasks",
            Hexalith.ChatBot.Contracts.Enums.AiActionRiskActionClass.InvokesTools => "invokes-tools",
            Hexalith.ChatBot.Contracts.Enums.AiActionRiskActionClass.ActsOnBehalf => "acts-on-behalf",
            _ => "unknown",
        };

    private static bool TryReadString(JsonElement element, string propertyName, out string? value)
    {
        value = null;
        if (element.ValueKind != JsonValueKind.Object ||
            !element.TryGetProperty(propertyName, out JsonElement property) ||
            property.ValueKind != JsonValueKind.String)
        {
            return false;
        }

        value = property.GetString();
        return !string.IsNullOrWhiteSpace(value);
    }

    private static bool TryReadInt64(JsonElement element, string propertyName, out long value)
    {
        value = 0;
        return element.ValueKind == JsonValueKind.Object &&
            element.TryGetProperty(propertyName, out JsonElement property) &&
            property.ValueKind == JsonValueKind.Number &&
            property.TryGetInt64(out value);
    }

    private static bool TryReadBool(JsonElement element, string propertyName, out bool value)
    {
        value = false;
        if (element.ValueKind != JsonValueKind.Object ||
            !element.TryGetProperty(propertyName, out JsonElement property) ||
            property.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
        {
            return false;
        }

        value = property.GetBoolean();
        return true;
    }

    private static IEnumerable<string> SafeRefArray(JsonElement element, string propertyName)
    {
        if (element.ValueKind != JsonValueKind.Object ||
            !element.TryGetProperty(propertyName, out JsonElement property) ||
            property.ValueKind != JsonValueKind.Array)
        {
            yield break;
        }

        foreach (JsonElement item in property.EnumerateArray())
        {
            string? value = item.ValueKind == JsonValueKind.String ? item.GetString() : null;
            if (!string.IsNullOrWhiteSpace(value))
            {
                yield return AuditMetadata.SafeOptionalToken(value)!;
            }
        }
    }

    private static IEnumerable<string> SafeAdminSubjectRefs(JsonElement element, string propertyName)
    {
        if (element.ValueKind != JsonValueKind.Object ||
            !element.TryGetProperty(propertyName, out JsonElement property) ||
            property.ValueKind != JsonValueKind.Array)
        {
            yield break;
        }

        foreach (JsonElement item in property.EnumerateArray())
        {
            string? value = item.ValueKind == JsonValueKind.String ? item.GetString() : null;
            if (AuditMetadata.SafeOptionalToken(value) is { } safeValue)
            {
                yield return safeValue;
            }
        }
    }
}
