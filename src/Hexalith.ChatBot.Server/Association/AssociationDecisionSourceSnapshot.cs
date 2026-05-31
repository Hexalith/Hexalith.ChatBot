using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Server.Association;

public sealed record AssociationDecisionSourceSnapshot(
    string AssociationId,
    string IntakeId,
    string TenantId,
    string SourceMailboxId,
    string SourceConversationId,
    string? SourceThreadId,
    IReadOnlyList<AssociationCandidate> Candidates,
    IReadOnlyList<AssociationExclusion> Exclusions,
    LifecycleState LifecycleState,
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
    string CorrelationId);
