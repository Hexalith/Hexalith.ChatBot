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
            // Story 10.6b producer: the started ("executing") event of the real low-risk AI-assistance lifecycle is a
            // NON-terminal AI response progress so the typed read exposes an in-flight response (ActiveStreamingProgress
            // non-null -> Stop control can enable). Metadata-only; no response text/chunks/prompts.
            AiResponseSequence: sourceVersion,
            AiResponseProgressState: "rendering",
            AiResponseTerminalReason: "none",
            AiResponseVisibilityState: "metadata_only",
            AiResponseIsTerminal: false,
            RedactionState: started.RedactionState,
            RetentionClass: started.RetentionClass);
    }

    public static PublishedAiOutcomeEvent FromCompleted(
        string tenantId,
        string projectId,
        string actorId,
        long sourceVersion,
        LowRiskAiAssistanceExecutionRecord record,
        IReadOnlyList<string>? authorizedContextReferences = null,
        IReadOnlyList<string>? excludedContextReasons = null,
        string? requesterId = null,
        string? sourceMessageId = null,
        string? sourceConversationItemId = null)
    {
        ArgumentNullException.ThrowIfNull(record);
        bool success = string.Equals(record.Outcome, "success", StringComparison.Ordinal);
        bool pendingApproval = string.Equals(record.Outcome, "pending-approval", StringComparison.Ordinal);
        // Story 10.6b producer: close the AI response with a server-verified terminal progress state. success -> completed,
        // failure -> failed, routed-to-approval -> unavailable (no inline response was delivered; the proposal is reviewed
        // through the Epic 4 approval surface). The token doubles as the terminal reason (valid AiResponseTerminalReason).
        string responseTerminalState = pendingApproval ? "unavailable" : success ? "completed" : "failed";
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
            RequesterId: requesterId,
            SourceConversationItemId: sourceConversationItemId,
            SourceMessageId: sourceMessageId,
            OperationId: record.ExecutionId,
            RiskClass: AiActionRiskClass.LowRisk,
            RiskActionClasses: [],
            PolicyReasonCode: record.PolicyReasonCode,
            PolicySnapshotId: record.PolicySnapshotId,
            PolicySnapshotVisibility: "authorized",
            ContextPackageId: record.ContextPackageId,
            ContextPackageVersion: record.ContextPackageVersion,
            ContextRedactionState: record.ContextRedactionState,
            AuthorizedContextReferences: authorizedContextReferences ?? record.SourceEvidenceIds,
            ExcludedContextReasons: excludedContextReasons,
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
            AiResponseSequence: sourceVersion,
            AiResponseProgressState: responseTerminalState,
            AiResponseTerminalReason: responseTerminalState,
            AiResponseVisibilityState: "metadata_only",
            AiResponseIsTerminal: true,
            RedactionState: record.RedactionState,
            RetentionClass: record.RetentionClass);
    }

    /// <summary>
    /// Translates an EventStore-published low-risk AI assistance domain event (delivered on the chatbot events
    /// topic as a <see cref="PublishedAiActionExecutionEvent"/>) into the append-only AI outcome rows the S1
    /// conversation projects. Mirrors <see cref="ApprovedAiActionOutcomeProjectionTranslator.TryCreatePublishedEvents"/>
    /// so the low-risk execution path (Story 4.4, AC5/AC6) reaches the conversation read model exactly like the
    /// approved-action path; returns an empty list for any non-low-risk or invalid envelope so the handler ignores it.
    /// </summary>
    public static IReadOnlyList<PublishedAiOutcomeEvent> TryCreatePublishedEvents(PublishedAiActionExecutionEvent published)
    {
        ArgumentNullException.ThrowIfNull(published);
        if (!string.Equals(published.Domain, ApprovedAiActionOutcomeProjectionTranslator.ChatBotDomain, StringComparison.Ordinal) ||
            published.SequenceNumber <= 0 ||
            published.Timestamp == default ||
            string.IsNullOrWhiteSpace(published.TenantId) ||
            string.IsNullOrWhiteSpace(published.EventTypeName) ||
            string.IsNullOrWhiteSpace(published.CorrelationId))
        {
            return [];
        }

        if (string.Equals(published.EventTypeName, typeof(LowRiskAiAssistanceExecutionStarted).FullName, StringComparison.Ordinal) &&
            published.LowRiskStarted is { } started &&
            string.Equals(published.CorrelationId, started.CorrelationId, StringComparison.Ordinal))
        {
            return [FromStarted(published.TenantId, "ai-action-executor", published.SequenceNumber, started)];
        }

        if (string.Equals(published.EventTypeName, typeof(LowRiskAiAssistanceExecutionSucceeded).FullName, StringComparison.Ordinal) &&
            published.LowRiskSucceeded is { } succeeded &&
            string.Equals(published.CorrelationId, succeeded.Record.CorrelationId, StringComparison.Ordinal))
        {
            return [FromCompletedEvent(published.TenantId, published.SequenceNumber, succeeded.Record, succeeded.ProjectId, succeeded.RequesterId, succeeded.SourceMessageId, succeeded.SourceConversationItemId, succeeded.AuthorizedContextReferences, succeeded.ExcludedContextReasons)];
        }

        if (string.Equals(published.EventTypeName, typeof(LowRiskAiAssistanceExecutionFailed).FullName, StringComparison.Ordinal) &&
            published.LowRiskFailed is { } failed &&
            string.Equals(published.CorrelationId, failed.Record.CorrelationId, StringComparison.Ordinal))
        {
            return [FromCompletedEvent(published.TenantId, published.SequenceNumber, failed.Record, failed.ProjectId, failed.RequesterId, failed.SourceMessageId, failed.SourceConversationItemId, failed.AuthorizedContextReferences, failed.ExcludedContextReasons)];
        }

        if (string.Equals(published.EventTypeName, typeof(LowRiskAiAssistanceRoutedToApproval).FullName, StringComparison.Ordinal) &&
            published.LowRiskRoutedToApproval is { } routed &&
            string.Equals(published.CorrelationId, routed.Record.CorrelationId, StringComparison.Ordinal))
        {
            return [FromCompletedEvent(published.TenantId, published.SequenceNumber, routed.Record, routed.ProjectId, routed.RequesterId, routed.SourceMessageId, routed.SourceConversationItemId, routed.AuthorizedContextReferences, routed.ExcludedContextReasons)];
        }

        return [];
    }

    private static PublishedAiOutcomeEvent FromCompletedEvent(
        string tenantId,
        long sourceVersion,
        LowRiskAiAssistanceExecutionRecord record,
        string projectId,
        string requesterId,
        string sourceMessageId,
        string? sourceConversationItemId,
        IReadOnlyList<string> authorizedContextReferences,
        IReadOnlyList<string> excludedContextReasons)
        => FromCompleted(
            tenantId,
            projectId,
            "ai-action-executor",
            sourceVersion,
            record,
            authorizedContextReferences,
            excludedContextReasons,
            requesterId,
            sourceMessageId,
            sourceConversationItemId);

    private static string WireToken(Enum value)
        => value.GetType()
            .GetField(value.ToString())!
            .GetCustomAttributes(typeof(System.Runtime.Serialization.EnumMemberAttribute), false)
            .OfType<System.Runtime.Serialization.EnumMemberAttribute>()
            .FirstOrDefault()
            ?.Value
            ?? value.ToString();
}
