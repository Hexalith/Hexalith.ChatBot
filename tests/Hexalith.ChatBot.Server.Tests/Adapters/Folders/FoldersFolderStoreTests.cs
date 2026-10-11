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
    [Fact]
    public async Task StoreMailboxAttachmentShouldUseFoldersClientIdempotencyAndReturnStableReferences()
    {
        Hexalith.Folders.Client.Generated.IClient client = DispatchProxy.Create<Hexalith.Folders.Client.Generated.IClient, RecordingFoldersClient>();
        RecordingFoldersClient recording = (RecordingFoldersClient)client;
        FoldersFolderStore store = new(client);
        StoreMailboxAttachmentRequest request = Request("hashref_2cf24dba5fb0a30e26e83b2ac5b9e29e1b161e5c1fa7425e73043362938b9824");

        MailboxAttachmentStorageResult result = await store.StoreMailboxAttachmentAsync(request, TestContext.Current.CancellationToken);
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

        recording.Requests.ShouldBeEmpty();
        AssertFailure(unavailable, ProjectConversationAttachmentStatus.Unavailable, "not-retryable", "graph_attachment_unavailable");
        AssertFailure(retryable, ProjectConversationAttachmentStatus.Retryable, "retryable", "graph_throttled");
        AssertFailure(tooLarge, ProjectConversationAttachmentStatus.Retryable, "retryable", "attachment_content_streaming_required");
        AssertFailure(unauthorized, ProjectConversationAttachmentStatus.Unavailable, "not-retryable", "attachment_content_unauthorized");
    }

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

    public class RecordingFoldersClient : DispatchProxy
    {
        public List<RecordedFoldersCall> Requests { get; } = [];

        public Exception? Failure { get; set; }

        protected override object Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            ArgumentNullException.ThrowIfNull(args);
            if (targetMethod?.Name != "AddFileAsync" || args is not { Length: 7 })
            {
                throw new InvalidOperationException("Unexpected Folders client operation.");
            }

            string folderId = (string)args[0]!;
            string workspaceId = (string)args[1]!;
            string idempotencyKey = (string)args[2]!;
            string correlationId = (string)args[3]!;
            string taskId = (string)args[4]!;
            FileMutationRequest body = (FileMutationRequest)args[5]!;
            CancellationToken cancellationToken = (CancellationToken)args[6]!;
            cancellationToken.ThrowIfCancellationRequested();
            Requests.Add(new RecordedFoldersCall(folderId, workspaceId, idempotencyKey, correlationId, taskId, body));
            if (Failure is not null)
            {
                throw Failure;
            }

            return Task.FromResult(new AcceptedCommand
            {
                AcceptedAt = DateTimeOffset.UtcNow,
                CorrelationId = correlationId,
                TaskId = taskId,
                Status = AcceptedCommandStatus.Accepted,
                IdempotentReplay = false,
            });
        }
    }

    public sealed record RecordedFoldersCall(
        string FolderId,
        string WorkspaceId,
        string IdempotencyKey,
        string CorrelationId,
        string TaskId,
        FileMutationRequest Body);
}
