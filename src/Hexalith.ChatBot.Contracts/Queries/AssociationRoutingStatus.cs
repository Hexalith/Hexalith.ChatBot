using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Queries;

/// <summary>
/// Metadata-only routing status for an email association workflow.
/// </summary>
public sealed record AssociationRoutingStatus(
    string AssociationId,
    string IntakeId,
    string SourceMailboxId,
    string SourceConversationId,
    string? SourceThreadId,
    LifecycleState LifecycleState,
    AssociationScoringOutcome Outcome,
    AssociationThresholdBand ThresholdBand,
    double ConfidenceScore,
    IReadOnlyList<AssociationReasonCode> ReasonCodes,
    IReadOnlyList<AssociationCandidate> Candidates,
    IReadOnlyList<AssociationExclusion> Exclusions,
    string ThresholdPolicyVersion,
    IReadOnlyList<AssociationEvidenceReference> EvidenceRefs,
    string KernelVersion,
    DateTimeOffset DetectedAt,
    string SourceProvenance,
    string RedactionState,
    string RetentionClass,
    string SchemaVersion,
    string CorrelationId,
    IReadOnlyList<string> DisabledActionReasonCodes,
    IReadOnlyList<string> NextActionReasonCodes);
