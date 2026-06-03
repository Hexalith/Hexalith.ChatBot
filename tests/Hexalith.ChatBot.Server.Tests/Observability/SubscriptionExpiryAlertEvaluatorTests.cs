using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Observability;
using Hexalith.ChatBot.Server.Projections;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Observability;

public sealed class SubscriptionExpiryAlertEvaluatorTests
{
    private static readonly DateTimeOffset FiredAt = new(2026, 6, 3, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void FiresPerDistinctMailboxForSubscriptionExpiry()
    {
        IReadOnlyList<OperationalAlertPayload> alerts = SubscriptionExpiryAlertEvaluator.Evaluate(
            [
                FailedItem("mb-1", "graph-subscription-expired"),
                FailedItem("mb-1", "graph-subscription-expired"),
                FailedItem("mb-2", "graph-subscription-expired"),
            ],
            "tenant-alpha",
            "01ARZ3NDEKTSV4RRFFQ69G5FAW",
            FiredAt);

        // One alert per distinct affected mailbox, deterministic order.
        alerts.Count.ShouldBe(2);
        alerts[0].AffectedScope.ShouldBe("tenant:tenant-alpha mailbox:mb-1");
        alerts[1].AffectedScope.ShouldBe("tenant:tenant-alpha mailbox:mb-2");
        alerts.ShouldAllBe(a => a.AlertKind == OperatorAlertKind.SubscriptionExpiryImminent);
        alerts.ShouldAllBe(a => a.OwnerRole == "mailbox-admin");
        alerts.ShouldAllBe(a => a.ReasonCode == "subscription_expiry_threshold_exceeded");
        alerts.ShouldAllBe(a => a.NextSafeAction == "renew-graph-subscription");
        alerts.ShouldAllBe(a => OperationalAlertPayload.IsValid(a));
    }

    [Fact]
    public void AffectedScopeIsSafeTokenNeverProjectDetail()
    {
        IReadOnlyList<OperationalAlertPayload> alerts = SubscriptionExpiryAlertEvaluator.Evaluate(
            [FailedItem("mb-1", "graph-subscription-expired", projectName: "TopSecretProject")],
            "tenant-alpha",
            "corr-1",
            FiredAt);

        alerts.ShouldHaveSingleItem();
        alerts[0].AffectedScope.ShouldNotContain("TopSecretProject");
        alerts[0].AffectedScope.ShouldBe("tenant:tenant-alpha mailbox:mb-1");
    }

    [Fact]
    public void SuppressesForOtherDegradationReasons()
        => SubscriptionExpiryAlertEvaluator.Evaluate(
            [FailedItem("mb-1", "graph-throttled"), FailedItem("mb-2", "graph-token-revoked")],
            "tenant-alpha",
            "corr-1",
            FiredAt)
            .ShouldBeEmpty();

    [Fact]
    public void SuppressesForNonFailedIngestionFamily()
        => SubscriptionExpiryAlertEvaluator.Evaluate(
            [FailedItem("mb-1", "graph-subscription-expired", family: OperationalQueueFamily.PendingApproval)],
            "tenant-alpha",
            "corr-1",
            FiredAt)
            .ShouldBeEmpty();

    private static AdminQueueSummaryProjectionItem FailedItem(
        string mailboxRef,
        string failureState,
        string? projectName = null,
        OperationalQueueFamily family = OperationalQueueFamily.FailedIngestion)
        => new(
            QueueRef: "queue:ingestion",
            ItemRef: $"i-{mailboxRef}",
            Status: "failed",
            OwnerClass: "mailbox",
            Health: ChatBotHealthStatus.Degraded,
            AgeSeconds: 10,
            QueueFamily: family,
            MailboxRef: mailboxRef,
            FailureState: failureState,
            ProjectName: projectName);
}
