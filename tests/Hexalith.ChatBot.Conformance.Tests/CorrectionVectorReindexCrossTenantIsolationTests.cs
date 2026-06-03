using System.Text.Json;

using Hexalith.ChatBot.Conformance.Tests.Harness;
using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Projections.DerivedStores;

using Shouldly;

namespace Hexalith.ChatBot.Conformance.Tests;

/// <summary>
/// Story 9.6 (AC1, FR91a/NFR9a) cross-tenant negative conformance: reusing the Story 1.12 leakage corpus, a foreign
/// tenant seeds entries into EACH of the four derived-store classes, then a <c>ReindexVectors</c> correction runs under
/// the bound tenant. The reindex never invalidates or rebuilds the foreign tenant's entries (tenant isolation is
/// physical partitioning — a reindex under one tenant cannot reach another's partition), and every serialized
/// bound-tenant reindex output is scanned for foreign sentinel tokens through the shared
/// <see cref="CrossTenantLeakageScanner"/>, extending that one gate rather than inventing a parallel style.
/// </summary>
public sealed class CorrectionVectorReindexCrossTenantIsolationTests
{
    private static readonly string ForeignResourceId = CrossTenantLeakageCorpus.ForeignNoteId;
    private static readonly string BoundResourceId = CrossTenantLeakageCorpus.OwnNoteId;
    private static readonly string ForeignContentSentinel = CrossTenantLeakageCorpus.Sentinel("candidate");
    private static readonly DateTimeOffset StartedAt = new(2026, 6, 3, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task AReindexUnderTheBoundTenantNeverInvalidatesOrRebuildsTheForeignTenantsEntries()
    {
        InMemoryDerivedStore store = new();
        InMemoryVectorReindexer reindexer = new(store, new InMemoryVectorReindexLedger(), new FixedClock(StartedAt.AddMinutes(1)));
        CancellationToken token = TestContext.Current.CancellationToken;
        string foreign = CrossTenantLeakageCorpus.ForeignTenant;
        string bound = CrossTenantLeakageCorpus.BoundTenant;

        // Hunt for any foreign sentinel in the bound tenant's outputs; exclude the bound tenant's OWN tokens (its tenant
        // id and the resource id it legitimately reindexes), which may appear in its own artifacts.
        IReadOnlyList<LeakageSentinel> foreignSentinels = CrossTenantLeakageCorpus.SentinelsExcluding(bound, BoundResourceId);

        // The foreign tenant seeds its derived entries (carrying a content sentinel) in every class.
        foreach (DerivedStoreClass cls in DerivedStorePartition.AllClasses)
        {
            await store.PutAsync(cls, foreign, ForeignResourceId, DerivedStoreEntry.Create(ForeignResourceId, ForeignContentSentinel), token);
        }

        // The bound tenant runs a correction reindex for its OWN resource id.
        VectorReindexOutcome outcome = await reindexer.ReindexVectorsAsync(
            bound,
            "assoc-bound:correction:7",
            7,
            [BoundResourceId],
            StartedAt,
            token);

        // The bound reindex only touched its OWN partition — it invalidated nothing of the foreign tenant's.
        outcome.EntriesInvalidated.ShouldBe(0);
        outcome.EntriesRebuilt.ShouldBe(4);

        foreach (DerivedStoreClass cls in DerivedStorePartition.AllClasses)
        {
            // The foreign tenant's entry is intact — its content sentinel survives untouched.
            DerivedStoreEntry? foreignEntry = await store.GetAsync(cls, foreign, ForeignResourceId, token);
            foreignEntry.ShouldNotBeNull();
            foreignEntry.ContentDigest.ShouldBe(ForeignContentSentinel);

            // The bound tenant's rebuilt entry is metadata-only and never carries a foreign sentinel.
            DerivedStoreEntry? boundEntry = await store.GetAsync(cls, bound, BoundResourceId, token);
            CrossTenantLeakageScanner.Scan("bound-tenant", $"vector-reindex.rebuilt.{cls}", JsonSerializer.Serialize(boundEntry), foreignSentinels);
        }

        // The reindex outcome itself leaks no foreign sentinel.
        CrossTenantLeakageScanner.Scan("bound-tenant", "vector-reindex.outcome", JsonSerializer.Serialize(outcome), foreignSentinels);
    }

    private sealed class FixedClock(DateTimeOffset now) : ISystemClock
    {
        public DateTimeOffset UtcNow { get; } = now;
    }
}
