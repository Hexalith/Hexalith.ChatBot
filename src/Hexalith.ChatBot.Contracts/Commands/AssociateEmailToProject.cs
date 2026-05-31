using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Commands;

/// <summary>
/// Records an authorized reviewer decision to associate a mailbox email workflow to a project.
/// </summary>
public sealed record AssociateEmailToProject(
    string AssociationId,
    string IntakeId,
    string ProjectId,
    AssociationDecisionKind DecisionKind,
    string? DecisionNote,
    string CandidateEvidenceFingerprint,
    long SourceVersion,
    string SchemaVersion) : IChatBotCommand;
