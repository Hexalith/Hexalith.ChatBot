using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Contracts.Queries;

namespace Hexalith.ChatBot.Contracts.Commands;

/// <summary>
/// Captures a deterministic metadata-only task intent for an already authorized project conversation item.
/// Tenant authority is supplied by authenticated server context.
/// </summary>
public sealed record CaptureTaskIntent(
    string ProjectId,
    string SourceMessageId,
    string RequesterPartyId,
    string DetectedIntentSummary,
    ProjectConversationDetectedActionKind DetectedActionKind,
    IReadOnlyList<TaskIntentSourceEvidenceOffset> SourceEvidenceOffsets,
    string KernelVersion,
    double ConfidenceScore,
    DateTimeOffset DetectedAt,
    string RedactionState,
    string RetentionClass,
    long SourceVersion,
    string CorrelationId,
    string? PolicySnapshotId,
    bool CorrectedContextReady,
    string SchemaVersion) : IChatBotCommand;
