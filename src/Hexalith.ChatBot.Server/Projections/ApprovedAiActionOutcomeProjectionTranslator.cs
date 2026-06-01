using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Contracts.Messages;
using Hexalith.ChatBot.Contracts.Queries;
using Hexalith.ChatBot.Server.Governance.AiMediation;

namespace Hexalith.ChatBot.Server.Projections;

internal static class ApprovedAiActionOutcomeProjectionTranslator
{
    public const string ChatBotDomain = "chatbot";

    public static PublishedAiOutcomeEvent FromStarted(
        string tenantId,
        string actorId,
        long sourceVersion,
        ApprovedAiActionExecutionStarted started)
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
            SourceConversationItemId: started.SourceConversationItemId,
            SourceMessageId: started.SourceMessageId,
            OperationId: started.ExecutionId,
            RiskClass: AiActionRiskClass.ApprovalRequired,
            RiskActionClasses: ["modifies-state"],
            PolicySnapshotId: started.PolicySnapshotId,
            PolicySnapshotVisibility: "authorized",
            CommandName: started.CommandName,
            CommandAllowlistVersion: started.CommandAllowlistVersion,
            ApprovalId: started.ApprovalId,
            ApprovalStatus: WireToken(ApprovalStatus.Approved),
            ExecutionStatus: "executing",
            AuditOperationId: $"audit:{started.ExecutionId}",
            AuditStatus: "available",
            SafeNextAction: "wait-for-command-outcome",
            RedactionState: started.RedactionState,
            RetentionClass: started.RetentionClass);
    }

    public static PublishedAiOutcomeEvent FromCompleted(
        string tenantId,
        string projectId,
        string actorId,
        long sourceVersion,
        ApprovedAiActionExecutionRecord record,
        string? requesterId = null,
        string? sourceMessageId = null,
        string? sourceConversationItemId = null)
    {
        ArgumentNullException.ThrowIfNull(record);
        bool success = string.Equals(record.Outcome, "success", StringComparison.Ordinal);
        return new PublishedAiOutcomeEvent(
            tenantId,
            AiOutcomeProjectionTranslator.AiOutcomeDomain,
            record.ExecutionId,
            sourceVersion,
            record.ExecutedAtUtc,
            record.CorrelationId,
            projectId,
            success ? AiOutcomeKind.ExecutionSucceeded : AiOutcomeKind.ExecutionFailed,
            success ? AiOutcomeStatus.Succeeded : AiOutcomeStatus.Failed,
            actorId,
            "ai",
            ProposalId: record.ProposalId,
            RequesterId: requesterId,
            SourceConversationItemId: sourceConversationItemId,
            SourceMessageId: sourceMessageId,
            OperationId: record.ExecutionId,
            RiskClass: AiActionRiskClass.ApprovalRequired,
            RiskActionClasses: ["modifies-state"],
            CommandName: record.CommandName,
            CommandAllowlistVersion: record.CommandAllowlistVersion,
            ApprovalId: record.ApprovalId,
            ApprovalStatus: WireToken(ApprovalStatus.Approved),
            ExecutionStatus: record.Outcome,
            ExecutionOutcomeCode: success ? "approved-ai-action-executed" : record.FailureCode,
            AuditOperationId: record.AuditOperationId,
            AuditStatus: record.AuditStatus,
            FailureCode: record.FailureCode,
            Retryability: record.Retryability,
            SafeNextAction: record.SafeNextAction,
            GeneratedContentVisibility: record.GeneratedContentVisibility,
            RedactionState: record.RedactionState,
            RetentionClass: record.RetentionClass);
    }

    public static PublishedAiOutcomeEvent FromOutcomeRecorded(
        string tenantId,
        string projectId,
        string actorId,
        long sourceVersion,
        ApprovedAiActionExecutionRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);
        return FromCompleted(tenantId, projectId, actorId, sourceVersion, record) with
        {
            OutcomeKind = AiOutcomeKind.OutcomeRecorded,
            OutcomeStatus = string.Equals(record.Outcome, "success", StringComparison.Ordinal) ? AiOutcomeStatus.Succeeded : AiOutcomeStatus.Failed,
            ExecutionOutcomeCode = string.Equals(record.Outcome, "success", StringComparison.Ordinal) ? "outcome-recorded" : record.FailureCode,
        };
    }

    public static PublishedAiOutcomeEvent FromRejected(
        string tenantId,
        string actorId,
        long sourceVersion,
        DateTimeOffset occurredAt,
        ApprovedAiActionExecutionRejected rejected)
    {
        ArgumentNullException.ThrowIfNull(rejected);
        ChatBotMessageCatalogEntry entry = ChatBotRefusalReasonCodes.CatalogEntryFor(rejected.ReasonCode);

        return new PublishedAiOutcomeEvent(
            tenantId,
            AiOutcomeProjectionTranslator.AiOutcomeDomain,
            rejected.ExecutionId,
            sourceVersion,
            occurredAt,
            rejected.CorrelationId,
            rejected.ProjectId,
            AiOutcomeKind.Refusal,
            AiOutcomeStatus.Blocked,
            actorId,
            "ai",
            ProposalId: rejected.ProposalId,
            RequesterId: rejected.RequesterId,
            SourceConversationItemId: rejected.SourceConversationItemId,
            SourceMessageId: rejected.SourceMessageId,
            OperationId: rejected.ExecutionId,
            RiskClass: AiActionRiskClass.ApprovalRequired,
            RiskActionClasses: ["modifies-state"],
            PolicySnapshotId: rejected.PolicySnapshotId,
            PolicySnapshotVisibility: string.IsNullOrWhiteSpace(rejected.PolicySnapshotId) ? "unavailable" : "authorized",
            CommandName: rejected.CommandName,
            CommandAllowlistVersion: rejected.CommandAllowlistVersion,
            ApprovalId: rejected.ApprovalId,
            ApprovalStatus: "blocked",
            ExecutionStatus: "blocked",
            ExecutionOutcomeCode: rejected.ReasonCode,
            AuditOperationId: $"audit:{rejected.ExecutionId}",
            AuditStatus: "available",
            FailureCode: rejected.ReasonCode,
            Retryability: string.Equals(entry.NextAction, ChatBotMessageNextActions.RetryLater, StringComparison.Ordinal) ? "retryable" : null,
            SafeNextAction: entry.NextAction,
            RedactionState: rejected.RedactionState,
            RetentionClass: rejected.RetentionClass);
    }

    public static IReadOnlyList<PublishedAiOutcomeEvent> TryCreatePublishedEvents(PublishedAiActionExecutionEvent published)
    {
        ArgumentNullException.ThrowIfNull(published);
        if (!string.Equals(published.Domain, ChatBotDomain, StringComparison.Ordinal) ||
            published.SequenceNumber <= 0 ||
            published.Timestamp == default ||
            string.IsNullOrWhiteSpace(published.TenantId) ||
            string.IsNullOrWhiteSpace(published.EventTypeName) ||
            string.IsNullOrWhiteSpace(published.CorrelationId))
        {
            return [];
        }

        if (string.Equals(published.EventTypeName, typeof(ApprovedAiActionExecutionStarted).FullName, StringComparison.Ordinal) &&
            published.Started is { } started &&
            string.Equals(published.CorrelationId, started.CorrelationId, StringComparison.Ordinal))
        {
            return [FromStarted(published.TenantId, "ai-action-executor", published.SequenceNumber, started)];
        }

        if (string.Equals(published.EventTypeName, typeof(ApprovedAiActionExecutionSucceeded).FullName, StringComparison.Ordinal) &&
            published.Succeeded is { } succeeded &&
            string.Equals(published.CorrelationId, succeeded.Record.CorrelationId, StringComparison.Ordinal))
        {
            return
            [
                FromCompleted(
                    published.TenantId,
                    succeeded.ProjectId,
                    "ai-action-executor",
                    published.SequenceNumber,
                    succeeded.Record,
                    succeeded.RequesterId,
                    succeeded.SourceMessageId,
                    succeeded.SourceConversationItemId),
                FromOutcomeRecorded(
                    published.TenantId,
                    succeeded.ProjectId,
                    "ai-action-executor",
                    published.SequenceNumber + 1,
                    succeeded.Record) with
                    {
                        RequesterId = succeeded.RequesterId,
                        SourceMessageId = succeeded.SourceMessageId,
                        SourceConversationItemId = succeeded.SourceConversationItemId,
                    },
            ];
        }

        if (string.Equals(published.EventTypeName, typeof(ApprovedAiActionExecutionFailed).FullName, StringComparison.Ordinal) &&
            published.Failed is { } failed &&
            string.Equals(published.CorrelationId, failed.Record.CorrelationId, StringComparison.Ordinal))
        {
            return
            [
                FromCompleted(
                    published.TenantId,
                    failed.ProjectId,
                    "ai-action-executor",
                    published.SequenceNumber,
                    failed.Record,
                    failed.RequesterId,
                    failed.SourceMessageId,
                    failed.SourceConversationItemId),
            ];
        }

        if (string.Equals(published.EventTypeName, typeof(ApprovedAiActionExecutionRejected).FullName, StringComparison.Ordinal) &&
            published.Rejected is { } rejected &&
            string.Equals(published.CorrelationId, rejected.CorrelationId, StringComparison.Ordinal))
        {
            return [FromRejected(published.TenantId, "ai-action-executor", published.SequenceNumber, published.Timestamp, rejected)];
        }

        return [];
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
