using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Server.Authorization;
using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Contracts.Projections;
using Hexalith.EventStore.Contracts.Streams;
using Hexalith.EventStore.Contracts.Identity;
using Hexalith.EventStore.Client.Streams;
using System.Security.Cryptography;

namespace Hexalith.ChatBot.Server.Projections;

/// <summary>Tenant-partitioned subject histories and indexes written through the EventStore CAS policy.</summary>
internal sealed class ReviewQueueStore(IReadModelStore store, Observability.IChatBotMetrics? metrics = null, Audit.ISystemClock? clock = null, IProjectConversationProjectionStore? conversations = null)
{
    private const string StoreName = ChatBotReadModelStoreNames.StateStoreName;

    public async Task ApplyAsync(ReviewQueueTransition fact, CancellationToken token)
    {
        _ = await ReadModelWritePolicy.UpdateAsync<ReviewQueueSubject>(store, StoreName,
            SubjectKey(fact.TenantId, fact.ItemRef), current => ReviewQueueSubject.Add(current, fact),
            new ReadModelWriteContext(Category: nameof(ReviewQueueSubject)), cancellationToken: token).ConfigureAwait(false);
        // Index writes also run on redelivery: a crash after any preceding write is repairable.
        await AddToIndexAsync(IndexKey(fact.TenantId, fact.Family), fact.ItemRef, token).ConfigureAwait(false);
        await AddToIndexAsync("chatbot-review-queue-tenants", fact.TenantId, token).ConfigureAwait(false);
    }

    internal Task<List<Lifecycle.Workflows.WorkflowReviewRetry>> RecordRetryAsync(string tenant, ReviewQueueRetryRequest retry, CancellationToken token)
        => UpdateRetryLinkAsync(tenant, retry.EventId, retry.FailedVersion, link => link.Requests.Any(r => r.RetryId == retry.RetryId)
            ? link : link with { Requests = [.. link.Requests, retry] }, token);

    internal Task<List<Lifecycle.Workflows.WorkflowReviewRetry>> LinkFailureAsync(ReviewQueueTransition fact, string eventId, CancellationToken token)
        => UpdateRetryLinkAsync(fact.TenantId, eventId, fact.Version, link => link with { Failure = link.Failure ?? fact }, token);

    private async Task<List<Lifecycle.Workflows.WorkflowReviewRetry>> UpdateRetryLinkAsync(string tenant, string eventId, long version, Func<ReviewQueueRetryLink, ReviewQueueRetryLink> update,
        CancellationToken token)
    {
        string key = $"{tenant}:review-queue:retry:{ProjectConversationStableId.Hash(eventId)}:{version}";
        _ = await ReadModelWritePolicy.UpdateAsync<ReviewQueueRetryLink>(store, StoreName, key,
            current => update(current ?? new(null, [])), new ReadModelWriteContext(Category: nameof(ReviewQueueRetryLink)),
            cancellationToken: token).ConfigureAwait(false);
        var link = (await store.GetAsync<ReviewQueueRetryLink>(StoreName, key, token).ConfigureAwait(false)).Value;
        List<Lifecycle.Workflows.WorkflowReviewRetry> ready = [];
        if (link?.Failure is not { } failure) { return ready; }
        foreach (var retry in link.Requests)
        {
            var subject = await ReadAsync(tenant, failure.ItemRef, token).ConfigureAwait(false);
            var ownerHead = subject?.History.Where(f => f.StreamId == failure.StreamId).MaxBy(f => f.Version);
            if (ownerHead?.Version != failure.Version || failure.State != "failed" || failure.Terminal ||
                !Lifecycle.Workflows.WorkflowReviewRetry.CanRejoin(failure, retry)) { continue; }
            var accepted = failure with { StreamId = retry.Stream, Version = retry.Version, OperationId = retry.RetryId,
                SourceMessageId = null, CorrelationId = retry.Correlation, At = retry.At, Terminal = false, State = "retry-requested",
                NextAction = "await-owner", Actor = retry.Actor, RetryTarget = null, OpensWork = false, FailedStream = failure.StreamId, FailedVersion = failure.Version };
            bool stillOutstanding = false;
            _ = await ReadModelWritePolicy.UpdateAsync<ReviewQueueSubject>(store, StoreName, SubjectKey(tenant, failure.ItemRef), current =>
            {
                if (current is null) { throw new InvalidDataException("review_queue_subject_missing"); }
                stillOutstanding = current.History.Where(f => f.StreamId == failure.StreamId).MaxBy(f => f.Version)?.Version == failure.Version;
                return stillOutstanding ? ReviewQueueSubject.Add(current, accepted) : current;
            }, cancellationToken: token).ConfigureAwait(false);
            if (stillOutstanding) { ready.Add(new(failure, retry)); }
        }
        return ready;
    }

