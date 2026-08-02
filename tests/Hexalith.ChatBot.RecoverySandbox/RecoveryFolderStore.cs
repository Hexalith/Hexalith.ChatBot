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

        return ValueTask.FromResult(MailboxAttachmentStorageResult.Succeeded(new StoredMailboxAttachmentReference(
            $"folder:{request.TenantId}:{request.IntakeId}",
            $"file:{request.ProviderAttachmentId}:{request.CorrelationId}",
            "no-duplicate",
            "not-retried",
            "eligible",
            [],
            $"storage-op:{request.CorrelationId}",
            $"idempotency:{request.CorrelationId}:{request.ProviderAttachmentId}")));
    }
}
