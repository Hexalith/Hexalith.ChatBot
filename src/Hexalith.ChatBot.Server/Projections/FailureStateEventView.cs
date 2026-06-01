using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Contracts.Messages;

namespace Hexalith.ChatBot.Server.Projections;

internal sealed record FailureStateEventView(
    string TenantId,
    string ProjectId,
    FailureStateKind FailureStateKind,
    FailureStatus FailureStatus,
    string MessageCatalogCode,
    DateTimeOffset OccurredAtUtc,
    long SourceVersion,
    string CorrelationId,
    string OperationId,
    string? SourceConversationItemId = null,
    string? AssociationId = null,
    string? SourceMessageId = null,
    string? WorkflowInstanceId = null,
    string? SupersedesWorkflowInstanceId = null,
    string? SupersededByWorkflowInstanceId = null,
    string? TaskId = null,
    string? AuditOperationId = null,
    string? AuditStatus = null,
    string? ClientAction = null,
    string? FailureCategory = null,
    string? FailureScope = null,
    string? FailureReasonCode = null,
    string? BlockedReason = null,
    bool? Retryable = null,
    int? RetryCount = null,
    int? MaxRetryCount = null,
    DateTimeOffset? NextRetryAtUtc = null,
    DateTimeOffset? LastRetryAtUtc = null,
    string? RetryOperationId = null,
    string? SafeNextAction = null,
    string? DuplicateSafetyState = null,
    string? DuplicateSuppressionId = null,
    string? DependencyName = null,
    DateTimeOffset? DegradedUntilUtc = null,
    string? EscalationTargetRole = null,
    string? ReprocessCreatedWorkflowInstanceId = null,
    string RedactionState = ChatBotDetailVisibility.MetadataOnly,
    string RetentionClass = "collaboration_input")
{
    public string StableItemId => ProjectConversationItemView.FailureStateItemIdFor(OperationId, FailureStateKind, SourceVersion);

    public static string KeyFor(string tenantId, string projectId, string itemId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(projectId);
        ArgumentException.ThrowIfNullOrWhiteSpace(itemId);
        return $"{tenantId}:project-conversation:{projectId}:{itemId}";
    }
}
