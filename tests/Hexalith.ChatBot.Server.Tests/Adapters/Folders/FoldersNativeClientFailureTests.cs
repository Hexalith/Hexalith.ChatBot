using System.Net;
using System.Security.Cryptography;
using Hexalith.ChatBot.Server.Adapters.Folders;
using Hexalith.ChatBot.Server.Adapters.Mailbox;
using Hexalith.Folders.Client.Native;
using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Adapters.Folders;

/// <summary>Checks native HTTP failure handling without requiring the source-only owner fixture.</summary>
public sealed class FoldersNativeClientFailureTests
{
    /// <summary>Maps native HTTP failures to bounded attachment failure reasons.</summary>
    /// <param name="status">The owner's HTTP status.</param>
    /// <param name="reason">The expected safe failure reason.</param>
    [Theory]
    [InlineData(401, "folders_authorization_unavailable")]
    [InlineData(403, "folders_authorization_unavailable")]
    [InlineData(404, "folders_authorization_unavailable")]
    [InlineData(409, "folders_concurrency_conflict")]
    [InlineData(422, "folders_request_unavailable")]
    [InlineData(503, "folders_store_retryable")]
    public async Task NativeRuntimeMapsSafeTypedFailures(int status, string reason)
    {
        using FoldersResponseTransport transport = new(_ => new HttpResponseMessage((HttpStatusCode)status)
        {
            Content = new StringContent("{\"code\":\"concurrency_conflict\"}"),
        });
        using HttpClient http = new(transport) { BaseAddress = new Uri("http://localhost/") };
        FoldersFolderStore store = new(new NativeClient(http), "tenant-a");

        MailboxAttachmentStorageResult result = await store.StoreMailboxAttachmentAsync(
            Request(), TestContext.Current.CancellationToken).ConfigureAwait(true);

        result.Stored.ShouldBeNull();
        AttachmentStorageFailure failure = result.Failure.ShouldNotBeNull();
        failure.ReasonCode.ShouldBe(reason);
        failure.DuplicateState.ShouldBe("not-evaluated");
    }

    /// <summary>Keeps malformed conflict details inside the safe failure mapping.</summary>
    [Fact]
    public async Task MalformedConflictProblemCannotEscapeNativeFailureMapping()
    {
        using FoldersResponseTransport transport = new(_ => new HttpResponseMessage(HttpStatusCode.Conflict)
        {
            Content = new StringContent("{\"code\":{\"unexpected\":true}}"),
        });
        using HttpClient http = new(transport) { BaseAddress = new Uri("http://localhost/") };
        FoldersFolderStore store = new(new NativeClient(http), "tenant-a");

        MailboxAttachmentStorageResult result = await store.StoreMailboxAttachmentAsync(
            Request(), TestContext.Current.CancellationToken).ConfigureAwait(true);

        result.Stored.ShouldBeNull();
        result.Failure.ShouldNotBeNull().ReasonCode.ShouldBe("folders_concurrency_conflict");
    }

    /// <summary>Maps malformed native responses and transport loss to retryable failures.</summary>
    [Fact]
    public async Task NativeMalformedResponseAndTransportLossFailSafely()
    {
        using FoldersResponseTransport transport = new(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{"),
        });
        using HttpClient http = new(transport) { BaseAddress = new Uri("http://localhost/") };
        FoldersFolderStore store = new(new NativeClient(http) { ReadResponseAsString = true }, "tenant-a");

        MailboxAttachmentStorageResult malformed = await store.StoreMailboxAttachmentAsync(
            Request(), TestContext.Current.CancellationToken).ConfigureAwait(true);

        malformed.Failure.ShouldNotBeNull().ReasonCode.ShouldBe("folders_store_retryable");
        using FoldersResponseTransport broken = new(_ => throw new HttpRequestException("secret transport details"));
        using HttpClient unavailable = new(broken) { BaseAddress = new Uri("http://localhost/") };
        MailboxAttachmentStorageResult failure = await new FoldersFolderStore(new NativeClient(unavailable), "tenant-a")
            .StoreMailboxAttachmentAsync(Request(), TestContext.Current.CancellationToken).ConfigureAwait(true);
        failure.Failure.ShouldNotBeNull().ReasonCode.ShouldBe("folders_store_retryable");
    }

    private static StoreMailboxAttachmentRequest Request()
    {
        byte[] bytes = [0, 255, 1, 128, 10, 13, 42];
        return new("tenant-a", "project-a", "association-a", "intake-a", "mailbox-a", "message-a", "attachment-a", 1, "test.bin",
            "application/octet-stream", bytes.Length, MailboxAttachmentContentResult.Available(bytes, "application/octet-stream",
                "hashref_" + Convert.ToHexStringLower(SHA256.HashData(bytes))), 1, "correlation-a");
    }
}
