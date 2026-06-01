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
            !string.Equals(published.EventTypeName, typeof(TaskIntentCaptured).FullName, StringComparison.Ordinal) ||
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
