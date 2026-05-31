using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.EventStore.Contracts.Events;

namespace Hexalith.ChatBot.Server.Association;

public sealed record MailboxEmailAssociationConfirmed(
    string AssociationId,
    string IntakeId,
    string TenantId,
    string ActorId,
    string ActorType,
    string SourceMailboxId,
    string SourceConversationId,
    string? SourceThreadId,
    AssociationDecisionKind DecisionKind,
    string ProjectId,
    string? ProjectDisplayName,
    IReadOnlyList<string> CandidateProjectIds,
    IReadOnlyList<AssociationEvidenceReference> EvidenceRefs,
    IReadOnlyList<AssociationConfidenceInput> ConfidenceInputs,
    double ConfidenceScore,
    AssociationThresholdBand ThresholdBand,
    IReadOnlyList<AssociationReasonCode> ReasonCodes,
    string ThresholdPolicyVersion,
    string DerivationKernelVersion,
    DateTimeOffset DetectedAt,
    DateTimeOffset DecidedAt,
    string SourceProvenance,
    string RedactionState,
    string RetentionClass,
    long SourceVersion,
    string SchemaVersion,
    string CorrelationId,
    string SurfaceOrigin,
    string? DecisionNote,
    string DecisionNoteRedactionState,
    string PolicySnapshotVersion) : IEventPayload;
