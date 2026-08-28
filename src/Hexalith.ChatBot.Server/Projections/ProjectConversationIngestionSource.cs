namespace Hexalith.ChatBot.Server.Projections;

/// <summary>Safe source identity required by the ChatBot-owned ingestion-binding workflow.</summary>
internal sealed record ProjectConversationIngestionSource(
    string TenantId,
    string ProjectId,
    string AssociationId,
    string IntakeId,
    string SourceMailboxId,
    string ProviderMessageId,
    IReadOnlyList<ProjectConversationIngestionAttachment> Attachments,
    long SourceVersion,
    string CorrelationId);
