using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Commands;

/// <summary>
/// Records an authorized reviewer decision to reject all current project-association candidates.
/// </summary>
public sealed record RejectEmailProjectAssociation(
    string AssociationId,
    string IntakeId,
    AssociationDecisionKind DecisionKind,
    string? DecisionNote,
    string CandidateEvidenceFingerprint,
    long SourceVersion,
    string SchemaVersion) : IChatBotCommand;
