using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Server.Adapters.Mailbox;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Adapters.Mailbox;

/// <summary>
/// Story 9.4 (AC1) coverage for the test-mode outbound adapter and the tenant-aware selection seam. The test-mode sender
/// records exactly one metadata-only trace record (carrying the run id), returns <c>Sent</c> with the test-mode adapter
/// ref, and performs no external call (none is reachable by construction). The selector routes a test tenant to the
/// test-mode sender (records, no production send) and a production tenant to the production sender unchanged (no trace).
/// </summary>
public sealed class TestModeOutboundMailboxSenderTests
{
    private static readonly DateTimeOffset Now = new(2026, 6, 3, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task SendRecordsExactlyOneMarkedRecordReturnsSentAndContactsNoExternalSystem()
    {
        InMemoryOutboundTraceStore traceStore = new();
        TestModeOutboundMailboxSender sender = new(traceStore, new WormAuditTestDataClock(Now));

        OutboundMailboxSendResult result = await sender.SendAsync(
            Request("replay-test:tenant-alpha", replayRunId: "replay-run-001"),
            CancellationToken.None);

        result.Kind.ShouldBe(OutboundMailboxSendResultKind.Sent);
        result.AdapterStatus.ShouldBe("sent");
        result.AdapterRef.ShouldBe(TestModeOutboundMailboxSender.TestModeAdapterRef);

        OutboundTraceRecord record = traceStore.EnumerateForTenant("replay-test:tenant-alpha").ShouldHaveSingleItem();
        record.SendId.ShouldBe("send-001");
        record.ReplayRunId.ShouldBe("replay-run-001");
        record.RecordedAtUtc.ShouldBe(Now);
    }

    [Fact]
    public async Task TraceRecordCarriesNoRecipientSubjectOrBodyTokens()
    {
        InMemoryOutboundTraceStore traceStore = new();
        TestModeOutboundMailboxSender sender = new(traceStore, new WormAuditTestDataClock(Now));

        _ = await sender.SendAsync(Request("replay-test:tenant-alpha", replayRunId: "replay-run-001"), CancellationToken.None);

        OutboundTraceRecord record = traceStore.EnumerateForTenant("replay-test:tenant-alpha").ShouldHaveSingleItem();
        string[] tokens =
        [
            record.TenantId, record.ProjectId, record.DraftId, record.ApprovalId, record.SendId,
            record.RequesterId, record.SendActorId, record.SenderAuthorityClass, record.AdapterMode, record.CorrelationId,
            record.ReplayRunId ?? string.Empty,
        ];
        tokens.ShouldNotContain(static token => token.Contains('@', StringComparison.Ordinal)); // no recipient addresses
        foreach (string banned in ReplayIsolationTestData.BannedMarkers)
        {
            tokens.ShouldAllBe(token => !token.Contains(banned, StringComparison.OrdinalIgnoreCase));
        }
    }

    [Fact]
    public async Task SelectorRoutesTestTenantToTestModeSenderAndRecords()
    {
        InMemoryOutboundTraceStore traceStore = new();
        RecordingProductionSender production = new();
        ReplayAwareOutboundMailboxSender selector = new(
            production,
            new TestModeOutboundMailboxSender(traceStore, new WormAuditTestDataClock(Now)));

        OutboundMailboxSendResult result = await selector.SendAsync(
            Request("replay-test:tenant-alpha", replayRunId: "replay-run-001"),
            CancellationToken.None);

        result.AdapterRef.ShouldBe(TestModeOutboundMailboxSender.TestModeAdapterRef);
        production.SendCount.ShouldBe(0); // the production sender is never reachable for a test tenant
        traceStore.EnumerateForTenant("replay-test:tenant-alpha").ShouldHaveSingleItem();
    }

    [Fact]
    public async Task SelectorRoutesProductionTenantToProductionSenderUnchangedWithNoTrace()
    {
        InMemoryOutboundTraceStore traceStore = new();
        RecordingProductionSender production = new();
        ReplayAwareOutboundMailboxSender selector = new(
            production,
            new TestModeOutboundMailboxSender(traceStore, new WormAuditTestDataClock(Now)));

        OutboundMailboxSendResult result = await selector.SendAsync(
            Request("tenant-alpha", replayRunId: null),
            CancellationToken.None);

        result.AdapterRef.ShouldBe("adapter:mailbox-outbound"); // the production sender's ref, unchanged
        production.SendCount.ShouldBe(1);
        traceStore.EnumerateForTenant("tenant-alpha").ShouldBeEmpty(); // production never writes the trace store
        traceStore.EnumerateTenants().ShouldBeEmpty();
    }

    private static OutboundMailboxSendRequest Request(string tenantId, string? replayRunId)
        => new(
            tenantId,
            "project-001",
            "draft-001",
            "approval-001",
            "send-001",
            "requester-001",
            "actor-alpha",
            SenderAuthorityClass.AuthenticatedUserSend,
            "send",
            "01ARZ3NDEKTSV4RRFFQ69G5FAW",
            replayRunId);

    private sealed class RecordingProductionSender : IOutboundMailboxSender
    {
        public int SendCount { get; private set; }

        public ValueTask<OutboundMailboxSendResult> SendAsync(OutboundMailboxSendRequest request, CancellationToken cancellationToken = default)
        {
            SendCount++;
            return ValueTask.FromResult(OutboundMailboxSendResult.Sent("adapter:mailbox-outbound"));
        }
    }

    private sealed class WormAuditTestDataClock(DateTimeOffset now) : Hexalith.ChatBot.Server.Audit.ISystemClock
    {
        public DateTimeOffset UtcNow { get; } = now;
    }
}
