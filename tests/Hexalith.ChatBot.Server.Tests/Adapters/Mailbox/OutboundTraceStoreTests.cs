using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Server.Adapters.Mailbox;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Adapters.Mailbox;

/// <summary>
/// Story 9.4 (AC1) coverage for the tenant-partitioned outbound-trace store and the metadata-only would-have-sent
/// record. The store partitions by tenant by construction (a test-tenant record is invisible to a production-tenant
/// enumerate, NFR9a); <see cref="OutboundTraceRecord.FromRequest"/> reduces every field to a safe bounded token and
/// carries no recipient/subject/body (NFR2/NFR42).
/// </summary>
public sealed class OutboundTraceStoreTests
{
    private static readonly DateTimeOffset Now = new(2026, 6, 3, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task RecordIsEnumerableOnlyWithinItsOwnTenantPartition()
    {
        InMemoryOutboundTraceStore store = new();
        await store.RecordAsync(Record("replay-test:tenant-alpha", "send-001"), CancellationToken.None);
        await store.RecordAsync(Record("tenant-beta", "send-002"), CancellationToken.None);

        store.EnumerateForTenant("replay-test:tenant-alpha").ShouldHaveSingleItem().SendId.ShouldBe("send-001");
        store.EnumerateForTenant("tenant-beta").ShouldHaveSingleItem().SendId.ShouldBe("send-002");

        // Cross-tenant read can never observe another tenant's record (tenant isolation by construction).
        store.EnumerateForTenant("replay-test:tenant-alpha").ShouldNotContain(static record => record.SendId == "send-002");
        store.EnumerateTenants().ShouldBe(["replay-test:tenant-alpha", "tenant-beta"], ignoreOrder: true);
    }

    [Fact]
    public async Task UnknownTenantYieldsAnEmptyEnumeration()
    {
        InMemoryOutboundTraceStore store = new();
        await store.RecordAsync(Record("replay-test:tenant-alpha", "send-001"), CancellationToken.None);

        store.EnumerateForTenant("tenant-never-seen").ShouldBeEmpty();
    }

    [Fact]
    public void FromRequestCarriesTheReplayMarkerAndOnlySafeTokens()
    {
        OutboundMailboxSendRequest request = new(
            "replay-test:tenant-alpha",
            "project-001",
            "draft-001",
            "approval-001",
            "send-001",
            "requester-001",
            "actor-alpha",
            SenderAuthorityClass.AuthenticatedUserSend,
            "send",
            "01ARZ3NDEKTSV4RRFFQ69G5FAW",
            ReplayRunId: "replay-run-001");

        OutboundTraceRecord record = OutboundTraceRecord.FromRequest(request, Now);

        record.TenantId.ShouldBe("replay-test:tenant-alpha");
        record.SendId.ShouldBe("send-001");
        record.SenderAuthorityClass.ShouldBe(nameof(SenderAuthorityClass.AuthenticatedUserSend));
        record.ReplayRunId.ShouldBe("replay-run-001");
        record.RecordedAtUtc.ShouldBe(Now);
        record.RecordedAtUtc.Offset.ShouldBe(TimeSpan.Zero);
    }

    [Fact]
    public void FromRequestReducesAnUnsafeFieldToASafeFallbackAndNeverLeaksContent()
    {
        // An unsafe token (whitespace/free-text content, banned markers) can never smuggle into the trace store: it is
        // replaced by the safe fallback. The replay marker stays null when unsafe (no fabricated marker).
        OutboundMailboxSendRequest request = new(
            "replay-test:tenant-alpha",
            "draft body leak attempt", // unsafe (whitespace/free text) — must NOT survive
            "draft-001",
            "approval-001",
            "send-001",
            "requester-001",
            "actor-alpha",
            SenderAuthorityClass.AuthenticatedUserSend,
            "send",
            "01ARZ3NDEKTSV4RRFFQ69G5FAW",
            ReplayRunId: "run with spaces"); // unsafe ⇒ dropped to null

        OutboundTraceRecord record = OutboundTraceRecord.FromRequest(request, Now);

        record.ProjectId.ShouldBe("redacted-ref");
        record.ReplayRunId.ShouldBeNull();

        // No banned/no-leak marker appears anywhere in the record's tokens.
        string[] tokens =
        [
            record.TenantId, record.ProjectId, record.DraftId, record.ApprovalId, record.SendId,
            record.RequesterId, record.SendActorId, record.SenderAuthorityClass, record.AdapterMode, record.CorrelationId,
        ];
        foreach (string banned in ReplayIsolationTestData.BannedMarkers)
        {
            tokens.ShouldAllBe(token => !token.Contains(banned, StringComparison.OrdinalIgnoreCase));
        }
    }

    private static OutboundTraceRecord Record(string tenantId, string sendId, string? replayRunId = "replay-run-001")
        => new(
            tenantId,
            "project-001",
            "draft-001",
            "approval-001",
            sendId,
            "requester-001",
            "actor-alpha",
            nameof(SenderAuthorityClass.AuthenticatedUserSend),
            "send",
            "01ARZ3NDEKTSV4RRFFQ69G5FAW",
            replayRunId,
            Now);
}
