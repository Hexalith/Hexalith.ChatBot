using Hexalith.ChatBot.Contracts.Commands;

namespace Hexalith.ChatBot.Server.Association.Scoring;

internal sealed record AssociationScoringInput(
    string AssociationId,
    string IntakeId,
    string SourceMailboxId,
    string SourceConversationId,
    string? SourceThreadId,
    IReadOnlyList<AssociationDeterministicSignal> Signals,
    IReadOnlyList<ProjectAssociationCandidateEvidence> AuthorizedCandidates,
    IReadOnlyList<AssociationExclusion> Exclusions,
    AssociationThresholdPolicySnapshot ThresholdPolicy,
    string KernelVersion,
    DateTimeOffset DetectedAt,
    string CorrelationId,
    MailboxExternalSenderPosture? ExternalSender = null,
    MailboxAuthenticityStrictnessPolicySnapshot? StrictnessPolicy = null,
    MailboxAuthenticityMetadata? Authenticity = null);
