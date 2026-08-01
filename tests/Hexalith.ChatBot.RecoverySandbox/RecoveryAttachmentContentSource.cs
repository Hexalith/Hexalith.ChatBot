using Hexalith.ChatBot.Server.Adapters.Mailbox;

namespace Hexalith.ChatBot.RecoverySandbox;

/// <summary>Controllable mailbox-content adapter used by the attachment workflow-item exercise.</summary>
internal sealed class RecoveryAttachmentContentSource(RecoveryScopedOutageState state) : IMailboxAttachmentContentSource
{
    /// <inheritdoc />
    public ValueTask<MailboxAttachmentContentResult> FetchAttachmentContentAsync(
        MailboxAttachmentContentRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();
        if (state.IsFaulted("attachment-processing"))
        {
            state.RecordFaultObservation("attachment-processing");
            return ValueTask.FromResult(MailboxAttachmentContentResult.Retryable("attachment_dependency_unavailable"));
        }

        _ = state.RecordEffect("attachment-processing", request.TenantId, request.CorrelationId);
        return ValueTask.FromResult(MailboxAttachmentContentResult.Available(
            new byte[] { 0x00 },
            "application/octet-stream",
            "sha256:recovery-metadata-only"));
    }
}
