using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.EventStore.Contracts.Events;

namespace Hexalith.ChatBot.Server.Association;

public sealed record MailboxEmailAssociatedToProject(
    string AssociationId,
    string IntakeId,
    string TenantId,
    string ProjectId,
    string? ProjectDisplayName,
    string SourceMailboxId,
    string SourceConversationId,
    string? SourceThreadId,
    IReadOnlyList<AssociationEvidenceReference> EvidenceRefs,
    IReadOnlyList<AssociationConfidenceInput> ConfidenceInputs,
    double ConfidenceScore,
    AssociationThresholdBand ThresholdBand,
    IReadOnlyList<AssociationReasonCode> ReasonCodes,
    string ThresholdPolicyVersion,
    string DerivationKernelVersion,
    DateTimeOffset DetectedAt,
    string RedactionState,
    string RetentionClass,
    long SourceVersion,
    string SchemaVersion,
    string CorrelationId) : IEventPayload;
