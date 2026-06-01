using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Queries;

/// <summary>
/// Durable metadata-only task-intent record captured from an authorized project conversation item.
/// </summary>
public sealed record TaskIntentRecord(
    string TaskIntentId,
    string TenantId,
    string ProjectId,
    string SourceMessageId,
    string RequesterPartyId,
    string DetectedIntentSummary,
    ProjectConversationDetectedActionKind DetectedActionKind,
    IReadOnlyList<TaskIntentSourceEvidenceOffset> SourceEvidenceOffsets,
    string KernelVersion,
    double ConfidenceScore,
    DateTimeOffset DetectedAt,
    TaskIntentState State,
    string SchemaVersion,
    string ReasonCode,
    string SourceProvenance,
    string RedactionState,
    string RetentionClass,
    long SourceVersion,
    string CorrelationId,
    string? PolicySnapshotId = null,
    string? CorrectionLineageId = null,
    bool ConversionReadinessBlocked = false,
    string? SafeNextAction = null,
    string? SupersedesTaskIntentId = null,
    string? SupersededByTaskIntentId = null);
