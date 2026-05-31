namespace Hexalith.ChatBot.Server.Association;

public sealed record CompleteMailboxAssociationCorrectionPropagation(
    string AssociationId,
    string CorrectionId,
    string WorkflowInstanceId,
    long SourceVersion,
    DateTimeOffset CompletedAtUtc,
    string DownstreamImpactStatus,
    string SchemaVersion);
