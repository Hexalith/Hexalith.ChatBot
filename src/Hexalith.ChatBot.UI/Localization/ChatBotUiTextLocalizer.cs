using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.UI.Design;

using Microsoft.Extensions.Localization;

namespace Hexalith.ChatBot.UI.Localization;

/// <summary>
/// Typed phrase-level localizer for governed ChatBot UI surfaces.
/// </summary>
public sealed class ChatBotUiTextLocalizer(IStringLocalizer<SharedResource> localizer)
{
    public string this[string key] => Get(key);

    public string Get(string key, params object[] arguments)
    {
        ArgumentNullException.ThrowIfNull(arguments);

        LocalizedString value = arguments.Length == 0 ? localizer[key] : localizer[key, arguments];
        if (value.ResourceNotFound)
        {
            throw new InvalidOperationException($"Missing ChatBot UI localization resource '{key}'.");
        }

        return value.Value;
    }

    public string ActorCategoryLabel(ChatBotActorCategory category)
        => Get(ChatBotGovernedUiText.GetActorCategoryResourceKey(category));

    public string ActorBadgeAccessibleLabel(ChatBotActorCategory category, string displayLabel)
        => Get(ChatBotUiTextKey.ActorBadgeAccessibleLabel, ActorCategoryLabel(category), displayLabel);

    public string ActorBadgeResolveAccessibleLabel(ChatBotActorCategory category, string displayLabel)
        => Get(ChatBotUiTextKey.ActorBadgeResolveAccessibleLabel, ActorCategoryLabel(category), displayLabel);

    public string ActorTypeLabel(ActorType actorType)
        => Get(actorType switch
        {
            ActorType.Human => ChatBotUiTextKey.ActorLabelHuman,
            ActorType.Ai => ChatBotUiTextKey.ActorLabelAi,
            ActorType.Service => ChatBotUiTextKey.ActorLabelService,
            ActorType.System => ChatBotUiTextKey.ActorLabelSystem,
            _ => throw new ArgumentOutOfRangeException(nameof(actorType), actorType, null),
        });

    public string BlockedReasonLabel(ChatBotBlockedReason reason)
        => Get(ChatBotGovernedUiText.GetBlockedReasonResourceKey(reason));

    public string BlockedStateAccessibleLabel(ChatBotBlockedReason reason, string reasonText, string safeNextAction)
        => Get(ChatBotUiTextKey.BlockedStateAccessibleLabel, BlockedReasonLabel(reason), reasonText, safeNextAction);

    public string ConfidenceBandLabel(ThresholdBand band)
        => Get(band switch
        {
            ThresholdBand.Below => ChatBotUiTextKey.ConfidenceBandBelow,
            ThresholdBand.Within => ChatBotUiTextKey.ConfidenceBandWithin,
            ThresholdBand.Above => ChatBotUiTextKey.ConfidenceBandAbove,
            ThresholdBand.Critical => ChatBotUiTextKey.ConfidenceBandCritical,
            _ => throw new ArgumentOutOfRangeException(nameof(band), band, null),
        });

    public string EvidenceStateLabel(ChatBotEvidenceState state)
        => Get(ChatBotGovernedUiText.GetEvidenceStateResourceKey(state));

    public string EvidenceAccessibleLabel(ChatBotEvidenceState state, string label, string? reason)
        => string.IsNullOrWhiteSpace(reason)
            ? Get(ChatBotUiTextKey.EvidenceAccessibleLabel, EvidenceStateLabel(state), label)
            : Get(ChatBotUiTextKey.EvidenceAccessibleLabelWithReason, EvidenceStateLabel(state), label, reason);

    public string ParticipantStatusLabel(string? status)
        => Get(status switch
        {
            "Resolved" => ChatBotUiTextKey.ParticipantStatusResolved,
            "Unresolved" => ChatBotUiTextKey.ParticipantStatusUnresolved,
            "Rejected" => ChatBotUiTextKey.ParticipantStatusRejected,
            "Quarantined" => ChatBotUiTextKey.ParticipantStatusQuarantined,
            "Blocked" => ChatBotUiTextKey.ParticipantStatusBlocked,
            _ => ChatBotUiTextKey.ParticipantStatusUnknown,
        });

