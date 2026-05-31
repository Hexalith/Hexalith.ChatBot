using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Commands;

/// <summary>
/// Records an authorized reviewer decision to keep an association workflow in needs-review.
/// </summary>
public sealed record MarkEmailAssociationNeedsReview(
    string AssociationId,
    string IntakeId,
    AssociationDecisionKind DecisionKind,
    string? DecisionNote,
    string CandidateEvidenceFingerprint,
    long SourceVersion,
    string SchemaVersion) : IChatBotCommand;
