namespace Hexalith.ChatBot.Contracts.Commands;

/// <summary>
/// Scores deterministic project-association evidence for a mailbox message.
/// </summary>
public sealed record ScoreMailboxMessageAssociation(
    string AssociationId,
    string IntakeId,
    string SourceMailboxId,
    string SourceConversationId,
    string? SourceThreadId,
    IReadOnlyList<AssociationDeterministicSignal> DeterministicSignals,
    AssociationThresholdPolicySnapshot? ThresholdPolicy,
    IReadOnlyList<AssociationCandidate>? Candidates,
    IReadOnlyList<AssociationExclusion>? Exclusions,
    AssociationScoringResult? Result,
    string ScoringKernelVersion,
    MailboxExternalSenderPosture? ExternalSender = null,
    MailboxAuthenticityStrictnessPolicySnapshot? StrictnessPolicy = null,
    MailboxAuthenticityMetadata? Authenticity = null) : IChatBotCommand;
