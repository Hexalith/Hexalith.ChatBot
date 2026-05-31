namespace Hexalith.ChatBot.Server.Association;

public sealed record DelayMailboxAssociationCorrectionPropagation(
    string AssociationId,
    string CorrectionId,
    string WorkflowInstanceId,
    long SourceVersion,
    DateTimeOffset DelayedAtUtc,
    string ResponsibleOwnerRole,
    string NextSafeAction,
    string ReasonCode,
    string SchemaVersion);
