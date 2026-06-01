using Hexalith.ChatBot.Contracts.Messages;

namespace Hexalith.ChatBot.Server.Projections;

internal static class FailureStateProjectionTranslator
{
    public const string FailureStateDomain = "failure-states";

    public static FailureStateEventView? TryCreateView(PublishedFailureStateEvent published)
    {
        ArgumentNullException.ThrowIfNull(published);
        if (!string.Equals(published.Domain, FailureStateDomain, StringComparison.Ordinal) ||
            !IsSafeMetadataToken(published.TenantId) ||
            !IsSafeMetadataToken(published.AggregateId) ||
            !IsSafeMetadataToken(published.ProjectId) ||
            !IsSafeMetadataToken(published.OperationId) ||
            !IsSafeMetadataToken(published.MessageCatalogCode) ||
            published.SourceVersion <= 0 ||
            published.OccurredAtUtc == default ||
            !IsSafeMetadataToken(published.CorrelationId) ||
            ChatBotMessageCatalog.Entries.All(entry => !string.Equals(entry.Code, published.MessageCatalogCode, StringComparison.Ordinal)))
        {
            return null;
        }

        return new FailureStateEventView(
            published.TenantId,
            published.ProjectId,
            published.FailureStateKind,
            published.FailureStatus,
            published.MessageCatalogCode,
            published.OccurredAtUtc,
            published.SourceVersion,
            published.CorrelationId,
            published.OperationId,
            SafeOptionalToken(published.SourceConversationItemId),
            SafeOptionalToken(published.AssociationId),
            SafeOptionalToken(published.SourceMessageId),
            SafeOptionalToken(published.WorkflowInstanceId),
            SafeOptionalToken(published.SupersedesWorkflowInstanceId),
            SafeOptionalToken(published.SupersededByWorkflowInstanceId),
            SafeOptionalToken(published.TaskId),
            SafeOptionalToken(published.AuditOperationId),
            SafeOptionalToken(published.AuditStatus),
            SafeOptionalToken(published.ClientAction),
            SafeOptionalToken(published.FailureCategory),
            SafeOptionalToken(published.FailureScope),
            SafeOptionalToken(published.FailureReasonCode),
            SafeOptionalToken(published.BlockedReason),
            published.Retryable,
            published.RetryCount,
            published.MaxRetryCount,
            published.NextRetryAtUtc,
            published.LastRetryAtUtc,
            SafeOptionalToken(published.RetryOperationId),
            SafeOptionalToken(published.SafeNextAction),
            SafeOptionalToken(published.DuplicateSafetyState),
            SafeOptionalToken(published.DuplicateSuppressionId),
            SafeOptionalToken(published.DependencyName),
            published.DegradedUntilUtc,
            SafeOptionalToken(published.EscalationTargetRole),
            SafeOptionalToken(published.ReprocessCreatedWorkflowInstanceId),
            SafeOptionalToken(published.RedactionState) ?? ChatBotDetailVisibility.MetadataOnly,
            SafeOptionalToken(published.RetentionClass) ?? "collaboration_input");
    }

    private static string? SafeOptionalToken(string? value)
        => IsSafeMetadataToken(value) ? value : null;

    private static bool IsSafeMetadataToken(string? value)
        => !string.IsNullOrWhiteSpace(value) &&
            value.Length <= 256 &&
            value.All(static c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_' or '.' or ':');
}
