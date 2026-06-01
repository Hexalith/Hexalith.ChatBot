using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Server.Governance.AiMediation;

namespace Hexalith.ChatBot.Server.Projections;

internal static class ApprovalProjectionTranslator
{
    public const string ApprovalDomain = "approvals";
    public const string ChatBotDomain = "chatbot";

    public static ApprovalEventView? TryCreateView(PublishedApprovalEvent published)
    {
        ArgumentNullException.ThrowIfNull(published);
        if (!string.Equals(published.Domain, ApprovalDomain, StringComparison.Ordinal) ||
            string.IsNullOrWhiteSpace(published.TenantId) ||
            string.IsNullOrWhiteSpace(published.AggregateId) ||
            string.IsNullOrWhiteSpace(published.ProjectId) ||
            string.IsNullOrWhiteSpace(published.ApprovalId) ||
            published.SourceVersion <= 0 ||
            published.OccurredAtUtc == default ||
            string.IsNullOrWhiteSpace(published.CorrelationId))
        {
            return null;
        }

        return new ApprovalEventView(
            published.TenantId,
            published.ProjectId,
            published.ApprovalId,
            published.EventKind,
            published.Status,
            published.OccurredAtUtc,
            published.SourceVersion,
            published.CorrelationId,
            published.ProposalId,
            published.SourceMessageId,
            published.SourceConversationItemId,
            published.RequesterId,
            published.RequesterActorType,
            published.RequestedAtUtc,
            published.CommandName,
            published.CommandAllowlistVersion,
            published.RiskClass,
            published.RiskActionClasses,
            published.AiRiskClass,
            published.AiRiskActionClasses,
            published.AiRiskInputTuple,
            published.PolicySnapshotId,
            published.PolicySnapshotVisibility,
            published.EvidenceReferences,
            published.EvidenceFreshnessStates,
            published.AffectedResourceReferences,
            published.RecipientReferences,
            published.SenderAuthorityClass,
            published.ExpectedPostStateRedactionState,
            published.ActionSummaryRedactionState,
            published.DecisionKind,
            published.DecisionActorId,
            published.DecisionActorType,
            published.DecidedAtUtc,
            published.AuthorityResult,
            published.DisabledReason,
            published.DecisionRationaleRedactionState,
            published.AuditOperationId,
            published.AuditStatus,
            published.CommandOutcomeStatus,
            published.OutcomeAtUtc,
            published.ProjectedOutcomeItemId,
            published.FailureCode,
            published.Retryability,
            published.SupersedesApprovalId,
            published.SupersededByApprovalId,
            published.SafeNextAction,
            published.RedactionState,
            published.RetentionClass);
    }

    public static ApprovalEventView? TryCreateView(PublishedAiActionApprovalEvent published)
    {
        ArgumentNullException.ThrowIfNull(published);
        if (!string.Equals(published.Domain, ChatBotDomain, StringComparison.Ordinal) ||
            !IsSupportedApprovalEvent(published.EventTypeName) ||
            published.SequenceNumber <= 0 ||
            published.Timestamp == default ||
            !IsSafeMetadataToken(published.TenantId) ||
            !IsSafeMetadataToken(published.AggregateId) ||
            !IsSafeMetadataToken(published.CorrelationId))
        {
            return null;
        }

        return published.EventTypeName switch
        {
            string eventType when string.Equals(eventType, typeof(AiActionApprovalRequested).FullName, StringComparison.Ordinal) =>
                TryCreateRequestView(published),
            string eventType when string.Equals(eventType, typeof(AiActionApprovalDecisionRecorded).FullName, StringComparison.Ordinal) =>
                TryCreateDecisionView(published),
            _ => null,
        };
    }

    private static ApprovalEventView? TryCreateRequestView(PublishedAiActionApprovalEvent published)
    {
        AiActionApprovalRequested? request = published.Request;
        if (request is null ||
            !IsSafeRequiredRequest(request) ||
            !string.Equals(published.CorrelationId, request.CorrelationId, StringComparison.Ordinal))
        {
            return null;
        }

        return new ApprovalEventView(
            published.TenantId!,
            request.ProjectId,
            request.ApprovalId,
            ApprovalEventKind.Request,
            ApprovalStatus.Pending,
            request.RequestedAtUtc == default ? published.Timestamp : request.RequestedAtUtc.ToUniversalTime(),
            request.SourceVersion,
            request.CorrelationId,
            request.ProposalId,
            request.SourceMessageId,
            SafeOptionalToken(request.SourceConversationItemId),
            request.RequesterId,
            SafeOptionalToken(request.RequesterActorType),
            request.RequestedAtUtc == default ? published.Timestamp : request.RequestedAtUtc.ToUniversalTime(),
            request.CommandName,
            request.CommandAllowlistVersion,
            AiRiskAsGenericRisk(request.AiRiskClass),
            request.AiRiskActionClasses,
            request.AiRiskClass,
            request.AiRiskActionClasses,
            request.RiskInputTuple,
            request.PolicySnapshotId,
            request.PolicySnapshotVisibility,
            request.EvidenceReferences,
            request.EvidenceFreshnessStates,
            request.AffectedResourceReferences,
            request.RecipientReferences,
            request.SenderAuthorityClass,
            request.ExpectedPostStateRedactionState,
            request.ActionSummaryRedactionState,
            DisabledReason: request.EvidenceFreshnessStates.Count != request.EvidenceReferences.Count ||
                request.EvidenceFreshnessStates.Any(static freshness => freshness is ApprovalEvidenceFreshness.Expired)
                    ? "evidence-expired"
                    : null,
            SafeNextAction: "review-ai-action",
            RedactionState: request.RedactionState,
            RetentionClass: request.RetentionClass);
    }

