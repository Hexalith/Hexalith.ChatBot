using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Observability;
using Hexalith.ChatBot.Server.Projections;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Observability;

public sealed class ApprovalQueueAgeAlertEvaluatorTests
{
    private static readonly DateTimeOffset Now = new(2026, 6, 3, 12, 0, 0, TimeSpan.Zero);
    private const int Threshold = ApprovalQueueAgeAlertEvaluator.BusinessDayAlertThresholdSeconds;

    [Fact]
    public void FiresForSingleItemOverThresholdWithValidSafeTokens()
    {
        OperationalAlertPayload? alert = ApprovalQueueAgeAlertEvaluator.Evaluate(
            [PendingItem("i-1", Threshold + 1)], "tenant-alpha", "01ARZ3NDEKTSV4RRFFQ69G5FAW", Now);

        alert.ShouldNotBeNull();
        alert.AlertKind.ShouldBe(OperatorAlertKind.ApprovalQueueAgeBreached);
        alert.ReasonCode.ShouldBe("approval_queue_age_threshold_exceeded");
        alert.OwnerRole.ShouldBe("operations-admin");
        alert.NextSafeAction.ShouldBe("review-approval-queue");
        alert.AffectedScope.ShouldBe("tenant:tenant-alpha");
        OperationalAlertPayload.IsValid(alert).ShouldBeTrue();
    }

    [Fact]
    public void FiresExactlyAtThresholdInclusive()
        => ApprovalQueueAgeAlertEvaluator.Evaluate(
            [PendingItem("i-1", Threshold)], "tenant-alpha", "corr-1", Now)
            .ShouldNotBeNull();

    [Fact]
    public void SuppressesJustUnderThreshold()
        => ApprovalQueueAgeAlertEvaluator.Evaluate(
            [PendingItem("i-1", Threshold - 1)], "tenant-alpha", "corr-1", Now)
            .ShouldBeNull();

    [Fact]
    public void FiresSingleAggregateForMultipleItemsOverThreshold()
    {
        OperationalAlertPayload? alert = ApprovalQueueAgeAlertEvaluator.Evaluate(
            [PendingItem("i-1", Threshold + 10), PendingItem("i-2", Threshold + 20), PendingItem("i-3", Threshold + 30)],
            "tenant-alpha",
            "corr-1",
            Now);

        // One aggregate alert for the tenant — never one per item.
        alert.ShouldNotBeNull();
        alert.AffectedScope.ShouldBe("tenant:tenant-alpha");
    }

    [Fact]
    public void SuppressesForTerminalItemsOverThreshold()
        => ApprovalQueueAgeAlertEvaluator.Evaluate(
            [PendingItem("i-1", Threshold + 100, isTerminal: true)], "tenant-alpha", "corr-1", Now)
            .ShouldBeNull();

    [Fact]
    public void SuppressesForNonApprovalQueueFamily()
        => ApprovalQueueAgeAlertEvaluator.Evaluate(
            [PendingItem("i-1", Threshold + 100, family: OperationalQueueFamily.FailedIngestion)],
            "tenant-alpha",
            "corr-1",
            Now)
            .ShouldBeNull();

    private static AdminQueueSummaryProjectionItem PendingItem(
        string itemRef,
        int ageSeconds,
        bool isTerminal = false,
        OperationalQueueFamily family = OperationalQueueFamily.PendingApproval)
        => new(
            QueueRef: "queue:approvals",
            ItemRef: itemRef,
            Status: "pending",
            OwnerClass: "operations",
            Health: ChatBotHealthStatus.Degraded,
            AgeSeconds: ageSeconds,
            QueueFamily: family,
            IsTerminal: isTerminal);
}
