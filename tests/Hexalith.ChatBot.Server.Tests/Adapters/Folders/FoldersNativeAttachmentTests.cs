extern alias FoldersOwner;
using System.Security.Cryptography;
using System.Text;
using Hexalith.ChatBot.Server.Adapters.Folders;
using Hexalith.ChatBot.Server.Adapters.Mailbox;
using Hexalith.Folders.Aggregates.Folder;
using Hexalith.Folders.Client.Native;
using Hexalith.Folders.NativeContent.Tests;
using Hexalith.Folders.Projections.TenantAccess;
using FoldersServerModule = FoldersOwner::Hexalith.Folders.Server.FoldersServerModule;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.TestHost;
using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Adapters.Folders;

public sealed class FoldersNativeAttachmentTests
{
    [Theory]
    [InlineData("invoice.pdf")]
    [InlineData("Facture été 日本語.pdf")]
    public async Task NativeHttpAddPersistsExactBytesAndOwnerIdentityThenReplaysAfterCompletion(string name)
    {
        var request = Request(name: name);
        var fixture = await FixtureAsync(request).ConfigureAwait(true);
        await using var cleanup = fixture.ConfigureAwait(true);
        var store = new FoldersFolderStore(new NativeClient(fixture.Client()), "tenant-a", fixture.Clock);
        var first = await store.StoreMailboxAttachmentAsync(request, TestContext.Current.CancellationToken).ConfigureAwait(true);
        first.Failure.ShouldBeNull(string.Join("; ", fixture.Requests) + " SDK=" + fixture.ContentFailure + " Gateway=" + fixture.Gateway.Failure + " Decision=" + fixture.Gateway.LastDecision);
        var reference = first.Stored.ShouldNotBeNull();
        var binding = (await fixture.BindingsAsync().ConfigureAwait(true)).Single();
        reference.FileId.ShouldBe(binding.Change.FileId);
        (await fixture.ReadAsync(binding).ConfigureAwait(true)).ShouldBe(request.Content.Content.ToArray());
        ("hashref_" + binding.Receipt.Sha256).ShouldBe(request.Content.ContentHashReference);
        var replay = await store.StoreMailboxAttachmentAsync(request, TestContext.Current.CancellationToken).ConfigureAwait(true);
        replay.Stored.ShouldNotBeNull().FileId.ShouldBe(reference.FileId);
        replay.Stored.DuplicateState.ShouldBe("duplicate-suppressed");
        fixture.Gateway.Mutations.ShouldBe(1);
        await fixture.ProjectLockAsync().ConfigureAwait(true);
        (await fixture.StateAsync().ConfigureAwait(true)).WorkspaceLockHolderTaskId.ShouldBeNull();
        fixture.Dapr.TrySaveByteOperations.ShouldContain(write => write.Key == "native-test:retained-events");
    }
    [Fact]
    public async Task ConsecutiveAttachmentsKeepBothPublicationsAndUseAHigherFence()
    {
        var first = Request(); var fixture = await FixtureAsync(first).ConfigureAwait(true);
        await using var cleanup = fixture.ConfigureAwait(true);
        var store = new FoldersFolderStore(new NativeClient(fixture.Client()), "tenant-a", fixture.Clock);
        (await store.StoreMailboxAttachmentAsync(first, TestContext.Current.CancellationToken).ConfigureAwait(true)).Stored.ShouldNotBeNull();
        var second = first with { ProviderAttachmentId = "attachment-b", Ordinal = 2, SafeDisplayName = "other.bin" };
        (await store.StoreMailboxAttachmentAsync(second, TestContext.Current.CancellationToken).ConfigureAwait(true)).Stored.ShouldNotBeNull();
        var bindings = await fixture.BindingsAsync().ConfigureAwait(true); bindings.Count.ShouldBe(2);
        foreach (var binding in bindings) (await fixture.ReadAsync(binding).ConfigureAwait(true)).ShouldBe(first.Content.Content.ToArray());
        var state = await fixture.StateAsync().ConfigureAwait(true); state.ContentPublications.Count.ShouldBe(2); state.WorkspaceLockFence.ShouldBe(2);
        state.WorkspaceCommitReference.ShouldBeNull();
    }
    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public async Task LostResponseAndExpiredLeaseRecoverTheOriginalPublication(bool expire, bool completionLoss)
    {
        var request = Request(); var fixture = await FixtureAsync(request).ConfigureAwait(true);
        await using var cleanup = fixture.ConfigureAwait(true);
        fixture.Gateway.AfterAppend = command =>
        {
            bool target = completionLoss ? command.CommandType == FoldersServerModule.NativeTaskCommandType
                && command.Payload.GetProperty("request").GetProperty("operation").GetString() == "complete"
                : command.CommandType == FoldersServerModule.MutateFilesCommandType;
            if (target) { fixture.Gateway.AfterAppend = null; throw new HttpRequestException("lost response"); }
            return Task.CompletedTask;
        };
        var store = new FoldersFolderStore(new NativeClient(fixture.Client()), "tenant-a", fixture.Clock);
        (await store.StoreMailboxAttachmentAsync(request, TestContext.Current.CancellationToken).ConfigureAwait(true)).Stored.ShouldBeNull();
        if (expire)
        {
            fixture.Clock.Now = fixture.Clock.Now.AddSeconds(301);
            var tenantStore = fixture.App.Services.GetRequiredService<IFolderTenantAccessProjectionStore>();
            var tenant = (await tenantStore.GetAsync("tenant-a", TestContext.Current.CancellationToken).ConfigureAwait(true))!; tenant.LastEventTimestamp = fixture.Clock.Now; await tenantStore.SaveAsync(tenant, TestContext.Current.CancellationToken).ConfigureAwait(true);
        }
        var original = (await fixture.StateAsync().ConfigureAwait(true)).ContentPublications.Values.Single();
        var replay = await store.StoreMailboxAttachmentAsync(request, TestContext.Current.CancellationToken).ConfigureAwait(true);
        replay.Failure.ShouldBeNull(); replay.Stored.ShouldNotBeNull().FileId.ShouldBe(original.FileId);
        fixture.Gateway.Mutations.ShouldBe(1); (await fixture.BindingsAsync().ConfigureAwait(true)).Count.ShouldBe(1);
        (await fixture.StateAsync().ConfigureAwait(true)).WorkspaceLockHolderTaskId.ShouldBeNull();
    }
    [Fact]
    public async Task ChangedBytesAndConcurrentAddNeverImplyDuplicateSuccess()
    {
        var request = Request(); var fixture = await FixtureAsync(request).ConfigureAwait(true);
        await using var cleanup = fixture.ConfigureAwait(true);
        var store = new FoldersFolderStore(new NativeClient(fixture.Client()), "tenant-a", fixture.Clock);
        (await store.StoreMailboxAttachmentAsync(request, TestContext.Current.CancellationToken).ConfigureAwait(true)).Stored.ShouldNotBeNull();
        byte[] other = Encoding.UTF8.GetBytes("changed");
        var changed = request with { Content = MailboxAttachmentContentResult.Available(other, "application/octet-stream", "hashref_" + Convert.ToHexStringLower(SHA256.HashData(other))) };
        AttachmentStorageIdentity.NativeOperationIdFor(changed).ShouldBe(AttachmentStorageIdentity.NativeOperationIdFor(request));
        var result = await store.StoreMailboxAttachmentAsync(changed, TestContext.Current.CancellationToken).ConfigureAwait(true);
        result.Stored.ShouldBeNull(); result.Failure.ShouldNotBeNull().ReasonCode.ShouldBe("folders_idempotency_conflict");
        (await fixture.BindingsAsync().ConfigureAwait(true)).Count.ShouldBe(1);
    }
    [Fact]
    public async Task WrongTenantAndForbiddenPathPublishNothing()
    {
        var request = Request(); var fixture = await FixtureAsync(request).ConfigureAwait(true);
        await using var cleanup = fixture.ConfigureAwait(true);
        var store = new FoldersFolderStore(new NativeClient(fixture.Client()), "tenant-a", fixture.Clock);
        (await store.StoreMailboxAttachmentAsync(request with { TenantId = "tenant-b" }, TestContext.Current.CancellationToken).ConfigureAwait(true)).Stored.ShouldBeNull();
        fixture.App.Configuration["Folders:WorkspaceFilePolicy:Excludes:0"] = "**";
        (await store.StoreMailboxAttachmentAsync(request, TestContext.Current.CancellationToken).ConfigureAwait(true)).Stored.ShouldBeNull();
        fixture.Gateway.Mutations.ShouldBe(0); (await fixture.StateAsync().ConfigureAwait(true)).ContentPublications.ShouldBeEmpty();
    }
    [Fact]
    public async Task ConcurrentIdenticalAddsProduceOneConfirmedPublication()
    {
        var request = Request(); var fixture = await FixtureAsync(request).ConfigureAwait(true);
        await using var cleanup = fixture.ConfigureAwait(true);
        var store = new FoldersFolderStore(new NativeClient(fixture.Client()), "tenant-a", fixture.Clock);
        var results = await Task.WhenAll(store.StoreMailboxAttachmentAsync(request, TestContext.Current.CancellationToken).AsTask(), store.StoreMailboxAttachmentAsync(request, TestContext.Current.CancellationToken).AsTask()).ConfigureAwait(true);
        (await fixture.StateAsync().ConfigureAwait(true)).ContentPublications.Count.ShouldBeLessThanOrEqualTo(1);
        foreach (var failure in results.Where(result => result.Failure is not null)) failure.Failure!.DuplicateState.ShouldBe("not-evaluated");
        var replay = await store.StoreMailboxAttachmentAsync(request, TestContext.Current.CancellationToken).ConfigureAwait(true);
        replay.Failure.ShouldBeNull(string.Join("; ", fixture.Requests) + " SDK=" + fixture.ContentFailure + " Decisions=" + fixture.Gateway.LastDecision);
        var published = (await fixture.BindingsAsync().ConfigureAwait(true)).Single();
        replay.Stored.ShouldNotBeNull().FileId.ShouldBe(published.Change.FileId);
        (await fixture.ReadAsync(published).ConfigureAwait(true)).ShouldBe(request.Content.Content.ToArray());
        foreach (var result in results.Where(result => result.Stored is not null)) result.Stored!.FileId.ShouldBe(published.Change.FileId);
        fixture.Gateway.Mutations.ShouldBe(1);
    }
    [Fact]
    public async Task OwnerReplayBetweenPreflightAndSubmissionPreservesDuplicateDecision()
    {
        var request = Request(); var fixture = await FixtureAsync(request).ConfigureAwait(true);
        await using var cleanup = fixture.ConfigureAwait(true);
        var competitor = new NativeClient(fixture.Client());
        bool raced = false; NativeAttachmentTestHandler handler = null!;
        handler = new NativeAttachmentTestHandler(async (message, token) =>
        {
            if (!raced && message.RequestUri!.AbsolutePath.EndsWith("/native/files/add", StringComparison.Ordinal))
            {
                raced = true;
                var body = Newtonsoft.Json.Linq.JObject.Parse(await message.Content!.ReadAsStringAsync(token).ConfigureAwait(true));
                var winner = await competitor.AddNativeFileAsync(
                    AttachmentStorageIdentity.FolderIdFor(request.TenantId, request.ProjectId),
                    AttachmentStorageIdentity.WorkspaceIdFor(request.TenantId, request.ProjectId, request.AssociationId),
                    request.CorrelationId, message.Headers.GetValues("X-Hexalith-Task-Id").Single(),
                    message.Headers.GetValues("Idempotency-Key").Single(), body, token).ConfigureAwait(true);
                winner.IdempotentReplay.ShouldBeFalse();
            }
            return await handler!.ForwardAsync(message, token).ConfigureAwait(true);
        }) { InnerHandler = fixture.App.GetTestServer().CreateHandler() };
        using var http = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
        var result = await new FoldersFolderStore(new NativeClient(http), "tenant-a", fixture.Clock)
            .StoreMailboxAttachmentAsync(request, TestContext.Current.CancellationToken).ConfigureAwait(true);
        result.Failure.ShouldBeNull(string.Join("; ", fixture.Requests) + " Decisions=" + fixture.Gateway.LastDecision);
        raced.ShouldBeTrue();
        var binding = (await fixture.BindingsAsync().ConfigureAwait(true)).Single();
        result.Stored.ShouldNotBeNull().FileId.ShouldBe(binding.Change.FileId);
        result.Stored.DuplicateState.ShouldBe("duplicate-suppressed");
        fixture.Gateway.LastDecision.ShouldNotBeNull().ShouldContain("\"idempotentReplay\":true");
        fixture.Gateway.Mutations.ShouldBe(1);
        (await fixture.ReadAsync(binding).ConfigureAwait(true)).ShouldBe(request.Content.Content.ToArray());
        (await fixture.StateAsync().ConfigureAwait(true)).WorkspaceLockHolderTaskId.ShouldBeNull();
    }
    [Theory]
    [InlineData(401, "folders_authorization_unavailable")]
    [InlineData(403, "folders_authorization_unavailable")]
    [InlineData(404, "folders_authorization_unavailable")]
    [InlineData(409, "folders_concurrency_conflict")]
    [InlineData(422, "folders_request_unavailable")]
    [InlineData(503, "folders_store_retryable")]
    public async Task NativeRuntimeMapsSafeTypedFailures(int status, string reason)
    {
        using var handler = new NativeAttachmentTestHandler((_, _) => Task.FromResult(new HttpResponseMessage((System.Net.HttpStatusCode)status)
            { Content = new StringContent("{\"code\":\"concurrency_conflict\"}") }));
        using var http = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
        var store = new FoldersFolderStore(new NativeClient(http), "tenant-a");
        var result = await store.StoreMailboxAttachmentAsync(Request(), TestContext.Current.CancellationToken).ConfigureAwait(true);
        result.Stored.ShouldBeNull(); result.Failure.ShouldNotBeNull().ReasonCode.ShouldBe(reason);
        result.Failure.DuplicateState.ShouldBe("not-evaluated");
    }
    [Fact]
    public async Task MalformedConflictProblemCannotEscapeNativeFailureMapping()
    {
        using var handler = new NativeAttachmentTestHandler((_, _) => Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.Conflict)
            { Content = new StringContent("{\"code\":{\"unexpected\":true}}") }));
        using var http = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
        var result = await new FoldersFolderStore(new NativeClient(http), "tenant-a").StoreMailboxAttachmentAsync(Request(), TestContext.Current.CancellationToken).ConfigureAwait(true);
        result.Stored.ShouldBeNull(); result.Failure.ShouldNotBeNull().ReasonCode.ShouldBe("folders_concurrency_conflict");
    }
    [Fact]
    public async Task NativeMalformedResponseAndTransportLossFailSafely()
    {
        using var handler = new NativeAttachmentTestHandler((_, _) => Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            { Content = new StringContent("{") }));
        using var http = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
        var store = new FoldersFolderStore(new NativeClient(http) { ReadResponseAsString = true }, "tenant-a");
        (await store.StoreMailboxAttachmentAsync(Request(), TestContext.Current.CancellationToken).ConfigureAwait(true)).Failure.ShouldNotBeNull().ReasonCode.ShouldBe("folders_store_retryable");
        using var broken = new NativeAttachmentTestHandler((_, _) => throw new HttpRequestException("secret transport details"));
        using var unavailable = new HttpClient(broken) { BaseAddress = new Uri("http://localhost/") };
        var failure = await new FoldersFolderStore(new NativeClient(unavailable), "tenant-a").StoreMailboxAttachmentAsync(Request(), TestContext.Current.CancellationToken).ConfigureAwait(true);
        failure.Failure.ShouldNotBeNull().ReasonCode.ShouldBe("folders_store_retryable");
    }
    [Fact]
    public async Task CallerCancellationStillReleasesAnEmptyNativeLease()
    {
        var request = Request(); var fixture = await FixtureAsync(request).ConfigureAwait(true);
        await using var cleanup = fixture.ConfigureAwait(true);
        using var cancellation = new CancellationTokenSource();
        NativeAttachmentTestHandler handler = null!;
        handler = new NativeAttachmentTestHandler(async (message, token) =>
        {
            if (message.RequestUri!.AbsolutePath.EndsWith("/native/files/add", StringComparison.Ordinal))
            { cancellation.Cancel(); token.ThrowIfCancellationRequested(); }
            return await handler!.ForwardAsync(message, token).ConfigureAwait(true);
        }) { InnerHandler = fixture.App.GetTestServer().CreateHandler() };
        using var http = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
        var store = new FoldersFolderStore(new NativeClient(http), "tenant-a", fixture.Clock);
        await Should.ThrowAsync<OperationCanceledException>(() => store.StoreMailboxAttachmentAsync(request, cancellation.Token).AsTask()).ConfigureAwait(true);
        (await fixture.StateAsync().ConfigureAwait(true)).WorkspaceLockHolderTaskId.ShouldBeNull();
        (await fixture.BindingsAsync().ConfigureAwait(true)).ShouldBeEmpty();
    }
    [Theory]
    [InlineData(true, true)]
    [InlineData(true, false)]
    [InlineData(false, false)]
    public async Task LegacyAcceptedIdentityIsRecoveredOnlyWithExactRetainedEvidence(bool retainedKey, bool closedTask)
    {
        var request = Request(); var fixture = await FixtureAsync(request).ConfigureAwait(true);
        await using var cleanup = fixture.ConfigureAwait(true);
        string operation = AttachmentStorageIdentity.OperationIdFor(request), task = AttachmentStorageIdentity.TaskIdFor(operation);
        var path = AttachmentStorageIdentity.PathFor(request);
        string workspace = AttachmentStorageIdentity.WorkspaceIdFor(request.TenantId, request.ProjectId, request.AssociationId);
        string key = Hexalith.Folders.Client.Convenience.NativeFileUpload.ComputeLegacyAddIdempotencyKey(operation, path,
            request.Content.ContentHashReference!, workspace, task);
        string file = await fixture.SeedHistoricalAsync(task, operation, key, path.NormalizedPath, path.DisplayName,
            request.Content.Content.ToArray(), retainedKey, closedTask).ConfigureAwait(true);
        var store = new FoldersFolderStore(new NativeClient(fixture.Client()), "tenant-a", fixture.Clock);
        var result = await store.StoreMailboxAttachmentAsync(request, TestContext.Current.CancellationToken).ConfigureAwait(true);
        if (retainedKey)
        {
            var stored = result.Stored.ShouldNotBeNull(); stored.FileId.ShouldBe(file); stored.IdempotencyKey.ShouldBe(key); stored.StorageOperationId.ShouldBe(operation);
        }
        else result.Stored.ShouldBeNull();
        fixture.Gateway.Mutations.ShouldBe(0);
        var state = await fixture.StateAsync().ConfigureAwait(true); state.ContentPublications.Count.ShouldBe(1);
        state.IsTaskClosed(fixture.Workspace, task).ShouldBe(closedTask); state.WorkspaceCommitReference.ShouldBeNull();
    }
    [Fact]
    public async Task TransientFreshEvidenceFailureCanRetryTheSameLogicalTaskWithAHigherFence()
    {
        var request = Request(); var fixture = await FixtureAsync(request).ConfigureAwait(true);
        await using var cleanup = fixture.ConfigureAwait(true);
        bool fail = true; NativeAttachmentTestHandler handler = null!;
        handler = new NativeAttachmentTestHandler(async (message, token) =>
        {
            if (fail && message.RequestUri!.AbsolutePath.EndsWith("operation-evidence", StringComparison.Ordinal)
                && (await message.Content!.ReadAsStringAsync(token).ConfigureAwait(true)).Contains("\"lookupKind\":\"absence\"", StringComparison.Ordinal))
            { fail = false; return new HttpResponseMessage(System.Net.HttpStatusCode.ServiceUnavailable) { Content = new StringContent("{}") }; }
            return await handler!.ForwardAsync(message, token).ConfigureAwait(true);
        }) { InnerHandler = fixture.App.GetTestServer().CreateHandler() };
        using var http = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
        var store = new FoldersFolderStore(new NativeClient(http), "tenant-a", fixture.Clock);
        (await store.StoreMailboxAttachmentAsync(request, TestContext.Current.CancellationToken).ConfigureAwait(true)).Failure.ShouldNotBeNull();
        (await fixture.StateAsync().ConfigureAwait(true)).WorkspaceLockHolderTaskId.ShouldBeNull();
        (await store.StoreMailboxAttachmentAsync(request, TestContext.Current.CancellationToken).ConfigureAwait(true)).Stored.ShouldNotBeNull();
        (await fixture.StateAsync().ConfigureAwait(true)).WorkspaceLockFence.ShouldBe(2);
        (await fixture.BindingsAsync().ConfigureAwait(true)).Count.ShouldBe(1);
    }
    [Fact]
    public async Task NativeLeaseRenewsAtTheTighterTenantBoundaryWithAControlledTimer()
    {
        var request = Request(); var fixture = await FixtureAsync(request).ConfigureAwait(true);
        await using var cleanup = fixture.ConfigureAwait(true);
        foreach (var pair in new Dictionary<string, int> { ["RenewalIntervalSeconds"] = 10, ["AuthorizationIntervalSeconds"] = 15,
            ["RevocationEffectSeconds"] = 60, ["ExpiredToStaleSeconds"] = 60 }) fixture.App.Configuration["Folders:NativeLease:" + pair.Key] = pair.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        var uploading = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var renewed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        fixture.Gateway.AfterAppend = command =>
        {
            if (command.CommandType == FoldersServerModule.NativeTaskCommandType && command.Payload.GetProperty("request").GetProperty("operation").GetString() == "renew") renewed.TrySetResult();
            return Task.CompletedTask;
        };
        NativeAttachmentTestHandler handler = null!;
        handler = new NativeAttachmentTestHandler(async (message, token) =>
        {
            if (message.RequestUri!.AbsolutePath.EndsWith("/native/files/add", StringComparison.Ordinal))
            { uploading.TrySetResult(); await release.Task.WaitAsync(token).ConfigureAwait(true); }
            return await handler!.ForwardAsync(message, token).ConfigureAwait(true);
        }) { InnerHandler = fixture.App.GetTestServer().CreateHandler() };
        using var http = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
        var store = new FoldersFolderStore(new NativeClient(http), "tenant-a", fixture.Clock);
        var operation = store.StoreMailboxAttachmentAsync(request, TestContext.Current.CancellationToken).AsTask();
        await uploading.Task.WaitAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);
        fixture.Clock.Advance(TimeSpan.FromSeconds(10));
        await renewed.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken).ConfigureAwait(true);
        (await fixture.StateAsync().ConfigureAwait(true)).WorkspaceLockEffectiveAt.ShouldBe(fixture.Clock.Now);
        release.TrySetResult();
        (await operation.ConfigureAwait(true)).Stored.ShouldNotBeNull();
        (await fixture.StateAsync().ConfigureAwait(true)).WorkspaceLockFence.ShouldBe(1);
    }
    [Theory]
    [InlineData("lock_workspace")]
    [InlineData("mutate_files")]
    public async Task FailedScheduledAuthorityObservationCancelsPendingNativeAdd(string deniedAction)
    {
        var request = Request(); var fixture = await FixtureAsync(request).ConfigureAwait(true);
        await using var cleanup = fixture.ConfigureAwait(true);
        var uploading = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var canceled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        NativeAttachmentTestHandler handler = null!;
        handler = new NativeAttachmentTestHandler(async (message, token) =>
        {
            if (message.RequestUri!.AbsolutePath.EndsWith("/native/files/add", StringComparison.Ordinal))
            {
                uploading.TrySetResult();
                try { await Task.Delay(Timeout.InfiniteTimeSpan, token).ConfigureAwait(true); }
                catch (OperationCanceledException) { canceled.TrySetResult(); throw; }
            }
            return await handler!.ForwardAsync(message, token).ConfigureAwait(true);
        }) { InnerHandler = fixture.App.GetTestServer().CreateHandler() };
        using var http = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
        var operation = new FoldersFolderStore(new NativeClient(http), "tenant-a", fixture.Clock)
            .StoreMailboxAttachmentAsync(request, TestContext.Current.CancellationToken).AsTask();
        await uploading.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken).ConfigureAwait(true);
        fixture.Authority.Denied.Add(deniedAction); fixture.Clock.Advance(TimeSpan.FromSeconds(15));
        await canceled.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken).ConfigureAwait(true);
        var result = await operation.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken).ConfigureAwait(true);
        result.Stored.ShouldBeNull(); result.Failure.ShouldNotBeNull().ReasonCode.ShouldBe("folders_store_retryable");
        fixture.Gateway.Mutations.ShouldBe(0); (await fixture.StateAsync().ConfigureAwait(true)).ContentPublications.ShouldBeEmpty();
    }
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task LostAcquireResponseCanRecoverOnlyItsOwnCleanExpiredLeaseAndContinue(bool recordExpiry)
    {
        var request = Request(); var fixture = await FixtureAsync(request).ConfigureAwait(true);
        await using var cleanup = fixture.ConfigureAwait(true);
        fixture.Gateway.AfterAppend = command =>
        {
            if (command.CommandType == FoldersServerModule.NativeTaskCommandType && command.Payload.GetProperty("request").GetProperty("operation").GetString() == "acquire")
            { fixture.Gateway.AfterAppend = null; throw new HttpRequestException("lost acquire response"); }
            return Task.CompletedTask;
        };
        var store = new FoldersFolderStore(new NativeClient(fixture.Client()), "tenant-a", fixture.Clock);
        (await store.StoreMailboxAttachmentAsync(request, TestContext.Current.CancellationToken).ConfigureAwait(true)).Stored.ShouldBeNull();
        var state = await fixture.StateAsync().ConfigureAwait(true); state.WorkspaceLockFence.ShouldBe(1); state.ContentPublications.ShouldBeEmpty();
        fixture.Clock.Now = state.WorkspaceLockExpiresAt!.Value.AddSeconds(1);
        if (recordExpiry) await fixture.RecordExpiryAsync(AttachmentStorageIdentity.NativeTaskIdFor(AttachmentStorageIdentity.NativeOperationIdFor(request))).ConfigureAwait(true);
        var tenants = fixture.App.Services.GetRequiredService<IFolderTenantAccessProjectionStore>();
        var tenant = (await tenants.GetAsync("tenant-a", TestContext.Current.CancellationToken).ConfigureAwait(true))!;
        tenant.LastEventTimestamp = fixture.Clock.Now; await tenants.SaveAsync(tenant, TestContext.Current.CancellationToken).ConfigureAwait(true);
        var retried = await store.StoreMailboxAttachmentAsync(request, TestContext.Current.CancellationToken).ConfigureAwait(true);
        retried.Failure.ShouldBeNull(string.Join("; ", fixture.Requests) + " Decisions=" + fixture.Gateway.LastDecision + " SDK=" + fixture.ContentFailure);
        retried.Stored.ShouldNotBeNull(); (await fixture.StateAsync().ConfigureAwait(true)).WorkspaceLockFence.ShouldBe(2);
        var next = request with { ProviderAttachmentId = "next-attachment", Ordinal = 2 };
        (await store.StoreMailboxAttachmentAsync(next, TestContext.Current.CancellationToken).ConfigureAwait(true)).Stored.ShouldNotBeNull();
        state = await fixture.StateAsync().ConfigureAwait(true); state.WorkspaceLockFence.ShouldBe(3); state.ContentPublications.Count.ShouldBe(2);
        foreach (var binding in await fixture.BindingsAsync().ConfigureAwait(true)) (await fixture.ReadAsync(binding).ConfigureAwait(true)).ShouldBe(request.Content.Content.ToArray());
        fixture.Gateway.Mutations.ShouldBe(2);
    }
    [Fact]
    public void LeaseObservationUsesEarlierRenewalAndPreservesShortExpiry()
    {
        var clock = new NativeContentClock();
        var lease = new NativeLease { EffectiveAt = clock.Now, ExpiresAt = clock.Now.AddSeconds(20), RenewalIntervalSeconds = 10, AuthorizationIntervalSeconds = 15 };
        FoldersFolderStore.NextLeaseObservationAt(lease, clock.Now).ShouldBe(clock.Now.AddSeconds(10));
        lease.ExpiresAt = clock.Now.AddSeconds(5);
        FoldersFolderStore.NextLeaseObservationAt(lease, clock.Now).ShouldBe(clock.Now.AddSeconds(5));
    }
    private static Task<NativeContentFixture> FixtureAsync(StoreMailboxAttachmentRequest request)
        => NativeContentFixture.CreateAsync(AttachmentStorageIdentity.FolderIdFor(request.TenantId, request.ProjectId),
            AttachmentStorageIdentity.WorkspaceIdFor(request.TenantId, request.ProjectId, request.AssociationId));
    private static StoreMailboxAttachmentRequest Request(string name = "test.bin")
    {
        byte[] bytes = [0, 255, 1, 128, 10, 13, 42];
        return new("tenant-a", "project-a", "association-a", "intake-a", "mailbox-a", "message-a", "attachment-a", 1, name,
            "application/octet-stream", bytes.Length, MailboxAttachmentContentResult.Available(bytes, "application/octet-stream",
                "hashref_" + Convert.ToHexStringLower(SHA256.HashData(bytes))), 1, "correlation-a");
    }
}
