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
        RecordingFoldersClient client = new();
        FoldersFolderStore store = new(client);
        StoreMailboxAttachmentRequest request = Request("hashref_abc");

        MailboxAttachmentStorageResult result = await store.StoreMailboxAttachmentAsync(request, TestContext.Current.CancellationToken);
        MailboxAttachmentStorageResult replay = await store.StoreMailboxAttachmentAsync(request, TestContext.Current.CancellationToken);

        StoredMailboxAttachmentReference stored = result.Stored.ShouldNotBeNull();
        StoredMailboxAttachmentReference replayed = replay.Stored.ShouldNotBeNull();
        stored.FolderId.ShouldBe(replayed.FolderId);
        stored.FileId.ShouldBe(replayed.FileId);
        stored.IdempotencyKey.ShouldBe(replayed.IdempotencyKey);
        stored.IdempotencyKey.ShouldStartWith("sha256:");
        client.Requests.Count.ShouldBe(2);
        client.Requests.ShouldAllBe(call => call.FolderId == stored.FolderId);
        client.Requests.ShouldAllBe(call => call.IdempotencyKey == stored.IdempotencyKey);
        client.Requests.ShouldAllBe(call => call.CorrelationId == "correlation-001");
        client.Requests.ShouldAllBe(call => call.Body.PathMetadata!.NormalizedPath.Contains("invoice.pdf", StringComparison.Ordinal));
    }

    [Fact]
    public async Task StoreMailboxAttachmentShouldReturnRetryableForOversizedInlineContentWithoutCallingFolders()
    {
        RecordingFoldersClient client = new();
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
        client.Requests.ShouldBeEmpty();
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

    private sealed class RecordingFoldersClient : Hexalith.Folders.Client.Generated.Client
    {
        public RecordingFoldersClient()
            : base(new HttpClient { BaseAddress = new Uri("http://folders.test") })
        {
        }

        public List<RecordedFoldersCall> Requests { get; } = [];

        public override Task<AcceptedCommand> AddFileAsync(
            string folderId,
            string workspaceId,
            string idempotency_Key,
            string x_Correlation_Id,
            string x_Hexalith_Task_Id,
            FileMutationRequest body,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Requests.Add(new RecordedFoldersCall(folderId, workspaceId, idempotency_Key, x_Correlation_Id, x_Hexalith_Task_Id, body));
            return Task.FromResult(new AcceptedCommand
            {
                AcceptedAt = DateTimeOffset.UtcNow,
                CorrelationId = x_Correlation_Id,
                TaskId = x_Hexalith_Task_Id,
                Status = AcceptedCommandStatus.Accepted,
                IdempotentReplay = false,
            });
        }
    }

    private sealed record RecordedFoldersCall(
        string FolderId,
        string WorkspaceId,
        string IdempotencyKey,
        string CorrelationId,
        string TaskId,
        FileMutationRequest Body);
}
