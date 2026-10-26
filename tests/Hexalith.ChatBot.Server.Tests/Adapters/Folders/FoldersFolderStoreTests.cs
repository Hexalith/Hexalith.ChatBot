using System.Reflection;
using System.Text.Json;

using Hexalith.ChatBot.Server.Adapters.Folders;
using Hexalith.ChatBot.Server.Adapters.Mailbox;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.Folders.Client.Generated;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Adapters.Folders;

public sealed class FoldersFolderStoreTests
{
    // AcceptedCommand is stubbed to test forwarding and result mapping. Positive observed
    // versions do not establish native Add/replay admission; the real owner may return 409.
    [Fact]
    public async Task StoreMailboxAttachmentShouldUseFoldersClientIdempotencyAndReturnStableReferences()
    {
        Hexalith.Folders.Client.Generated.IClient client = DispatchProxy.Create<Hexalith.Folders.Client.Generated.IClient, RecordingFoldersClient>();
        RecordingFoldersClient recording = (RecordingFoldersClient)client;
        FoldersFolderStore store = new(client);
        StoreMailboxAttachmentRequest request = Request("hashref_2cf24dba5fb0a30e26e83b2ac5b9e29e1b161e5c1fa7425e73043362938b9824");

        MailboxAttachmentStorageResult result = await store.StoreMailboxAttachmentAsync(request, TestContext.Current.CancellationToken);
        recording.IdempotentReplay = true;
        MailboxAttachmentStorageResult replay = await store.StoreMailboxAttachmentAsync(request, TestContext.Current.CancellationToken);

        StoredMailboxAttachmentReference stored = result.Stored.ShouldNotBeNull();
        StoredMailboxAttachmentReference replayed = replay.Stored.ShouldNotBeNull();
        stored.FolderId.ShouldBe(replayed.FolderId);
        stored.FileId.ShouldBe(replayed.FileId);
        stored.IdempotencyKey.ShouldBe(replayed.IdempotencyKey);
        stored.IdempotencyKey.ShouldStartWith("sha256:");
        recording.Requests.Count.ShouldBe(2);
        recording.Requests.ShouldAllBe(call => call.FolderId == stored.FolderId);
        recording.Requests.ShouldAllBe(call => call.IdempotencyKey == stored.IdempotencyKey);
        recording.Requests.ShouldAllBe(call => call.CorrelationId == "correlation-001");
        recording.Requests.ShouldAllBe(call => call.Body.PathMetadata!.NormalizedPath.Contains("invoice.pdf", StringComparison.Ordinal));
        recording.Requests.ShouldAllBe(call => call.Body.PathMetadata!.PathPolicyClass == PathMetadataPathPolicyClass.Metadata_only);
        recording.Requests.ShouldAllBe(call => call.Body.OperationId == stored.StorageOperationId);
        recording.Requests.ShouldAllBe(call => call.Body.ExpectedFileVersion == 17);
        recording.Requests.ShouldAllBe(call => call.Body.ExpectedLockFence == 29);
        recording.MetadataRequests.ShouldAllBe(call => call.FolderId == stored.FolderId
            && call.WorkspaceId == recording.Requests[0].WorkspaceId
            && call.CorrelationId == "correlation-001"
            && call.TaskId == recording.Requests[0].TaskId
            && call.Consistency == ReadConsistencyClass.Snapshot_per_task
            && call.Body.RequestSchemaVersion == "v2"
            && call.Body.Paths.Single().NormalizedPath == recording.Requests[0].Body.PathMetadata.NormalizedPath);
        recording.LockRequests.ShouldAllBe(call => call.FolderId == stored.FolderId
            && call.WorkspaceId == recording.Requests[0].WorkspaceId
            && call.CorrelationId == "correlation-001"
            && call.Consistency == ReadConsistencyClass.Read_your_writes);
        recording.Calls.ShouldBe(new[]
        {
            "GetFolderFileMetadataAsync", "GetWorkspaceLockAsync", "AddFileAsync",
            "GetFolderFileMetadataAsync", "GetWorkspaceLockAsync", "AddFileAsync",
        });
        stored.DuplicateState.ShouldBe("unique");
        replayed.DuplicateState.ShouldBe("duplicate-suppressed");
    }