    private static string FactDigest(ReviewQueueTransition fact)
        => Convert.ToHexString(SHA256.HashData(System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(fact)));

    private static string CheckpointKey(string tenant, string streamId, long version)
        => $"{tenant}:review-queue:stream:{ProjectConversationStableId.Hash(streamId)}:{version}";

    public async Task RecordCoverageAsync(ProjectionRequest request, ProjectionEventDto envelope, CancellationToken token, IEnumerable<ReviewQueueTransition>? facts = null)
    {
        var entry = new ReviewQueueCheckpoint(envelope.SequenceNumber, envelope.MessageId, Convert.ToHexString(SHA256.HashData(envelope.Payload)),
            (facts ?? []).ToDictionary(f => f.ItemRef, f => f.Family, StringComparer.Ordinal),
            envelope.EventTypeName.Split(',', 2)[0], envelope.Timestamp, envelope.CorrelationId,
            (facts ?? []).ToDictionary(f => f.ItemRef, FactDigest, StringComparer.Ordinal));
        bool inserted = false;
        _ = await ReadModelWritePolicy.UpdateAsync<ReviewQueueCheckpoint>(store, StoreName,
            CheckpointKey(request.TenantId, request.AggregateId, envelope.SequenceNumber),
            current =>
            {
                inserted = current is null;
                return current is null || current.Digest == entry.Digest && current.MessageId == entry.MessageId ? entry : current;
            },
            new ReadModelWriteContext(Category: nameof(ReviewQueueCheckpoint)),
            cancellationToken: token).ConfigureAwait(false);
        if (inserted && envelope.Timestamp != default)
        {
            metrics?.RecordReviewProjectionDelay(request.TenantId, ((clock?.UtcNow ?? DateTimeOffset.UtcNow) - envelope.Timestamp).TotalSeconds);
        }
    }

    internal async Task<Contracts.Queries.ReviewQueueCoverage> CheckSourceAsync(string tenant, string streamId,
        string publicScope, IAuthoritativeEventStreamReader source, CancellationToken token)
    {
        AggregateIdentity identity;
        try { identity = new AggregateIdentity(tenant, "chatbot", streamId); }
        catch (ArgumentException) { return new(publicScope, "source-unavailable", null, "operations-admin", "verify-source"); }
        var result = await source.ReadAsync(identity, token).ConfigureAwait(false);
        var now = clock?.UtcNow ?? DateTimeOffset.UtcNow;
        var stream = result.Stream;
        if (!result.IsAuthoritative || stream is null || stream.Identity.TenantId != tenant || stream.Identity.AggregateId != streamId ||
            stream.Identity.Domain != "chatbot" || stream.ObservedAt > now || now - stream.ObservedAt >= TimeSpan.FromMinutes(1) || string.IsNullOrWhiteSpace(stream.ObservationId) || stream.Events.Count != stream.Head ||
            stream.Head == 0 && (stream.NativeProof is not { HeadSequence: 0, Erased: false } proof ||
                proof.TenantId != tenant || proof.Domain != "chatbot" || proof.AggregateId != streamId || string.IsNullOrWhiteSpace(proof.SourceIncarnation)))
        { return new(publicScope, "source-unavailable", null, "operations-admin", "verify-source"); }
        Dictionary<long, ReviewQueueCheckpoint> checkpoints = [];
        long expectedVersion = 0;
        foreach (var item in stream.Events)
        {
            var entry = (await store.GetAsync<ReviewQueueCheckpoint>(StoreName, CheckpointKey(tenant, streamId, item.SequenceNumber), token).ConfigureAwait(false)).Value;
            if (item.SequenceNumber != ++expectedVersion || entry?.Version != item.SequenceNumber ||
                entry.EventType != item.EventTypeName.Split(',', 2)[0] || entry.Timestamp != item.Timestamp ||
                entry.Correlation != item.CorrelationId || entry.MessageId != item.MessageId ||
                entry.Digest != Convert.ToHexString(SHA256.HashData(item.Payload)))
            { return new(publicScope, "projection-behind", stream.Head, "operations-admin", "replay-source"); }
            checkpoints[item.SequenceNumber] = entry;
            foreach (var (reference, family) in entry.Subjects)
            {
                var rows = await ReadIndexAsync(IndexKey(tenant, family), token).ConfigureAwait(false);
                var subject = await ReadAsync(tenant, reference, token).ConfigureAwait(false);
                if (!rows.Contains(reference, StringComparer.Ordinal) || subject is null ||
                    !subject.History.Any(fact => fact.TenantId == tenant && fact.StreamId == streamId && fact.Version == item.SequenceNumber))
                { return new(publicScope, "projection-incomplete", stream.Head, "operations-admin", "replay-source"); }
            }
        }
        foreach (var family in OperationalQueueFamilies.All)
        {
            foreach (var subject in await ReadFamilyAsync(tenant, family, token).ConfigureAwait(false))
            {
                foreach (var fact in subject.History.Where(f => f.StreamId == streamId))
                {
                    if (!checkpoints.TryGetValue(fact.Version, out var checkpoint) ||
                        !(checkpoint.Subjects.TryGetValue(fact.ItemRef, out var expected) && expected == fact.Family &&
                            checkpoint.FactDigests?.GetValueOrDefault(fact.ItemRef) == FactDigest(fact) ||
                        MatchesRetry(subject, fact, stream.Events[checked((int)fact.Version - 1)])))
                    { return new(publicScope, "projection-incomplete", stream.Head, "operations-admin", "replay-source"); }
                }
            }
        }
        return new(publicScope, "current", stream.Head, "operations-admin", "none");
    }

