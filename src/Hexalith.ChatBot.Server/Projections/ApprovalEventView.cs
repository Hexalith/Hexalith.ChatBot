using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Server.Projections;

internal sealed record ApprovalEventView(
    string TenantId,
    string ProjectId,
    string ApprovalId,
    ApprovalEventKind EventKind,
    ApprovalStatus Status,
    DateTimeOffset OccurredAtUtc,
    long SourceVersion,
    string CorrelationId,
    string? ProposalId = null,
    string? SourceMessageId = null,
    string? SourceConversationItemId = null,
    string? RequesterId = null,
    string? RequesterActorType = null,
    DateTimeOffset? RequestedAtUtc = null,
    string? CommandName = null,
    string? CommandAllowlistVersion = null,
    RiskClass? RiskClass = null,
    IReadOnlyList<string>? RiskActionClasses = null,
    AiActionRiskClass? AiRiskClass = null,
    IReadOnlyList<string>? AiRiskActionClasses = null,
    string? AiRiskInputTuple = null,
    string? PolicySnapshotId = null,
    string? PolicySnapshotVisibility = null,
    IReadOnlyList<string>? EvidenceReferences = null,
    IReadOnlyList<ApprovalEvidenceFreshness>? EvidenceFreshnessStates = null,
    IReadOnlyList<string>? AffectedResourceReferences = null,
    IReadOnlyList<string>? RecipientReferences = null,
    string? SenderAuthorityClass = null,
    string? ExpectedPostStateRedactionState = null,
    string? ActionSummaryRedactionState = null,
    ApprovalDecisionKind? DecisionKind = null,
    string? DecisionActorId = null,
    string? DecisionActorType = null,
    DateTimeOffset? DecidedAtUtc = null,
    string? AuthorityResult = null,
    string? DisabledReason = null,
    string? DecisionRationaleRedactionState = null,
    string? AuditOperationId = null,
    string? AuditStatus = null,
    string? CommandOutcomeStatus = null,
    DateTimeOffset? OutcomeAtUtc = null,
    string? ProjectedOutcomeItemId = null,
    string? FailureCode = null,
    string? Retryability = null,
    string? SupersedesApprovalId = null,
    string? SupersededByApprovalId = null,
    string? SafeNextAction = null,
    string RedactionState = "metadata_only",
    string RetentionClass = "collaboration_input")
{
    public string StableItemId => ProjectConversationItemView.ApprovalItemIdFor(ApprovalId, EventKind, SourceVersion);

    public static string KeyFor(string tenantId, string projectId, string approvalId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(projectId);
        ArgumentException.ThrowIfNullOrWhiteSpace(approvalId);
        return $"{tenantId}:project-conversation:{projectId}:approval:{approvalId}";
    }

    public ApprovalEventView WithRequestContext(ApprovalEventView request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (!string.Equals(TenantId, request.TenantId, StringComparison.Ordinal) ||
            !string.Equals(ProjectId, request.ProjectId, StringComparison.Ordinal) ||
            !string.Equals(ApprovalId, request.ApprovalId, StringComparison.Ordinal))
        {
            return this;
        }

        return this with
        {
            ProposalId = ProposalId ?? request.ProposalId,
            SourceMessageId = SourceMessageId ?? request.SourceMessageId,
            SourceConversationItemId = SourceConversationItemId ?? request.SourceConversationItemId,
            RequesterId = RequesterId ?? request.RequesterId,
            RequesterActorType = RequesterActorType ?? request.RequesterActorType,
            RequestedAtUtc = RequestedAtUtc ?? request.RequestedAtUtc,
            CommandName = CommandName ?? request.CommandName,
            CommandAllowlistVersion = CommandAllowlistVersion ?? request.CommandAllowlistVersion,
            RiskClass = RiskClass ?? request.RiskClass,
            RiskActionClasses = RiskActionClasses ?? request.RiskActionClasses,
            AiRiskClass = AiRiskClass ?? request.AiRiskClass,
            AiRiskActionClasses = AiRiskActionClasses ?? request.AiRiskActionClasses,
            AiRiskInputTuple = AiRiskInputTuple ?? request.AiRiskInputTuple,
            PolicySnapshotId = PolicySnapshotId ?? request.PolicySnapshotId,
            PolicySnapshotVisibility = PolicySnapshotVisibility ?? request.PolicySnapshotVisibility,
            EvidenceReferences = EvidenceReferences ?? request.EvidenceReferences,
            EvidenceFreshnessStates = EvidenceFreshnessStates ?? request.EvidenceFreshnessStates,
            AffectedResourceReferences = AffectedResourceReferences ?? request.AffectedResourceReferences,
            RecipientReferences = RecipientReferences ?? request.RecipientReferences,
            SenderAuthorityClass = SenderAuthorityClass ?? request.SenderAuthorityClass,
            ExpectedPostStateRedactionState = ExpectedPostStateRedactionState ?? request.ExpectedPostStateRedactionState,
            ActionSummaryRedactionState = ActionSummaryRedactionState ?? request.ActionSummaryRedactionState,
        };
    }
}
