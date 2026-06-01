using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.Folders.Client.Convenience;
using Hexalith.Folders.Client.Generated;

using FoldersClient = Hexalith.Folders.Client.Generated.IClient;

namespace Hexalith.ChatBot.Server.Adapters.Folders;

internal sealed class FoldersFolderStore(FoldersClient folders) : IFolderStore
{
    public async ValueTask<MailboxAttachmentStorageResult> StoreMailboxAttachmentAsync(
        StoreMailboxAttachmentRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        if (request.Content.Kind is not Adapters.Mailbox.MailboxAttachmentContentResultKind.Available)
        {
            return MailboxAttachmentStorageResult.Failed(FailureForContentKind(request.Content.Kind, request.Content.ReasonCode));
        }

        if (request.Content.Content.Length > FileUpload.InlineTransportBoundaryBytes)
        {
            return MailboxAttachmentStorageResult.Failed(new AttachmentStorageFailure(
                ProjectConversationAttachmentStatus.Retryable,
                "not-evaluated",
                "retryable",
                "not-eligible",
                "attachment_content_streaming_required"));
        }

        string folderId = AttachmentStorageIdentity.FolderIdFor(request.TenantId, request.ProjectId);
        string workspaceId = AttachmentStorageIdentity.WorkspaceIdFor(request.TenantId, request.ProjectId, request.AssociationId);
        string operationId = AttachmentStorageIdentity.OperationIdFor(request);
        string taskId = AttachmentStorageIdentity.TaskIdFor(operationId);
        FileUploadDescriptor descriptor = new()
        {
            FolderId = folderId,
            WorkspaceId = workspaceId,
            OperationId = operationId,
            PathMetadata = AttachmentStorageIdentity.PathFor(request),
            MediaType = AttachmentStorageIdentity.SafeMediaType(request.Content.MediaType ?? request.ContentType),
            ContentMediaType = request.ContentType,
            FileOperationKind = FileMutationRequestFileOperationKind.Add,
            ContentHashReference = request.Content.ContentHashReference,
        };
        FileMutationRequest mutation = FileUpload.BuildInlineFileMutation(
            request.Content.Content,
            descriptor.MediaType,
            descriptor.PathMetadata,
            descriptor.OperationId,
            descriptor.FileOperationKind,
            descriptor.ContentMediaType,
            descriptor.ContentHashReference);
        string idempotencyKey = FileUpload.ComputeIdempotencyKey(mutation, descriptor.WorkspaceId, taskId);

        try
        {
            AcceptedCommand accepted = await folders
                .AddFileAsync(folderId, workspaceId, idempotencyKey, request.CorrelationId, taskId, mutation, cancellationToken)
                .ConfigureAwait(false);

            return MailboxAttachmentStorageResult.Succeeded(new StoredMailboxAttachmentReference(
                folderId,
                AttachmentStorageIdentity.FileIdFor(operationId),
                accepted.IdempotentReplay ? "duplicate-suppressed" : "unique",
                "not-retryable",
                "pending-scan",
                [],
                operationId,
                idempotencyKey));
        }
        catch (FileUploadStreamingRequiredException)
        {
            return MailboxAttachmentStorageResult.Failed(new AttachmentStorageFailure(
                ProjectConversationAttachmentStatus.Retryable,
                "not-evaluated",
                "retryable",
                "not-eligible",
                "attachment_content_streaming_required"));
        }
        catch (HexalithFoldersApiException ex) when (ex.StatusCode is 401 or 403)
        {
            return MailboxAttachmentStorageResult.Failed(new AttachmentStorageFailure(
                ProjectConversationAttachmentStatus.Unavailable,
                "not-evaluated",
                "not-retryable",
                "not-eligible",
                "folders_authorization_unavailable"));
        }
        catch (HexalithFoldersApiException ex) when (ex.StatusCode is 409)
        {
            return MailboxAttachmentStorageResult.Failed(new AttachmentStorageFailure(
                ProjectConversationAttachmentStatus.Retryable,
                "duplicate-pending",
                "retryable",
                "not-eligible",
                "folders_duplicate_replay_pending"));
        }
        catch (HexalithFoldersApiException ex) when (ex.StatusCode is 413 or 429 or 503)
        {
            return MailboxAttachmentStorageResult.Failed(new AttachmentStorageFailure(
                ProjectConversationAttachmentStatus.Retryable,
                "not-evaluated",
                "retryable",
                "not-eligible",
                "folders_store_retryable"));
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or InvalidOperationException)
        {
            return MailboxAttachmentStorageResult.Failed(new AttachmentStorageFailure(
                ProjectConversationAttachmentStatus.Retryable,
                "not-evaluated",
                "retryable",
                "not-eligible",
                "folders_store_retryable"));
        }
    }

    private static AttachmentStorageFailure FailureForContentKind(Adapters.Mailbox.MailboxAttachmentContentResultKind kind, string reasonCode)
        => kind switch
        {
            Adapters.Mailbox.MailboxAttachmentContentResultKind.TooLarge => new(
                ProjectConversationAttachmentStatus.Retryable,
                "not-evaluated",
                "retryable",
                "not-eligible",
                "attachment_content_streaming_required"),
            Adapters.Mailbox.MailboxAttachmentContentResultKind.Unauthorized => new(
                ProjectConversationAttachmentStatus.Unavailable,
                "not-evaluated",
                "not-retryable",
                "not-eligible",
                "attachment_content_unauthorized"),
            Adapters.Mailbox.MailboxAttachmentContentResultKind.Retryable => new(
                ProjectConversationAttachmentStatus.Retryable,
                "not-evaluated",
                "retryable",
                "not-eligible",
                reasonCode),
            _ => new(
                ProjectConversationAttachmentStatus.Unavailable,
                "not-evaluated",
                "not-retryable",
                "not-eligible",
                reasonCode),
        };
}