    public string ParticipantBlockedReasonLabel(string? reason)
        => Get(reason switch
        {
            "NotFound" => ChatBotUiTextKey.ParticipantBlockedReasonNotFound,
            "AmbiguousMatch" => ChatBotUiTextKey.ParticipantBlockedReasonAmbiguousMatch,
            "RestrictedParty" => ChatBotUiTextKey.ParticipantBlockedReasonRestrictedParty,
            "ErasedParty" => ChatBotUiTextKey.ParticipantBlockedReasonErasedParty,
            "TenantMismatch" => ChatBotUiTextKey.ParticipantBlockedReasonTenantMismatch,
            "DirectoryDegraded" => ChatBotUiTextKey.ParticipantBlockedReasonDirectoryDegraded,
            "DirectoryUnavailable" => ChatBotUiTextKey.ParticipantBlockedReasonDirectoryUnavailable,
            "InvalidEvidence" => ChatBotUiTextKey.ParticipantBlockedReasonInvalidEvidence,
            "UnauthorizedActor" => ChatBotUiTextKey.ParticipantBlockedReasonUnauthorizedActor,
            "UnresolvedParticipant" => ChatBotUiTextKey.ParticipantBlockedReasonUnresolvedParticipant,
            _ => ChatBotUiTextKey.ParticipantBlockedReasonUnknown,
        });

    public string ParticipantReviewActionLabel(string action)
        => Get(action switch
        {
            "Link" => ChatBotUiTextKey.ParticipantReviewActionLink,
            "CreatePending" => ChatBotUiTextKey.ParticipantReviewActionCreatePending,
            "Reject" => ChatBotUiTextKey.ParticipantReviewActionReject,
            "Quarantine" => ChatBotUiTextKey.ParticipantReviewActionQuarantine,
            _ => ChatBotUiTextKey.ParticipantReviewActionUnknown,
        });

    public string AttachmentStatusLabel(string? status)
        => Get(status switch
        {
            "Captured" => ChatBotUiTextKey.AttachmentStatusCaptured,
            "Pending" => ChatBotUiTextKey.AttachmentStatusPending,
            "Unavailable" => ChatBotUiTextKey.AttachmentStatusUnavailable,
            "Rejected" => ChatBotUiTextKey.AttachmentStatusRejected,
            "Unsafe" => ChatBotUiTextKey.AttachmentStatusUnsafe,
            "Failed" => ChatBotUiTextKey.AttachmentStatusFailed,
            "Retryable" => ChatBotUiTextKey.AttachmentStatusRetryable,
            _ => ChatBotUiTextKey.AttachmentStatusUnavailable,
        });

    public string DecisionKindLabel(string? kind)
        => Get(kind switch
        {
            "Associate" or "associate" => ChatBotUiTextKey.DecisionKindAssociate,
            "Reject" or "reject" => ChatBotUiTextKey.DecisionKindReject,
            "Defer" or "defer" => ChatBotUiTextKey.DecisionKindDefer,
            "NeedsReview" or "needs-review" => ChatBotUiTextKey.DecisionKindNeedsReview,
            _ => ChatBotUiTextKey.DecisionUnavailableValue,
        });

    public string CorrectionKindLabel(string? kind)
        => Get(kind switch
        {
            "ProjectReassignment" or "project-reassignment" => ChatBotUiTextKey.CorrectionKindProjectReassignment,
            _ => ChatBotUiTextKey.DecisionUnavailableValue,
        });

    public string RedactionStateLabel(string? state)
        => Get(state switch
        {
            "Metadata_only" or "metadata_only" => ChatBotUiTextKey.DecisionMetadataOnlyValue,
            "Redacted" or "redacted" => ChatBotUiTextKey.DecisionRedactedValue,
            "Unavailable" or "unavailable" => ChatBotUiTextKey.DecisionUnavailableValue,
            _ => ChatBotUiTextKey.DecisionUnavailableValue,
        });