    [Fact]
    public async Task StoreMailboxAttachmentShouldReturnRetryableForOversizedInlineContentWithoutCallingFolders()
    {
        Hexalith.Folders.Client.Generated.IClient client = DispatchProxy.Create<Hexalith.Folders.Client.Generated.IClient, RecordingFoldersClient>();
        RecordingFoldersClient recording = (RecordingFoldersClient)client;
        FoldersFolderStore store = new(client);
        byte[] content = new byte[Hexalith.Folders.Client.Convenience.FileUpload.InlineTransportBoundaryBytes + 1];

        MailboxAttachmentStorageResult result = await store.StoreMailboxAttachmentAsync(
            Request("hashref_large") with
            {
                Content = MailboxAttachmentContentResult.Available(content, "application/pdf", "hashref_large"),
            },
            TestContext.Current.CancellationToken);

        result.Failure.ShouldNotBeNull();
        result.Failure.Status.ShouldBe(ProjectConversationAttachmentStatus.Retryable);
        recording.Calls.ShouldBeEmpty();
        recording.Requests.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(401, ProjectConversationAttachmentStatus.Unavailable, "not-evaluated", "not-retryable", "folders_authorization_unavailable")]
    [InlineData(403, ProjectConversationAttachmentStatus.Unavailable, "not-evaluated", "not-retryable", "folders_authorization_unavailable")]
    [InlineData(409, ProjectConversationAttachmentStatus.Retryable, "duplicate-pending", "retryable", "folders_duplicate_replay_pending")]
    [InlineData(413, ProjectConversationAttachmentStatus.Retryable, "not-evaluated", "retryable", "folders_store_retryable")]
    [InlineData(429, ProjectConversationAttachmentStatus.Retryable, "not-evaluated", "retryable", "folders_store_retryable")]
    [InlineData(503, ProjectConversationAttachmentStatus.Retryable, "not-evaluated", "retryable", "folders_store_retryable")]
    public async Task StoreMailboxAttachmentShouldMapFoldersApiFailuresToSafeMetadata(
        int statusCode,
        ProjectConversationAttachmentStatus expectedStatus,
        string expectedDuplicateState,
        string expectedRetryState,
        string expectedReasonCode)
    {
        Hexalith.Folders.Client.Generated.IClient client = DispatchProxy.Create<Hexalith.Folders.Client.Generated.IClient, RecordingFoldersClient>();
        ((RecordingFoldersClient)client).Failure = ApiException(statusCode);
        FoldersFolderStore store = new(client);

        MailboxAttachmentStorageResult result = await store.StoreMailboxAttachmentAsync(
            Request("hashref_2cf24dba5fb0a30e26e83b2ac5b9e29e1b161e5c1fa7425e73043362938b9824"),
            TestContext.Current.CancellationToken);

        AttachmentStorageFailure failure = result.Failure.ShouldNotBeNull();
        result.Stored.ShouldBeNull();
        failure.Status.ShouldBe(expectedStatus);
        failure.DuplicateState.ShouldBe(expectedDuplicateState);
        failure.RetryState.ShouldBe(expectedRetryState);
        failure.AiContextEligibility.ShouldBe("not-eligible");
        failure.ReasonCode.ShouldBe(expectedReasonCode);

        string serialized = JsonSerializer.Serialize(result);
        serialized.ShouldNotContain("raw folders exception text", Case.Insensitive);
        serialized.ShouldNotContain("provider payload", Case.Insensitive);
        serialized.ShouldNotContain("/home/secret", Case.Insensitive);
        serialized.ShouldNotContain("folder-project-001", Case.Insensitive);
        serialized.ShouldNotContain("file-attachment-001", Case.Insensitive);
    }

    [Fact]
    public async Task StoreMailboxAttachmentShouldMapUnavailableContentKindsWithoutCallingFolders()
    {
        Hexalith.Folders.Client.Generated.IClient client = DispatchProxy.Create<Hexalith.Folders.Client.Generated.IClient, RecordingFoldersClient>();
        RecordingFoldersClient recording = (RecordingFoldersClient)client;
        FoldersFolderStore store = new(client);

        MailboxAttachmentStorageResult unavailable = await store.StoreMailboxAttachmentAsync(
            Request("hashref_2cf24dba5fb0a30e26e83b2ac5b9e29e1b161e5c1fa7425e73043362938b9824") with
            {
                Content = MailboxAttachmentContentResult.Unavailable("graph_attachment_unavailable"),
            },
            TestContext.Current.CancellationToken);
        MailboxAttachmentStorageResult retryable = await store.StoreMailboxAttachmentAsync(
            Request("hashref_2cf24dba5fb0a30e26e83b2ac5b9e29e1b161e5c1fa7425e73043362938b9824") with
            {
                Content = MailboxAttachmentContentResult.Retryable("graph_throttled"),
            },
            TestContext.Current.CancellationToken);
        MailboxAttachmentStorageResult tooLarge = await store.StoreMailboxAttachmentAsync(
            Request("hashref_2cf24dba5fb0a30e26e83b2ac5b9e29e1b161e5c1fa7425e73043362938b9824") with
            {
                Content = MailboxAttachmentContentResult.TooLarge(),
            },
            TestContext.Current.CancellationToken);
        MailboxAttachmentStorageResult unauthorized = await store.StoreMailboxAttachmentAsync(
            Request("hashref_2cf24dba5fb0a30e26e83b2ac5b9e29e1b161e5c1fa7425e73043362938b9824") with
            {
                Content = MailboxAttachmentContentResult.Unauthorized(),
            },
            TestContext.Current.CancellationToken);

        recording.Calls.ShouldBeEmpty();
        recording.Requests.ShouldBeEmpty();
        AssertFailure(unavailable, ProjectConversationAttachmentStatus.Unavailable, "not-retryable", "graph_attachment_unavailable");
        AssertFailure(retryable, ProjectConversationAttachmentStatus.Retryable, "retryable", "graph_throttled");
        AssertFailure(tooLarge, ProjectConversationAttachmentStatus.Retryable, "retryable", "attachment_content_streaming_required");
        AssertFailure(unauthorized, ProjectConversationAttachmentStatus.Unavailable, "not-retryable", "attachment_content_unauthorized");
    }

