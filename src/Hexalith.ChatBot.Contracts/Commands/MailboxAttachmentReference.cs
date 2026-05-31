namespace Hexalith.ChatBot.Contracts.Commands;

/// <summary>
/// Provider-owned attachment reference captured without downloading or storing attachment content.
/// </summary>
/// <param name="ProviderAttachmentId">Opaque provider attachment identifier.</param>
/// <param name="Name">Provider-supplied attachment name when available.</param>
/// <param name="ContentType">Provider-supplied content type when available.</param>
/// <param name="SizeInBytes">Provider-supplied attachment size when available.</param>
public sealed record MailboxAttachmentReference(
    string ProviderAttachmentId,
    string? Name,
    string? ContentType,
    long? SizeInBytes);