    public string ApprovalEventKindLabel(string? kind)
        => Get(kind switch
        {
            "request" or "Request" => ChatBotUiTextKey.ApprovalEventKindRequest,
            "decision" or "Decision" => ChatBotUiTextKey.ApprovalEventKindDecision,
            "outcome" or "Outcome" => ChatBotUiTextKey.ApprovalEventKindOutcome,
            _ => ChatBotUiTextKey.DecisionUnavailableValue,
        });

    public string ApprovalStatusLabel(string? status)
        => Get(status switch
        {
            "pending" or "Pending" => ChatBotUiTextKey.ApprovalStatusPending,
            "approved" or "Approved" => ChatBotUiTextKey.ApprovalStatusApproved,
            "rejected" or "Rejected" => ChatBotUiTextKey.ApprovalStatusRejected,
            "revision-requested" or "RevisionRequested" => ChatBotUiTextKey.ApprovalStatusRevisionRequested,
            "cancelled" or "Cancelled" => ChatBotUiTextKey.ApprovalStatusCancelled,
            "executed" or "Executed" => ChatBotUiTextKey.ApprovalStatusExecuted,
            "failed" or "Failed" => ChatBotUiTextKey.ApprovalStatusFailed,
            _ => ChatBotUiTextKey.DecisionUnavailableValue,
        });

    public string ApprovalDecisionKindLabel(string? kind)
        => Get(kind switch
        {
            "approve" or "Approve" => ChatBotUiTextKey.ApprovalDecisionKindApprove,
            "reject" or "Reject" => ChatBotUiTextKey.ApprovalDecisionKindReject,
            "request-revision" or "RequestRevision" => ChatBotUiTextKey.ApprovalDecisionKindRequestRevision,
            "cancel" or "Cancel" => ChatBotUiTextKey.ApprovalDecisionKindCancel,
            _ => ChatBotUiTextKey.DecisionUnavailableValue,
        });

    public string ApprovalEvidenceFreshnessLabel(string? freshness)
        => Get(freshness switch
        {
            "fresh" or "Fresh" => ChatBotUiTextKey.ApprovalEvidenceFreshnessFresh,
            "stale" or "Stale" => ChatBotUiTextKey.ApprovalEvidenceFreshnessStale,
            "expired" or "Expired" => ChatBotUiTextKey.ApprovalEvidenceFreshnessExpired,
            _ => ChatBotUiTextKey.DecisionUnavailableValue,
        });

    public string ApprovalDisabledReasonLabel(string? reason)
        => Get(reason switch
        {
            "insufficient-authority" or "InsufficientAuthority" => ChatBotUiTextKey.ApprovalDisabledReasonInsufficientAuthority,
            "state-not-permitted" or "StateNotPermitted" => ChatBotUiTextKey.ApprovalDisabledReasonStateNotPermitted,
            "dependency-degraded" or "DependencyDegraded" => ChatBotUiTextKey.ApprovalDisabledReasonDependencyDegraded,
            "awaiting-other-actor" or "AwaitingOtherActor" => ChatBotUiTextKey.ApprovalDisabledReasonAwaitingOtherActor,
            "policy-blocked" or "PolicyBlocked" => ChatBotUiTextKey.ApprovalDisabledReasonPolicyBlocked,
            "evidence-expired" or "EvidenceExpired" => ChatBotUiTextKey.ApprovalDisabledReasonEvidenceExpired,
            _ => ChatBotUiTextKey.DecisionUnavailableValue,
        });

