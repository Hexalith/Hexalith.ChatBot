using System.Runtime.Serialization;
using System.Text.Json.Serialization;

using Hexalith.ChatBot.Contracts.Serialization;

namespace Hexalith.ChatBot.Contracts.Queries;

[JsonConverter(typeof(JsonEnumMemberStringConverter<LowRiskAiAssistanceKind>))]
public enum LowRiskAiAssistanceKind
{
    [EnumMember(Value = "summarize-visible-context")]
    SummarizeVisibleContext,

    [EnumMember(Value = "explain-visible-evidence")]
    ExplainVisibleEvidence,
}

public sealed record LowRiskAiAssistanceExecutionRecord(
    string ExecutionId,
    string ProposalId,
    string AssistanceKind,
    string Outcome,
    string ProviderName,
    string ModelVersion,
    DateTimeOffset GeneratedAtUtc,
    IReadOnlyList<string> SourceEvidenceIds,
    string ContextPackageId,
    string ContextPackageVersion,
    string ContextRedactionState,
    string PolicySnapshotId,
    string PolicyReasonCode,
    string AuditOperationId,
    string AuditStatus,
    string CorrelationId,
    string GeneratedSummaryRedactionState,
    string GeneratedContentVisibility,
    string SafeNextAction,
    string? FailureCode = null,
    string? Retryability = null,
    string RedactionState = "metadata_only",
    string RetentionClass = "collaboration_input",
    string SchemaVersion = "chatbot.low-risk-ai-assistance-execution-record.v1");
