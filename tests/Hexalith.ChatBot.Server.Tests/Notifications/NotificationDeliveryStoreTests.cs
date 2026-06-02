using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Server.Notifications;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Notifications;

/// <summary>
/// Store-level coverage for the metadata-only <c>(tenant × recipient)</c>-keyed history and digest seams: the
/// length-prefixed key must isolate ambiguous tenant/recipient pairs that a naive concatenation would collide, and
/// <see cref="INotificationDigestStore.DrainPendingDigest"/> must be destructive and isolated per pair.
/// </summary>
public sealed class NotificationDeliveryStoreTests
{
    private static readonly DateTimeOffset Now = new(2026, 6, 2, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void HistoryStoreKeyIsCollisionSafeAcrossAmbiguousTenantRecipientPairs()
    {
        InMemoryNotificationDeliveryHistoryStore store = new();
        // Naive concatenation would collapse ("ab","cd"), ("abc","d") and ("a","bcd") onto the same "abcd" key.
        store.RecordImmediatePush("ab", "cd", Now);

        store.GetImmediatePushTimestamps("ab", "cd").Count.ShouldBe(1);
        store.GetImmediatePushTimestamps("abc", "d").ShouldBeEmpty();
        store.GetImmediatePushTimestamps("a", "bcd").ShouldBeEmpty();
    }

    [Fact]
    public void DigestStoreKeyIsCollisionSafeAcrossAmbiguousTenantRecipientPairs()
    {
        InMemoryNotificationDigestStore store = new();
        store.Append(Entry("ab", "cd"));

        store.GetPendingEntries("ab", "cd").Count.ShouldBe(1);
        store.GetPendingEntries("abc", "d").ShouldBeEmpty();
        store.GetPendingEntries("a", "bcd").ShouldBeEmpty();
    }

    [Fact]
    public void DrainPendingDigestReturnsAllEntriesThenLeavesThePairEmpty()
    {
        InMemoryNotificationDigestStore store = new();
        store.Append(Entry("tenant-alpha", "operator-001"));
        store.Append(Entry("tenant-alpha", "operator-001"));

        NotificationDigest digest = store.DrainPendingDigest("tenant-alpha", "operator-001");
        digest.TenantRef.ShouldBe("tenant-alpha");
        digest.RecipientRef.ShouldBe("operator-001");
        digest.RolledUpCount.ShouldBe(2);
        digest.Entries.Count.ShouldBe(2);

        // Draining is destructive — the pending entries are gone and a second drain yields an empty digest.
        store.GetPendingEntries("tenant-alpha", "operator-001").ShouldBeEmpty();
        store.DrainPendingDigest("tenant-alpha", "operator-001").RolledUpCount.ShouldBe(0);
    }

    [Fact]
    public void DrainPendingDigestIsIsolatedPerPair()
    {
        InMemoryNotificationDigestStore store = new();
        store.Append(Entry("tenant-alpha", "operator-001"));
        store.Append(Entry("tenant-beta", "operator-001"));

        store.DrainPendingDigest("tenant-alpha", "operator-001").RolledUpCount.ShouldBe(1);
        // Beta's pending digest is untouched by draining alpha's.
        store.GetPendingEntries("tenant-beta", "operator-001").Count.ShouldBe(1);
    }

    private static NotificationDigestEntry Entry(string tenantRef, string recipientRef)
        => new(
            NotificationStateClass.Failure,
            NotificationChannel.OperatorAlert,
            AdminRole.OperationsAdmin,
            AdminScope.Operate,
            recipientRef,
            tenantRef,
            null,
            "queue:operations",
            "review_needed",
            "01ARZ3NDEKTSV4RRFFQ69G5FAW",
            NotificationContentVisibility.MetadataRedacted,
            Now);
}