    public string FailureStateKindLabel(string? kind)
        => Get(kind switch
        {
            "failure" or "Failure" => ChatBotUiTextKey.FailureStateKindFailure,
            "retry-queued" or "RetryQueued" => ChatBotUiTextKey.FailureStateKindRetryQueued,
            "retry-accepted" or "RetryAccepted" => ChatBotUiTextKey.FailureStateKindRetryAccepted,
            "retry-exhausted" or "RetryExhausted" => ChatBotUiTextKey.FailureStateKindRetryExhausted,
            "blocked" or "Blocked" => ChatBotUiTextKey.FailureStateKindBlocked,
            "duplicate-suppressed" or "DuplicateSuppressed" => ChatBotUiTextKey.FailureStateKindDuplicateSuppressed,
            "dependency-degraded" or "DependencyDegraded" => ChatBotUiTextKey.FailureStateKindDependencyDegraded,
            "projection-retryable" or "ProjectionRetryable" => ChatBotUiTextKey.FailureStateKindProjectionRetryable,
            "terminal-failure" or "TerminalFailure" => ChatBotUiTextKey.FailureStateKindTerminalFailure,
            "reprocess-created" or "ReprocessCreated" => ChatBotUiTextKey.FailureStateKindReprocessCreated,
            _ => ChatBotUiTextKey.DecisionUnavailableValue,
        });

    public string FailureStatusLabel(string? status)
        => Get(status switch
        {
            "retryable" or "Retryable" => ChatBotUiTextKey.FailureStatusRetryable,
            "terminal" or "Terminal" => ChatBotUiTextKey.FailureStatusTerminal,
            "blocked" or "Blocked" => ChatBotUiTextKey.FailureStatusBlocked,
            "degraded" or "Degraded" => ChatBotUiTextKey.FailureStatusDegraded,
            "resolved" or "Resolved" => ChatBotUiTextKey.FailureStatusResolved,
            "unknown" or "Unknown" => ChatBotUiTextKey.FailureStatusUnknown,
            _ => ChatBotUiTextKey.FailureStatusUnknown,
        });

    public string FailureBlockedReasonLabel(string? reason)
        => Get(reason switch
        {
            "insufficient-authority" or "InsufficientAuthority" => ChatBotUiTextKey.FailureBlockedReasonInsufficientAuthority,
            "state-not-permitted" or "StateNotPermitted" => ChatBotUiTextKey.FailureBlockedReasonStateNotPermitted,
            "dependency-degraded" or "DependencyDegraded" => ChatBotUiTextKey.FailureBlockedReasonDependencyDegraded,
            "awaiting-other-actor" or "AwaitingOtherActor" => ChatBotUiTextKey.FailureBlockedReasonAwaitingOtherActor,
            "policy-blocked" or "PolicyBlocked" => ChatBotUiTextKey.FailureBlockedReasonPolicyBlocked,
            "unresolved-participant" or "UnresolvedParticipant" => ChatBotUiTextKey.FailureBlockedReasonUnresolvedParticipant,
            "participant-directory-degraded" or "ParticipantDirectoryDegraded" => ChatBotUiTextKey.FailureBlockedReasonParticipantDirectoryDegraded,
            "candidate-required" or "CandidateRequired" => ChatBotUiTextKey.FailureBlockedReasonCandidateRequired,
            "evidence-expired" or "EvidenceExpired" => ChatBotUiTextKey.FailureBlockedReasonEvidenceExpired,
            "not-authorized" or "NotAuthorized" => ChatBotUiTextKey.FailureBlockedReasonNotAuthorized,
            "projection-pending" or "ProjectionPending" => ChatBotUiTextKey.FailureBlockedReasonProjectionPending,
            "terminal-state" or "TerminalState" => ChatBotUiTextKey.FailureBlockedReasonTerminalState,
            "already-decided" or "AlreadyDecided" => ChatBotUiTextKey.FailureBlockedReasonAlreadyDecided,
            "already-corrected" or "AlreadyCorrected" => ChatBotUiTextKey.FailureBlockedReasonAlreadyCorrected,
            "audit-unavailable" or "AuditUnavailable" => ChatBotUiTextKey.FailureBlockedReasonAuditUnavailable,
            "duplicate-suppressed" or "DuplicateSuppressed" => ChatBotUiTextKey.FailureBlockedReasonDuplicateSuppressed,
            "retry-exhausted" or "RetryExhausted" => ChatBotUiTextKey.FailureBlockedReasonRetryExhausted,
            "reprocess-created" or "ReprocessCreated" => ChatBotUiTextKey.FailureBlockedReasonReprocessCreated,
            "correction-delayed" or "CorrectionDelayed" => ChatBotUiTextKey.FailureBlockedReasonCorrectionDelayed,
            "unsafe-context" or "UnsafeContext" => ChatBotUiTextKey.FailureBlockedReasonUnsafeContext,
            _ => ChatBotUiTextKey.DecisionUnavailableValue,
        });

