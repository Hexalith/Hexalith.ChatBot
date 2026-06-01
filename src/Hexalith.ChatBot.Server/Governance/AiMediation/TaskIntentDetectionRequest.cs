using Hexalith.ChatBot.Contracts.Queries;

namespace Hexalith.ChatBot.Server.Governance.AiMediation;

internal sealed record TaskIntentDetectionRequest(
    string TenantId,
    string ProjectId,
    string SourceMessageId,
    string RequesterPartyId,
    IReadOnlyList<string> SafeIntentSignals,
    IReadOnlyList<TaskIntentSourceEvidenceOffset> SourceEvidenceOffsets,
    string RedactionState,
    string RetentionClass,
    long SourceVersion,
    string CorrelationId,
    bool TenantScopeResolved,
    bool ProjectAuthorized,
    bool SourceMessageAuthorized,
    bool AuditReady,
    bool CorrectedContextReady,
    double ConfidenceScore,
    string SourceProvenance,
    string? PolicySnapshotId = null,
    string? CorrectionLineageId = null,
    DateTimeOffset? DetectedAt = null,
    string KernelVersion = DeterministicTaskIntentKernel.CurrentKernelVersion,
    string SchemaVersion = DeterministicTaskIntentKernel.CurrentSchemaVersion);
