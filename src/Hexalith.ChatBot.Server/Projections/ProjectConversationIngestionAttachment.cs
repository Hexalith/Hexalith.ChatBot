namespace Hexalith.ChatBot.Server.Projections;

/// <summary>Provider-ordered attachment metadata needed to fetch one governed ingestion payload.</summary>
internal sealed record ProjectConversationIngestionAttachment(
    string ProviderAttachmentId,
    int Ordinal,
    string? ContentType);
