using System.Linq;
using System.Text.Json;

using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Gateway;
using Hexalith.ChatBot.Server.Gateway.Stages;
using Hexalith.ChatBot.Server.Notifications;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Notifications;

public sealed class NotificationThrottleCoordinatorTests
{
    private static readonly ISystemClock Clock = new FixedClock(new DateTimeOffset(2026, 6, 2, 12, 0, 0, TimeSpan.Zero));

    [Fact]
    public async Task ThrottledNotificationAlwaysProducesADigestEntryNeverDropped()
    {
        Harness harness = new();
        // Hourly ceiling 0 — every notification overflows into the digest.
        NotificationThrottleOutcome outcome = await harness.RunAsync(new NotificationThrottleCeilings(0, 30), Delivery());

        outcome.Delivered.ShouldBe(0);
        outcome.Throttled.ShouldBe(1);
        outcome.AuditUnavailable.ShouldBe(0);
        harness.Sink.Deliveries.ShouldBeEmpty();
        harness.Digest.GetPendingEntries("tenant-alpha", "operator-001").Count.ShouldBe(1);
    }

    [Fact]
    public async Task AuthorizedItemContextDeliveryPreservesItemIdentityInDigest()
    {
        Harness harness = new();
        await harness.RunAsync(new NotificationThrottleCeilings(0, 30), Delivery(visibility: NotificationContentVisibility.ItemContext, itemRef: "item-77"));

        NotificationDigestEntry entry = harness.Digest.GetPendingEntries("tenant-alpha", "operator-001").ShouldHaveSingleItem();
        entry.ItemRef.ShouldBe("item-77");
        entry.StateClass.ShouldBe(NotificationStateClass.Failure);
        entry.Scope.ShouldBe(AdminScope.Operate);
        entry.ReasonCode.ShouldBe("review_needed"); // safe next-action affordance preserved
    }

    [Fact]
    public async Task RedactedDeliveryYieldsMetadataRedactedEntryIndistinguishableFromNotFound()
    {
        Harness harness = new();
        await harness.RunAsync(new NotificationThrottleCeilings(0, 30), Delivery(visibility: NotificationContentVisibility.MetadataRedacted, itemRef: "item-secret"));

        NotificationDigestEntry entry = harness.Digest.GetPendingEntries("tenant-alpha", "operator-001").ShouldHaveSingleItem();
        // A redacted entry omits the item ref entirely — indistinguishable from safe-not-found; it never reveals what
        // the immediate push would have redacted.
        entry.Visibility.ShouldBe(NotificationContentVisibility.MetadataRedacted);
        entry.ItemRef.ShouldBeNull();
    }

    [Fact]
    public async Task SerializedDigestForARedactedEntryContainsNoRestrictedDetail()
    {
        Harness harness = new();
        await harness.RunAsync(new NotificationThrottleCeilings(0, 30), Delivery(visibility: NotificationContentVisibility.MetadataRedacted, itemRef: "project-acme-item-9"));

        string json = JsonSerializer.Serialize(
            harness.Digest.GetPendingEntries("tenant-alpha", "operator-001"),
            new JsonSerializerOptions(JsonSerializerDefaults.Web));
        json.ShouldNotContain("project-acme-item-9");
        json.ShouldNotContain("project-", Case.Insensitive);
        json.ShouldNotContain("secret", Case.Insensitive);
        json.ShouldNotContain("@");
    }

    [Fact]
    public async Task CountersAndDigestAreIsolatedPerTenantAndRecipient()
    {
        Harness harness = new();
        // Tenant alpha's recipient is throttled (ceiling 0). Tenant beta's same-named recipient is independent and delivers.
        await harness.RunAsync(new NotificationThrottleCeilings(0, 30), Delivery(tenantRef: "tenant-alpha", itemRef: "item-from-alpha"));
        await harness.RunAsync(NotificationThrottleCeilings.SafeDefaults, Delivery(tenantRef: "tenant-beta"));

        // Alpha's overflow never appears in beta's digest and vice-versa.
        harness.Digest.GetPendingEntries("tenant-alpha", "operator-001").Count.ShouldBe(1);
        harness.Digest.GetPendingEntries("tenant-beta", "operator-001").ShouldBeEmpty();
        harness.History.GetImmediatePushTimestamps("tenant-alpha", "operator-001").ShouldBeEmpty();
        harness.History.GetImmediatePushTimestamps("tenant-beta", "operator-001").Count.ShouldBe(1);
    }

