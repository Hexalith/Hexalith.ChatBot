using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Commands;

/// <summary>
/// Records an authorized correction that supersedes a prior email-to-project association.
/// </summary>
public sealed record CorrectEmailProjectAssociation(
    string AssociationId,
    string IntakeId,
    string PriorProjectId,
    string TargetProjectId,
    AssociationCorrectionKind CorrectionKind,
    string? CorrectionRationale,
    string PredecessorAssociationId,
    string CandidateEvidenceFingerprint,
    long SourceVersion,
    string SchemaVersion) : IChatBotCommand;