    private static bool MatchesRetry(ReviewQueueSubject subject, ReviewQueueTransition fact, StreamReadEvent source)
    {
        if (fact.State != "retry-requested" || fact.Family != OperationalQueueFamily.RetryableOperation ||
            source.EventTypeName.Split(',', 2)[0] != typeof(Operations.WorkflowRetryRequested).FullName ||
            fact.At != source.Timestamp || fact.CorrelationId != source.CorrelationId) { return false; }
        var retry = System.Text.Json.JsonSerializer.Deserialize<Operations.WorkflowRetryRequested>(source.Payload, System.Text.Json.JsonSerializerOptions.Web);
        return retry is not null && retry.RetryId == fact.OperationId && retry.ExpectedFailedSourceVersion == fact.FailedVersion &&
            subject.History.Any(f => f.StreamId == fact.FailedStream && f.Version == fact.FailedVersion && f.SourceMessageId == retry.FailedEventId);
    }

    public static string SubjectKey(string tenant, string itemRef) => $"{tenant}:review-queue:subject:{itemRef}";

    private static string IndexKey(string tenant, OperationalQueueFamily family)
        => $"{tenant}:review-queue:index:{OperationalQueueFamilies.ToWireValue(family)}";

    public async Task<ReviewQueueSubject?> ReadAsync(string tenant, string itemRef, CancellationToken token)
    {
        var current = await ReadNamedAsync(tenant, itemRef, token).ConfigureAwait(false);
        if (conversations is null || !itemRef.StartsWith("approval:", StringComparison.Ordinal)) { return current; }
        var retained = await conversations.ReadApprovalEventsAsync(tenant, token).ConfigureAwait(false);
        return MergeRetained(tenant, current, retained.Where(v => ToRetained(v).ItemRef == itemRef));
    }

    private static ReviewQueueTransition ToRetained(ApprovalEventView view)
        => new(view.TenantId, "retained-approval-" + ProjectConversationStableId.Hash(view.ProjectId + "|" + view.ApprovalId),
            view.SourceVersion, view.AuditOperationId ?? view.StableItemId, view.CorrelationId, OperationalQueueFamily.PendingApproval,
            view.ApprovalId, view.ProjectId, [view.ProjectId], view.Status.ToString(), view.Status != ApprovalStatus.Pending,
            view.DecisionKind?.ToString() ?? view.DisabledReason, "approval-reviewer", view.SafeNextAction ?? "review", view.OccurredAtUtc,
            Risk: view.RiskClass?.ToString().ToLowerInvariant(), Actor: view.DecisionActorId ?? view.RequesterId,
            ResourceIds: view.AffectedResourceReferences?.ToArray(), RetainedApproval: true);

    private ReviewQueueSubject? MergeRetained(string tenant, ReviewQueueSubject? named, IEnumerable<ApprovalEventView> retained)
    {
        ApprovalEventView[] views = retained.Where(view => view.TenantId == tenant)
            .OrderBy(view => view.SourceVersion).ToArray();
        var result = named;
        foreach (var view in views)
        {
            if (named?.History.Any(f => f.Version == view.SourceVersion && f.State == view.Status.ToString()) == true) { continue; }
            result = ReviewQueueSubject.Add(result, ToRetained(view));
        }

        if (result is null || result.Current.Terminal || clock is null)
        {
            return result;
        }

        var matching = views.Where(view => view.SourceVersion == result.Current.Version && view.Status.ToString() == result.Current.State)
            .MaxBy(view => view.EventKind);
        return matching is null ? result : result with
        {
            ApprovalPriority = ApprovalQueueItemBuilder.TryBuild(matching, Contracts.Commands.ApprovalPriorityWeights.SafeDefaults, clock),
        };
    }

