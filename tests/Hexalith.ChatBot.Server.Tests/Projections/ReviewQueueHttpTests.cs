using System.Net;
using System.Diagnostics.Metrics;
using Hexalith.ChatBot.Server.Observability;
using Hexalith.ChatBot.Server.Operations.PeriodicEnforcement;
using System.Net.Http.Json;
using System.Text.Json;
using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Contracts.Queries;
using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Authorization;
using Hexalith.ChatBot.Server.Governance.Policy;
using Hexalith.ChatBot.Server.Projections;
using Hexalith.ChatBot.Server.Tests.Gateway.Status;
using Hexalith.ChatBot.Server.Tests.TrustedAuthority;
using Hexalith.ChatBot.Tests.TrustedAuthority;
using Hexalith.EventStore.Client.Streams;
using Hexalith.EventStore.Contracts.Identity;
using Hexalith.EventStore.Contracts.Projections;
using Hexalith.EventStore.Contracts.Streams;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Projections;

public sealed class ReviewQueueHttpTests
{
    [Fact]
    public async Task HumanReadsUseCurrentProjectResourceAndTenantPolicyOnEveryRequest()
    {
        var token = TestContext.Current.CancellationToken;
        var clock = new TrustedAuthorityClock();
        var owner = new SyntheticOwnerAuthorityProvider(clock);
        var queues = new ReviewQueueStore(new AtomicOperationStatusReadModelStore(), clock: clock);
        string tenant = "tenant-alpha";
        const string project = "project-one";
        const string stream = "stream-one";
        var fact = new ReviewQueueTransition(tenant, stream, 1, "event-1", "correlation", OperationalQueueFamily.PendingApproval,
            "approval-one", project, [project], "pending", false, null, "reviewer", "review", clock.UtcNow);
        var second = fact with { SubjectId = "approval-two", Version = 2, OperationId = "event-2" };
        var subjects = new[] { fact, second };
        var events = subjects.Select(item => new StreamReadEvent(item.Version, "event-type", [1], "json", 1, item.OperationId,
            "correlation", null, clock.UtcNow, "requester")).ToArray();
        foreach (var item in subjects)
        {
            await queues.ApplyAsync(item, token);
            var envelope = new ProjectionEventDto("event-type", [1], "json", item.Version, clock.UtcNow, "correlation", item.OperationId);
            await queues.RecordCoverageAsync(new(tenant, "chatbot", stream, [envelope]), envelope, token, [item]);
        }
        var identity = new AggregateIdentity(tenant, "chatbot", stream);
        var policyId = new AggregateIdentity(tenant, "chatbot", ReviewQueueDisclosurePolicy.StreamId);
        AuthoritativeEventStream? policy = null;
        AuthoritativeEventStream? queueSource = new(identity, 2, clock.UtcNow, events, "verified-stream");
        var emptyId = new AggregateIdentity(tenant, "chatbot", "empty-stream");
        var empty = new AuthoritativeEventStream(emptyId, 0, clock.UtcNow, [], "verified-empty")
        { NativeProof = new(1, tenant, "chatbot", emptyId.AggregateId, "incarnation", 0, null, "prefix", 1, false) };
        var source = new ReviewQueueTestStreamReader(key => key == identity ? queueSource : key == emptyId ? empty : key == policyId ? policy : null);

        using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.ConfigureFiniteReceiver().ConfigureServices(services =>
            {
                SdkRouteFixtureAuthorization.AllowCanonicalRouteHandlers(services);
                services.AddSingleton<ISystemClock>(clock);
                services.AddSingleton<IChatBotOwnerAuthorityProvider>(owner);
                services.AddSingleton(queues);
                services.AddSingleton<IAuthoritativeEventStreamReader>(source);
                services.AddKeyedSingleton<IAuthoritativeEventStreamReader>("review-queue-native", source);
                services.AddFixturePrincipalAuthentication().AddSingleton<IStartupFilter>(
                    new TrustedAuthorityPrincipalStartupFilter(_ => TrustedAuthorityFixture.Principal(tenant)));
            }));
        using HttpClient client = factory.CreateClient();
        var meter = (ChatBotMetrics)factory.Services.GetRequiredService<IChatBotMetrics>();
        var worker = factory.Services.GetRequiredService<IPeriodicEnforcementInputSource>();
        List<int> depths = [];
        using var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, observer) =>
        {
            if (ReferenceEquals(instrument.Meter, meter.Meter) && instrument.Name == ChatBotMetrics.ReviewDepth)
            { observer.EnableMeasurementEvents(instrument); }
        };
        listener.SetMeasurementEventCallback<int>((_, value, _, _) => depths.Add(value));
        listener.Start();
        await worker.GetTenantInputsAsync(tenant, token);
        depths.ShouldBeEmpty();

        const string summaryUrl = "/api/v1/admin/review-queues/pending-approval/summary";
        var summary = (await client.GetFromJsonAsync<AdminQueueSummary>(summaryUrl, token)).ShouldNotBeNull();
        summary.Buckets.ShouldBeEmpty();
        summary.DisclosureState.ShouldBe("policy-unavailable");
        var approved = ApprovedMinimum(policyId, clock.UtcNow);
        policy = approved with { Head = 1, Events = [approved.Events[0]] };
        (await client.GetFromJsonAsync<AdminQueueSummary>(summaryUrl, token))!.Buckets.ShouldBeEmpty();
        policy = approved with { Events = [approved.Events[0], approved.Events[1] with { UserId = "requester" }] };
        (await client.GetFromJsonAsync<AdminQueueSummary>(summaryUrl, token))!.Buckets.ShouldBeEmpty();
        policy = approved;
        await worker.GetTenantInputsAsync(tenant, token);
        depths.ShouldBe([2]);
        summary = (await client.GetFromJsonAsync<AdminQueueSummary>(summaryUrl, token)).ShouldNotBeNull();
        summary.Buckets.ShouldHaveSingleItem().Count.ShouldBe(2);
        summary.VisibleItemRefs.ShouldBeEmpty();
        policy = policy with { ObservedAt = clock.UtcNow.AddHours(-1) };
        await worker.GetTenantInputsAsync(tenant, token);
        depths.ShouldBe([2]);
        (await client.GetFromJsonAsync<AdminQueueSummary>(summaryUrl, token))!.Buckets.ShouldBeEmpty();

        string detailUrl = $"/api/v1/review-queues/pending-approval/items/{fact.ItemRef}?sourceVersion=1";
        var detail = (await client.GetFromJsonAsync<OperationalQueueItemDetail>(detailUrl, token)).ShouldNotBeNull();
        detail.History.ShouldNotBeNull().ShouldHaveSingleItem().OperationId.ShouldBe("event-1");
        string projectUrl = $"/api/v1/projects/{project}/review-queues/pending-approval?sourceStreamId={stream}";
        var page = await client.GetFromJsonAsync<JsonElement>(projectUrl, token);
        page.GetProperty("rows").GetArrayLength().ShouldBe(2);
        page.GetProperty("coverage").GetProperty("reason").GetString().ShouldBe("current");

        var next = events[1] with { SequenceNumber = 3, MessageId = "event-3" };
        queueSource = new(identity, 3, clock.UtcNow, [.. events, next], "verified-stream");
        var behind = (await client.GetFromJsonAsync<JsonElement>(projectUrl, token)).GetProperty("coverage");
        behind.GetProperty("scope").GetString().ShouldBe(stream);
        behind.GetProperty("reason").GetString().ShouldBe("projection-behind");
        behind.GetProperty("knownSourceVersion").GetInt64().ShouldBe(3);
        behind.GetProperty("ownerRole").GetString().ShouldBe("operations-admin");
        behind.GetProperty("nextAction").GetString().ShouldBe("replay-source");

        queueSource = null;
        var unavailable = (await client.GetFromJsonAsync<JsonElement>(projectUrl, token)).GetProperty("coverage");
        unavailable.GetProperty("reason").GetString().ShouldBe("source-unavailable");
        unavailable.GetProperty("nextAction").GetString().ShouldBe("verify-source");

        var emptyPage = await client.GetFromJsonAsync<JsonElement>($"/api/v1/projects/{project}/review-queues/pending-approval?sourceStreamId=empty-stream", token);
        emptyPage.GetProperty("coverage").GetProperty("reason").GetString().ShouldBe("current");
        var saved = queueSource;
        queueSource = new(identity, 0, clock.UtcNow, [], "new-incarnation") { NativeProof = empty.NativeProof! with { AggregateId = stream } };
        var stale = await client.GetFromJsonAsync<JsonElement>($"/api/v1/projects/{project}/review-queues/pending-approval?sourceStreamId={stream}", token);
        stale.GetProperty("coverage").GetProperty("reason").GetString().ShouldBe("projection-incomplete");
        stale.GetProperty("rows").GetArrayLength().ShouldBe(0);
        queueSource = saved;
        var cross = fact with { StreamId = "later-source", At = fact.At.AddMinutes(1), OperationId = "other-owner", State = "decided", Terminal = true };
        await queues.ApplyAsync(cross, token);
        using (var ambiguous = await client.GetAsync(detailUrl, token)) { ambiguous.IsSuccessStatusCode.ShouldBeFalse(); }
        var revision = (await queues.ReadAsync(tenant, fact.ItemRef, token))!.Revision;
        detailUrl += "&revision=" + revision;
        var multi = (await client.GetFromJsonAsync<OperationalQueueItemDetail>(detailUrl, token))!;
        multi.History!.Select(f => f.SourceStreamId).ShouldBe([stream, "later-source"]);

        owner.Allows = evidence => evidence.ResourceId != fact.SubjectId;
        using var denied = await client.GetAsync(detailUrl, token);
        denied.IsSuccessStatusCode.ShouldBeFalse();
        (await denied.Content.ReadAsStringAsync(token)).ShouldNotContain("event-1");
        owner.Allows = _ => true;
        tenant = "tenant-beta";
        using var foreign = await client.GetAsync(detailUrl, token);
        foreign.StatusCode.ShouldBe(denied.StatusCode);
        (await foreign.Content.ReadAsStringAsync(token)).ShouldNotContain(fact.SubjectId);
    }

    [Fact]
    public void StaleProposalsDoNotPoisonValidRotationButContradictoryActivationsFailClosed()
    {
        var now = DateTimeOffset.UtcNow;
        var id = new AggregateIdentity("tenant-alpha", "chatbot", ReviewQueueDisclosurePolicy.StreamId);
        var initial = ApprovedMinimum(id, now);
        var pending = JsonSerializer.Deserialize<TenantPolicyChangePendingApproval>(initial.Events[0].Payload)!;
        var active = JsonSerializer.Deserialize<TenantPolicySnapshotActivated>(initial.Events[1].Payload)!;
        var rejected = new TenantPolicyChangeRejected(id.AggregateId, "stale", 1, "correlation");
        StreamReadEvent Event(object data, long sequence, string actor) => new(sequence, data.GetType().FullName!, JsonSerializer.SerializeToUtf8Bytes(data),
            "json", 1, "message-" + sequence, "correlation", null, now, actor);
        var second = pending with { SourcePolicySnapshotId = active.ActivatedPolicySnapshotId, ProposedPolicySnapshotId = "proposed-two", SourceVersion = 3 };
        var approved = active with { SourceVersion = 4, SupersededPolicySnapshotId = "active", ActivatedPolicySnapshotId = "next" };
        var rotated = initial with { Head = 6, Events = [..initial.Events, Event(pending, 3, "requester"), Event(rejected, 4, "approver"),
            Event(second, 5, "requester"), Event(approved, 6, "approver")] };
        ReviewQueueDisclosurePolicy.FromStream(id.TenantId, rotated, now)!.Value.Snapshot.ShouldBe("next");
        var conflict = rotated with { Head = 8, Events = [..rotated.Events, Event(pending, 7, "requester"), Event(active, 8, "approver")] };
        ReviewQueueDisclosurePolicy.FromStream(id.TenantId, conflict, now).ShouldBeNull();
    }

    private static AuthoritativeEventStream ApprovedMinimum(AggregateIdentity identity, DateTimeOffset now)
    {
        var change = new TenantPolicyChangeSet([new(TenantPolicyKnobIds.ReviewQueueMinimum, NumberValue: 2)]);
        string[] knobs = [TenantPolicyKnobIds.ReviewQueueMinimum];
        var pending = new TenantPolicyChangePendingApproval(identity.AggregateId, identity.TenantId, "base", "proposed", knobs, change,
            "requester", "requester-ref", "review", "old", "new", now, 1, "request-correlation");
        var approved = new TenantPolicySnapshotActivated(identity.AggregateId, identity.TenantId, "base", "active", knobs, change,
            TenantPolicyApprovalStatus.Approved, "requester-ref", "approver-ref", "review", now, 2, "approval-correlation");
        StreamReadEvent Event(object payload, long seq, string actor) => new(seq, payload.GetType().FullName!,
            JsonSerializer.SerializeToUtf8Bytes(payload), "json", 1, $"event-{seq}", "correlation", null, now, actor);
        return new(identity, 2, now, [Event(pending, 1, "requester"), Event(approved, 2, "approver")], "verified-policy");
    }
}
