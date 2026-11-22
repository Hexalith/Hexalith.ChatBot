using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Server.Adapters.Folders;
using Hexalith.ChatBot.Server.Adapters.Mailbox;
using Hexalith.ChatBot.Server.Projections;
using Hexalith.ChatBot.Server.Lifecycle.Workflows;
using Hexalith.ChatBot.Server.Operations;

namespace Hexalith.ChatBot.Server.Lifecycle.Attachments;

internal sealed class AttachmentCaptureCoordinator(
    IProjectConversationProjectionStore projectionStore,
    IMailboxAttachmentContentSource contentSource,
    IFolderStore folderStore,
    IAttachmentSafetyPolicy? safetyPolicy = null,
    IAttachmentAuthorizationService? authorizationService = null,
    IAttachmentUnsafeHandlingResolver? unsafeHandlingResolver = null,
    IWorkflowReviewCommandWriter? reviewWriter = null) : IAttachmentCaptureCoordinator
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

        int evaluated = 0;
        string? confirmedSubject = null;
        int stored = 0;
        int degraded = 0;
        foreach (ProjectConversationAttachmentStorageCandidate candidate in candidates)
        {
            cancellationToken.ThrowIfCancellationRequested();

            string subjectId = ProjectConversationStableId.Hash(System.Text.Json.JsonSerializer.Serialize(
                new[] { candidate.AssociationId, candidate.IntakeId, candidate.ProviderAttachmentId, candidate.Ordinal.ToString(System.Globalization.CultureInfo.InvariantCulture) }));
            if (request.SubjectId is not null && (request.SubjectId != subjectId || request.ProjectId != candidate.ProjectId ||
                request.AssociationId != candidate.AssociationId || candidate.SourceVersion > request.SourceVersion)) { continue; }
            evaluated++;
            async Task RecordAsync(string state, string reason)
            {
                if (reviewWriter is null) { return; }
                var transition = new RecordWorkflowReviewTransition(OperationalQueueFamily.FailedAttachment, subjectId,
                    candidate.ProjectId, Math.Max(candidate.SourceVersion, request.SourceVersion), request.Attempt, state, reason,
                    new WorkflowReviewTarget(candidate.AssociationId, candidate.IntakeId, subjectId));
                await reviewWriter.RecordAsync(candidate.TenantId, request.CorrelationId, transition, cancellationToken).ConfigureAwait(false);
            }

            string failureState = request.Attempt >= Retry.RetryFailurePolicy.DefaultMaxAttempts - 1 ? "exhausted" : "failed";
            if (request.Attempt > 0) { await RecordAsync("retrying", "retry_started").ConfigureAwait(false); }

            async ValueTask<T> ObserveDependencyAsync<T>(Func<ValueTask<T>> operation, string unavailable)
            {
                try { return await operation().ConfigureAwait(false); }
                catch (Exception ex) when (ex is not (OperationCanceledException or OutOfMemoryException or StackOverflowException))
                {
                    await RecordAsync(failureState, unavailable).ConfigureAwait(false);
                    throw;
                }
            }

            AttachmentAuthorizationResult authorization = _authorizationService.Authorize(candidate);
            if (authorization.State is not AttachmentAuthorizationState.Authorized)
            {
                await RecordAsync(failureState, "attachment_authority_unavailable").ConfigureAwait(false);
                if (authorization.RedactedOutcome is not null)
                {
                    await projectionStore.UpsertAttachmentSafetyOutcomeAsync(authorization.RedactedOutcome, cancellationToken).ConfigureAwait(false);
                }

                degraded++;
                continue;
            }

            MailboxAttachmentContentResult content = await ObserveDependencyAsync(() => contentSource.FetchAttachmentContentAsync(
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
                    cancellationToken), "attachment_content_unavailable").ConfigureAwait(false);

            ProjectConversationAttachmentSafetyOutcomeView? safety = null;
            if (_safetyPolicy is not null)
            {
                safety = await ObserveDependencyAsync(() => EvaluateSafetyAsync(candidate, content, request,
                    ProjectConversationAttachmentStatus.Pending, null, null, cancellationToken), "attachment_scan_unavailable").ConfigureAwait(false);
                if (safety.ScanStatus is not ProjectConversationAttachmentStatus.Captured)
                {
                    await RecordAsync(failureState, "attachment_safety_blocked").ConfigureAwait(false);
                    await projectionStore.UpsertAttachmentSafetyOutcomeAsync(safety, cancellationToken).ConfigureAwait(false);
                    degraded++;
                    continue;
                }
            }

            bool confirmationOnly = candidate.RetryState == "awaiting-confirmation";
            if (!confirmationOnly && folderStore.NeedsSubmissionFence)
            {
                await projectionStore.UpsertAttachmentStorageOutcomeAsync(ProjectConversationAttachmentStorageOutcomeView.Failed(candidate,
                    ProjectConversationAttachmentStatus.Retryable, "not-evaluated", "awaiting-confirmation", "not-eligible",
                    Math.Max(request.SourceVersion, candidate.SourceVersion), request.CorrelationId), cancellationToken).ConfigureAwait(false);
            }
            var upload = new StoreMailboxAttachmentRequest(
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
                        request.CorrelationId);
            MailboxAttachmentStorageResult storage = await ObserveDependencyAsync(() => confirmationOnly
                ? folderStore.ConfirmMailboxAttachmentAsync(upload, cancellationToken) : folderStore.StoreMailboxAttachmentAsync(upload, cancellationToken),
                "attachment_storage_unavailable").ConfigureAwait(false);

            ProjectConversationAttachmentStorageOutcomeView outcome = ToOutcome(candidate, storage, request);
            // Commit first: if the visible storage outcome removes this candidate, redelivery must
            // already have its queue fact. The Folders adapter rejoins the original operation on retry.
            if (storage.Failure is { } failure)
            {
                if (confirmationOnly) { outcome = outcome with { StorageStatus = ProjectConversationAttachmentStatus.Retryable, RetryState = "awaiting-confirmation" }; }
                await RecordAsync(!confirmationOnly && failure.RetryState == "not-retryable" ? "nonretryable" : failureState, failure.ReasonCode).ConfigureAwait(false);
            }
            else if (storage.Stored is { ContentConfirmed: true })
            {
                await RecordAsync("recovered", "folder_content_confirmed").ConfigureAwait(false);
                confirmedSubject = subjectId;
            }
            else if (storage.IsStored)
            {
                await RecordAsync(failureState, "attachment_confirmation_pending").ConfigureAwait(false);
                outcome = ProjectConversationAttachmentStorageOutcomeView.Failed(candidate, ProjectConversationAttachmentStatus.Retryable,
                    "not-evaluated", "awaiting-confirmation", "not-eligible", upload.SourceVersion, request.CorrelationId);
            }
            await projectionStore.UpsertAttachmentStorageOutcomeAsync(outcome, cancellationToken).ConfigureAwait(false);
            if (storage.Stored is { ContentConfirmed: true })
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

        return new AttachmentCaptureCoordinatorResult(evaluated, stored, degraded, request.SubjectId is null ? null : confirmedSubject, request.SubjectId is not null && confirmedSubject is not null ? request.SourceVersion : null);
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