    [Theory]
    [InlineData(6, 43, VisiblePathMetadataPathPolicyClass.Metadata_only)]
    [InlineData(6, 43, VisiblePathMetadataPathPolicyClass.Content_allowed)]
    [InlineData(102, 901, VisiblePathMetadataPathPolicyClass.Metadata_only)]
    [InlineData(102, 901, VisiblePathMetadataPathPolicyClass.Content_allowed)]
    public async Task StoreMailboxAttachmentShouldForwardExactObservationsAndPreserveOwnerConflict(
        long version, long fence, VisiblePathMetadataPathPolicyClass pathPolicyClass)
    {
        Hexalith.Folders.Client.Generated.IClient client = DispatchProxy.Create<Hexalith.Folders.Client.Generated.IClient, RecordingFoldersClient>();
        RecordingFoldersClient recording = (RecordingFoldersClient)client;
        recording.ChangeMetadata = metadata =>
        {
            FileMetadataItem file = metadata.Items.Single();
            file.Version = version;
            file.Path.PathPolicyClass = pathPolicyClass;
        };
        recording.ChangeLock = workspaceLock => workspaceLock.Lease.LockFence = fence;
        // Native Add rejects a visible positive version; it cannot be turned into an overwrite
        // or a replay by decrementing that version or borrowing a different task's fence.
        recording.Failure = ApiException(409);

        MailboxAttachmentStorageResult result = await new FoldersFolderStore(client).StoreMailboxAttachmentAsync(
            Request(ValidHash), TestContext.Current.CancellationToken);

        FileMutationRequest mutation = recording.Requests.Single().Body;
        mutation.ExpectedFileVersion.ShouldBe(version);
        mutation.ExpectedLockFence.ShouldBe(fence);
        mutation.FileOperationKind.ShouldBe(FileMutationRequestFileOperationKind.Add);
        result.Stored.ShouldBeNull();
        result.Failure.ShouldNotBeNull().ReasonCode.ShouldBe("folders_duplicate_replay_pending");
    }

    [Theory]
    [InlineData("empty")]
    [InlineData("ambiguous")]
    [InlineData("missing-version")]
    [InlineData("negative-version")]
    [InlineData("missing-file-id")]
    [InlineData("wrong-path")]
    [InlineData("directory")]
    [InlineData("stale")]
    [InlineData("wrong-consistency")]
    [InlineData("truncated")]
    public async Task StoreMailboxAttachmentShouldRejectUnusableMetadataWithoutReadingLockOrUploading(string scenario)
    {
        Hexalith.Folders.Client.Generated.IClient client = DispatchProxy.Create<Hexalith.Folders.Client.Generated.IClient, RecordingFoldersClient>();
        RecordingFoldersClient recording = (RecordingFoldersClient)client;
        recording.ChangeMetadata = metadata =>
        {
            FileMetadataItem file = metadata.Items.Single();
            switch (scenario)
            {
                case "empty": metadata.Items.Clear(); break;
                case "ambiguous": metadata.Items.Add(file); break;
                case "missing-version": file.Version = 0; break;
                case "negative-version": file.Version = -1; break;
                case "missing-file-id": file.FileId = null!; break;
                case "wrong-path": file.Path.NormalizedPath = "other.pdf"; break;
                case "directory": file.Kind = FileMetadataItemKind.Directory; break;
                case "stale": metadata.Freshness.Stale = true; break;
                case "wrong-consistency": metadata.Freshness.ReadConsistency = ReadConsistencyClass.Eventually_consistent; break;
                case "truncated": metadata.Limits.IsTruncated = true; break;
                default: throw new InvalidOperationException("Unknown scenario.");
            }
        };

        MailboxAttachmentStorageResult result = await new FoldersFolderStore(client).StoreMailboxAttachmentAsync(
            Request(ValidHash), TestContext.Current.CancellationToken);

        AssertFailure(result, ProjectConversationAttachmentStatus.Retryable, "retryable", "folders_write_evidence_unavailable");
        recording.Calls.ShouldBe(new[] { "GetFolderFileMetadataAsync" });
        recording.Requests.ShouldBeEmpty();
    }

