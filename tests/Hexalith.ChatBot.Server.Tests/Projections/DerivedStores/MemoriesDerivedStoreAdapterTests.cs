using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Projections.DerivedStores;
using Hexalith.Memories.Client.Rest;
using Hexalith.Memories.Contracts.V1.DerivedStores;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Projections.DerivedStores;

/// <summary>Focused contract-adapter coverage for live diagnostic CRUD/enumeration and correction status mapping.</summary>
public sealed class MemoriesDerivedStoreAdapterTests
{
    private static readonly DateTimeOffset StartedAt = new(2026, 8, 28, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task DiagnosticAdapterUsesTenantScopedRemoteCrudAndEnumerationForAllFourCategories()
    {
        RecordingHandler handler = new(request => request.Method switch
        {
            { } method when method == HttpMethod.Put => new HttpResponseMessage(HttpStatusCode.NoContent),
            { } method when method == HttpMethod.Delete => Json(new DiagnosticStoreDeleteResult(true)),
            { } method when method == HttpMethod.Get && request.RequestUri!.Segments[^1] == "resource-001" =>
                Json(new DiagnosticStoreEntry("resource-001", "digest-001")),
            _ => Json<IReadOnlyList<DiagnosticStoreEntry>>([new DiagnosticStoreEntry("resource-001", "digest-001")]),
        });
        MemoriesDerivedStore store = new(Client(handler));
        CancellationToken token = TestContext.Current.CancellationToken;

        foreach (DerivedStoreClass cls in DerivedStorePartition.AllClasses)
        {
            DerivedStoreEntry entry = DerivedStoreEntry.Create("resource-001", "digest-001");
            await store.PutAsync(cls, "tenant-alpha", entry.ResourceId, entry, token);
            (await store.GetAsync(cls, "tenant-alpha", entry.ResourceId, token)).ShouldBe(entry);
            (await store.EnumerateResourceIdsAsync(cls, "tenant-alpha", token)).ShouldBe(["resource-001"]);
            (await store.InvalidateAsync(cls, "tenant-alpha", entry.ResourceId, token)).ShouldBeTrue();
        }

        handler.Requests.Count.ShouldBe(16);
        handler.Requests.ShouldAllBe(static request => request.Uri.Contains("tenant-alpha", StringComparison.Ordinal));
        store.EnumerateTenants().ShouldBe(["tenant-alpha"]);
        DerivedStorePartition.AllClasses.ShouldAllBe(cls => store.EnumerateResourceIds(cls, "tenant-alpha").Count == 0);
    }

    [Fact]
    public async Task CorrectionAdapterStartsThenQueriesUntilItReceivesTerminalSuccessEvidence()
    {
        DerivedStoreCorrectionStatus pending = Status(DerivedStoreCorrectionState.Pending, completedAtUtc: null);
        DerivedStoreCorrectionStatus succeeded = Status(DerivedStoreCorrectionState.Succeeded, StartedAt.AddMinutes(2));
        RecordingHandler handler = new(request => request.Method == HttpMethod.Post ? Json(pending) : Json(succeeded));
        MemoriesVectorReindexer reindexer = new(Client(handler), new FixedClock(StartedAt.AddMinutes(2)));

        VectorReindexOutcome started = await reindexer.ReindexCanonicalVectorsAsync(
            "tenant-alpha",
            "association-001",
            "intake-001",
            "correction-001",
            7,
            "case-corrected",
            remoteOperationId: null,
            StartedAt,
            TestContext.Current.CancellationToken);
        VectorReindexOutcome outcome = await reindexer.ReindexCanonicalVectorsAsync(
            "tenant-alpha",
            "association-001",
            "intake-001",
            "correction-001",
            7,
            "case-corrected",
            started.RemoteOperationId,
            StartedAt,
            TestContext.Current.CancellationToken);

        started.IsTerminal.ShouldBeFalse();
        started.RemoteOperationId.ShouldBe("operation-001");
        outcome.IsTerminal.ShouldBeTrue();
        outcome.FailureReasonCode.ShouldBeNull();
        outcome.EntriesInvalidated.ShouldBe(4);
        outcome.EntriesRebuilt.ShouldBe(4);
        handler.Requests.Count.ShouldBe(2);
        handler.Requests[0].Body.ShouldContain("\"intakeId\":\"intake-001\"");
        handler.Requests[0].Body.ShouldContain("\"correctedCaseId\":\"case-corrected\"");
        handler.Requests[1].Uri.ShouldContain("operation-001");
    }

    [Fact]
    public async Task CorrectionAdapterKeepsRunningStatusNonterminalForDurableWorkflowPolling()
    {
        DerivedStoreCorrectionStatus pending = Status(DerivedStoreCorrectionState.Pending, completedAtUtc: null);
        DerivedStoreCorrectionStatus running = Status(DerivedStoreCorrectionState.Running, completedAtUtc: null);
        RecordingHandler handler = new(request => request.Method == HttpMethod.Post ? Json(pending) : Json(running));
        MemoriesVectorReindexer reindexer = new(Client(handler), new FixedClock(StartedAt.AddMinutes(1)));

        VectorReindexOutcome started = await reindexer.ReindexCanonicalVectorsAsync(
            "tenant-alpha",
            "association-001",
            "intake-001",
            "correction-001",
            7,
            "case-corrected",
            remoteOperationId: null,
            StartedAt,
            TestContext.Current.CancellationToken);
        VectorReindexOutcome outcome = await reindexer.ReindexCanonicalVectorsAsync(
            "tenant-alpha",
            "association-001",
            "intake-001",
            "correction-001",
            7,
            "case-corrected",
            started.RemoteOperationId,
            StartedAt,
            TestContext.Current.CancellationToken);

        started.IsTerminal.ShouldBeFalse();
        outcome.IsTerminal.ShouldBeFalse();
        outcome.FailureReasonCode.ShouldBeNull();
        outcome.RemoteOperationId.ShouldBe("operation-001");
        handler.Requests.Select(static request => request.Method).ShouldBe(["POST", "GET"]);
    }

    private static MemoriesClient Client(HttpMessageHandler handler)
    {
        HttpClient httpClient = new(handler) { BaseAddress = new Uri("http://memories/") };
        return new MemoriesClient(
            httpClient,
            Options.Create(new MemoriesClientOptions()),
            NullLogger<MemoriesClient>.Instance);
    }

    private static DerivedStoreCorrectionStatus Status(
        DerivedStoreCorrectionState state,
        DateTimeOffset? completedAtUtc)
        => new(
            "operation-001",
            state,
            "association-001",
            "intake-001",
            "correction-001",
            7,
            "case-prior",
            "case-corrected",
            4,
            4,
            VersionGuardSkipped: false,
            StartedAt.AddMinutes(60),
            completedAtUtc,
            FailureReasonCode: null);

    private static HttpResponseMessage Json<T>(T value)
        => new(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(value, options: new JsonSerializerOptions(JsonSerializerDefaults.Web)),
        };

    private sealed class RecordingHandler(Func<HttpRequestMessage, HttpResponseMessage> response) : HttpMessageHandler
    {
        public List<(string Method, string Uri, string Body)> Requests { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            string body = request.Content is null
                ? string.Empty
                : await request.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            Requests.Add((request.Method.Method, request.RequestUri!.ToString(), body));
            return response(request);
        }
    }

    private sealed class FixedClock(DateTimeOffset utcNow) : ISystemClock
    {
        public DateTimeOffset UtcNow => utcNow;
    }
}
