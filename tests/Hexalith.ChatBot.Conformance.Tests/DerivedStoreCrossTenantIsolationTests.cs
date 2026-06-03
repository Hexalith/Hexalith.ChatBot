using System.Text.Json;

using Hexalith.ChatBot.Conformance.Tests.Harness;
using Hexalith.ChatBot.Server.Projections.DerivedStores;

using Shouldly;

namespace Hexalith.ChatBot.Conformance.Tests;

/// <summary>
/// Story 9.5 (AC1/AC2, FR55a/NFR9a) cross-tenant negative conformance: reusing the Story 1.12 leakage corpus, a foreign
/// tenant's sentinel is seeded into EACH of the four derived-store classes, then every read is attempted through the
/// bound tenant's scope. No foreign sentinel is ever observable (a key miss at the store layer), and every serialized
/// bound-tenant output is scanned for foreign sentinel tokens through the shared <see cref="CrossTenantLeakageScanner"/>
/// — extending that one gate to the derived-store entries and the probe result, rather than inventing a parallel style.
/// </summary>
public sealed class DerivedStoreCrossTenantIsolationTests
{
    // The foreign-tenant resource id we seed (a resource-id-channel sentinel from the corpus) and a derived-content
    // sentinel token standing in for the (never-stored) embedding/candidate payload.
    private static readonly string ForeignResourceId = CrossTenantLeakageCorpus.ForeignNoteId;
    private static readonly string ForeignContentSentinel = CrossTenantLeakageCorpus.Sentinel("candidate");

    [Fact]
    public async Task ForeignSentinelSeededInEveryClassIsNeverObservableThroughTheBoundTenantsScope()
    {
        InMemoryDerivedStore store = new();
        CancellationToken token = TestContext.Current.CancellationToken;
        string foreign = CrossTenantLeakageCorpus.ForeignTenant;
        string bound = CrossTenantLeakageCorpus.BoundTenant;

        // Sentinels we hunt for in any bound-tenant-rendered artifact; the bound tenant's OWN token is excluded because
        // it may legitimately appear in its own outputs.
        IReadOnlyList<LeakageSentinel> foreignSentinels = CrossTenantLeakageCorpus.SentinelsExcluding(bound);

        foreach (DerivedStoreClass cls in DerivedStorePartition.AllClasses)
        {
            // Seed the foreign tenant's partition.
            await store.PutAsync(cls, foreign, ForeignResourceId, DerivedStoreEntry.Create(ForeignResourceId, ForeignContentSentinel), token);

            // Positive control: the foreign tenant CAN read its own seeded entry (the seed really happened — non-vacuous).
            (await store.GetAsync(cls, foreign, ForeignResourceId, token)).ShouldNotBeNull();

            // Negative: the bound tenant attempts the SAME logical resource id through its own scope — a key miss.
            DerivedStoreEntry? throughBound = await store.GetAsync(cls, bound, ForeignResourceId, token);
            throughBound.ShouldBeNull();
            store.EnumerateResourceIds(cls, bound).ShouldBeEmpty();

            // Scan the rendered bound-tenant artifacts for ANY foreign sentinel token — none may appear.
            CrossTenantLeakageScanner.Scan("bound-tenant", $"derived-store.get.{cls}", JsonSerializer.Serialize(throughBound), foreignSentinels);
            CrossTenantLeakageScanner.Scan("bound-tenant", $"derived-store.enumerate.{cls}", JsonSerializer.Serialize(store.EnumerateResourceIds(cls, bound)), foreignSentinels);
        }
    }
}
