using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Commands;

/// <summary>
/// Records an authorized reviewer decision to defer an association workflow item.
/// </summary>
public sealed record DeferEmailProjectAssociation(
    string AssociationId,
    string IntakeId,
    AssociationDecisionKind DecisionKind,
    string? DecisionNote,
    string CandidateEvidenceFingerprint,
    long SourceVersion,
    string SchemaVersion) : IChatBotCommand;