    [Fact]
    public async Task StoreMailboxAttachmentShouldKeepCanonicalIdentityWhenOwnerObservationsChange()
    {
        Hexalith.Folders.Client.Generated.IClient client = DispatchProxy.Create<Hexalith.Folders.Client.Generated.IClient, RecordingFoldersClient>();
        RecordingFoldersClient recording = (RecordingFoldersClient)client;
        recording.Failure = ApiException(409);
        FoldersFolderStore store = new(client);

        await store.StoreMailboxAttachmentAsync(Request(ValidHash), TestContext.Current.CancellationToken);
        recording.ChangeMetadata = metadata => metadata.Items.Single().Version = 18;
        recording.ChangeLock = workspaceLock => workspaceLock.Lease.LockFence = 30;
        await store.StoreMailboxAttachmentAsync(Request(ValidHash), TestContext.Current.CancellationToken);

        recording.Requests.Count.ShouldBe(2);
        recording.Requests[0].Body.ExpectedFileVersion.ShouldBe(17);
        recording.Requests[1].Body.ExpectedFileVersion.ShouldBe(18);
        recording.Requests[0].Body.ExpectedLockFence.ShouldBe(29);
        recording.Requests[1].Body.ExpectedLockFence.ShouldBe(30);
        recording.Requests[1].IdempotencyKey.ShouldBe(recording.Requests[0].IdempotencyKey);
        recording.Requests[1].TaskId.ShouldBe(recording.Requests[0].TaskId);
        recording.Requests[1].Body.OperationId.ShouldBe(recording.Requests[0].Body.OperationId);
    }

    [Theory]
    [InlineData("no-lease")]
    [InlineData("missing-fence")]
    [InlineData("negative-fence")]
    [InlineData("missing-lock-id")]
    [InlineData("other-holder")]
    [InlineData("missing-holder")]
    [InlineData("other-workspace")]
    [InlineData("redacted-workspace")]
    [InlineData("unlocked")]
    [InlineData("released")]
    [InlineData("expired")]
    [InlineData("not-effective")]
    [InlineData("stale")]
    [InlineData("wrong-consistency")]
    public async Task StoreMailboxAttachmentShouldRejectUnusableLockEvidenceWithoutUploading(string scenario)
    {
        Hexalith.Folders.Client.Generated.IClient client = DispatchProxy.Create<Hexalith.Folders.Client.Generated.IClient, RecordingFoldersClient>();
        RecordingFoldersClient recording = (RecordingFoldersClient)client;
        recording.ChangeLock = workspaceLock =>
        {
            switch (scenario)
            {
                case "no-lease": workspaceLock.Lease = null!; break;
                case "missing-fence": workspaceLock.Lease.LockFence = 0; break;
                case "negative-fence": workspaceLock.Lease.LockFence = -1; break;
                case "missing-lock-id": workspaceLock.Lease.LockId = null!; break;
                case "other-holder": workspaceLock.Lease.HolderRef = "another-task"; break;
                case "missing-holder": workspaceLock.Lease.HolderRef = null!; break;
                case "other-workspace": workspaceLock.WorkspaceReference.Value = "another-workspace"; break;
                case "redacted-workspace": workspaceLock.WorkspaceReference.Redaction.Visibility = RedactionMetadataVisibility.Redacted; break;
                case "unlocked": workspaceLock.LockState = LockState.Unlocked; break;
                case "released": workspaceLock.Lease.LeaseStatus = LockLeaseStatus.Released; break;
                case "expired": workspaceLock.Lease.ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(-1); break;
                case "not-effective": workspaceLock.Lease.EffectiveAt = DateTimeOffset.UtcNow.AddMinutes(1); break;
                case "stale": workspaceLock.Freshness.Stale = true; break;
                case "wrong-consistency": workspaceLock.Freshness.ReadConsistency = ReadConsistencyClass.Eventually_consistent; break;
                default: throw new InvalidOperationException("Unknown scenario.");
            }
        };

        MailboxAttachmentStorageResult result = await new FoldersFolderStore(client).StoreMailboxAttachmentAsync(
            Request(ValidHash), TestContext.Current.CancellationToken);

        AssertFailure(result, ProjectConversationAttachmentStatus.Retryable, "retryable", "folders_write_evidence_unavailable");
        recording.Calls.ShouldBe(new[] { "GetFolderFileMetadataAsync", "GetWorkspaceLockAsync" });
        recording.Requests.ShouldBeEmpty();
    }