    public string FailureCatalogHeadline(string? code)
        => Get(code switch
        {
            "authorization_denied" => ChatBotUiTextKey.FailureCatalogAuthorizationDenied,
            "refusal_blocked_action" => ChatBotUiTextKey.FailureCatalogRefusalBlockedAction,
            "unresolved_participant" => ChatBotUiTextKey.FailureCatalogUnresolvedParticipant,
            "association_evidence_expired" => ChatBotUiTextKey.FailureCatalogAssociationEvidenceExpired,
            "association_stale_evidence" => ChatBotUiTextKey.FailureCatalogAssociationStaleEvidence,
            "association_correction_audit_unavailable" => ChatBotUiTextKey.FailureCatalogAssociationCorrectionAuditUnavailable,
            "association_correction_propagation_delayed" => ChatBotUiTextKey.FailureCatalogAssociationCorrectionPropagationDelayed,
            "duplicate_suppressed" => ChatBotUiTextKey.FailureCatalogDuplicateSuppressed,
            "retry_queued" => ChatBotUiTextKey.FailureCatalogRetryQueued,
            "retry_accepted" => ChatBotUiTextKey.FailureCatalogRetryAccepted,
            "retry_exhausted" => ChatBotUiTextKey.FailureCatalogRetryExhausted,
            "terminal_failure" => ChatBotUiTextKey.FailureCatalogTerminalFailure,
            "audit_unavailable" => ChatBotUiTextKey.FailureCatalogAuditUnavailable,
            "dependency_degraded" => ChatBotUiTextKey.FailureCatalogDependencyDegraded,
            "failed_attachment" => ChatBotUiTextKey.FailureCatalogFailedAttachment,
            "failed_command" => ChatBotUiTextKey.FailureCatalogFailedCommand,
            "recoverable_mailbox_degradation" => ChatBotUiTextKey.FailureCatalogRecoverableMailboxDegradation,
            "projection_retryable" => ChatBotUiTextKey.FailureCatalogProjectionRetryable,
            "reprocess_created" => ChatBotUiTextKey.FailureCatalogReprocessCreated,
            _ => ChatBotUiTextKey.DecisionUnavailableValue,
        });

    public string FailureCatalogReason(string? code)
        => Get(code switch
        {
            "authorization_denied" => ChatBotUiTextKey.FailureCatalogReasonAuthorizationDenied,
            "refusal_blocked_action" => ChatBotUiTextKey.FailureCatalogReasonRefusalBlockedAction,
            "unresolved_participant" => ChatBotUiTextKey.FailureCatalogReasonUnresolvedParticipant,
            "association_evidence_expired" => ChatBotUiTextKey.FailureCatalogReasonAssociationEvidenceExpired,
            "association_stale_evidence" => ChatBotUiTextKey.FailureCatalogReasonAssociationStaleEvidence,
            "association_correction_audit_unavailable" => ChatBotUiTextKey.FailureCatalogReasonAssociationCorrectionAuditUnavailable,
            "association_correction_propagation_delayed" => ChatBotUiTextKey.FailureCatalogReasonAssociationCorrectionPropagationDelayed,
            "duplicate_suppressed" => ChatBotUiTextKey.FailureCatalogReasonDuplicateSuppressed,
            "retry_queued" => ChatBotUiTextKey.FailureCatalogReasonRetryQueued,
            "retry_accepted" => ChatBotUiTextKey.FailureCatalogReasonRetryAccepted,
            "retry_exhausted" => ChatBotUiTextKey.FailureCatalogReasonRetryExhausted,
            "terminal_failure" => ChatBotUiTextKey.FailureCatalogReasonTerminalFailure,
            "audit_unavailable" => ChatBotUiTextKey.FailureCatalogReasonAuditUnavailable,
            "dependency_degraded" => ChatBotUiTextKey.FailureCatalogReasonDependencyDegraded,
            "failed_attachment" => ChatBotUiTextKey.FailureCatalogReasonFailedAttachment,
            "failed_command" => ChatBotUiTextKey.FailureCatalogReasonFailedCommand,
            "recoverable_mailbox_degradation" => ChatBotUiTextKey.FailureCatalogReasonRecoverableMailboxDegradation,
            "projection_retryable" => ChatBotUiTextKey.FailureCatalogReasonProjectionRetryable,
            "reprocess_created" => ChatBotUiTextKey.FailureCatalogReasonReprocessCreated,
            _ => ChatBotUiTextKey.DecisionUnavailableReason,
        });

