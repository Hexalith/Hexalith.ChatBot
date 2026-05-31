using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.EventStore.Contracts.Events;

namespace Hexalith.ChatBot.Server.Association;

public sealed record MailboxEmailAssociationCorrected(
    string AssociationId,
    string IntakeId,
    string TenantId,
    string ActorId,
    string ActorType,
    string SourceMailboxId,
    string SourceConversationId,
    string? SourceThreadId,
    AssociationCorrectionKind CorrectionKind,
    string PriorProjectId,
    string CorrectedProjectId,
    string? CorrectedProjectDisplayName,
    string PredecessorAssociationId,
    string SupersedesAssociationId,
    IReadOnlyList<string> CandidateProjectIds,
    IReadOnlyList<AssociationEvidenceReference> EvidenceRefs,
    IReadOnlyList<AssociationConfidenceInput> ConfidenceInputs,
    double ConfidenceScore,
    AssociationThresholdBand ThresholdBand,
    IReadOnlyList<AssociationReasonCode> ReasonCodes,
    string ThresholdPolicyVersion,
    string DerivationKernelVersion,
    DateTimeOffset DetectedAt,
    DateTimeOffset CorrectedAt,
    string SourceProvenance,
    string RedactionState,
    string RetentionClass,
    long SourceVersion,
    string SchemaVersion,
    string CorrelationId,
    string SurfaceOrigin,
    string? CorrectionRationale,
    string CorrectionRationaleRedactionState,
    string PolicySnapshotVersion,
    string DownstreamImpactStatus) : IEventPayload;