    /// <summary>Enumerates safe-denial and transient failures at both observation boundaries.</summary>
    public static IEnumerable<object[]> ObservationFailures()
    {
        foreach (string operation in new[] { "GetFolderFileMetadataAsync", "GetWorkspaceLockAsync" })
        {
            foreach (int status in new[] { 400, 401, 403, 404, 408, 409, 410, 413, 422, 429, 500, 503 })
            {
                yield return [operation, status];
            }
        }
    }

    [Theory]
    [MemberData(nameof(ObservationFailures))]
    public async Task StoreMailboxAttachmentShouldFailSafelyWhenAnObservationFails(string operation, int status)
    {
        Hexalith.Folders.Client.Generated.IClient client = DispatchProxy.Create<Hexalith.Folders.Client.Generated.IClient, RecordingFoldersClient>();
        RecordingFoldersClient recording = (RecordingFoldersClient)client;
        recording.FailureAt = operation;
        recording.Failure = ApiException(status);

        MailboxAttachmentStorageResult result = await new FoldersFolderStore(client).StoreMailboxAttachmentAsync(
            Request(ValidHash), TestContext.Current.CancellationToken);

        result.Stored.ShouldBeNull();
        AttachmentStorageFailure failure = result.Failure.ShouldNotBeNull();
        failure.Status.ShouldBe(status is 400 or 401 or 403 or 404 or 410 or 413 or 422 ? ProjectConversationAttachmentStatus.Unavailable : ProjectConversationAttachmentStatus.Retryable);
        failure.ReasonCode.ShouldBe(status switch
        {
            401 or 403 => "folders_authorization_unavailable",
            404 or 410 => "folders_resource_unavailable",
            400 or 413 or 422 => "folders_request_unavailable",
            409 => "folders_write_evidence_unavailable",
            _ => "folders_store_retryable",
        });
        failure.AiContextEligibility.ShouldBe("not-eligible");
        failure.DuplicateState.ShouldBe("not-evaluated");
        failure.RetryState.ShouldBe(failure.Status == ProjectConversationAttachmentStatus.Unavailable ? "not-retryable" : "retryable");
        recording.Requests.ShouldBeEmpty();
        recording.Calls.Last().ShouldBe(operation);
        JsonSerializer.Serialize(result).ShouldNotContain("raw folders", Case.Insensitive);
        JsonSerializer.Serialize(result).ShouldNotContain("/home/secret", Case.Insensitive);
    }

    [Theory]
    [InlineData("metadata")]
    [InlineData("lock")]
    public async Task StoreMailboxAttachmentShouldMapMalformedOwnerHttpResponsesToRetryable(string boundary)
    {
        StoreMailboxAttachmentRequest attachment = Request(ValidHash);
        int calls = 0;
        using FoldersResponseTransport transport = new(_ =>
        {
            calls++;
            string body = boundary == "lock" && calls == 1
                ? MetadataJson(attachment)
                : "{\"privateProviderPayload\":\"/home/secret\"}";
            return new HttpResponseMessage(System.Net.HttpStatusCode.OK) { Content = new StringContent(body) };
        });
        using HttpClient http = new(transport) { BaseAddress = new Uri("https://folders.invalid/") };
        FoldersFolderStore store = new(new Hexalith.Folders.Client.Generated.Client(http));

        MailboxAttachmentStorageResult result = await store.StoreMailboxAttachmentAsync(attachment, TestContext.Current.CancellationToken);

        AssertFailure(result, ProjectConversationAttachmentStatus.Retryable, "retryable", "folders_store_retryable");
        calls.ShouldBe(boundary == "metadata" ? 1 : 2);
        JsonSerializer.Serialize(result).ShouldNotContain("/home/secret", Case.Insensitive);
    }

    [Fact]
    public async Task StoreMailboxAttachmentShouldRejectTheOwnersUnfencedHttpLease()
    {
        StoreMailboxAttachmentRequest attachment = Request(ValidHash);
        string workspaceId = AttachmentStorageIdentity.WorkspaceIdFor(attachment.TenantId, attachment.ProjectId, attachment.AssociationId);
        string taskId = AttachmentStorageIdentity.TaskIdFor(AttachmentStorageIdentity.OperationIdFor(attachment));
        int calls = 0;
        using FoldersResponseTransport transport = new(_ =>
        {
            calls++;
            // Mirrors the owner HTTP lease shape: no lockFence is serialized.
            object freshness = new { readConsistency = "read_your_writes", observedAt = DateTimeOffset.UtcNow, stale = false };
            string body = calls == 1 ? MetadataJson(attachment) : JsonSerializer.Serialize(new
            {
                workspaceReference = new { value = workspaceId, classification = "operator_sanitized", redaction = new { visibility = "metadata_only", reasonCode = "not_redacted" } },
                lockState = "locked",
                lease = new { lockId = "owner-lock", leaseStatus = "active", acquiredAt = DateTimeOffset.UtcNow.AddMinutes(-1), effectiveAt = DateTimeOffset.UtcNow.AddMinutes(-1), expiresAt = DateTimeOffset.UtcNow.AddMinutes(5), holderRef = taskId },
                retryEligibility = new { retryable = false, reasonCode = "lock_active", correlationId = attachment.CorrelationId, taskId, currentState = "locked", freshness },
                freshness,
            });
            return new HttpResponseMessage(System.Net.HttpStatusCode.OK) { Content = new StringContent(body) };
        });
        using HttpClient http = new(transport) { BaseAddress = new Uri("https://folders.invalid/") };

        MailboxAttachmentStorageResult result = await new FoldersFolderStore(new Hexalith.Folders.Client.Generated.Client(http))
            .StoreMailboxAttachmentAsync(attachment, TestContext.Current.CancellationToken);

        AssertFailure(result, ProjectConversationAttachmentStatus.Retryable, "retryable", "folders_write_evidence_unavailable");
        calls.ShouldBe(2);
    }

