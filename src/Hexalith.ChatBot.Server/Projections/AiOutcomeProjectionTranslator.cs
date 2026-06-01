using Hexalith.ChatBot.Contracts.Messages;

namespace Hexalith.ChatBot.Server.Projections;

internal static class AiOutcomeProjectionTranslator
{
    public const string AiOutcomeDomain = "ai-outcomes";

    public static AiOutcomeEventView? TryCreateView(PublishedAiOutcomeEvent published)
    {
        ArgumentNullException.ThrowIfNull(published);
        if (!string.Equals(published.Domain, AiOutcomeDomain, StringComparison.Ordinal) ||
            !IsSafeMetadataToken(published.TenantId) ||
            !IsSafeMetadataToken(published.AggregateId) ||
            !IsSafeMetadataToken(published.ProjectId) ||
            !IsSafeMetadataToken(published.ActorId) ||
            !IsSafeActorType(published.ActorType) ||
            published.SourceVersion <= 0 ||
            published.OccurredAtUtc == default ||
            !IsSafeMetadataToken(published.CorrelationId) ||
            !HasStableOutcomeIdentity(published))
        {
            return null;
        }

        return new AiOutcomeEventView(
            published.TenantId,
            published.ProjectId,
            published.OutcomeKind,
            published.OutcomeStatus,
            published.OccurredAtUtc,
            published.SourceVersion,
            published.CorrelationId,
            published.ActorId,
            published.ActorType,
            SafeOptionalToken(published.ProposalId),
            SafeOptionalToken(published.RequestId),
            SafeOptionalToken(published.RequesterId),
            SafeOptionalToken(published.SourceConversationItemId),
            SafeOptionalToken(published.SourceMessageId),
            SafeOptionalToken(published.OperationId),
            published.RiskClass,
            SafeOptionalTokens(published.RiskActionClasses),
            SafeOptionalToken(published.PolicySnapshotId),
            SafeOptionalToken(published.PolicySnapshotVisibility),
            SafeOptionalToken(published.ContextPackageId),
            SafeOptionalToken(published.ContextPackageVersion),
            SafeOptionalToken(published.ContextRedactionState),
            SafeOptionalTokens(published.AuthorizedContextReferences),
            SafeOptionalTokens(published.ExcludedContextReasons),
            SafeOptionalToken(published.GeneratedSummaryRedactionState),
            SafeOptionalToken(published.GeneratedContentVisibility),
            SafeOptionalToken(published.CommandName),
            SafeOptionalToken(published.CommandAllowlistVersion),
            SafeOptionalToken(published.ApprovalId),
            SafeOptionalToken(published.ApprovalStatus),
            SafeOptionalToken(published.ExecutionStatus),
            SafeOptionalToken(published.ExecutionOutcomeCode),
            SafeOptionalToken(published.AuditOperationId),
            SafeOptionalToken(published.AuditStatus),
            SafeOptionalToken(published.FailureCode),
            SafeOptionalToken(published.Retryability),
            SafeOptionalToken(published.SafeNextAction),
            SafeOptionalToken(published.SupersedesAiOutcomeId),
            SafeOptionalToken(published.SupersededByAiOutcomeId),
            SafeOptionalToken(published.RedactionState) ?? ChatBotDetailVisibility.MetadataOnly,
            SafeOptionalToken(published.RetentionClass) ?? "collaboration_input");
    }

    private static bool HasStableOutcomeIdentity(PublishedAiOutcomeEvent published)
        => IsSafeMetadataToken(published.ProposalId) ||
            IsSafeMetadataToken(published.OperationId) ||
            IsSafeMetadataToken(published.RequestId);

    private static IReadOnlyList<string>? SafeOptionalTokens(IReadOnlyList<string>? values)
        => values?.Where(IsSafeMetadataToken).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();

    private static string? SafeOptionalToken(string? value)
        => IsSafeMetadataToken(value) ? value : null;

    private static bool IsSafeActorType(string? value)
        => value is "ai" or "service" or "system";

    private static bool IsSafeMetadataToken(string? value)
        => !string.IsNullOrWhiteSpace(value) &&
            value.Length <= 256 &&
            value.All(static c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_' or '.' or ':');
}