    private static ApprovalEventView? TryCreateDecisionView(PublishedAiActionApprovalEvent published)
    {
        AiActionApprovalDecisionRecorded? decision = published.Decision;
        if (decision is null ||
            !IsSafeRequiredDecision(decision) ||
            !string.Equals(published.CorrelationId, decision.CorrelationId, StringComparison.Ordinal))
        {
            return null;
        }

        return new ApprovalEventView(
            published.TenantId!,
            decision.ProjectId,
            decision.ApprovalId,
            ApprovalEventKind.Decision,
            StatusFor(decision.DecisionKind),
            decision.DecidedAtUtc == default ? published.Timestamp : decision.DecidedAtUtc.ToUniversalTime(),
            decision.SourceVersion,
            decision.CorrelationId,
            decision.ProposalId,
            decision.SourceMessageId,
            DecisionKind: decision.DecisionKind,
            DecisionActorId: decision.DecisionActorId,
            DecisionActorType: decision.DecisionActorType,
            DecidedAtUtc: decision.DecidedAtUtc == default ? published.Timestamp : decision.DecidedAtUtc.ToUniversalTime(),
            AuthorityResult: decision.AuthorityResult,
            DisabledReason: decision.DisabledReason,
            DecisionRationaleRedactionState: decision.DecisionRationaleRedactionState,
            AuditOperationId: decision.AuditOperationId,
            AuditStatus: decision.AuditStatus,
            PolicySnapshotId: decision.PolicySnapshotId,
            SafeNextAction: decision.SafeNextAction,
            RedactionState: decision.RedactionState,
            RetentionClass: decision.RetentionClass);
    }

    private static bool IsSupportedApprovalEvent(string? eventTypeName)
        => string.Equals(eventTypeName, typeof(AiActionApprovalRequested).FullName, StringComparison.Ordinal) ||
            string.Equals(eventTypeName, typeof(AiActionApprovalDecisionRecorded).FullName, StringComparison.Ordinal);

    private static bool IsSafeRequiredRequest(AiActionApprovalRequested request)
        => IsSafeMetadataToken(request.ProjectId) &&
            IsSafeMetadataToken(request.ApprovalId) &&
            IsSafeMetadataToken(request.ProposalId) &&
            IsSafeMetadataToken(request.TaskIntentId) &&
            IsSafeMetadataToken(request.SourceMessageId) &&
            IsSafeMetadataToken(request.RequesterId) &&
            IsSafeMetadataToken(request.CommandName) &&
            IsSafeMetadataToken(request.CommandAllowlistVersion) &&
            IsSafeMetadataToken(request.RiskInputTuple) &&
            IsSafeMetadataToken(request.PolicySnapshotId) &&
            IsSafeMetadataToken(request.PolicySnapshotVisibility) &&
            IsSafeMetadataToken(request.SenderAuthorityClass) &&
            IsSafeMetadataToken(request.ExpectedPostStateRedactionState) &&
            IsSafeMetadataToken(request.ActionSummaryRedactionState) &&
            IsSafeMetadataToken(request.CorrelationId) &&
            IsSafeMetadataToken(request.RedactionState) &&
            IsSafeMetadataToken(request.RetentionClass) &&
            request.SourceVersion > 0 &&
            request.RequestedAtUtc != default &&
            request.EvidenceReferences.All(IsSafeMetadataToken) &&
            request.AiRiskActionClasses.All(IsSafeMetadataToken) &&
            request.AffectedResourceReferences.All(IsSafeMetadataToken) &&
            request.RecipientReferences.All(IsSafeMetadataToken);

    private static bool IsSafeRequiredDecision(AiActionApprovalDecisionRecorded decision)
        => IsSafeMetadataToken(decision.ProjectId) &&
            IsSafeMetadataToken(decision.ApprovalId) &&
            IsSafeMetadataToken(decision.ProposalId) &&
            IsSafeMetadataToken(decision.SourceMessageId) &&
            IsSafeMetadataToken(decision.DecisionActorId) &&
            IsSafeMetadataToken(decision.DecisionActorType) &&
            IsSafeMetadataToken(decision.AuthorityResult) &&
            IsSafeMetadataToken(decision.DecisionRationaleRedactionState) &&
            IsSafeMetadataToken(decision.AuditOperationId) &&
            IsSafeMetadataToken(decision.AuditStatus) &&
            IsSafeMetadataToken(decision.PolicySnapshotId) &&
            IsSafeMetadataToken(decision.SafeNextAction) &&
            IsSafeMetadataToken(decision.CorrelationId) &&
            IsSafeMetadataToken(decision.RedactionState) &&
            IsSafeMetadataToken(decision.RetentionClass) &&
            decision.SourceVersion > 0 &&
            decision.DecidedAtUtc != default;

    private static ApprovalStatus StatusFor(ApprovalDecisionKind decision)
        => decision switch
        {
            ApprovalDecisionKind.Approve => ApprovalStatus.Approved,
            ApprovalDecisionKind.Reject => ApprovalStatus.Rejected,
            ApprovalDecisionKind.RequestRevision => ApprovalStatus.RevisionRequested,
            ApprovalDecisionKind.Cancel => ApprovalStatus.Cancelled,
            _ => ApprovalStatus.Failed,
        };

    private static RiskClass AiRiskAsGenericRisk(AiActionRiskClass risk)
        => risk is AiActionRiskClass.LowRisk ? RiskClass.Low : RiskClass.High;

    private static string? SafeOptionalToken(string? value)
        => IsSafeMetadataToken(value) ? value : null;

    private static bool IsSafeMetadataToken(string? value)
        => !string.IsNullOrWhiteSpace(value) &&
            value.Length <= 280 &&
            value.All(static c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_' or '.' or ':');
}