    private static string MetadataJson(StoreMailboxAttachmentRequest attachment)
        => JsonSerializer.Serialize(new
        {
            items = new[]
            {
                new
                {
                    fileId = "owner-file", version = 17, kind = "file", sensitivity = "tenant_sensitive", redaction = "not_redacted",
                    path = new { normalizedPath = AttachmentStorageIdentity.PathFor(attachment).NormalizedPath, displayName = attachment.SafeDisplayName, pathPolicyClass = "metadata_only", unicodeNormalization = "NFC" },
                },
            },
            limits = new { queryFamily = "metadata", configuredLimit = 1, actualCount = 1, actualBytes = 5, elapsedMilliseconds = 1, isTruncated = false },
            freshness = new { readConsistency = "snapshot_per_task", observedAt = DateTimeOffset.UtcNow, stale = false },
        });

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public async Task StoreMailboxAttachmentShouldMapResponseStreamFailuresToRetryable(int failingCall)
    {
        StoreMailboxAttachmentRequest attachment = Request(ValidHash);
        int calls = 0;
        using FailingFoldersResponseStream responseStream = new();
        using FoldersResponseTransport transport = new(_ =>
        {
            calls++;
            if (calls == failingCall)
            {
                return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
                {
                    Content = new StreamContent(responseStream),
                };
            }

            return new HttpResponseMessage(System.Net.HttpStatusCode.OK) { Content = new StringContent(MetadataJson(attachment)) };
        });
        using HttpClient http = new(transport) { BaseAddress = new Uri("https://folders.invalid/") };

        MailboxAttachmentStorageResult result = await new FoldersFolderStore(new Hexalith.Folders.Client.Generated.Client(http))
            .StoreMailboxAttachmentAsync(attachment, TestContext.Current.CancellationToken);

        AssertFailure(result, ProjectConversationAttachmentStatus.Retryable, "retryable", "folders_store_retryable");
        responseStream.ReadAttempted.ShouldBeTrue();
        calls.ShouldBe(failingCall);
        JsonSerializer.Serialize(result).ShouldNotContain("/home/secret", Case.Insensitive);
        JsonSerializer.Serialize(result).ShouldNotContain("Private provider", Case.Insensitive);
    }

    [Theory]
    [InlineData("invoice.pdf", 2)]
    [InlineData("sales report.pdf", 0)]
    [InlineData("résumé.pdf", 0)]
    public async Task StoreMailboxAttachmentShouldMapGeneratedRequestValidationToUnavailable(string fileName, int expectedCalls)
    {
        StoreMailboxAttachmentRequest attachment = Request(ValidHash) with { SafeDisplayName = fileName };
        string workspaceId = AttachmentStorageIdentity.WorkspaceIdFor(attachment.TenantId, attachment.ProjectId, attachment.AssociationId);
        string taskId = AttachmentStorageIdentity.TaskIdFor(AttachmentStorageIdentity.OperationIdFor(attachment));
        int calls = 0;
        using FoldersResponseTransport transport = new(_ =>
        {
            calls++;
            // A fenced fixture reaches generated mutation validation; it does not emulate owner admission.
            object freshness = new { readConsistency = "read_your_writes", observedAt = DateTimeOffset.UtcNow, stale = false };
            string body = calls == 1 ? MetadataJson(attachment) : JsonSerializer.Serialize(new
            {
                workspaceReference = new { value = workspaceId, classification = "operator_sanitized", redaction = new { visibility = "metadata_only", reasonCode = "not_redacted" } },
                lockState = "locked",
                lease = new { lockId = "owner-lock", lockFence = 29, leaseStatus = "active", acquiredAt = DateTimeOffset.UtcNow.AddMinutes(-1), effectiveAt = DateTimeOffset.UtcNow.AddMinutes(-1), expiresAt = DateTimeOffset.UtcNow.AddMinutes(5), holderRef = taskId },
                retryEligibility = new { retryable = false, reasonCode = "lock_active", correlationId = attachment.CorrelationId, taskId, currentState = "locked", freshness },
                freshness,
            });
            return new HttpResponseMessage(System.Net.HttpStatusCode.OK) { Content = new StringContent(body) };
        });
        using HttpClient http = new(transport) { BaseAddress = new Uri("https://folders.invalid/") };

        MailboxAttachmentStorageResult result = await new FoldersFolderStore(new Hexalith.Folders.Client.Generated.Client(http))
            .StoreMailboxAttachmentAsync(attachment, TestContext.Current.CancellationToken);

        AssertFailure(result, ProjectConversationAttachmentStatus.Unavailable, "not-retryable", "folders_request_unavailable");
        calls.ShouldBe(expectedCalls);
        JsonSerializer.Serialize(result).ShouldNotContain("noncanonical", Case.Insensitive);
        JsonSerializer.Serialize(result).ShouldNotContain(fileName, Case.Insensitive);
    }