    private async Task<ReviewQueueSubject?> ReadNamedAsync(string tenant, string itemRef, CancellationToken token)
        => (await store.GetAsync<ReviewQueueSubject>(StoreName, SubjectKey(tenant, itemRef), token).ConfigureAwait(false)).Value;

    public Task<string[]> ReadTenantsAsync(CancellationToken token) => ReadIndexAsync("chatbot-review-queue-tenants", token);

    public async Task<IReadOnlyList<ReviewQueueSubject>> ReadFamilyAsync(string tenant, OperationalQueueFamily family, CancellationToken token)
    {
        string[] refs = await ReadIndexAsync(IndexKey(tenant, family), token).ConfigureAwait(false);
        List<ReviewQueueSubject> subjects = [];
        foreach (string reference in refs)
        {
            ReviewQueueSubject? subject = await ReadNamedAsync(tenant, reference, token).ConfigureAwait(false);
            if (subject?.Current is null || subject.Current.TenantId != tenant || subject.Current.Family != family || subject.Current.ItemRef != reference ||
                subject.History.Any(f => f.TenantId != tenant || f.Family != family || f.ItemRef != reference))
            {
                throw new InvalidDataException("review_queue_index_incomplete");
            }
            subjects.Add(subject);
        }
        if (family == OperationalQueueFamily.PendingApproval && conversations is not null)
        {
            var byItem = subjects.ToDictionary(s => s.Current.ItemRef, StringComparer.Ordinal);
            var retained = (await conversations.ReadApprovalEventsAsync(tenant, token).ConfigureAwait(false))
                .Where(view => view.TenantId == tenant);
            foreach (var group in retained.GroupBy(view => ToRetained(view).ItemRef, StringComparer.Ordinal))
            {
                byItem[group.Key] = MergeRetained(tenant, byItem.GetValueOrDefault(group.Key), group)!;
            }
            return byItem.Values.ToArray();
        }
        return subjects;
    }

    public async Task<bool> CanReadAsync(ReviewQueueSubject subject, ChatBotAuthorityPrincipal principal,
        ChatBotRequestAuthorizer authorizer, string operation, CancellationToken token, string? requiredProject = null)
    {
        if (!authorizer.IsCurrent(principal) || principal.Context.TenantId != subject.Current.TenantId) { return false; }
        string[] projects = subject.History.SelectMany(item => item.ProjectIds.Concat((item.ResourceIds ?? [])
            .Where(resource => resource.StartsWith("project:", StringComparison.Ordinal)).Select(resource => resource["project:".Length..])))
            .Distinct(StringComparer.Ordinal).ToArray();
        if (projects.Length == 0 && subject.Current.Intake is { } intake)
        {
            var associations = await ReadFamilyAsync(subject.Current.TenantId, OperationalQueueFamily.AmbiguousAssociation, token).ConfigureAwait(false);
            projects = associations.Where(a => a.Current.Intake == intake).SelectMany(a => a.Current.ProjectIds).Distinct(StringComparer.Ordinal).ToArray();
        }
        if (requiredProject is not null && !projects.Contains(requiredProject, StringComparer.Ordinal)) { return false; }
        string[] resources = subject.History.SelectMany(f => new[] { f.SubjectId, f.RetainedApproval ? null : f.StreamId, f.Mailbox }.Concat(f.ResourceIds ?? [])).OfType<string>()
            .Distinct(StringComparer.Ordinal).ToArray();
        return await authorizer.HasProjectAuthoritiesAsync(principal.Context, projects, operation, token, principal).ConfigureAwait(false) &&
            await authorizer.HasQueueResourceAuthoritiesAsync(principal, resources, operation, token).ConfigureAwait(false);
    }

    private async Task<string[]> ReadIndexAsync(string key, CancellationToken token)
        => (await store.GetAsync<string[]>(StoreName, key, token).ConfigureAwait(false)).Value ?? [];

    private async Task AddToIndexAsync(string key, string value, CancellationToken token)
        => _ = await ReadModelWritePolicy.UpdateAsync<string[]>(store, StoreName, key,
            existing => existing?.Contains(value, StringComparer.Ordinal) == true ? existing : [.. existing ?? [], value],
            new ReadModelWriteContext(Category: "ReviewQueueIndex"), cancellationToken: token).ConfigureAwait(false);
}
