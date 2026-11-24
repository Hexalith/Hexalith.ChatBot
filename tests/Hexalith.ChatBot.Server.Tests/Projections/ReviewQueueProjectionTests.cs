using System.Text.Json;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Server.Adapters.Parties;
using Hexalith.ChatBot.Server.Association;
using Hexalith.ChatBot.Server.Association.Participants;
using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Operations;
using Hexalith.ChatBot.Server.Projections;
using Hexalith.ChatBot.Server.Tests.Gateway.Status;
using Hexalith.EventStore.Contracts.Projections;
using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Projections;

public sealed class ReviewQueueProjectionTests
{
    private static readonly DateTimeOffset At = new(2026, 11, 22, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task SdkDeliveryBindsTrustedMetadataAndRetainsAllFamiliesAcrossRestart()
    {
        var token = TestContext.Current.CancellationToken;
        var bytes = new AtomicOperationStatusReadModelStore();
        var store = new ReviewQueueStore(bytes);
        var conversation = new InMemoryProjectConversationProjectionStore();
        var clock = new SystemClock();
        ChatBotDomainProjectionHandler Handler() => new(null!, null!, new(new InMemoryAssociationProjectionStore(), clock),
            new(new InMemoryParticipantResolutionProjectionStore(), clock, new UnavailableParticipantDisplayDirectory()),
            new(conversation), new(conversation), new(conversation), null!, new ReviewQueueStore(bytes));

        var handler = Handler();
        void Send(string stream, long version, object eventPayload)
        {
            var item = new ProjectionEventDto(eventPayload.GetType().FullName!, JsonSerializer.SerializeToUtf8Bytes(eventPayload),
                "json", version, At.AddMinutes(version), "correlation-001", $"event-{version}");
            handler.Project(new("tenant-alpha", "chatbot", stream, [item]));
        }

        var candidate = new MailboxAssociationCandidatesGenerated("association-payload", "intake", "tenant-foreign",
            "mailbox", "source-conversation", null, [], [], LifecycleState.NeedsReview, 0.55,
            AssociationThresholdBand.Ambiguous, AssociationScoringOutcome.CandidatesGenerated, [], "policy", "kernel",
            At, "metadata_only", "retained", 9999, "v1", "ignored-correlation");
        Send("association-owner", 1, candidate);
        var participant = new MailboxParticipantUnresolved("resolution", "intake", "participant-a", "evidence-secret",
            "fingerprint", ParticipantResolutionBlockedReason.NotFound, [], "mailbox", "source", "kernel", "metadata_only", "retained", 500, "v1");
        Send("resolution", 1, participant);
        Send("resolution", 2, participant with { SourceParticipantId = "participant-b" });

        foreach (var family in new[] { OperationalQueueFamily.FailedAttachment, OperationalQueueFamily.FailedIngestion })
        {
            var command = new RecordWorkflowReviewTransition(family, "same-subject", "project", 5, 0, "failed", "source_unavailable");
            Send(command.StreamId("tenant-alpha"), 1, new WorkflowReviewTransitionRecorded(command));
        }
        var request = ProjectConversationProjectionTests.ApprovalRequestPublished(8).Request! with { CorrelationId = "correlation-001" };
        var decision = ProjectConversationProjectionTests.ApprovalDecisionPublished(9).Decision! with { CorrelationId = "correlation-001" };
        Send("project", 2, decision);
        Send("project", 1, request);
        handler = Handler();
        Send("project", 1, request);
        foreach (var family in OperationalQueueFamilies.All)
        {
            var records = await store.ReadFamilyAsync("tenant-alpha", family, token);
            records.ShouldNotBeEmpty(family.ToString());
            (await store.ReadFamilyAsync("tenant-foreign", family, token)).ShouldBeEmpty();
            if (family is OperationalQueueFamily.RetryableOperation or OperationalQueueFamily.UnresolvedParticipant)
            { records.Count.ShouldBe(2); }
        }
        var approval = (await store.ReadFamilyAsync("tenant-alpha", OperationalQueueFamily.PendingApproval, token)).Single();
        approval.Current.Terminal.ShouldBeTrue();
        approval.History.Length.ShouldBe(2);
        approval.Current.At.ShouldBe(decision.DecidedAtUtc);
        var association = (await store.ReadFamilyAsync("tenant-alpha", OperationalQueueFamily.AmbiguousAssociation, token)).Single();
        association.Current.SubjectId.ShouldBe("association-owner");
        association.Current.Version.ShouldBe(1);
        association.Current.CorrelationId.ShouldBe("correlation-001");
        (await store.ReadFamilyAsync("tenant-alpha", OperationalQueueFamily.UnresolvedParticipant, token)).ShouldAllBe(x => x.Current.NextAction == "review");
        var unresolved = (await store.ReadFamilyAsync("tenant-alpha", OperationalQueueFamily.UnresolvedParticipant, token)).First();
        bytes.StoredJson(ReviewQueueStore.SubjectKey("tenant-alpha", unresolved.Current.ItemRef)).ShouldNotContain("evidence-secret");
    }

    [Fact]
    public async Task ACommittedRetryWaitsForTheExactFailureAndJoinsOneOperation()
    {
        var token = TestContext.Current.CancellationToken;
        var store = new ReviewQueueStore(new AtomicOperationStatusReadModelStore());
        var failure = new ReviewQueueTransition("tenant", "owner-stream", 3, "failure", "correlation", OperationalQueueFamily.RetryableOperation,
            "owner-stream", "project", ["project"], "failed", false, "source_unavailable", "operations-admin", "retry", At,
            ProducerFamily: OperationalQueueFamily.FailedIngestion, RetryTarget: new("association", "intake", "resolve"), Generation: 7);
        var retry = new ReviewQueueRetryRequest("retry", "failure-event", 3, "failed-ingestion", "retry-stream", 5, At.AddMinutes(1), "retry-correlation", "requester");
        (await store.RecordRetryAsync("tenant", retry, token)).ShouldBeEmpty();
        (await store.RecordRetryAsync("other", retry, token)).ShouldBeEmpty();
        await store.ApplyAsync(failure, token);
        (await store.LinkFailureAsync(failure, "wrong-event", token)).ShouldBeEmpty();
        var wrong = retry with { RetryId = "wrong-class", OperationClass = "failed-attachment", Version = 4 };
        (await store.RecordRetryAsync("tenant", wrong, token)).ShouldBeEmpty();
        var ready = (await store.LinkFailureAsync(failure, "failure-event", token)).ShouldHaveSingleItem();
        (await store.RecordRetryAsync("tenant", retry, token)).ShouldHaveSingleItem().InstanceId.ShouldBe(ready.InstanceId);
        var subject = (await store.ReadAsync("tenant", failure.ItemRef, token)).ShouldNotBeNull();
        subject.History.Length.ShouldBe(2);
        subject.Current.State.ShouldBe("retry-requested");
        subject.Current.Terminal.ShouldBeFalse();
        (await store.ReadAsync("other", failure.ItemRef, token)).ShouldBeNull();

        var recovered = failure with { Version = 4, OperationId = "owner-confirmed", State = "recovered", Terminal = true, OpensWork = false };
        await store.ApplyAsync(recovered, token);
        (await store.RecordRetryAsync("tenant", retry, token)).ShouldBeEmpty();
        subject = (await store.ReadAsync("tenant", failure.ItemRef, token)).ShouldNotBeNull();
        (await store.RecordRetryAsync("tenant", retry with { RetryId = "late", Version = 6 }, token)).ShouldBeEmpty();
        subject.Current.OperationId.ShouldBe("owner-confirmed");
        subject.History.Length.ShouldBe(3);
    }

    [Fact]
    public async Task RedeliveryRepairsInterruptedFamilyIndexAndPreservesOriginalFact()
    {
        var token = TestContext.Current.CancellationToken;
        var bytes = new InterruptedIndexStore();
        var queues = new ReviewQueueStore(bytes);
        var fact = new ReviewQueueTransition("tenant", "stream", 1, "event", "correlation", OperationalQueueFamily.PendingApproval,
            "approval", "project", ["project"], "waiting", false, null, "reviewer", "review", At);
        await Should.ThrowAsync<IOException>(() => queues.ApplyAsync(fact, token));
        (await queues.ReadAsync("tenant", fact.ItemRef, token)).ShouldNotBeNull();
        (await queues.ReadFamilyAsync("tenant", fact.Family, token)).ShouldBeEmpty();
        await new ReviewQueueStore(bytes).ApplyAsync(fact with { At = At.AddHours(1) }, token);
        var repaired = (await queues.ReadFamilyAsync("tenant", fact.Family, token)).ShouldHaveSingleItem();
        repaired.History.ShouldHaveSingleItem().At.ShouldBe(At);
    }

    [Fact]
    public void ReopenUsesItsOwnIntervalAndDifferentProducersHaveDifferentRevisions()
    {
        var first = new ReviewQueueTransition("tenant", "owner", 1, "first", "c", OperationalQueueFamily.FailedIngestion, "subject", "project",
            ["project"], "failed", false, "source", "ops", "retry", At);
        var closed = first with { Version = 2, State = "recovered", Terminal = true, At = At.AddDays(1) };
        var reopened = first with { Version = 3, At = At.AddDays(2) };
        var subject = ReviewQueueSubject.Add(ReviewQueueSubject.Add(ReviewQueueSubject.Add(null, reopened), closed), first);
        subject.ToItem(At.AddDays(2).AddSeconds(10)).AgeSeconds.ShouldBe(10);
        var foreign = first with { StreamId = "foreign", At = At.AddMinutes(15) };
        var combined = ReviewQueueSubject.Add(subject, foreign);
        combined.Revision.ShouldNotBe(subject.Revision);
        combined.History.Select(f => f.StreamId).ShouldBe(["owner", "foreign", "owner", "owner"]);
    }

    [Fact]
    public async Task UpgradeIncludesRetainedApprovalsWithoutANewEventAndRespectsNamedTerminal()
    {
        var token = TestContext.Current.CancellationToken;
        var conversation = new InMemoryProjectConversationProjectionStore();
        var request = ApprovalProjectionTranslator.TryCreateView(ProjectConversationProjectionTests.ApprovalRequestPublished(8))!;
        await conversation.UpsertApprovalEventAsync(request, token);
        var queues = new ReviewQueueStore(new AtomicOperationStatusReadModelStore(), clock: new SystemClock(), conversations: conversation);
        var retained = (await queues.ReadFamilyAsync(request.TenantId, OperationalQueueFamily.PendingApproval, token)).ShouldHaveSingleItem();
        var detail = (await queues.ReadAsync(request.TenantId, retained.Current.ItemRef, token)).ShouldNotBeNull().ToItem(At);
        var priority = retained.ToItem(At);
        detail.GroupKey.ShouldBe(priority.GroupKey);
        detail.PriorityExplanation.ShouldBe(priority.PriorityExplanation);
        priority.GroupProjectRef.ShouldBe("project:" + request.ProjectId);
        priority.GroupKey.ShouldNotBeNull().ShouldStartWith("sha256:");
        priority.GroupRequesterRef.ShouldBe("requester:" + request.RequesterId);
        priority.PriorityScore.ShouldBeGreaterThan(0);
        var worker = new Server.Operations.PeriodicEnforcement.ProjectionBackedPeriodicEnforcementInputSource(conversation, new SystemClock(), queues);
        (await worker.GetTenantInputsAsync(request.TenantId, token)).QueueItems.ShouldHaveSingleItem().GroupProjectRef.ShouldBe("project:" + request.ProjectId);
        await queues.ApplyAsync(retained.Current with { StreamId = "named", Version = 9, RetainedApproval = false, Terminal = true, State = "approved" }, token);
        (await worker.GetTenantInputsAsync(request.TenantId, token)).QueueItems.ShouldBeEmpty();
    }

    private sealed class InterruptedIndexStore : Hexalith.EventStore.Client.Projections.IReadModelStore
    {
        private readonly AtomicOperationStatusReadModelStore _bytes = new();
        private bool _interrupt = true;

        public Task<Hexalith.EventStore.Client.Projections.ReadModelEntry<T>> GetAsync<T>(string storeName, string key, CancellationToken cancellationToken = default)
            where T : class => _bytes.GetAsync<T>(storeName, key, cancellationToken);
        public Task SaveAsync<T>(string storeName, string key, T value, CancellationToken cancellationToken = default)
            where T : class => _bytes.SaveAsync(storeName, key, value, cancellationToken);
        public Task<bool> TrySaveAsync<T>(string storeName, string key, T value, string etag, CancellationToken cancellationToken = default) where T : class
        {
            if (_interrupt && key.Contains(":review-queue:index:", StringComparison.Ordinal))
            {
                _interrupt = false;
                throw new IOException("interrupted family index");
            }
            return _bytes.TrySaveAsync(storeName, key, value, etag, cancellationToken);
        }
    }

    [Fact]
    public async Task TwoConsumersPreserveBothFactsWithoutReopeningOrRewritingTheDecision()
    {
        var token = TestContext.Current.CancellationToken;
        var bytes = new AtomicOperationStatusReadModelStore { RaceNextPair = true };
        var fact = new ReviewQueueTransition("tenant-a", "stream", 5, "request", "correlation", OperationalQueueFamily.PendingApproval,
            "approval", "project", ["project"], "pending", false, null, "reviewer", "review", At);
        var closed = fact with { Version = 6, OperationId = "decision", State = "rejected", Terminal = true, At = At.AddMinutes(1) };
        await Task.WhenAll(new ReviewQueueStore(bytes).ApplyAsync(fact, token), new ReviewQueueStore(bytes).ApplyAsync(closed, token));
        var restarted = new ReviewQueueStore(bytes);
        await restarted.ApplyAsync(closed with { At = At.AddDays(1) }, token);
        var item = (await restarted.ReadAsync("tenant-a", fact.ItemRef, token)).ShouldNotBeNull();
        item.Current.At.ShouldBe(closed.At);
        item.Current.OperationId.ShouldBe("decision");
        item.History.Length.ShouldBe(2);
        bytes.ConditionalConflicts.ShouldBeGreaterThan(0);
        await restarted.ApplyAsync(fact with { IdentityProject = "second", ProjectIds = ["second"] }, token);
        (await restarted.ReadFamilyAsync("tenant-a", fact.Family, token)).Count.ShouldBe(2);
    }
}
