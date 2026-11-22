using System.Collections.Concurrent;

using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Server.Adapters.Folders;
using Hexalith.ChatBot.Server.Adapters.Mailbox;

namespace Hexalith.ChatBot.RecoverySandbox;

/// <summary>
/// Minimal real folder store behind the recovery attachment-capture exercise: it genuinely stores content only
/// when the upstream <c>IMailboxAttachmentContentSource</c> actually delivered content, so a storage outcome
/// independently reflects whatever the real <c>AttachmentCaptureCoordinator</c> received rather than mirroring
/// the fault switch itself. Production uses <c>UnavailableFolderStore</c> (no live Folders binding) which would
/// always fail regardless of content-source outcome, masking the dependency under test.
/// </summary>
internal sealed class RecoveryFolderStore : IFolderStore
{
    private readonly ConcurrentDictionary<(string Tenant, string Intake, string File), byte[]> _files = new();

    /// <inheritdoc />
    public ValueTask<MailboxAttachmentStorageResult> StoreMailboxAttachmentAsync(
        StoreMailboxAttachmentRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();
        if (request.Content.Kind is not MailboxAttachmentContentResultKind.Available)
        {
            return ValueTask.FromResult(MailboxAttachmentStorageResult.Failed(new AttachmentStorageFailure(
                ProjectConversationAttachmentStatus.Retryable,
                "not-evaluated",
                "retryable",
                "not-eligible",
                request.Content.ReasonCode)));
        }

        string fileId = $"file:{request.ProviderAttachmentId}:{request.CorrelationId}";
        var stored = _files.GetOrAdd((request.TenantId, request.IntakeId, fileId), _ => request.Content.Content.ToArray());
        if (!request.Content.Content.Span.SequenceEqual(stored))
        {
            return ValueTask.FromResult(MailboxAttachmentStorageResult.Failed(new AttachmentStorageFailure(
                ProjectConversationAttachmentStatus.Unavailable, "conflict", "not-retryable", "not-eligible", "attachment_content_conflict")));
        }

        return ValueTask.FromResult(MailboxAttachmentStorageResult.Succeeded(new StoredMailboxAttachmentReference(
            $"folder:{request.TenantId}:{request.IntakeId}",
            fileId,
            "no-duplicate",
            "not-retried",
            "eligible",
            [],
            $"storage-op:{request.CorrelationId}",
            $"idempotency:{request.CorrelationId}:{request.ProviderAttachmentId}",
            ContentConfirmed: true)));
    }
}
