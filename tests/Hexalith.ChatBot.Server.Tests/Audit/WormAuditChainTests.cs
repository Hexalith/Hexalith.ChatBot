using System.Reflection;

using Hexalith.ChatBot.Server.Audit;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Audit;

/// <summary>
/// Story 9.1 (AC1, NFR49a) coverage for the append-only WORM store and per-tenant hash chain: predecessor linkage,
/// monotonic per-tenant sequence, deterministic hashing, tenant isolation, and the compile-time guarantee that no
/// update/delete member exists on the store contract.
/// </summary>
public sealed class WormAuditChainTests
{
    [Fact]
    public async Task GenesisRecordUsesSentinelPredecessorAndSequenceZero()
    {
        InMemoryWormAuditStore store = new();

        WormAuditAppendOutcome outcome = await store.AppendAsync(WormAuditTestData.Envelope("tenant-alpha"), CancellationToken.None);

        outcome.Succeeded.ShouldBeTrue();
        outcome.Record!.Sequence.ShouldBe(0);
        outcome.Record.PredecessorHash.ShouldBe(WormAuditChainHasher.GenesisPredecessorHash);
        outcome.Record.Envelope.PredecessorHash.ShouldBe(WormAuditChainHasher.GenesisPredecessorHash);
    }

    [Fact]
    public async Task EachAppendedRecordCarriesPredecessorHashOfPriorRecord()
    {
        InMemoryWormAuditStore store = new();

        WormAuditChainRecord first = (await store.AppendAsync(WormAuditTestData.Envelope("tenant-alpha", resourceId: "r1"), CancellationToken.None)).Record!;
        WormAuditChainRecord second = (await store.AppendAsync(WormAuditTestData.Envelope("tenant-alpha", resourceId: "r2"), CancellationToken.None)).Record!;
        WormAuditChainRecord third = (await store.AppendAsync(WormAuditTestData.Envelope("tenant-alpha", resourceId: "r3"), CancellationToken.None)).Record!;

        second.PredecessorHash.ShouldBe(first.RecordHash);
        third.PredecessorHash.ShouldBe(second.RecordHash);
    }

    [Fact]
    public async Task SequenceIsMonotonicPerTenant()
    {
        InMemoryWormAuditStore store = new();

        for (int i = 0; i < 5; i++)
        {
            _ = await store.AppendAsync(WormAuditTestData.Envelope("tenant-alpha", resourceId: $"r{i}"), CancellationToken.None);
        }

        IReadOnlyList<WormAuditChainRecord> chain = store.EnumerateChain("tenant-alpha");
        chain.Select(static record => record.Sequence).ShouldBe([0, 1, 2, 3, 4]);
    }

    [Fact]
    public async Task TenantChainsAreIsolatedWithNoCrossTenantLinkage()
    {
        InMemoryWormAuditStore store = new();

        WormAuditChainRecord alpha = (await store.AppendAsync(WormAuditTestData.Envelope("tenant-alpha"), CancellationToken.None)).Record!;
        WormAuditChainRecord beta = (await store.AppendAsync(WormAuditTestData.Envelope("tenant-beta"), CancellationToken.None)).Record!;

        // Each tenant's chain starts at its own genesis — beta does not chain off alpha.
        beta.Sequence.ShouldBe(0);
        beta.PredecessorHash.ShouldBe(WormAuditChainHasher.GenesisPredecessorHash);
        beta.PredecessorHash.ShouldNotBe(alpha.RecordHash);

        store.EnumerateChain("tenant-alpha").ShouldHaveSingleItem();
        store.EnumerateChain("tenant-beta").ShouldHaveSingleItem();
        store.EnumerateChain("tenant-unknown").ShouldBeEmpty();
    }

    [Fact]
    public void HashingIsDeterministicAcrossRepeatedSerialization()
    {
        AuditEnvelope envelope = WormAuditTestData.Envelope("tenant-alpha");

        string first = WormAuditChainHasher.ComputeRecordHash(envelope, WormAuditChainHasher.GenesisPredecessorHash, 0);
        string second = WormAuditChainHasher.ComputeRecordHash(envelope, WormAuditChainHasher.GenesisPredecessorHash, 0);

        first.ShouldBe(second);
        first.Length.ShouldBe(64); // lowercase hex SHA-256
    }

    [Fact]
    public void HashChangesWhenAnyHashInputComponentChanges()
    {
        AuditEnvelope envelope = WormAuditTestData.Envelope("tenant-alpha");
        string baseHash = WormAuditChainHasher.ComputeRecordHash(envelope, WormAuditChainHasher.GenesisPredecessorHash, 0);

        // Different sequence, different predecessor, and a mutated envelope each yield a different digest.
        WormAuditChainHasher.ComputeRecordHash(envelope, WormAuditChainHasher.GenesisPredecessorHash, 1).ShouldNotBe(baseHash);
        WormAuditChainHasher.ComputeRecordHash(envelope, new string('a', 64), 0).ShouldNotBe(baseHash);
        WormAuditChainHasher.ComputeRecordHash(envelope with { Outcome = "tampered" }, WormAuditChainHasher.GenesisPredecessorHash, 0).ShouldNotBe(baseHash);
    }

    [Fact]
    public void WormStoreContractExposesNoDeleteOrUpdateMember()
    {
        // AC1: deletion and in-place mutation must be impossible at the contract layer, not merely by convention.
        IEnumerable<string> memberNames = typeof(IWormAuditStore)
            .GetMembers(BindingFlags.Public | BindingFlags.Instance)
            .Select(static member => member.Name);

        foreach (string forbidden in new[] { "Delete", "Update", "Remove", "Mutate", "Replace", "Set", "Clear" })
        {
            memberNames.ShouldAllBe(name => !name.Contains(forbidden, StringComparison.OrdinalIgnoreCase));
        }
    }

    [Fact]
    public async Task TamperingARecordIsDetectableByRehashing()
    {
        InMemoryWormAuditStore store = new();
        WormAuditChainRecord record = (await store.AppendAsync(WormAuditTestData.Envelope("tenant-alpha"), CancellationToken.None)).Record!;

        // Simulate an at-rest mutation: the envelope body changed but the stored RecordHash did not.
        WormAuditChainRecord tampered = record with { Envelope = record.Envelope with { Outcome = "tampered" } };
        string recomputed = WormAuditChainHasher.ComputeRecordHash(tampered.Envelope, tampered.PredecessorHash, tampered.Sequence);

        recomputed.ShouldNotBe(tampered.RecordHash);
    }
}
