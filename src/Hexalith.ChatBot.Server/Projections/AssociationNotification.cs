using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Server.Projections;

internal sealed record AssociationNotification(
    string TenantId,
    string AssociationId,
    string IntakeId,
    string SourceMailboxId,
    string SourceConversationId,
    string? SourceThreadId,
    string? ProjectId,
    string? ProjectDisplayName,
    AssociationScoringOutcome Outcome,
    AssociationThresholdBand ThresholdBand,
    double ConfidenceScore,
    IReadOnlyList<AssociationCandidate> Candidates,
    IReadOnlyList<AssociationExclusion> Exclusions,
    string ThresholdPolicyVersion,
    string DerivationKernelVersion,
    string RedactionState,
    string RetentionClass,
    long SourceVersion,
    DateTimeOffset DetectedAt,
    string CorrelationId);
