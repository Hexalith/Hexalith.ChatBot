using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.EventStore.Contracts.Events;

namespace Hexalith.ChatBot.Server.Association;

public sealed record MailboxAssociationCandidatesGenerated(
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
    AssociationScoringOutcome Outcome,
    IReadOnlyList<AssociationReasonCode> ReasonCodes,
    string ThresholdPolicyVersion,
    string DerivationKernelVersion,
    DateTimeOffset DetectedAt,
    string RedactionState,
    string RetentionClass,
    long SourceVersion,
    string SchemaVersion,
    string CorrelationId) : IEventPayload;
