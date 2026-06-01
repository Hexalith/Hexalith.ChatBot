using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Contracts.Queries;
using Hexalith.ChatBot.Server.Governance.AiMediation;

namespace Hexalith.ChatBot.Server.Projections;

internal static class LowRiskAiOutcomeProjectionTranslator
{
    public static PublishedAiOutcomeEvent FromStarted(
        string tenantId,
        string actorId,
        long sourceVersion,
        LowRiskAiAssistanceExecutionStarted started)
    {
        ArgumentNullException.ThrowIfNull(started);
        return new PublishedAiOutcomeEvent(
            tenantId,
            AiOutcomeProjectionTranslator.AiOutcomeDomain,
            started.ExecutionId,
            sourceVersion,
            started.StartedAtUtc,
            started.CorrelationId,
            started.ProjectId,
            AiOutcomeKind.ExecutionStarted,
            AiOutcomeStatus.Executing,
            actorId,
            "ai",
            ProposalId: started.ProposalId,
            RequesterId: started.RequesterId,
            SourceMessageId: started.SourceMessageId,
            OperationId: started.ExecutionId,
            RiskClass: AiActionRiskClass.LowRisk,
            RiskActionClasses: [],
            PolicyReasonCode: started.PolicyReasonCode,
            PolicySnapshotId: started.PolicySnapshotId,
            PolicySnapshotVisibility: "authorized",
            ContextPackageId: started.ContextPackageId,
            ContextPackageVersion: started.ContextPackageVersion,
            ExecutionStatus: "executing",
            AuditOperationId: $"audit:{started.ExecutionId}",
            AuditStatus: "available",
            SafeNextAction: "none",
            RedactionState: started.RedactionState,
            RetentionClass: started.RetentionClass);
    }

    public static PublishedAiOutcomeEvent FromCompleted(
        string tenantId,
        string projectId,
        string actorId,
        long sourceVersion,
        LowRiskAiAssistanceExecutionRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);
        bool success = string.Equals(record.Outcome, "success", StringComparison.Ordinal);
        bool pendingApproval = string.Equals(record.Outcome, "pending-approval", StringComparison.Ordinal);
        return new PublishedAiOutcomeEvent(
            tenantId,
            AiOutcomeProjectionTranslator.AiOutcomeDomain,
            record.ExecutionId,
            sourceVersion,
            record.GeneratedAtUtc,
            record.CorrelationId,
            projectId,
            pendingApproval ? AiOutcomeKind.ApprovalLinked : success ? AiOutcomeKind.ExecutionSucceeded : AiOutcomeKind.ExecutionFailed,
            pendingApproval ? AiOutcomeStatus.PendingApproval : success ? AiOutcomeStatus.Succeeded : AiOutcomeStatus.Failed,
            actorId,
            "ai",
            ProposalId: record.ProposalId,
            OperationId: record.ExecutionId,
            RiskClass: AiActionRiskClass.LowRisk,
            RiskActionClasses: [],
            PolicyReasonCode: record.PolicyReasonCode,
            PolicySnapshotId: record.PolicySnapshotId,
            PolicySnapshotVisibility: "authorized",
            ContextPackageId: record.ContextPackageId,
            ContextPackageVersion: record.ContextPackageVersion,
            ContextRedactionState: record.ContextRedactionState,
            AuthorizedContextReferences: record.SourceEvidenceIds,
            GeneratedSummaryRedactionState: record.GeneratedSummaryRedactionState,
            GeneratedContentVisibility: record.GeneratedContentVisibility,
            ApprovalStatus: pendingApproval ? WireToken(ApprovalStatus.Pending) : null,
            ExecutionStatus: record.Outcome,
            ExecutionOutcomeCode: pendingApproval ? record.FailureCode : success ? "low-risk-assistance-generated" : record.FailureCode,
            AuditOperationId: record.AuditOperationId,
            AuditStatus: record.AuditStatus,
            FailureCode: record.FailureCode,
            Retryability: record.Retryability,
            SafeNextAction: record.SafeNextAction,
            RedactionState: record.RedactionState,
            RetentionClass: record.RetentionClass);
    }

    private static string WireToken(Enum value)
        => value.GetType()
            .GetField(value.ToString())!
            .GetCustomAttributes(typeof(System.Runtime.Serialization.EnumMemberAttribute), false)
            .OfType<System.Runtime.Serialization.EnumMemberAttribute>()
            .FirstOrDefault()
            ?.Value
            ?? value.ToString();
}
