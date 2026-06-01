using System.Globalization;
using System.Security.Cryptography;
using System.Text;

using Hexalith.Folders.Client.Generated;

namespace Hexalith.ChatBot.Server.Adapters.Folders;

internal static class AttachmentStorageIdentity
{
    public const string PathPolicyClass = "governed-mailbox-attachment";

    public static string FolderIdFor(string tenantId, string projectId)
        => $"folder:{Hash($"{tenantId}|{projectId}")}";

    public static string WorkspaceIdFor(string tenantId, string projectId, string associationId)
        => $"workspace:{Hash($"{tenantId}|{projectId}|{associationId}")}";

    public static string OperationIdFor(StoreMailboxAttachmentRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        string contentReference = string.IsNullOrWhiteSpace(request.Content.ContentHashReference)
            ? "content-reference-unavailable"
            : request.Content.ContentHashReference;
        return $"mailbox-attachment:{Hash($"{request.TenantId}|{request.ProjectId}|{request.AssociationId}|{request.IntakeId}|{request.MailboxId}|{request.ProviderMessageId}|{request.ProviderAttachmentId}|{request.Ordinal.ToString(CultureInfo.InvariantCulture)}|{contentReference}")}";
    }

    public static string FileIdFor(string operationId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(operationId);
        return $"file:{Hash(operationId)}";
    }

    public static string TaskIdFor(string operationId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(operationId);
        return $"task:{Hash(operationId)}";
    }

    public static PathMetadata PathFor(StoreMailboxAttachmentRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        string fileName = SafeFileName(request.SafeDisplayName);
        return new PathMetadata
        {
            NormalizedPath = $"mailbox/{Hash(request.ProviderMessageId)}/{request.Ordinal.ToString(CultureInfo.InvariantCulture)}-{fileName}",
            DisplayName = fileName,
            PathPolicyClass = PathPolicyClass,
            UnicodeNormalization = PathMetadataUnicodeNormalization.NFC,
        };
    }

    public static string SafeMediaType(string? contentType)
        => string.IsNullOrWhiteSpace(contentType) || contentType.Any(static character => char.IsControl(character) || char.IsWhiteSpace(character))
            ? "application/octet-stream"
            : contentType;

    private static string SafeFileName(string? value)
    {
        string candidate = string.IsNullOrWhiteSpace(value) ? "attachment.bin" : value.Trim();
        candidate = candidate.Replace('\\', '/').Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).LastOrDefault() ?? "attachment.bin";
        char[] safe = candidate
            .Where(static character => !char.IsControl(character) && character is not '/' and not '\\' and not ':' and not '*'
                and not '?' and not '"' and not '<' and not '>' and not '|')
            .Take(120)
            .ToArray();
        string result = new(safe);
        return string.IsNullOrWhiteSpace(result) || result is "." or ".." ? "attachment.bin" : result;
    }

    private static string Hash(string value)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
}
