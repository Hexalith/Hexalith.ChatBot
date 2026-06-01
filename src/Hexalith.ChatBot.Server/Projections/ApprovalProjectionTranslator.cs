namespace Hexalith.ChatBot.Server.Projections;

internal static class ApprovalProjectionTranslator
{
    public const string ApprovalDomain = "approvals";

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
}