    public string FailureNextActionLabel(string? action)
        => Get(action switch
        {
            "retry-later" or "RetryLater" => ChatBotUiTextKey.FailureNextActionRetryLater,
            "request-access" or "RequestAccess" => ChatBotUiTextKey.FailureNextActionRequestAccess,
            "escalate" or "Escalate" => ChatBotUiTextKey.FailureNextActionEscalate,
            "none" or "None" => ChatBotUiTextKey.FailureNextActionNone,
            _ => ChatBotUiTextKey.SafeNextActionDefault,
        });

    public string OffSurfaceRedactedNotice()
        => Get(ChatBotUiTextKey.OffSurfaceRedactedNotice);

    public string OffSurfaceEscalationGuidance()
        => Get(ChatBotUiTextKey.OffSurfaceEscalationGuidance);

    public string OffSurfaceUnavailableReason()
        => Get(ChatBotUiTextKey.OffSurfaceUnavailableReason);

    public string ActiveFilterSummary(string filterDescription, int resultCount)
        => Get(ChatBotUiTextKey.ActiveFilterSummaryTemplate, filterDescription, resultCount);

    public string RecoveryDuplicateSafeRetry()
        => Get(ChatBotUiTextKey.RecoveryDuplicateSafeRetry);

    public string RecoverySafeNextAction(ChatBotRecoveryFlow flow)
        => Get(flow switch
        {
            ChatBotRecoveryFlow.AssociationReview => ChatBotUiTextKey.RecoverySafeNextActionAssociationReview,
            ChatBotRecoveryFlow.AiActionReview => ChatBotUiTextKey.RecoverySafeNextActionAiActionReview,
            ChatBotRecoveryFlow.QueueRetry => ChatBotUiTextKey.RecoverySafeNextActionQueueRetry,
            ChatBotRecoveryFlow.Correction => ChatBotUiTextKey.RecoverySafeNextActionCorrection,
            ChatBotRecoveryFlow.TenantConfiguration => ChatBotUiTextKey.RecoverySafeNextActionTenantConfiguration,
            _ => throw new ArgumentOutOfRangeException(nameof(flow), flow, null),
        });

    public string FeedbackKindLabel(ChatBotFeedbackKind kind)
        => Get(ChatBotGovernedUiText.GetFeedbackKindResourceKey(kind));

    public string RiskActionClassLabel(ChatBotRiskActionClass riskClass)
        => Get(ChatBotGovernedUiText.GetRiskActionClassResourceKey(riskClass));

    public string RiskAccessibleLabel(ChatBotRiskActionClass riskClass, string policyReason)
        => Get(ChatBotUiTextKey.RiskAccessibleLabel, RiskActionClassLabel(riskClass), policyReason);

    public string StatusAccessibleLabel(ChatBotFeedbackKind kind, string message)
        => Get(ChatBotUiTextKey.StatusAccessibleLabel, FeedbackKindLabel(kind), message);

    public string WhyUnavailableAccessibleLabel(string disabledReason)
        => Get(ChatBotUiTextKey.WhyUnavailableAccessible, disabledReason);
}
