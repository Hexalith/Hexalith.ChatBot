using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.Folders.Client.Convenience;
using Hexalith.Folders.Client.Generated;
using Hexalith.Folders.Client.Native;
using Newtonsoft.Json.Linq;

namespace Hexalith.ChatBot.Server.Adapters.Folders;

internal sealed partial class FoldersFolderStore
{
    private const int RequestedLeaseSeconds = 300;

    private readonly INativeClient? _native;
    private readonly string? _nativeTenant;
    private readonly TimeProvider _nativeClock = TimeProvider.System;

    internal FoldersFolderStore(INativeClient native, string tenantId, TimeProvider? clock = null) : this(null!)
    {
        _native = native ?? throw new ArgumentNullException(nameof(native));
        _nativeTenant = tenantId;
        _nativeClock = clock ?? TimeProvider.System;
    }

    public bool NeedsSubmissionFence => _native is null;

    public ValueTask<MailboxAttachmentStorageResult> ConfirmMailboxAttachmentAsync(StoreMailboxAttachmentRequest request, CancellationToken cancellationToken)
        => _native is null ? ValueTask.FromResult(EvidenceUnavailable()) : StoreNativeAsync(request, cancellationToken, confirmationOnly: true);

    private async ValueTask<MailboxAttachmentStorageResult> StoreNativeAsync(StoreMailboxAttachmentRequest request, CancellationToken token, bool confirmationOnly = false)
    {
        if (!string.Equals(request.TenantId, _nativeTenant, StringComparison.Ordinal)) return NativeFailure(false, "folders_authorization_unavailable");
        if (request.Content.Kind is not Adapters.Mailbox.MailboxAttachmentContentResultKind.Available)
            return MailboxAttachmentStorageResult.Failed(FailureForContentKind(request.Content.Kind, request.Content.ReasonCode));
        if (request.Content.Content.Length > FileUpload.InlineTransportBoundaryBytes) return NativeFailure(true, "attachment_content_streaming_required");
        string folder = AttachmentStorageIdentity.FolderIdFor(request.TenantId, request.ProjectId);
        string workspace = AttachmentStorageIdentity.WorkspaceIdFor(request.TenantId, request.ProjectId, request.AssociationId);
        string operation = AttachmentStorageIdentity.NativeOperationIdFor(request);
        string task = AttachmentStorageIdentity.NativeTaskIdFor(operation);
        string media = AttachmentStorageIdentity.SafeMediaType(request.Content.MediaType ?? request.ContentType);
        var path = AttachmentStorageIdentity.NativePathFor(request);
        string hash = "hashref_" + Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(request.Content.Content.Span));
        if (request.Content.ContentHashReference is { } explicitHash && explicitHash != hash) return NativeFailure(false, "folders_request_unavailable");
        NativeLease? lease = null;
        CancellationTokenSource? maintenance = null;
        Task? maintain = null;
        using var work = CancellationTokenSource.CreateLinkedTokenSource(token);
        try
        {
            // Canonical helper invocation never serializes the invalid historical mutation.
            string legacyOperation = AttachmentStorageIdentity.OperationIdFor(request);
            string legacyTask = AttachmentStorageIdentity.TaskIdFor(legacyOperation);
            var legacyPath = AttachmentStorageIdentity.PathFor(request);
            string legacyKey = NativeFileUpload.ComputeLegacyAddIdempotencyKey(legacyOperation, legacyPath, hash, workspace, legacyTask);
            var prior = await QueryAsync(legacyTask, legacyOperation, legacyPath, legacyKey, true, NativeEvidenceTokens.Accepted, 0, work.Token).ConfigureAwait(false);
            if (prior.Status == NativeEvidenceTokens.Accepted) return await FinishAsync(prior, legacyTask, legacyOperation, legacyKey, work.Token, nativeCompletion: false).ConfigureAwait(false);
            if (prior.Status != "missing") return EvidenceUnavailable();
            string key = FileUpload.ComputeIdempotencyKey(KeyOnly(operation, path, hash), workspace, task);
            prior = await QueryAsync(task, operation, path, key, false, NativeEvidenceTokens.Accepted, 0, work.Token).ConfigureAwait(false);
            if (prior.Status == NativeEvidenceTokens.Accepted) return await FinishAsync(prior, task, operation, key, work.Token).ConfigureAwait(false);
            if (prior.Status != "missing") return EvidenceUnavailable();
            if (confirmationOnly) return EvidenceUnavailable();
            NativeLease acquired = await _native!.ChangeNativeTaskLeaseAsync(folder, workspace, request.CorrelationId, task,
                new NativeLeaseRequest { Operation = "acquire", RequestedLeaseSeconds = RequestedLeaseSeconds }, work.Token).ConfigureAwait(false);
            if (acquired is null || acquired.LockFence <= 0 || string.IsNullOrWhiteSpace(acquired.LockId) ||
                string.IsNullOrWhiteSpace(acquired.OwnershipProof)) return EvidenceUnavailable();
            // Retain the ownership proof for release even when the returned timing is unusable.
            lease = acquired;
            if (!IsUsableLeaseWindow(acquired, _nativeClock.GetUtcNow())) return EvidenceUnavailable();
            long fence = acquired.LockFence;
            maintenance = CancellationTokenSource.CreateLinkedTokenSource(work.Token);
            maintain = MaintainAsync();
            NativeEvidence oldAbsence = await QueryAsync(task, operation, legacyPath, key, true, "absence", fence, work.Token).ConfigureAwait(false);
            NativeEvidence absence = await QueryAsync(task, operation, path, key, false, "absence", fence, work.Token).ConfigureAwait(false);
            work.Token.ThrowIfCancellationRequested();
            if (oldAbsence.Status != "absent" || absence.Status != "absent" || absence.ExpectedFileVersion != 0
                || absence.LockFence != fence) return EvidenceUnavailable();
            var mutation = FileUpload.BuildInlineFileMutation(request.Content.Content, media, path, operation,
                FileMutationRequestFileOperationKind.Add, request.ContentType, hash, absence.ExpectedFileVersion, absence.LockFence);
            // Reuse SDK validation and canonical construction, then select the supported native v1 wire schema.
            var body = JObject.FromObject(mutation); body["requestSchemaVersion"] = "v1";
            NativeAcceptedCommand accepted = await _native.AddNativeFileAsync(folder, workspace, request.CorrelationId, task, key, body, work.Token).ConfigureAwait(false);
            NativeEvidence confirmed = await QueryAsync(task, operation, path, key, false, NativeEvidenceTokens.Accepted, 0, work.Token).ConfigureAwait(false);
            await StopMaintenanceAsync().ConfigureAwait(false);
            work.Token.ThrowIfCancellationRequested();
            var result = await FinishAsync(confirmed, task, operation, key, work.Token, accepted.IdempotentReplay).ConfigureAwait(false);
            if (result.Stored is not null) lease = null;
            return result;

            async Task MaintainAsync()
            {
                try
                {
                    DateTimeOffset lastAuthorization = _nativeClock.GetUtcNow();
                    while (true)
                    {
                        NativeLease current = lease!;
                        DateTimeOffset next = NextLeaseObservationAt(current, lastAuthorization);
                        TimeSpan delay = next - _nativeClock.GetUtcNow();
                        await Task.Delay(delay > TimeSpan.Zero ? delay : TimeSpan.Zero, _nativeClock, maintenance!.Token).ConfigureAwait(false);
                        DateTimeOffset now = _nativeClock.GetUtcNow();
                        if (now >= current.ExpiresAt) throw new InvalidOperationException("folders_lease_expired");
                        DateTimeOffset renewalAt = current.EffectiveAt.AddSeconds(current.RenewalIntervalSeconds);
                        bool renewalDue = now >= renewalAt;
                        NativeLease observed = await _native.ChangeNativeTaskLeaseAsync(folder, workspace, request.CorrelationId, task,
                            LeaseRequest(renewalDue ? "renew" : "validate", current), maintenance.Token).ConfigureAwait(false);
                        now = _nativeClock.GetUtcNow();
                        if (observed is null || !IsUsableLeaseWindow(observed, now) ||
                            observed.LockFence != current.LockFence || observed.LockId != current.LockId ||
                            observed.OwnershipProof != current.OwnershipProof ||
                            !MatchesObservationWindow(current, observed, renewalDue ? renewalAt : null))
                        {
                            throw new InvalidOperationException("folders_lease_observation_invalid");
                        }

                        lease = observed;
                        lastAuthorization = now;
                    }
                }
                catch (OperationCanceledException) when (maintenance!.IsCancellationRequested) { }
                catch (Exception) { await work.CancelAsync().ConfigureAwait(false); }
            }
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { throw; }
        catch (HexalithFoldersNativeApiException error) when (error.StatusCode is 401 or 403 or 404)
        { return NativeFailure(false, "folders_authorization_unavailable"); }
        catch (HexalithFoldersNativeApiException error) when (error.StatusCode == 409)
        {
            string? code = null;
            try { if (JObject.Parse(error.Response ?? string.Empty)["code"] is JValue { Type: JTokenType.String } value) code = (string?)value; }
            catch (Newtonsoft.Json.JsonException) { }
            return code switch
            {
                "native_evidence_changed" or "native_contention" => NativeFailure(true, "folders_concurrency_conflict"),
                "idempotency_conflict" => NativeFailure(false, "folders_idempotency_conflict"),
                "target_conflict" => NativeFailure(false, "folders_target_conflict"),
                _ => NativeFailure(false, "folders_concurrency_conflict"),
            };
        }
        catch (HexalithFoldersNativeApiException error) when (error.StatusCode is 400 or 422)
        { return NativeFailure(false, "folders_request_unavailable"); }
        catch (Exception error) when (error is Newtonsoft.Json.JsonException or ArgumentException
            or System.Reflection.TargetInvocationException { InnerException: Newtonsoft.Json.JsonException })
        { return NativeFailure(false, "folders_request_unavailable"); }
        catch (Exception error) when (error is HexalithFoldersNativeApiException or HttpRequestException or IOException or OperationCanceledException or InvalidOperationException)
        { return NativeFailure(true, "folders_store_retryable"); }
        finally
        {
            await StopMaintenanceAsync().ConfigureAwait(false);
            if (lease is not null)
            {
                // Cancellation must not skip safe release; the owner rejects release whenever staged work remains.
                using var cleanup = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                try { _ = await _native!.ChangeNativeTaskLeaseAsync(folder, workspace, request.CorrelationId, task,
                    LeaseRequest("release", lease), cleanup.Token).ConfigureAwait(false); }
                catch (Exception) { /* Retain the exact owner lease for source-confirmed replay recovery. */ }
            }
            maintenance?.Dispose();
        }

        async Task StopMaintenanceAsync()
        {
            if (maintenance is null) return;
            await maintenance.CancelAsync().ConfigureAwait(false);
            if (maintain is not null) await maintain.ConfigureAwait(false);
        }
        Task<NativeEvidence> QueryAsync(string queryTask, string queryOperation, PathMetadata queryPath, string key, bool legacy,
            string kind, long fence, CancellationToken cancellation)
            => _native!.GetNativeOperationEvidenceAsync(folder, workspace, request.CorrelationId, queryTask, new NativeEvidenceRequest
            { LookupKind = kind, OperationId = queryOperation, AcceptedKey = key, Legacy = legacy, LockFence = fence,
                PathMetadata = new NativePathMetadata { NormalizedPath = queryPath.NormalizedPath, DisplayName = queryPath.DisplayName,
                    PathPolicyClass = "metadata_only", UnicodeNormalization = "NFC" }, ContentHashReference = hash,
                ByteLength = request.Content.Content.Length, MediaType = media }, cancellation);
        async Task<MailboxAttachmentStorageResult> FinishAsync(NativeEvidence evidence, string queryTask, string queryOperation,
            string key, CancellationToken cancellation, bool replay = true, bool nativeCompletion = true)
        {
            if (evidence.Status != NativeEvidenceTokens.Accepted || evidence.TaskId != queryTask || evidence.OperationId != queryOperation
                || evidence.AcceptedKey != key || evidence.ContentHashReference != hash || evidence.ByteLength != request.Content.Content.Length
                || evidence.MediaType != media || evidence.ExpectedFileVersion < 0 || evidence.LockFence <= 0
                || string.IsNullOrWhiteSpace(evidence.Fingerprint) || string.IsNullOrWhiteSpace(evidence.FileId)
                || evidence.IncarnationId != evidence.FileId || evidence.FileVersion != evidence.ExpectedFileVersion + 1)
                return EvidenceUnavailable();
            if (!evidence.Completed && nativeCompletion)
            {
                if (string.IsNullOrEmpty(evidence.LockId) || string.IsNullOrEmpty(evidence.OwnershipProof)) return EvidenceUnavailable();
                _ = await _native!.ChangeNativeTaskLeaseAsync(folder, workspace, request.CorrelationId, queryTask, new NativeLeaseRequest
                { Operation = "complete", RequestedLeaseSeconds = RequestedLeaseSeconds, LockId = evidence.LockId, OwnershipProof = evidence.OwnershipProof,
                    LockFence = evidence.LockFence, OperationId = queryOperation, AcceptedKey = key, Fingerprint = evidence.Fingerprint }, cancellation).ConfigureAwait(false);
            }
            return MailboxAttachmentStorageResult.Succeeded(new StoredMailboxAttachmentReference(folder, evidence.FileId,
                replay ? "duplicate-suppressed" : "unique", "not-retryable", "pending-scan", [], queryOperation, key, ContentConfirmed: true));
        }
    }
    internal static DateTimeOffset NextLeaseObservationAt(NativeLease lease, DateTimeOffset lastAuthorization)
        => new[] { lease.EffectiveAt.AddSeconds(lease.RenewalIntervalSeconds),
            lastAuthorization.AddSeconds(lease.AuthorizationIntervalSeconds), lease.ExpiresAt }.Min();

