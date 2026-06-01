using Hexalith.ChatBot.Contracts.Queries;
using Hexalith.ChatBot.Server.Governance.AiMediation;

namespace Hexalith.ChatBot.Server.Projections;

internal static class TaskIntentProjectionTranslator
{
    public const string ChatBotDomain = "chatbot";

    public static TaskIntentRecord? TryCreateRecord(PublishedTaskIntentEvent published)
    {
        ArgumentNullException.ThrowIfNull(published);

        TaskIntentRecord? record = published.Record;
        if (!string.Equals(published.Domain, ChatBotDomain, StringComparison.Ordinal) ||
            !IsSupportedTaskIntentEvent(published.EventTypeName) ||
            published.SequenceNumber <= 0 ||
            published.Timestamp == default ||
            record is null ||
            !string.Equals(published.TenantId, record.TenantId, StringComparison.Ordinal) ||
            !IsSafeMetadataToken(published.AggregateId) ||
            !IsSafeMetadataToken(published.CorrelationId) ||
            !IsValidRecord(record))
        {
            return null;
        }

        return record;
    }

    public static AiOutcomeEventView? TryCreateAiOutcome(PublishedTaskIntentEvent published)
    {
        ArgumentNullException.ThrowIfNull(published);
        if (!string.Equals(published.Domain, ChatBotDomain, StringComparison.Ordinal) ||
            !string.Equals(published.EventTypeName, typeof(TaskIntentConvertedToAiActionProposal).FullName, StringComparison.Ordinal) ||
            published.SequenceNumber <= 0 ||
            published.Timestamp == default ||
            published.Proposal is null ||
            published.Record is null ||
            !string.Equals(published.TenantId, published.Record.TenantId, StringComparison.Ordinal) ||
            !IsValidRecord(published.Record) ||
            !IsSafeMetadataToken(published.Proposal.ProposalId) ||
            !IsSafeMetadataToken(published.Proposal.IntendedCommandName) ||
            !IsSafeMetadataToken(published.Proposal.SafeNextAction))
        {
            return null;
        }

        return new AiOutcomeEventView(
            published.Record.TenantId,
            published.Record.ProjectId,
            Hexalith.ChatBot.Contracts.Enums.AiOutcomeKind.Proposal,
            Hexalith.ChatBot.Contracts.Enums.AiOutcomeStatus.Proposed,
            published.Timestamp,
            published.Record.SourceVersion,
            published.Record.CorrelationId,
            published.Proposal.ReviewerId,
            "human",
            ProposalId: published.Proposal.ProposalId,
            RequesterId: published.Proposal.RequesterId,
            SourceConversationItemId: published.Proposal.SourceConversationItemId,
            SourceMessageId: published.Proposal.SourceMessageId,
            PolicySnapshotId: published.Proposal.PolicySnapshotId,
            PolicySnapshotVisibility: "metadata_only",
            AuthorizedContextReferences: published.Proposal.EvidenceReferences,
            ExcludedContextReasons: [],
            GeneratedSummaryRedactionState: "metadata_only",
            GeneratedContentVisibility: "metadata_only",
            CommandName: published.Proposal.IntendedCommandName,
            AuditOperationId: published.Record.AuditOperationId,
            AuditStatus: "recorded",
            SafeNextAction: published.Proposal.SafeNextAction,
            RedactionState: published.Proposal.RedactionState,
            RetentionClass: published.Proposal.RetentionClass);
    }

    private static bool IsSupportedTaskIntentEvent(string? eventTypeName)
        => string.Equals(eventTypeName, typeof(TaskIntentCaptured).FullName, StringComparison.Ordinal) ||
            string.Equals(eventTypeName, typeof(TaskIntentConvertedToAiActionProposal).FullName, StringComparison.Ordinal) ||
            string.Equals(eventTypeName, typeof(TaskIntentDispositionMarked).FullName, StringComparison.Ordinal);

    private static bool IsValidRecord(TaskIntentRecord record)
        => IsSafeMetadataToken(record.TaskIntentId) &&
            IsSafeMetadataToken(record.TenantId) &&
            IsSafeMetadataToken(record.ProjectId) &&
            IsSafeMetadataToken(record.SourceMessageId) &&
            IsSafeMetadataToken(record.RequesterPartyId) &&
            !string.IsNullOrWhiteSpace(record.DetectedIntentSummary) &&
            record.DetectedIntentSummary.Length <= DeterministicTaskIntentKernel.SummaryMaxLength &&
            record.SourceEvidenceOffsets is { Count: > 0 } &&
            record.SourceEvidenceOffsets.All(static evidence => IsSafeMetadataToken(evidence.EvidenceReference)) &&
            IsSafeMetadataToken(record.KernelVersion) &&
            record.ConfidenceScore is >= 0 and <= 1 &&
            record.DetectedAt != default &&
            IsSafeMetadataToken(record.SchemaVersion) &&
            IsSafeMetadataToken(record.ReasonCode) &&
            IsSafeMetadataToken(record.SourceProvenance) &&
            IsSafeMetadataToken(record.RedactionState) &&
            IsSafeMetadataToken(record.RetentionClass) &&
            record.SourceVersion > 0 &&
            IsSafeMetadataToken(record.CorrelationId);

    private static bool IsSafeMetadataToken(string? value)
        => !string.IsNullOrWhiteSpace(value) &&
            value.Length <= 280 &&
            value.All(static c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_' or '.' or ':');
}
