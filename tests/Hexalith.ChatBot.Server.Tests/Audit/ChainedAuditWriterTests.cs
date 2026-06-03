using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Gateway;
using Hexalith.ChatBot.Server.Gateway.Stages;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Audit;

/// <summary>
/// Story 9.1 (AC1, two-phase audit D4) coverage for the <see cref="ChainedAuditWriter"/> decorator: a successful
/// post-commit write appends to the WORM chain; a failing chain append surfaces <see cref="AuditWriteResult.Unavailable"/>
/// (fail-open-then-reconcile) so the gateway's existing reconcile path fires; and the pre-commit / authorization-failure
/// gates pass straight through, never touching the chain.
/// </summary>
public sealed class ChainedAuditWriterTests
{
    [Fact]
    public async Task SuccessfulPostCommitAppendsToChainAndRecordsHistory()
    {
        InMemoryAuditWriter inner = new();
        InMemoryWormAuditStore store = new();
        ChainedAuditWriter writer = new(inner, store);
        AuditEnvelope envelope = WormAuditTestData.Envelope("tenant-alpha", resourceId: "res-9");

        AuditWriteResult result = await writer.RecordPostCommitAsync(envelope, CancellationToken.None);

        result.Succeeded.ShouldBeTrue();
        store.EnumerateChain("tenant-alpha").ShouldHaveSingleItem();
        // The inner history surface (Story 1.9) still sees the envelope.
        writer.GetPostCommitEnvelopes("tenant-alpha", "res-9").ShouldHaveSingleItem();
    }

    [Fact]
    public async Task FailingChainAppendReturnsUnavailableForReconcilePath()
    {
        InMemoryAuditWriter inner = new();
        ChainedAuditWriter writer = new(inner, new WormAuditTestData.FailingWormAuditStore());

        AuditWriteResult result = await writer.RecordPostCommitAsync(WormAuditTestData.Envelope("tenant-alpha"), CancellationToken.None);

        // Fail-open: the chain append failure surfaces as Unavailable so QueueReplayIntent + reconcile alert fire — the
        // commit is never blocked. The inner writer still recorded the envelope (history is unaffected).
        result.Succeeded.ShouldBeFalse();
        result.ReasonCode.ShouldBe("worm_store_unavailable");
        inner.Envelopes.ShouldHaveSingleItem();
    }

    [Fact]
    public async Task PreCommitAndAuthorizationGatesDoNotTouchTheChain()
    {
        InMemoryAuditWriter inner = new();
        InMemoryWormAuditStore store = new();
        ChainedAuditWriter writer = new(inner, store);

        AuditWriteResult preCommit = await writer.RecordPreCommitAsync(WormAuditTestData.Envelope("tenant-alpha"), CancellationToken.None);

        preCommit.Succeeded.ShouldBeTrue();
        // The pre-commit gate is unaffected by the WORM chain — nothing was appended.
        store.EnumerateChain("tenant-alpha").ShouldBeEmpty();
        store.EnumerateTenants().ShouldBeEmpty();
    }

    [Fact]
    public async Task AuthorizationFailurePassesThroughToInnerWriterAndNeverTouchesTheChain()
    {
        InMemoryAuditWriter inner = new();
        InMemoryWormAuditStore store = new();
        ChainedAuditWriter writer = new(inner, store);
        ChatBotAuthorizationFailureAuditFact fact = new(
            TenantId: "tenant-alpha",
            ActorId: "actor-1",
            CommandType: "TestCommand",
            ReasonCode: "command_not_allowlisted",
            CorrelationId: "01ARZ3NDEKTSV4RRFFQ69G5FAW",
            TaskId: null,
            SurfaceOrigin: "worker");

        await writer.RecordAuthorizationFailureAsync(fact, CancellationToken.None);

        // Authorization-failure writes are pre-commit gate facts: they go straight to the inner writer and never chain.
        inner.AuthorizationFailures.ShouldHaveSingleItem().ShouldBe(fact);
        store.EnumerateChain("tenant-alpha").ShouldBeEmpty();
        store.EnumerateTenants().ShouldBeEmpty();
    }
}
