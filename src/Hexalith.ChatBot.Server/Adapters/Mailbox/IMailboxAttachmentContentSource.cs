namespace Hexalith.ChatBot.Server.Adapters.Mailbox;

internal interface IMailboxAttachmentContentSource
{
    ValueTask<MailboxAttachmentContentResult> FetchAttachmentContentAsync(
        MailboxAttachmentContentRequest request,
        CancellationToken cancellationToken = default);
}

internal sealed record MailboxAttachmentContentRequest(
    string TenantId,
    string ProjectId,
    string AssociationId,
    string IntakeId,
    string MailboxId,
    string ProviderMessageId,
    string ProviderAttachmentId,
    int Ordinal,
    long SourceVersion,
    string CorrelationId);

internal enum MailboxAttachmentContentResultKind
{
    Available,
    Unavailable,
    Retryable,
    TooLarge,
    Unauthorized,
}

internal sealed record MailboxAttachmentContentResult(
    MailboxAttachmentContentResultKind Kind,
    ReadOnlyMemory<byte> Content,
    string? MediaType,
    string? ContentHashReference,
    string ReasonCode)
{
    public static MailboxAttachmentContentResult Available(
        ReadOnlyMemory<byte> content,
        string? mediaType,
        string? contentHashReference = null)
        => new(MailboxAttachmentContentResultKind.Available, content, mediaType, contentHashReference, "available");

    public static MailboxAttachmentContentResult Unavailable(string reasonCode)
        => SafeNonContent(MailboxAttachmentContentResultKind.Unavailable, reasonCode);

    public static MailboxAttachmentContentResult Retryable(string reasonCode)
        => SafeNonContent(MailboxAttachmentContentResultKind.Retryable, reasonCode);

    public static MailboxAttachmentContentResult TooLarge()
        => SafeNonContent(MailboxAttachmentContentResultKind.TooLarge, "attachment_content_too_large");

    public static MailboxAttachmentContentResult Unauthorized()
        => SafeNonContent(MailboxAttachmentContentResultKind.Unauthorized, "attachment_content_unauthorized");

    public override string ToString()
        => $"{Kind}:{ReasonCode}";

    private static MailboxAttachmentContentResult SafeNonContent(MailboxAttachmentContentResultKind kind, string reasonCode)
        => new(kind, ReadOnlyMemory<byte>.Empty, null, null, SafeReason(reasonCode));

    private static string SafeReason(string? value)
        => string.IsNullOrWhiteSpace(value) ||
            value.Any(static character => char.IsControl(character) || char.IsWhiteSpace(character) ||
                !(char.IsLetterOrDigit(character) || character is '_' or '-'))
            ? "attachment_content_unavailable"
            : value;
}
