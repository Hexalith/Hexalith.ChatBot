using Hexalith.ChatBot.Conformance.Tests.Harness;
using Hexalith.ChatBot.Server.Projections;

using Shouldly;

namespace Hexalith.ChatBot.Conformance.Tests;

/// <summary>
/// AC5 — store-layer tenant partitioning is proven, not assumed. The governed-operation read model keys are
/// tenant-prefixed (<c>{tenant}:governed-operation:{noteId}</c>); the SAME logical note id seeded under two
/// tenants reads back only its own view; a foreign-tenant notification — even a higher version, a duplicate, or
/// an out-of-order one — can never overwrite or advance the caller tenant's view. DAPR-backed validation stays
/// on the env-gated Tier-3 path (this suite never depends on Redis/DAPR).
/// </summary>
public sealed class CrossTenantStorePartitioningTests
{
    private static readonly DateTimeOffset RecordedAt = new(2026, 5, 31, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public void KeyForShouldBeTenantPrefixedAndDistinctAcrossTenants()
    {
        string boundKey = GovernedOperationView.KeyFor(CrossTenantLeakageCorpus.BoundTenant, CrossTenantLeakageCorpus.ForeignNoteId);
        string foreignKey = GovernedOperationView.KeyFor(CrossTenantLeakageCorpus.ForeignTenant, CrossTenantLeakageCorpus.ForeignNoteId);

        boundKey.ShouldBe($"{CrossTenantLeakageCorpus.BoundTenant}:governed-operation:{CrossTenantLeakageCorpus.ForeignNoteId}");
        boundKey.ShouldStartWith($"{CrossTenantLeakageCorpus.BoundTenant}:");
        foreignKey.ShouldStartWith($"{CrossTenantLeakageCorpus.ForeignTenant}:");
        // Same logical note id, different tenants → the key is never shared across tenants.
        boundKey.ShouldNotBe(foreignKey);
    }

    [Fact]
    public async Task SameNoteIdUnderTwoTenantsShouldReadBackOnlyItsOwnView()
    {
        InMemoryGovernedOperationProjectionStore store = new();
        GovernedOperationProjectionHandler handler = new(store, new FixedConformanceClock());
        string noteId = CrossTenantLeakageCorpus.ForeignNoteId;
        CancellationToken token = TestContext.Current.CancellationToken;

        _ = await handler.HandleAsync(Notification(CrossTenantLeakageCorpus.BoundTenant, noteId, 1), token);
        _ = await handler.HandleAsync(Notification(CrossTenantLeakageCorpus.ForeignTenant, noteId, 1), token);

        GovernedOperationView? boundView = await store.GetAsync(CrossTenantLeakageCorpus.BoundTenant, noteId, token);
        GovernedOperationView? foreignView = await store.GetAsync(CrossTenantLeakageCorpus.ForeignTenant, noteId, token);

        boundView.ShouldNotBeNull();
        foreignView.ShouldNotBeNull();
        boundView.TenantId.ShouldBe(CrossTenantLeakageCorpus.BoundTenant);
        foreignView.TenantId.ShouldBe(CrossTenantLeakageCorpus.ForeignTenant);
        // A third, unseeded tenant gets a safe-not-found, proving the read is genuinely tenant-scoped.
        (await store.GetAsync("tenant-gamma", noteId, token)).ShouldBeNull();
    }

    [Fact]
    public async Task ForeignTenantNotificationsShouldNeverOverwriteOrAdvanceTheCallerTenantsView()
    {
        InMemoryGovernedOperationProjectionStore store = new();
        GovernedOperationProjectionHandler handler = new(store, new FixedConformanceClock());
        string noteId = CrossTenantLeakageCorpus.ForeignNoteId;
        CancellationToken token = TestContext.Current.CancellationToken;

        // The caller's (bound) view sits at source version 2.
        _ = await handler.HandleAsync(Notification(CrossTenantLeakageCorpus.BoundTenant, noteId, 2), token);
        GovernedOperationView before = (await store.GetAsync(CrossTenantLeakageCorpus.BoundTenant, noteId, token)).ShouldNotBeNull();

        // A foreign tenant delivers a higher version, a duplicate, and an out-of-order older version for the SAME
        // logical note id. None of these may copy into or advance the caller tenant's view.
        _ = await handler.HandleAsync(Notification(CrossTenantLeakageCorpus.ForeignTenant, noteId, 5), token);
        _ = await handler.HandleAsync(Notification(CrossTenantLeakageCorpus.ForeignTenant, noteId, 5), token);
        _ = await handler.HandleAsync(Notification(CrossTenantLeakageCorpus.ForeignTenant, noteId, 1, RecordedAt.AddMinutes(-5)), token);

        GovernedOperationView afterBound = (await store.GetAsync(CrossTenantLeakageCorpus.BoundTenant, noteId, token)).ShouldNotBeNull();
        GovernedOperationView afterForeign = (await store.GetAsync(CrossTenantLeakageCorpus.ForeignTenant, noteId, token)).ShouldNotBeNull();

        afterBound.ShouldBe(before);
        afterBound.SourceVersion.ShouldBe(2);
        // The foreign tenant advances only its OWN view (higher version wins within the tenant; the stale/dup are dropped).
        afterForeign.SourceVersion.ShouldBe(5);
    }

    [Fact]
    public async Task DuplicateAndStaleNotificationsShouldBeIdempotentWithinATenant()
    {
        InMemoryGovernedOperationProjectionStore store = new();
        GovernedOperationProjectionHandler handler = new(store, new FixedConformanceClock());
        string noteId = CrossTenantLeakageCorpus.OwnNoteId;
        CancellationToken token = TestContext.Current.CancellationToken;

        GovernedOperationProjectionHandler.ProjectionOutcome first = await handler.HandleAsync(Notification(CrossTenantLeakageCorpus.BoundTenant, noteId, 1), token);
        GovernedOperationProjectionHandler.ProjectionOutcome duplicate = await handler.HandleAsync(Notification(CrossTenantLeakageCorpus.BoundTenant, noteId, 1), token);
        GovernedOperationProjectionHandler.ProjectionOutcome stale = await handler.HandleAsync(Notification(CrossTenantLeakageCorpus.BoundTenant, noteId, 1, RecordedAt.AddMinutes(-9)), token);

        first.ShouldBe(GovernedOperationProjectionHandler.ProjectionOutcome.Applied);
        duplicate.ShouldBe(GovernedOperationProjectionHandler.ProjectionOutcome.Ignored);
        stale.ShouldBe(GovernedOperationProjectionHandler.ProjectionOutcome.Ignored);
    }

    private static GovernedNoteRecordedNotification Notification(string tenantId, string noteId, long sourceVersion, DateTimeOffset? recordedAt = null)
        => new(tenantId, noteId, noteId, sourceVersion, recordedAt ?? RecordedAt, CrossTenantIsolationHarness.CorrelationId);
}
