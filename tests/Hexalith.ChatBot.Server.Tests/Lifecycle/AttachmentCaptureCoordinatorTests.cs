using System.Text.Json;
using Hexalith.ChatBot.Server.Gateway;
using Hexalith.ChatBot.Server.Lifecycle.Workflows;
using Hexalith.ChatBot.Server.Tests.Projections;
using Hexalith.EventStore.Client.Gateway;
using Hexalith.EventStore.Client.Streams;
using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.EventStore.Contracts.Streams;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;

using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Server.Adapters.Folders;
using Hexalith.ChatBot.Server.Adapters.Mailbox;
using Hexalith.ChatBot.Server.Association.Intake;
using Hexalith.ChatBot.Server.Lifecycle.Attachments;
using Hexalith.ChatBot.Server.Projections;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Lifecycle;

public sealed class AttachmentCaptureCoordinatorTests
{
    private const string Tenant = "tenant-alpha";
    private const string OtherTenant = "tenant-beta";
    private const string IntakeId = "01ARZ3NDEKTSV4RRFFQ69G5FAY";
    private const string AssociationId = "01ARZ3NDEKTSV4RRFFQ69G5FAV";
    private const string CorrelationId = "01ARZ3NDEKTSV4RRFFQ69G5FAW";
    private static readonly DateTimeOffset DetectedAt = new(2026, 6, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task CaptureShouldStoreAvailableAttachmentAndProjectFolderFileReferences()
    {
        InMemoryProjectConversationProjectionStore store = await StoreWithAssociationAndAttachments().ConfigureAwait(true);
        FakeMailboxContentSource content = new(MailboxAttachmentContentResult.Available("hello"u8.ToArray(), "text/plain", "hashref_abc"));
        FakeFolderStore folders = new();
        AttachmentCaptureCoordinator coordinator = new(store, content, folders);

        AttachmentCaptureCoordinatorResult result = await coordinator.CaptureAsync(Request(10), TestContext.Current.CancellationToken);

        result.ShouldBe(new AttachmentCaptureCoordinatorResult(1, 1, 0));
        folders.Requests.Count.ShouldBe(1);
        ProjectConversationItemView attachment = await SingleAttachmentAsync(store).ConfigureAwait(true);
        attachment.AttachmentStorageStatus.ShouldBe(ProjectConversationAttachmentStatus.Captured);
        attachment.AttachmentFolderId.ShouldBe("folder-project-001");
        attachment.AttachmentFileId.ShouldBe("file-graph-attachment-001-0");
        attachment.AttachmentDuplicateState.ShouldBe("unique");
        attachment.AttachmentRetryState.ShouldBe("not-retryable");
        attachment.AttachmentAiContextEligibility.ShouldBe("pending-scan");
    }

    [Fact]
    public async Task CaptureShouldDegradeUnavailableContentWithoutFolderOrFileReferences()
    {
        InMemoryProjectConversationProjectionStore store = await StoreWithAssociationAndAttachments().ConfigureAwait(true);
        AttachmentCaptureCoordinator coordinator = new(
            store,
            new FakeMailboxContentSource(MailboxAttachmentContentResult.Unavailable("graph_attachment_unavailable")),
            new ContentAwareFolderStore());

        AttachmentCaptureCoordinatorResult result = await coordinator.CaptureAsync(Request(10), TestContext.Current.CancellationToken);

        result.ShouldBe(new AttachmentCaptureCoordinatorResult(1, 0, 1));
        ProjectConversationItemView attachment = await SingleAttachmentAsync(store).ConfigureAwait(true);
        attachment.AttachmentStorageStatus.ShouldBe(ProjectConversationAttachmentStatus.Unavailable);
        attachment.AttachmentFolderId.ShouldBeNull();
        attachment.AttachmentFileId.ShouldBeNull();
        attachment.AttachmentRetryState.ShouldBe("not-retryable");
    }

    [Fact]
    public async Task CaptureShouldProjectFoldersUnavailableAsRetryableMetadataOnlyState()
    {
        InMemoryProjectConversationProjectionStore store = await StoreWithAssociationAndAttachments().ConfigureAwait(true);
        AttachmentCaptureCoordinator coordinator = new(
            store,
            new FakeMailboxContentSource(MailboxAttachmentContentResult.Available("hello"u8.ToArray(), "text/plain", "hashref_abc")),
            new UnavailableFolderStore());

        await coordinator.CaptureAsync(Request(10), TestContext.Current.CancellationToken);

        ProjectConversationItemView attachment = await SingleAttachmentAsync(store).ConfigureAwait(true);
        attachment.AttachmentStorageStatus.ShouldBe(ProjectConversationAttachmentStatus.Retryable);
        attachment.AttachmentFolderId.ShouldBeNull();
        attachment.AttachmentFileId.ShouldBeNull();
        attachment.AttachmentRetryState.ShouldBe("retryable");
    }

    [Fact]
    public async Task CaptureReplayShouldSuppressDuplicateStorageAfterSuccess()
    {
        InMemoryProjectConversationProjectionStore store = await StoreWithAssociationAndAttachments().ConfigureAwait(true);
        FakeFolderStore folders = new();
        AttachmentCaptureCoordinator coordinator = new(
            store,
            new FakeMailboxContentSource(MailboxAttachmentContentResult.Available("hello"u8.ToArray(), "text/plain", "hashref_abc")),
            folders);

        AttachmentCaptureCoordinatorResult first = await coordinator.CaptureAsync(Request(10), TestContext.Current.CancellationToken);
        AttachmentCaptureCoordinatorResult replay = await coordinator.CaptureAsync(Request(10), TestContext.Current.CancellationToken);

        first.EvaluatedCount.ShouldBe(1);
        replay.EvaluatedCount.ShouldBe(0);
        folders.Requests.Count.ShouldBe(1);
    }

    [Fact]
    public async Task CaptureShouldKeepDuplicateProviderAttachmentIdsDistinctByOrdinal()
    {
        InMemoryProjectConversationProjectionStore store = await StoreWithAssociationAndAttachments(
            new MailboxAttachmentReference("graph-attachment-duplicate", "one.pdf", "application/pdf", 100),
            new MailboxAttachmentReference("graph-attachment-duplicate", "two.pdf", "application/pdf", 200)).ConfigureAwait(true);
        var review = new RecordingReviewWriter();
        AttachmentCaptureCoordinator coordinator = new(
            store,
            new FakeMailboxContentSource(MailboxAttachmentContentResult.Available("hello"u8.ToArray(), "application/pdf", "hashref_abc")),
            new FakeFolderStore(), reviewWriter: review);

        AttachmentCaptureCoordinatorResult result = await coordinator.CaptureAsync(Request(10), TestContext.Current.CancellationToken);

        result.StoredCount.ShouldBe(2);
        review.Transitions.Count(t => t.State == "recovered").ShouldBe(2);
        ProjectConversationItemView[] attachments = (await store.ReadPageAsync(Tenant, "project-001", null, 25, TestContext.Current.CancellationToken))
            .Items
            .Where(static item => item.Kind == ProjectConversationItemKind.Attachment)
            .OrderBy(static item => item.AttachmentDisplayName, StringComparer.Ordinal)
            .ToArray();
        attachments.Select(static item => item.AttachmentFileId).ShouldBe(["file-graph-attachment-duplicate-0", "file-graph-attachment-duplicate-1"], ignoreOrder: false);
    }

    [Fact]
    public async Task StorageOutcomeShouldRejectStaleReplayWithoutChangingReferences()
    {
        InMemoryProjectConversationProjectionStore store = await StoreWithAssociationAndAttachments().ConfigureAwait(true);
        ProjectConversationAttachmentStorageCandidate candidate = (await store
            .GetAttachmentStorageCandidatesAsync(Tenant, IntakeId, TestContext.Current.CancellationToken))
            .ShouldHaveSingleItem();

        await store.UpsertAttachmentStorageOutcomeAsync(
            ProjectConversationAttachmentStorageOutcomeView.Stored(candidate, "folder-current", "file-current", "unique", "not-retryable", "pending-scan", [], 10, CorrelationId),
            TestContext.Current.CancellationToken).ConfigureAwait(true);
        await store.UpsertAttachmentStorageOutcomeAsync(
            ProjectConversationAttachmentStorageOutcomeView.Failed(candidate, ProjectConversationAttachmentStatus.Retryable, "not-evaluated", "retryable", "not-eligible", 9, CorrelationId),
            TestContext.Current.CancellationToken).ConfigureAwait(true);
        await store.UpsertAttachmentStorageOutcomeAsync(
            ProjectConversationAttachmentStorageOutcomeView.Failed(candidate, ProjectConversationAttachmentStatus.Retryable, "not-evaluated", "retryable", "not-eligible", 11, CorrelationId),
            TestContext.Current.CancellationToken).ConfigureAwait(true);

        ProjectConversationItemView attachment = await SingleAttachmentAsync(store).ConfigureAwait(true);
        attachment.AttachmentStorageStatus.ShouldBe(ProjectConversationAttachmentStatus.Captured);
        attachment.AttachmentFolderId.ShouldBe("folder-current");
        attachment.AttachmentFileId.ShouldBe("file-current");
    }

    [Fact]
    public async Task CaptureShouldNotStoreForCorrectingAssociationState()
    {
        InMemoryProjectConversationProjectionStore store = await StoreWithAssociationAndAttachmentsState(lifecycleState: LifecycleState.CorrectionDelayed).ConfigureAwait(true);
        FakeFolderStore folders = new();
        AttachmentCaptureCoordinator coordinator = new(
            store,
            new FakeMailboxContentSource(MailboxAttachmentContentResult.Available("hello"u8.ToArray(), "text/plain", "hashref_abc")),
            folders);

        AttachmentCaptureCoordinatorResult result = await coordinator.CaptureAsync(Request(10), TestContext.Current.CancellationToken);

        result.EvaluatedCount.ShouldBe(0);
        folders.Requests.ShouldBeEmpty();
    }

    [Fact]
    public async Task CaptureShouldNotStoreForSupersededAssociationState()
    {
        InMemoryProjectConversationProjectionStore store = await StoreWithAssociationAndAttachmentsState(
            supersededByAssociationId: "01ARZ3NDEKTSV4RRFFQ69G5FC9",
            isCorrectedContextStale: true).ConfigureAwait(true);
        FakeFolderStore folders = new();
        AttachmentCaptureCoordinator coordinator = new(
            store,
            new FakeMailboxContentSource(MailboxAttachmentContentResult.Available("hello"u8.ToArray(), "text/plain", "hashref_abc")),
            folders);

        AttachmentCaptureCoordinatorResult result = await coordinator.CaptureAsync(Request(10), TestContext.Current.CancellationToken);

        result.EvaluatedCount.ShouldBe(0);
        folders.Requests.ShouldBeEmpty();
    }

    [Fact]
    public async Task CaptureShouldUseTenantScopedCandidatesOnly()
    {
        InMemoryProjectConversationProjectionStore store = await StoreWithAssociationAndAttachments().ConfigureAwait(true);
        await AddAssociationAndAttachmentsAsync(store, OtherTenant, "other-project", LifecycleState.Associated).ConfigureAwait(true);
        FakeFolderStore folders = new();
        AttachmentCaptureCoordinator coordinator = new(
            store,
            new FakeMailboxContentSource(MailboxAttachmentContentResult.Available("hello"u8.ToArray(), "text/plain", "hashref_abc")),
            folders);

        AttachmentCaptureCoordinatorResult result = await coordinator.CaptureAsync(Request(10), TestContext.Current.CancellationToken);

        result.StoredCount.ShouldBe(1);
        folders.Requests.ShouldHaveSingleItem().TenantId.ShouldBe(Tenant);
        (await store.ReadPageAsync(OtherTenant, "other-project", null, 25, TestContext.Current.CancellationToken))
            .Items
            .Single(static item => item.Kind == ProjectConversationItemKind.Attachment)
            .AttachmentStorageStatus
            .ShouldBe(ProjectConversationAttachmentStatus.Pending);
    }

    [Fact]
    public async Task StorageOutcomeShouldHideFolderFileReferencesForRedactedAttachmentRows()
    {
        InMemoryProjectConversationProjectionStore store = await StoreWithAssociationAndAttachmentsState(redactionState: "redacted").ConfigureAwait(true);
        ProjectConversationAttachmentStorageCandidate candidate = (await store
            .GetAttachmentStorageCandidatesAsync(Tenant, IntakeId, TestContext.Current.CancellationToken))
            .ShouldHaveSingleItem();

        await store.UpsertAttachmentStorageOutcomeAsync(
            ProjectConversationAttachmentStorageOutcomeView.Stored(candidate, "folder-hidden", "file-hidden", "unique", "not-retryable", "pending-scan", [], 10, CorrelationId),
            TestContext.Current.CancellationToken).ConfigureAwait(true);

        ProjectConversationItemView attachment = await SingleAttachmentAsync(store).ConfigureAwait(true);
        attachment.AttachmentStorageStatus.ShouldBe(ProjectConversationAttachmentStatus.Captured);
        attachment.AttachmentFolderId.ShouldBeNull();
        attachment.AttachmentFileId.ShouldBeNull();
        attachment.AttachmentDisplayName.ShouldBeNull();
    }

    [Fact]
    public async Task CaptureShouldScanCleanAttachmentBeforeGrantingFileAndAiActions()
    {
        InMemoryProjectConversationProjectionStore store = await StoreWithAssociationAndAttachments().ConfigureAwait(true);
        FakeFolderStore folders = new();
        AttachmentCaptureCoordinator coordinator = new(
            store,
            new FakeMailboxContentSource(MailboxAttachmentContentResult.Available("hello"u8.ToArray(), "application/pdf", "hashref_abc")),
            folders,
            new DefaultAttachmentSafetyPolicy(new FixedAttachmentScanner(AttachmentScanResult.Clean())));

        AttachmentCaptureCoordinatorResult result = await coordinator.CaptureAsync(Request(10), TestContext.Current.CancellationToken);

        result.ShouldBe(new AttachmentCaptureCoordinatorResult(1, 1, 0));
        folders.Requests.Count.ShouldBe(1);
        ProjectConversationItemView attachment = await SingleAttachmentAsync(store).ConfigureAwait(true);
        attachment.AttachmentStorageStatus.ShouldBe(ProjectConversationAttachmentStatus.Captured);
        attachment.AttachmentScanStatus.ShouldBe(ProjectConversationAttachmentStatus.Captured);
        attachment.AttachmentAiContextEligibility.ShouldBe("eligible");
        attachment.AttachmentAllowedActions.ShouldBe(["add-to-ai-context", "open-governed-file"], ignoreOrder: true);
        attachment.SafeNextAction.ShouldBe("none");
    }

    [Fact]
    public async Task CaptureShouldNotStoreUnsafeAttachmentAndShouldProjectSafeMetadataOnlyState()
    {
        InMemoryProjectConversationProjectionStore store = await StoreWithAssociationAndAttachments().ConfigureAwait(true);
        FakeFolderStore folders = new();
        AttachmentCaptureCoordinator coordinator = new(
            store,
            new FakeMailboxContentSource(MailboxAttachmentContentResult.Available("unsafe"u8.ToArray(), "application/pdf", "hashref_unsafe")),
            folders,
            new DefaultAttachmentSafetyPolicy(new FixedAttachmentScanner(AttachmentScanResult.Unsafe("raw signature family must not leak"))));

        AttachmentCaptureCoordinatorResult result = await coordinator.CaptureAsync(Request(10), TestContext.Current.CancellationToken);

        result.ShouldBe(new AttachmentCaptureCoordinatorResult(1, 0, 1));
        folders.Requests.ShouldBeEmpty();
        ProjectConversationItemView attachment = await SingleAttachmentAsync(store).ConfigureAwait(true);
        attachment.AttachmentStorageStatus.ShouldBe(ProjectConversationAttachmentStatus.Pending);
        attachment.AttachmentScanStatus.ShouldBe(ProjectConversationAttachmentStatus.Unsafe);
        attachment.AttachmentFolderId.ShouldBeNull();
        attachment.AttachmentFileId.ShouldBeNull();
        attachment.AttachmentAiContextEligibility.ShouldBe("not-eligible");
        attachment.AttachmentAllowedActions.ShouldBeEmpty();
        attachment.SafeNextAction.ShouldBe("quarantine-review");
        JsonSerializer.Serialize(attachment).ShouldNotContain("signature", Case.Insensitive);
    }

    [Fact]
    public async Task CaptureShouldApplyResolvedUnsafeHandlingAndScanOnlyOnce()
    {
        InMemoryProjectConversationProjectionStore store = await StoreWithAssociationAndAttachments().ConfigureAwait(true);
        CountingAttachmentScanner scanner = new(AttachmentScanResult.Unsafe("raw scanner family"));
        AttachmentCaptureCoordinator coordinator = new(
            store,
            new FakeMailboxContentSource(MailboxAttachmentContentResult.Available("unsafe"u8.ToArray(), "application/pdf", "hashref_unsafe")),
            new FakeFolderStore(),
            new DefaultAttachmentSafetyPolicy(scanner),
            unsafeHandlingResolver: new FixedUnsafeHandlingResolver(AttachmentUnsafeHandling.RejectMessage));

        AttachmentCaptureCoordinatorResult result = await coordinator.CaptureAsync(Request(10), TestContext.Current.CancellationToken);

        result.ShouldBe(new AttachmentCaptureCoordinatorResult(1, 0, 1));
        scanner.Calls.ShouldBe(1);
        ProjectConversationItemView attachment = await SingleAttachmentAsync(store).ConfigureAwait(true);
        attachment.AttachmentScanStatus.ShouldBe(ProjectConversationAttachmentStatus.Rejected);
        attachment.SafeNextAction.ShouldBe("review-source-evidence");
        attachment.AttachmentAllowedActions.ShouldBeEmpty();
    }

    [Fact]
    public async Task CaptureShouldRedactUnauthorizedAttachmentWithoutTouchingContentOrFolders()
    {
        InMemoryProjectConversationProjectionStore store = await StoreWithAssociationAndAttachmentsState(redactionState: "redacted").ConfigureAwait(true);
        CountingMailboxContentSource content = new(MailboxAttachmentContentResult.Available("secret"u8.ToArray(), "application/pdf", "hashref_secret"));
        FakeFolderStore folders = new();
        AttachmentCaptureCoordinator coordinator = new(
            store,
            content,
            folders,
            new DefaultAttachmentSafetyPolicy(new FixedAttachmentScanner(AttachmentScanResult.Clean())),
            new ProjectionAttachmentAuthorizationService());

        AttachmentCaptureCoordinatorResult result = await coordinator.CaptureAsync(Request(10), TestContext.Current.CancellationToken);

        result.ShouldBe(new AttachmentCaptureCoordinatorResult(1, 0, 1));
        content.Calls.ShouldBe(0);
        folders.Requests.ShouldBeEmpty();
        ProjectConversationItemView attachment = await SingleAttachmentAsync(store).ConfigureAwait(true);
        attachment.AttachmentDisplayName.ShouldBeNull();
        attachment.AttachmentFolderId.ShouldBeNull();
        attachment.AttachmentFileId.ShouldBeNull();
        attachment.AttachmentScanStatus.ShouldBe(ProjectConversationAttachmentStatus.Unavailable);
        attachment.AttachmentAiContextEligibility.ShouldBe("redacted");
        attachment.AttachmentAllowedActions.ShouldBeEmpty();
    }

    [Fact]
    public async Task DuplicateProviderIdsHaveDistinctFailureSubjectsAndOnlyTheRetriedOneExhausts()
    {
        var token = TestContext.Current.CancellationToken;
        var store = await StoreWithAssociationAndAttachments(new("duplicate", "a", "application/pdf", 1), new("duplicate", "b", "application/pdf", 2));
        var review = new RecordingReviewWriter();
        var coordinator = new AttachmentCaptureCoordinator(store, new FakeMailboxContentSource(MailboxAttachmentContentResult.Available("data"u8.ToArray(), "application/pdf")),
            new UnavailableFolderStore(), reviewWriter: review);
        await coordinator.CaptureAsync(Request(10), token);
        review.Transitions.Select(x => x.SubjectId).Distinct().Count().ShouldBe(2);
        var target = review.Transitions[0];
        await coordinator.CaptureAsync(Request(10) with { ProjectId = target.ProjectId, AssociationId = AssociationId, SubjectId = target.SubjectId, Attempt = 4 }, token);
        review.Transitions.Where(x => x.SubjectId == target.SubjectId).Select(x => x.State).ShouldBe(["failed", "retrying", "exhausted"]);
    }

    [Fact]
    public async Task AcceptedUploadRemainsReconcilableAndDoesNotResubmit()
    {
        var token = TestContext.Current.CancellationToken;
        var store = await StoreWithAssociationAndAttachments();
        var folder = new FakeFolderStore(confirmed: false);
        var review = new RecordingReviewWriter();
        var coordinator = new AttachmentCaptureCoordinator(store, new FakeMailboxContentSource(MailboxAttachmentContentResult.Available("content"u8.ToArray(), "text/plain")),
            folder, reviewWriter: review);
        (await coordinator.CaptureAsync(Request(10), token)).StoredCount.ShouldBe(0);
        var failed = review.Transitions.ShouldHaveSingleItem();
        failed.Reason.ShouldBe("attachment_confirmation_pending");
        await store.UpsertAttachmentSafetyOutcomeAsync(new(Tenant, failed.ProjectId, AssociationId, IntakeId, "graph-attachment-001", 0,
            ProjectConversationAttachmentStatus.Captured, "pending", [], "not-retryable", "none", "scan_clean", 10, CorrelationId, "quarantine"), token);
        var scoped = Request(10) with { ProjectId = failed.ProjectId, AssociationId = AssociationId, SubjectId = failed.SubjectId, Attempt = 1 };
        (await coordinator.CaptureAsync(scoped with { SubjectId = "missing" }, token)).EvaluatedCount.ShouldBe(0);
        (await coordinator.CaptureAsync(scoped with { SourceVersion = 9 }, token)).EvaluatedCount.ShouldBe(0);
        (await coordinator.CaptureAsync(scoped, token)).ConfirmedSubjectId.ShouldBeNull();
        folder.Requests.Count.ShouldBe(1);
        review.Transitions.ShouldNotContain(t => t.State == "recovered");
    }

    [Fact]
    public async Task PermanentFolderFailureRetainsOwnerClassification()
    {
        var review = new RecordingReviewWriter();
        var coordinator = new AttachmentCaptureCoordinator(await StoreWithAssociationAndAttachments(),
            new FakeMailboxContentSource(MailboxAttachmentContentResult.Unavailable("source_unavailable")), new ContentAwareFolderStore(), reviewWriter: review);
        await coordinator.CaptureAsync(Request(10), TestContext.Current.CancellationToken);
        review.Transitions.ShouldHaveSingleItem().State.ShouldBe("nonretryable");
    }

    [Fact]
    public async Task AcceptedTransitionReceiptWithoutCommitCannotMarkCaptured()
    {
        var token = TestContext.Current.CancellationToken;
        var store = await StoreWithAssociationAndAttachments();
        var gateway = new UncommittedGateway();
        using var services = new ServiceCollection().AddSingleton<IAuthoritativeEventStreamReader>(new ReviewQueueTestStreamReader(_ => null)).BuildServiceProvider();
        var writer = new WorkflowReviewCommandWriter(gateway, new DataProtectionChatBotAdmissionMarker(new EphemeralDataProtectionProvider()),
            services.GetRequiredService<IServiceScopeFactory>());
        var coordinator = new AttachmentCaptureCoordinator(store, new FakeMailboxContentSource(MailboxAttachmentContentResult.Available("data"u8.ToArray(), "text/plain")),
            new FakeFolderStore(), reviewWriter: writer);
        (await Should.ThrowAsync<InvalidOperationException>(() => coordinator.CaptureAsync(Request(10), token))).Message.ShouldBe("workflow_review_commit_unconfirmed");
        gateway.Submitted.ShouldBe(1);
        (await SingleAttachmentAsync(store)).AttachmentStorageStatus.ShouldBe(ProjectConversationAttachmentStatus.Pending);
    }

    private sealed class UncommittedGateway : IEventStoreGatewayClient
    {
        public int Submitted { get; private set; }
        public Task<SubmitCommandResponse> SubmitCommandAsync(SubmitCommandRequest request, CancellationToken cancellationToken = default)
        {
            Submitted++;
            return Task.FromResult(new SubmitCommandResponse(request.CorrelationId!, MessageId: request.MessageId));
        }
        public Task<EventStoreQueryResult> SubmitQueryAsync(SubmitQueryRequest request, string? ifNoneMatch = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<EventStoreQueryResult<T>> SubmitQueryAsync<T>(SubmitQueryRequest request, string? ifNoneMatch = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<StreamReadPage> ReadStreamAsync(StreamReadRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class RecordingReviewWriter : Server.Lifecycle.Workflows.IWorkflowReviewCommandWriter
    {
        public List<Server.Operations.RecordWorkflowReviewTransition> Transitions { get; } = [];
        public Task RecordAsync(string tenant, string correlation, Server.Operations.RecordWorkflowReviewTransition command, CancellationToken token)
        {
            Transitions.Add(command);
            return Task.CompletedTask;
        }
    }

    private static AttachmentCaptureCoordinatorRequest Request(long sourceVersion)
        => new(Tenant, IntakeId, sourceVersion, CorrelationId);

    private static async Task<InMemoryProjectConversationProjectionStore> StoreWithAssociationAndAttachments(
        params MailboxAttachmentReference[] attachments)
    {
        InMemoryProjectConversationProjectionStore store = new();
        await AddAssociationAndAttachmentsAsync(
            store,
            Tenant,
            "project-001",
            LifecycleState.Associated,
            "metadata_only",
            attachments.Length == 0
                ? [new MailboxAttachmentReference("graph-attachment-001", "invoice.pdf", "application/pdf", 4096)]
                : attachments).ConfigureAwait(true);
        return store;
    }

    private static async Task<InMemoryProjectConversationProjectionStore> StoreWithAssociationAndAttachmentsState(
        LifecycleState lifecycleState = LifecycleState.Associated,
        string redactionState = "metadata_only",
        string? supersededByAssociationId = null,
        bool isCorrectedContextStale = false)
    {
        InMemoryProjectConversationProjectionStore store = new();
        await AddAssociationAndAttachmentsAsync(
            store,
            Tenant,
            "project-001",
            lifecycleState,
            redactionState,
            supersededByAssociationId: supersededByAssociationId,
            isCorrectedContextStale: isCorrectedContextStale).ConfigureAwait(true);
        return store;
    }

    private static async Task AddAssociationAndAttachmentsAsync(
        InMemoryProjectConversationProjectionStore store,
        string tenantId,
        string projectId,
        LifecycleState lifecycleState,
        string redactionState = "metadata_only",
        IReadOnlyList<MailboxAttachmentReference>? attachments = null,
        string? supersededByAssociationId = null,
        bool isCorrectedContextStale = false)
    {
        await store.UpsertAttachmentReferencesAsync(
            ProjectConversationAttachmentSetView.FromIntake(
                tenantId,
                new MailboxMessageIntakeCaptured(
                    IntakeId,
                    "graph-message-001",
                    "<internet-message-001@example.test>",
                    "conversation-001",
                    "thread-001",
                    "controlled-mailbox-001",
                    new MailboxParticipantIdentity("sender-safe-label", "redacted"),
                    [],
                    DetectedAt,
                    DetectedAt.AddMinutes(-2),
                    DetectedAt.AddMinutes(-3),
                    attachments ?? [new MailboxAttachmentReference("graph-attachment-001", "invoice.pdf", "application/pdf", 4096)],
                    "UTC",
                    "opaque-source-context",
                    "m365-mailbox-intake",
                    "association-deterministic.kernel.m0.v1",
                    redactionState,
                    "collaboration_input",
                    1),
                8,
                CorrelationId),
            TestContext.Current.CancellationToken).ConfigureAwait(true);

        await store.UpsertAsync(
            new ProjectConversationItemView(
                tenantId,
                projectId,
                "Project One",
                AssociationId,
                IntakeId,
                ProjectConversationItemKind.EmailDerived,
                ProjectConversationActorKind.Mailbox,
                "Mailbox event",
                DetectedAt,
                lifecycleState,
                AssociationThresholdBand.Auto,
                0.9,
                AssociationId,
                "controlled-mailbox-001",
                "graph-message-001",
                "<internet-message-001@example.test>",
                "conversation-001",
                "thread-001",
                DetectedAt,
                DetectedAt.AddMinutes(-2),
                DetectedAt.AddMinutes(-3),
                "UTC",
                "Microsoft 365 mailbox",
                "m365-mailbox-intake",
                redactionState,
                "collaboration_input",
                ProjectConversationItemView.CurrentSchemaVersion,
                9,
                CorrelationId,
                SupersededByAssociationId: supersededByAssociationId,
                IsCorrectedContextStale: isCorrectedContextStale),
            TestContext.Current.CancellationToken).ConfigureAwait(true);
    }

    private static async Task<ProjectConversationItemView> SingleAttachmentAsync(InMemoryProjectConversationProjectionStore store)
        => (await store.ReadPageAsync(Tenant, "project-001", null, 25, TestContext.Current.CancellationToken).ConfigureAwait(true))
            .Items
            .Single(static item => item.Kind == ProjectConversationItemKind.Attachment);

    private sealed class FakeMailboxContentSource(MailboxAttachmentContentResult result) : IMailboxAttachmentContentSource
    {
        public ValueTask<MailboxAttachmentContentResult> FetchAttachmentContentAsync(
            MailboxAttachmentContentRequest request,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.FromResult(result);
        }
    }

    private sealed class CountingMailboxContentSource(MailboxAttachmentContentResult result) : IMailboxAttachmentContentSource
    {
        public int Calls { get; private set; }

        public ValueTask<MailboxAttachmentContentResult> FetchAttachmentContentAsync(
            MailboxAttachmentContentRequest request,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Calls++;
            return ValueTask.FromResult(result);
        }
    }

    private sealed class FakeFolderStore(bool confirmed = true) : IFolderStore
    {
        public List<StoreMailboxAttachmentRequest> Requests { get; } = [];

        public ValueTask<MailboxAttachmentStorageResult> StoreMailboxAttachmentAsync(
            StoreMailboxAttachmentRequest request,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Requests.Add(request);
            return ValueTask.FromResult(MailboxAttachmentStorageResult.Succeeded(new StoredMailboxAttachmentReference(
                $"folder-{request.ProjectId}",
                $"file-{request.ProviderAttachmentId}-{request.Ordinal}",
                "unique",
                "not-retryable",
                "pending-scan",
                [],
                $"operation-{request.ProviderAttachmentId}-{request.Ordinal}",
                $"idempotency-{request.ProviderAttachmentId}-{request.Ordinal}", ContentConfirmed: confirmed)));
        }
    }

    private sealed class ContentAwareFolderStore : IFolderStore
    {
        public ValueTask<MailboxAttachmentStorageResult> StoreMailboxAttachmentAsync(
            StoreMailboxAttachmentRequest request,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.FromResult(MailboxAttachmentStorageResult.Failed(new AttachmentStorageFailure(
                ProjectConversationAttachmentStatus.Unavailable,
                "not-evaluated",
                "not-retryable",
                "not-eligible",
                request.Content.ReasonCode)));
        }
    }

    private sealed class FixedAttachmentScanner(AttachmentScanResult result) : IAttachmentScanner
    {
        public ValueTask<AttachmentScanResult> ScanAsync(AttachmentScanRequest request, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.FromResult(result);
        }
    }

    private sealed class CountingAttachmentScanner(AttachmentScanResult result) : IAttachmentScanner
    {
        public int Calls { get; private set; }

        public ValueTask<AttachmentScanResult> ScanAsync(AttachmentScanRequest request, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Calls++;
            return ValueTask.FromResult(result);
        }
    }

    private sealed class FixedUnsafeHandlingResolver(string unsafeHandling) : IAttachmentUnsafeHandlingResolver
    {
        public ValueTask<string> ResolveUnsafeHandlingAsync(
            AttachmentUnsafeHandlingResolutionRequest request,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.FromResult(unsafeHandling);
        }
    }
}
