using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Server.Adapters.Folders;
using Hexalith.ChatBot.Server.Adapters.Mailbox;
using Hexalith.ChatBot.Server.Projections;

namespace Hexalith.ChatBot.Server.Lifecycle.Attachments;

internal interface IAttachmentCaptureCoordinator
{
    Task<AttachmentCaptureCoordinatorResult> CaptureAsync(
        AttachmentCaptureCoordinatorRequest request,
        CancellationToken cancellationToken = default);
}

internal sealed record AttachmentCaptureCoordinatorRequest(
    string TenantId,
    string IntakeId,
    long SourceVersion,
    string CorrelationId);

internal sealed record AttachmentCaptureCoordinatorResult(
    int EvaluatedCount,
    int StoredCount,
    int DegradedCount);

internal sealed class AttachmentCaptureCoordinator(
    IProjectConversationProjectionStore projectionStore,
    IMailboxAttachmentContentSource contentSource,
    IFolderStore folderStore,
    IAttachmentSafetyPolicy? safetyPolicy = null,
    IAttachmentAuthorizationService? authorizationService = null,
    IAttachmentUnsafeHandlingResolver? unsafeHandlingResolver = null) : IAttachmentCaptureCoordinator
{
    private readonly IAttachmentSafetyPolicy? _safetyPolicy = safetyPolicy;
    private readonly IAttachmentAuthorizationService _authorizationService = authorizationService ?? new ProjectionAttachmentAuthorizationService();
    private readonly IAttachmentUnsafeHandlingResolver _unsafeHandlingResolver = unsafeHandlingResolver ?? new DefaultAttachmentUnsafeHandlingResolver();

    public async Task<AttachmentCaptureCoordinatorResult> CaptureAsync(
        AttachmentCaptureCoordinatorRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        IReadOnlyList<ProjectConversationAttachmentStorageCandidate> candidates = await projectionStore
            .GetAttachmentStorageCandidatesAsync(request.TenantId, request.IntakeId, cancellationToken)
            .ConfigureAwait(false);

        int stored = 0;
        int degraded = 0;
        foreach (ProjectConversationAttachmentStorageCandidate candidate in candidates)
        {
            cancellationToken.ThrowIfCancellationRequested();

            AttachmentAuthorizationResult authorization = _authorizationService.Authorize(candidate);
            if (authorization.State is not AttachmentAuthorizationState.Authorized)
            {
                if (authorization.RedactedOutcome is not null)
                {
                    await projectionStore.UpsertAttachmentSafetyOutcomeAsync(authorization.RedactedOutcome, cancellationToken).ConfigureAwait(false);
                }

                degraded++;
                continue;
            }

            MailboxAttachmentContentResult content = await contentSource
                .FetchAttachmentContentAsync(
                    new MailboxAttachmentContentRequest(
                        candidate.TenantId,
                        candidate.ProjectId,
                        candidate.AssociationId,
                        candidate.IntakeId,
                        candidate.SourceMailboxId,
                        candidate.ProviderMessageId,
                        candidate.ProviderAttachmentId,
                        candidate.Ordinal,
                        Math.Max(request.SourceVersion, candidate.SourceVersion),
                        request.CorrelationId),
                    cancellationToken)
                .ConfigureAwait(false);

            ProjectConversationAttachmentSafetyOutcomeView? safety = null;
            if (_safetyPolicy is not null)
            {
                safety = await EvaluateSafetyAsync(candidate, content, request, ProjectConversationAttachmentStatus.Pending, null, null, cancellationToken)
                    .ConfigureAwait(false);
                if (safety.ScanStatus is not ProjectConversationAttachmentStatus.Captured)
                {
                    await projectionStore.UpsertAttachmentSafetyOutcomeAsync(safety, cancellationToken).ConfigureAwait(false);
                    degraded++;
                    continue;
                }
            }

            MailboxAttachmentStorageResult storage = await folderStore
                .StoreMailboxAttachmentAsync(
                    new StoreMailboxAttachmentRequest(
                        candidate.TenantId,
                        candidate.ProjectId,
                        candidate.AssociationId,
                        candidate.IntakeId,
                        candidate.SourceMailboxId,
                        candidate.ProviderMessageId,
                        candidate.ProviderAttachmentId,
                        candidate.Ordinal,
                        candidate.SafeDisplayName,
                        candidate.ContentType,
                        candidate.SizeInBytes,
                        content,
                        Math.Max(request.SourceVersion, candidate.SourceVersion),
                        request.CorrelationId),
                    cancellationToken)
                .ConfigureAwait(false);

            ProjectConversationAttachmentStorageOutcomeView outcome = ToOutcome(candidate, storage, request);
            await projectionStore.UpsertAttachmentStorageOutcomeAsync(outcome, cancellationToken).ConfigureAwait(false);
            if (storage.IsStored)
            {
                if (safety is not null)
                {
                    await projectionStore.UpsertAttachmentSafetyOutcomeAsync(safety, cancellationToken).ConfigureAwait(false);
                }

                stored++;
            }
            else
            {
                degraded++;
            }
        }

        return new AttachmentCaptureCoordinatorResult(candidates.Count, stored, degraded);
    }

    private async ValueTask<ProjectConversationAttachmentSafetyOutcomeView> EvaluateSafetyAsync(
        ProjectConversationAttachmentStorageCandidate candidate,
        MailboxAttachmentContentResult content,
        AttachmentCaptureCoordinatorRequest request,
        ProjectConversationAttachmentStatus storageStatus,
        string? folderId,
        string? fileId,
        CancellationToken cancellationToken)
    {
        if (_safetyPolicy is null)
        {
            throw new InvalidOperationException("Attachment safety policy is not configured.");
        }

        long sourceVersion = Math.Max(request.SourceVersion, candidate.SourceVersion);
        string unsafeHandling = await _unsafeHandlingResolver
            .ResolveUnsafeHandlingAsync(
                new AttachmentUnsafeHandlingResolutionRequest(
                    candidate.TenantId,
                    candidate.ProjectId,
                    candidate.AssociationId,
                    candidate.IntakeId,
                    candidate.SourceMailboxId,
                    candidate.ProviderMessageId,
                    candidate.ProviderAttachmentId,
                    candidate.Ordinal,
                    sourceVersion,
                    request.CorrelationId),
                cancellationToken)
            .ConfigureAwait(false);
        return await _safetyPolicy
            .EvaluateAsync(
                new AttachmentSafetyPolicyRequest(
                    candidate.TenantId,
                    candidate.ProjectId,
                    candidate.AssociationId,
                    candidate.IntakeId,
                    candidate.SourceMailboxId,
                    candidate.ProviderMessageId,
                    candidate.ProviderAttachmentId,
                    candidate.Ordinal,
                    candidate.SafeDisplayName,
                    candidate.ContentType,
                    candidate.SizeInBytes,
                    storageStatus,
                    folderId,
                    fileId,
                    content,
                    sourceVersion,
                    request.CorrelationId,
                    unsafeHandling),
                cancellationToken)
            .ConfigureAwait(false);
    }

    private static ProjectConversationAttachmentStorageOutcomeView ToOutcome(
        ProjectConversationAttachmentStorageCandidate candidate,
        MailboxAttachmentStorageResult storage,
        AttachmentCaptureCoordinatorRequest request)
    {
        long sourceVersion = Math.Max(request.SourceVersion, candidate.SourceVersion);
        if (storage.Stored is not null)
        {
            return ProjectConversationAttachmentStorageOutcomeView.Stored(
                candidate,
                storage.Stored.FolderId,
                storage.Stored.FileId,
                storage.Stored.DuplicateState,
                storage.Stored.RetryState,
                storage.Stored.AiContextEligibility,
                storage.Stored.AllowedActions,
                sourceVersion,
                request.CorrelationId);
        }

        AttachmentStorageFailure failure = storage.Failure ?? new(
            Contracts.Enums.ProjectConversationAttachmentStatus.Retryable,
            "not-evaluated",
            "retryable",
            "not-eligible",
            "attachment_storage_retryable");
        return ProjectConversationAttachmentStorageOutcomeView.Failed(
            candidate,
            failure.Status,
            failure.DuplicateState,
            failure.RetryState,
            failure.AiContextEligibility,
            sourceVersion,
            request.CorrelationId);
    }
}
