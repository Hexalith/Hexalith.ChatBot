using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.Folders.Client.Convenience;
using Hexalith.Folders.Client.Generated;

using FoldersClient = Hexalith.Folders.Client.Generated.IClient;

namespace Hexalith.ChatBot.Server.Adapters.Folders;

/// <summary>Submits attachments using observations supplied by the Folders owner.</summary>
/// <param name="folders">The authorized Folders client.</param>
internal sealed class FoldersFolderStore(FoldersClient folders) : IFolderStore
{
    /// <inheritdoc />
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
        PathMetadata path = AttachmentStorageIdentity.PathFor(request);
        bool uploadStarted = false;

        try
        {
            FileMetadataResult metadata = await folders.GetFolderFileMetadataAsync(
                folderId, workspaceId, request.CorrelationId, taskId, ReadConsistencyClass.Snapshot_per_task,
                new FileMetadataRequest { RequestSchemaVersion = "v2", Paths = [path] }, cancellationToken)
                .ConfigureAwait(false);

            // Missing and forbidden paths share a safe 404. Neither that response nor an empty
            // result proves version zero. The current metadata API only exposes visible versions.
            if (metadata?.Items is not { Count: 1 }
                || metadata.Freshness is not { Stale: false, ReadConsistency: ReadConsistencyClass.Snapshot_per_task }
                || metadata.Limits is not { IsTruncated: false })
            {
                return EvidenceUnavailable();
            }

            FileMetadataItem file = metadata.Items.Single();
            if (file is not { Version: > 0, Kind: FileMetadataItemKind.File, Redaction: FileMetadataItemRedaction.Not_redacted }
                || string.IsNullOrWhiteSpace(file.FileId)
                || file.Path?.NormalizedPath != path.NormalizedPath
                || file.Path.PathPolicyClass is not (VisiblePathMetadataPathPolicyClass.Content_allowed or VisiblePathMetadataPathPolicyClass.Metadata_only))
            {
                return EvidenceUnavailable();
            }

            WorkspaceLockStatus workspaceLock = await folders.GetWorkspaceLockAsync(
                folderId, workspaceId, request.CorrelationId, ReadConsistencyClass.Read_your_writes, cancellationToken)
                .ConfigureAwait(false);
            DateTimeOffset now = DateTimeOffset.UtcNow;
            if (workspaceLock is not { LockState: LockState.Locked, Lease: { LockFence: > 0, LeaseStatus: LockLeaseStatus.Active } }
                || workspaceLock.WorkspaceReference?.Value != workspaceId
                || workspaceLock.WorkspaceReference.Redaction?.Visibility != RedactionMetadataVisibility.Metadata_only
                || workspaceLock.Freshness is not { Stale: false, ReadConsistency: ReadConsistencyClass.Read_your_writes }
                || string.IsNullOrWhiteSpace(workspaceLock.Lease.LockId)
                || workspaceLock.Lease.HolderRef != taskId
                || workspaceLock.Lease.EffectiveAt > now
                || workspaceLock.Lease.ExpiresAt <= now)
            {
                return EvidenceUnavailable();
            }

            FileUploadDescriptor descriptor = new()
            {
                FolderId = folderId,
                WorkspaceId = workspaceId,
                OperationId = operationId,
                PathMetadata = path,
                MediaType = AttachmentStorageIdentity.SafeMediaType(request.Content.MediaType ?? request.ContentType),
                ContentMediaType = request.ContentType,
                FileOperationKind = FileMutationRequestFileOperationKind.Add,
                ContentHashReference = request.Content.ContentHashReference,
                ExpectedFileVersion = file.Version,
                ExpectedLockFence = workspaceLock.Lease.LockFence,
            };
            // Forward the observation unchanged. Only the owner can admit Add/replay; a current
            // version is not proof of absence or of the original replay preconditions.
            FileMutationRequest mutation = FileUpload.BuildInlineFileMutation(
                request.Content.Content, descriptor.MediaType, descriptor.PathMetadata, descriptor.OperationId,
                descriptor.FileOperationKind, descriptor.ContentMediaType, descriptor.ContentHashReference,
                descriptor.ExpectedFileVersion, descriptor.ExpectedLockFence);
            string idempotencyKey = FileUpload.ComputeIdempotencyKey(mutation, workspaceId, taskId);

            uploadStarted = true;
            AcceptedCommand accepted = await folders
                .UploadFileAsync(descriptor, request.Content.Content, idempotencyKey, request.CorrelationId, taskId, cancellationToken)
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
        catch (Exception ex) when (ex is ArgumentException { ParamName: "contentHashReference" }
            or Newtonsoft.Json.JsonException
            or System.Reflection.TargetInvocationException { InnerException: Newtonsoft.Json.JsonException })
        {
            // The SDK rejects mismatched bytes/hash before transport. Generated request validation
            // also runs through JSON callbacks, which may wrap validation failures in reflection.
            return MailboxAttachmentStorageResult.Failed(new AttachmentStorageFailure(
                ProjectConversationAttachmentStatus.Unavailable,
                "not-evaluated",
                "not-retryable",
                "not-eligible",
                "folders_request_unavailable"));
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
        catch (HexalithFoldersApiException ex) when (ex.StatusCode is 404 or 410)
        {
            return MailboxAttachmentStorageResult.Failed(new AttachmentStorageFailure(
                ProjectConversationAttachmentStatus.Unavailable,
                "not-evaluated",
                "not-retryable",
                "not-eligible",
                "folders_resource_unavailable"));
        }
        catch (HexalithFoldersApiException ex) when (ex.StatusCode is 400 or 422 || (ex.StatusCode is 413 && !uploadStarted))
        {
            return MailboxAttachmentStorageResult.Failed(new AttachmentStorageFailure(
                ProjectConversationAttachmentStatus.Unavailable,
                "not-evaluated",
                "not-retryable",
                "not-eligible",
                "folders_request_unavailable"));
        }
        catch (HexalithFoldersApiException ex) when (ex.StatusCode is 409)
        {
            if (!uploadStarted)
            {
                return EvidenceUnavailable();
            }

            return MailboxAttachmentStorageResult.Failed(new AttachmentStorageFailure(
                ProjectConversationAttachmentStatus.Retryable,
                "duplicate-pending",
                "retryable",
                "not-eligible",
                "folders_duplicate_replay_pending"));
        }
        // Includes owner query timeouts, dependency failures and malformed successful HTTP
        // responses, which the generated client wraps in HexalithFoldersApiException.
        catch (HexalithFoldersApiException)
        {
            return MailboxAttachmentStorageResult.Failed(new AttachmentStorageFailure(
                ProjectConversationAttachmentStatus.Retryable,
                "not-evaluated",
                "retryable",
                "not-eligible",
                "folders_store_retryable"));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException or InvalidOperationException)
        {
            return MailboxAttachmentStorageResult.Failed(new AttachmentStorageFailure(
                ProjectConversationAttachmentStatus.Retryable,
                "not-evaluated",
                "retryable",
                "not-eligible",
                "folders_store_retryable"));
        }
    }

    private static MailboxAttachmentStorageResult EvidenceUnavailable()
        => MailboxAttachmentStorageResult.Failed(new AttachmentStorageFailure(
            ProjectConversationAttachmentStatus.Retryable,
            "not-evaluated",
            "retryable",
            "not-eligible",
            "folders_write_evidence_unavailable"));

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