    [Theory]
    [InlineData("GetFolderFileMetadataAsync", true)]
    [InlineData("GetFolderFileMetadataAsync", false)]
    [InlineData("GetWorkspaceLockAsync", true)]
    [InlineData("GetWorkspaceLockAsync", false)]
    [InlineData("AddFileAsync", true)]
    [InlineData("AddFileAsync", false)]
    public async Task StoreMailboxAttachmentShouldMapDependencyTimeoutToRetryable(string operation, bool taskCanceled)
    {
        using CancellationTokenSource cancellation = new();
        Hexalith.Folders.Client.Generated.IClient client = DispatchProxy.Create<Hexalith.Folders.Client.Generated.IClient, RecordingFoldersClient>();
        RecordingFoldersClient recording = (RecordingFoldersClient)client;
        recording.FailureAt = operation;
        recording.Failure = taskCanceled
            ? new TaskCanceledException("Private provider timeout at /home/secret.")
            : new OperationCanceledException("Private provider timeout at /home/secret.");

        MailboxAttachmentStorageResult result = await new FoldersFolderStore(client)
            .StoreMailboxAttachmentAsync(Request(ValidHash), cancellation.Token);

        cancellation.IsCancellationRequested.ShouldBeFalse();
        AssertFailure(result, ProjectConversationAttachmentStatus.Retryable, "retryable", "folders_store_retryable");
        recording.Calls.Last().ShouldBe(operation);
        recording.Requests.Count.ShouldBe(operation == "AddFileAsync" ? 1 : 0);
        recording.CancellationTokens.ShouldAllBe(token => token == cancellation.Token);
        JsonSerializer.Serialize(result).ShouldNotContain("/home/secret", Case.Insensitive);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public async Task StoreMailboxAttachmentShouldMapHttpDependencyCancellationToRetryable(int failingCall)
    {
        using CancellationTokenSource cancellation = new();
        StoreMailboxAttachmentRequest attachment = Request(ValidHash);
        int calls = 0;
        using FoldersResponseTransport transport = new(_ =>
        {
            calls++;
            if (calls == failingCall)
            {
                throw new OperationCanceledException("Private provider cancellation at /home/secret.");
            }

            return new HttpResponseMessage(System.Net.HttpStatusCode.OK) { Content = new StringContent(MetadataJson(attachment)) };
        });
        using HttpClient http = new(transport) { BaseAddress = new Uri("https://folders.invalid/") };

        MailboxAttachmentStorageResult result = await new FoldersFolderStore(new Hexalith.Folders.Client.Generated.Client(http))
            .StoreMailboxAttachmentAsync(attachment, cancellation.Token);

        cancellation.IsCancellationRequested.ShouldBeFalse();
        AssertFailure(result, ProjectConversationAttachmentStatus.Retryable, "retryable", "folders_store_retryable");
        calls.ShouldBe(failingCall);
        JsonSerializer.Serialize(result).ShouldNotContain("/home/secret", Case.Insensitive);
    }

    [Fact]
    public async Task StoreMailboxAttachmentShouldRejectMismatchedContentHashWithoutUploading()
    {
        Hexalith.Folders.Client.Generated.IClient client = DispatchProxy.Create<Hexalith.Folders.Client.Generated.IClient, RecordingFoldersClient>();
        RecordingFoldersClient recording = (RecordingFoldersClient)client;
        string mismatchedHash = "hashref_" + new string('0', 64);

        MailboxAttachmentStorageResult result = await new FoldersFolderStore(client).StoreMailboxAttachmentAsync(
            Request(mismatchedHash), TestContext.Current.CancellationToken);

        AssertFailure(result, ProjectConversationAttachmentStatus.Unavailable, "not-retryable", "folders_request_unavailable");
        recording.Calls.ShouldBe(new[] { "GetFolderFileMetadataAsync", "GetWorkspaceLockAsync" });
        recording.Requests.ShouldBeEmpty();
        JsonSerializer.Serialize(result).ShouldNotContain(mismatchedHash, Case.Insensitive);
        JsonSerializer.Serialize(result).ShouldNotContain("explicit content hash", Case.Insensitive);
    }

    [Fact]
    public async Task StoreMailboxAttachmentShouldKeepDistinctAcceptedAttachmentsUnique()
    {
        Hexalith.Folders.Client.Generated.IClient client = DispatchProxy.Create<Hexalith.Folders.Client.Generated.IClient, RecordingFoldersClient>();
        RecordingFoldersClient recording = (RecordingFoldersClient)client;
        FoldersFolderStore store = new(client);
        StoreMailboxAttachmentRequest request = Request(ValidHash);

        MailboxAttachmentStorageResult first = await store.StoreMailboxAttachmentAsync(request, TestContext.Current.CancellationToken);
        MailboxAttachmentStorageResult second = await store.StoreMailboxAttachmentAsync(
            request with { ProviderAttachmentId = "attachment-002", Ordinal = 1 }, TestContext.Current.CancellationToken);

        first.Stored.ShouldNotBeNull().DuplicateState.ShouldBe("unique");
        second.Stored.ShouldNotBeNull().DuplicateState.ShouldBe("unique");
        recording.Requests.Count.ShouldBe(2);
        recording.Requests[0].IdempotencyKey.ShouldNotBe(recording.Requests[1].IdempotencyKey);
    }

    [Theory]
    [InlineData("GetFolderFileMetadataAsync", true)]
    [InlineData("GetFolderFileMetadataAsync", false)]
    [InlineData("GetWorkspaceLockAsync", true)]
    [InlineData("GetWorkspaceLockAsync", false)]
    [InlineData("AddFileAsync", true)]
    [InlineData("AddFileAsync", false)]
    public async Task StoreMailboxAttachmentShouldPropagateCallerCancellation(string operation, bool taskCanceled)
    {
        using CancellationTokenSource cancellation = new();
        Hexalith.Folders.Client.Generated.IClient client = DispatchProxy.Create<Hexalith.Folders.Client.Generated.IClient, RecordingFoldersClient>();
        RecordingFoldersClient recording = (RecordingFoldersClient)client;
        recording.FailureAt = operation;
        recording.Failure = taskCanceled ? new TaskCanceledException() : new OperationCanceledException();
        recording.BeforeResponse = () =>
        {
            if (recording.Calls.Last() == operation)
            {
                cancellation.Cancel();
            }
        };

        await Should.ThrowAsync<OperationCanceledException>(async () =>
            await new FoldersFolderStore(client).StoreMailboxAttachmentAsync(Request(ValidHash), cancellation.Token).ConfigureAwait(false));
        recording.Calls.Last().ShouldBe(operation);
        recording.CancellationTokens.Count.ShouldBe(recording.Calls.Count);
        recording.CancellationTokens.ShouldAllBe(token => token == cancellation.Token);
    }

    private const string ValidHash = "hashref_2cf24dba5fb0a30e26e83b2ac5b9e29e1b161e5c1fa7425e73043362938b9824";

    private static StoreMailboxAttachmentRequest Request(string contentHashReference)
        => new(
            "tenant-alpha",
            "project-001",
            "association-001",
            "intake-001",
            "mailbox-001",
            "message-001",
            "attachment-001",
            0,
            "invoice.pdf",
            "application/pdf",
            5,
            MailboxAttachmentContentResult.Available("hello"u8.ToArray(), "application/pdf", contentHashReference),
            10,
            "correlation-001");

    private static void AssertFailure(
        MailboxAttachmentStorageResult result,
        ProjectConversationAttachmentStatus expectedStatus,
        string expectedRetryState,
        string expectedReasonCode)
    {
        AttachmentStorageFailure failure = result.Failure.ShouldNotBeNull();
        result.Stored.ShouldBeNull();
        failure.Status.ShouldBe(expectedStatus);
        failure.DuplicateState.ShouldBe("not-evaluated");
        failure.RetryState.ShouldBe(expectedRetryState);
        failure.AiContextEligibility.ShouldBe("not-eligible");
        failure.ReasonCode.ShouldBe(expectedReasonCode);
    }

    private static HexalithFoldersApiException ApiException(int statusCode)
        => new(
            "raw folders exception text with provider payload and /home/secret",
            statusCode,
            "raw folders response with provider payload and /home/secret",
            new Dictionary<string, IEnumerable<string>>(StringComparer.Ordinal),
            null!);

}