    private static bool IsUsableLeaseWindow(NativeLease lease, DateTimeOffset now)
        => lease.EffectiveAt != default &&
            lease.EffectiveAt <= now &&
            lease.ExpiresAt > now &&
            lease.RenewalIntervalSeconds is >= 1 and <= 30 &&
            lease.AuthorizationIntervalSeconds is >= 1 and <= 15;

    private static bool MatchesObservationWindow(NativeLease current, NativeLease observed, DateTimeOffset? renewalAt)
        // Renewal advances the window at or after its scheduled boundary. Validation keeps both times.
        => renewalAt is { } boundary
            ? observed.EffectiveAt >= boundary &&
                observed.ExpiresAt - observed.EffectiveAt == TimeSpan.FromSeconds(RequestedLeaseSeconds)
            : observed.EffectiveAt == current.EffectiveAt && observed.ExpiresAt == current.ExpiresAt;

    private static FileMutationRequest KeyOnly(string operation, PathMetadata path, string hash)
        => new() { OperationId = operation, PathMetadata = path, FileOperationKind = FileMutationRequestFileOperationKind.Add, ContentHashReference = hash };
    private static NativeLeaseRequest LeaseRequest(string operation, NativeLease lease)
        => new() { Operation = operation, RequestedLeaseSeconds = RequestedLeaseSeconds, LockId = lease.LockId,
            OwnershipProof = lease.OwnershipProof, LockFence = lease.LockFence };
    private static MailboxAttachmentStorageResult NativeFailure(bool retryable, string reason)
        => MailboxAttachmentStorageResult.Failed(new AttachmentStorageFailure(
            retryable ? ProjectConversationAttachmentStatus.Retryable : ProjectConversationAttachmentStatus.Unavailable,
            "not-evaluated", retryable ? "retryable" : "not-retryable", "not-eligible", reason));
}