    [Fact]
    public async Task AuditUnavailableFailsClosedWithNoSideEffectAndNoCounterAdvance()
    {
        Harness harness = new() { Audit = { PreCommitResult = AuditWriteResult.Unavailable() } };
        NotificationThrottleOutcome outcome = await harness.RunAsync(NotificationThrottleCeilings.SafeDefaults, Delivery());

        outcome.Delivered.ShouldBe(0);
        outcome.Throttled.ShouldBe(0);
        outcome.AuditUnavailable.ShouldBe(1);
        harness.Sink.Deliveries.ShouldBeEmpty();
        harness.Digest.GetPendingEntries("tenant-alpha", "operator-001").ShouldBeEmpty();
        // Critical: an unaudited delivery never advances the recipient's ceiling counter.
        harness.History.GetImmediatePushTimestamps("tenant-alpha", "operator-001").ShouldBeEmpty();
    }

    [Fact]
    public async Task AuditedImmediateDeliveryEmitsExactlyOneMetadataOnlyEnvelopePerDecision()
    {
        Harness harness = new();
        await harness.RunAsync(NotificationThrottleCeilings.SafeDefaults, Delivery(visibility: NotificationContentVisibility.ItemContext, itemRef: "item-77"));

        AuditEnvelope envelope = harness.Audit.Envelopes.ShouldHaveSingleItem();
        envelope.TenantId.ShouldBe("tenant-alpha");
        envelope.SourceEvidenceRefs.ShouldContain("admin-operation:notification-delivery");
        envelope.SourceEvidenceRefs.ShouldContain("throttle-decision:delivered");
        envelope.SourceEvidenceRefs.ShouldContain("notification-state-class:failure");
        envelope.SourceEvidenceRefs.ShouldContain("throttle-window-hour:0");
        envelope.SourceEvidenceRefs.ShouldContain("throttle-window-day:0");
        envelope.SourceEvidenceRefs.ShouldContain("digest-rolled-up-count:0");

        string json = JsonSerializer.Serialize(harness.Audit.Envelopes, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        json.ShouldNotContain("secret", Case.Insensitive);
        json.ShouldNotContain("@");
        json.ShouldNotContain("address", Case.Insensitive);
    }

    [Fact]
    public async Task ThrottleDecisionEnvelopeCarriesDigestTokenAndWindowSnapshot()
    {
        Harness harness = new();
        await harness.RunAsync(new NotificationThrottleCeilings(0, 30), Delivery());

        AuditEnvelope envelope = harness.Audit.Envelopes.ShouldHaveSingleItem();
        envelope.SourceEvidenceRefs.ShouldContain("throttle-decision:digest");
        envelope.SourceEvidenceRefs.ShouldContain("digest-rolled-up-count:1");
        envelope.Outcome.ShouldBe("throttled");
    }

    [Fact]
    public async Task ImmediatePushAdvancesCounterSoTheNextDeliveryThrottles()
    {
        Harness harness = new();
        // Hourly ceiling 1: the first delivery pushes (advancing the counter); the second, to the same recipient, throttles.
        NotificationThrottleOutcome outcome = await harness.RunAsync(
            new NotificationThrottleCeilings(1, 30),
            Delivery(),
            Delivery());

        outcome.Delivered.ShouldBe(1);
        outcome.Throttled.ShouldBe(1);
        harness.Sink.Deliveries.Count.ShouldBe(1);
        harness.Digest.GetPendingEntries("tenant-alpha", "operator-001").Count.ShouldBe(1);
        harness.History.GetImmediatePushTimestamps("tenant-alpha", "operator-001").Count.ShouldBe(1);
    }

    [Fact]
    public async Task RedactedDeliveryEnvelopeOmitsItemRefIndistinguishableFromNotFound()
    {
        Harness harness = new();
        await harness.RunAsync(
            new NotificationThrottleCeilings(0, 30),
            Delivery(visibility: NotificationContentVisibility.MetadataRedacted, itemRef: "item-secret-9"));

        AuditEnvelope envelope = harness.Audit.Envelopes.ShouldHaveSingleItem();
        // NFR2: a redacted delivery's audit envelope never carries a per-resource item ref — the audit trail must stay
        // indistinguishable from safe-not-found and never become a covert channel for what the push would have redacted.
        envelope.SourceEvidenceRefs.ShouldNotContain(r => r.StartsWith("notification-item:", StringComparison.Ordinal));

        string json = JsonSerializer.Serialize(harness.Audit.Envelopes, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        json.ShouldNotContain("item-secret-9");
    }

    [Fact]
    public async Task ItemContextDeliveryEnvelopeCarriesTheSafeItemRef()
    {
        Harness harness = new();
        await harness.RunAsync(
            NotificationThrottleCeilings.SafeDefaults,
            Delivery(visibility: NotificationContentVisibility.ItemContext, itemRef: "item-77"));

        // For an authorized ItemContext delivery the safe item ref is preserved in the metadata-only envelope.
        harness.Audit.Envelopes.ShouldHaveSingleItem()
            .SourceEvidenceRefs.ShouldContain("notification-item:item-77");
    }

    [Fact]
    public async Task PerRecipientThrottlingIsIsolatedWithinTheSameTenant()
    {
        Harness harness = new();
        // Same tenant, two recipients: ceiling 0 throttles operator-001; operator-002 has its own history/digest and delivers.
        await harness.RunAsync(new NotificationThrottleCeilings(0, 30), Delivery(recipientRef: "operator-001"));
        await harness.RunAsync(NotificationThrottleCeilings.SafeDefaults, Delivery(recipientRef: "operator-002"));

        // One recipient's volume never throttles or leaks into another's digest/history, even under the same tenant.
        harness.Digest.GetPendingEntries("tenant-alpha", "operator-001").Count.ShouldBe(1);
        harness.Digest.GetPendingEntries("tenant-alpha", "operator-002").ShouldBeEmpty();
        harness.History.GetImmediatePushTimestamps("tenant-alpha", "operator-001").ShouldBeEmpty();
        harness.History.GetImmediatePushTimestamps("tenant-alpha", "operator-002").Count.ShouldBe(1);
    }

    [Fact]
    public async Task SuccessiveOverflowsAccumulateAndRolledUpCountSnapshotIncrements()
    {
        Harness harness = new();
        // Ceiling 0 — every delivery overflows; entries accumulate and the audit rolled-up-count snapshot grows 1,2,3.
        await harness.RunAsync(new NotificationThrottleCeilings(0, 30), Delivery(), Delivery(), Delivery());

        harness.Digest.GetPendingEntries("tenant-alpha", "operator-001").Count.ShouldBe(3);
        harness.Audit.Envelopes
            .Select(static e => e.SourceEvidenceRefs.First(static r => r.StartsWith("digest-rolled-up-count:", StringComparison.Ordinal)))
            .ShouldBe(["digest-rolled-up-count:1", "digest-rolled-up-count:2", "digest-rolled-up-count:3"]);
    }

    private static NotificationDelivery Delivery(
        string tenantRef = "tenant-alpha",
        string recipientRef = "operator-001",
        NotificationContentVisibility visibility = NotificationContentVisibility.MetadataRedacted,
        string? itemRef = null)
        => new(
            NotificationStateClass.Failure,
            NotificationChannel.OperatorAlert,
            AdminRole.OperationsAdmin,
            AdminScope.Operate,
            recipientRef,
            tenantRef,
            visibility is NotificationContentVisibility.ItemContext ? itemRef : null,
            "queue:operations",
            "review_needed",
            "01ARZ3NDEKTSV4RRFFQ69G5FAW",
            visibility,
            Clock.UtcNow);

    private sealed class Harness
    {
        public InMemoryNotificationSink Sink { get; } = new();

        public InMemoryNotificationDeliveryHistoryStore History { get; } = new();

        public InMemoryNotificationDigestStore Digest { get; } = new();

        public RecordingAuditWriter Audit { get; } = new();

        public ValueTask<NotificationThrottleOutcome> RunAsync(NotificationThrottleCeilings ceilings, params NotificationDelivery[] deliveries)
        {
            NotificationThrottleCoordinator coordinator = new(Sink, History, Digest, Audit, Clock);
            string tenantRef = deliveries.Length > 0 ? deliveries[0].TenantRef : "tenant-alpha";
            return coordinator.EvaluateAndDeliverAsync(deliveries, ceilings, tenantRef, TestContext.Current.CancellationToken);
        }
    }

    private sealed class FixedClock(DateTimeOffset now) : ISystemClock
    {
        public DateTimeOffset UtcNow { get; } = now;
    }

    private sealed class RecordingAuditWriter : IAuditWriter
    {
        public List<AuditEnvelope> Envelopes { get; } = [];

        public AuditWriteResult PreCommitResult { get; set; } = AuditWriteResult.Success;

        public ValueTask RecordAuthorizationFailureAsync(ChatBotAuthorizationFailureAuditFact fact, CancellationToken cancellationToken)
            => ValueTask.CompletedTask;

        public ValueTask<AuditWriteResult> RecordPreCommitAsync(AuditEnvelope envelope, CancellationToken cancellationToken)
        {
            Envelopes.Add(envelope);
            return ValueTask.FromResult(PreCommitResult);
        }

        public ValueTask<AuditWriteResult> RecordPostCommitAsync(AuditEnvelope envelope, CancellationToken cancellationToken)
        {
            Envelopes.Add(envelope);
            return ValueTask.FromResult(AuditWriteResult.Success);
        }
    }
}
