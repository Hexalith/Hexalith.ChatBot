namespace Hexalith.ChatBot.Server.Association;

public sealed record StartMailboxAssociationCorrectionPropagation(
    string AssociationId,
    string IntakeId,
    string CorrectionId,
    string WorkflowInstanceId,
    string PriorProjectId,
    string CorrectedProjectId,
    IReadOnlyList<string> RequiredStoreKeys,
    long SourceVersion,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset EstimatedCompletionAtUtc,
    string ResponsibleOwnerRole,
    string NextSafeAction,
    string SchemaVersion);
