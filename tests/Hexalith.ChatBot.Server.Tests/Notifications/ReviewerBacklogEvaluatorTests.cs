using System.Linq;
using System.Security.Claims;
using System.Text.Json;

using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Gateway.Stages;
using Hexalith.ChatBot.Server.Notifications;
using Hexalith.ChatBot.Server.Projections;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Notifications;

public sealed class ReviewerBacklogEvaluatorTests
{
    private static readonly DateTimeOffset Now = new(2026, 6, 2, 12, 0, 0, TimeSpan.Zero);
    private static readonly ISystemClock Clock = new FixedClock(Now);

    [Fact]
    public void ExactlyAtThresholdDoesNotAlertAndTheNextItemCrosses()
    {
        // Default threshold = 25 (the NFR46 cap). Strictly-greater-than: 25 open items → no alert.
        IReadOnlyList<ReviewerBacklogAlert> atThreshold = Evaluate(OpenItems("reviewer-a", 25), ReviewerBacklogThreshold.SafeDefault);
        atThreshold.ShouldBeEmpty();

        // The 26th open item crosses the boundary.
        IReadOnlyList<ReviewerBacklogAlert> overThreshold = Evaluate(OpenItems("reviewer-a", 26), ReviewerBacklogThreshold.SafeDefault);
        ReviewerBacklogAlert alert = overThreshold.ShouldHaveSingleItem();
        alert.BacklogDepth.ShouldBe(26);
        alert.Threshold.ShouldBe(25);
        alert.ReviewerRef.ShouldBe("reviewer-a");
    }

    [Fact]
    public void TerminalDecidedAndResolvedItemsAreExcludedFromTheOpenCount()
    {
        List<AdminQueueSummaryProjectionItem> items =
        [
            .. OpenItems("reviewer-a", 11),
            Item("reviewer-a", status: "approved"),
            Item("reviewer-a", status: "rejected"),
            Item("reviewer-a", status: "revision-requested"),
            Item("reviewer-a", status: "RevisionRequested"),
            Item("reviewer-a", status: "cancelled"),
            Item("reviewer-a", status: "executed"),
            Item("reviewer-a", status: "failed"),
            Item("reviewer-a", status: "Skipped"),
            Item("reviewer-a", status: "resolved"),
            Item("reviewer-a", status: "pending", isTerminal: true),
        ];

        // Only the 11 open items count; with threshold 10 → alert with depth exactly 11 (terminal items never inflate it).
        ReviewerBacklogAlert alert = Evaluate(items, new ReviewerBacklogThreshold(10)).ShouldHaveSingleItem();
        alert.BacklogDepth.ShouldBe(11);
    }

    [Fact]
    public void NonPendingApprovalFamilyItemsAreNotCounted()
    {
        List<AdminQueueSummaryProjectionItem> items =
        [
            .. OpenItems("reviewer-a", 11),
            Item("reviewer-a", family: OperationalQueueFamily.RetryableOperation),
            Item("reviewer-a", family: OperationalQueueFamily.FailedIngestion),
        ];

        Evaluate(items, new ReviewerBacklogThreshold(10)).ShouldHaveSingleItem().BacklogDepth.ShouldBe(11);
    }

    [Fact]
    public void OldestItemAgeIsServerMeasuredAndIgnoresItemSuppliedAndFutureTime()
    {
        List<AdminQueueSummaryProjectionItem> items =
        [
            // Server-measured age via freshness: 3600s old.
            Item("reviewer-a", freshness: Now.AddSeconds(-3600), ageSeconds: 5),
            // A future freshness timestamp clamps to 0 — never trusts item/client time.
            Item("reviewer-a", freshness: Now.AddSeconds(600), ageSeconds: 99999),
            .. OpenItems("reviewer-a", 9),
        ];

        ReviewerBacklogAlert alert = Evaluate(items, new ReviewerBacklogThreshold(10)).ShouldHaveSingleItem();
        alert.BacklogDepth.ShouldBe(11);
        // The oldest open item's server-measured age dominates; the future-stamped item contributes 0, never 99999.
        alert.OldestItemAgeSeconds.ShouldBe(3600);
    }

    [Fact]
    public void UnassignedItemsCreateNoReviewerBacklog()
    {
        List<AdminQueueSummaryProjectionItem> items =
        [
            Item(assignee: null),
            Item(assignee: "  "),
            .. Enumerable.Range(0, 30).Select(_ => Item(assignee: null)),
        ];

        // 31 open but unassigned items never attribute to a real reviewer — no phantom backlog.
        Evaluate(items, new ReviewerBacklogThreshold(10)).ShouldBeEmpty();
    }

    [Fact]
    public void AlertIsTheAggregateRedactedFormCarryingNoItemContext()
    {
        ReviewerBacklogAlert alert = Evaluate(OpenItems("reviewer-a", 26), ReviewerBacklogThreshold.SafeDefault).ShouldHaveSingleItem();

        alert.Notification.Visibility.ShouldBe(NotificationContentVisibility.MetadataRedacted);
        alert.Notification.ItemRef.ShouldBeNull();
        alert.Notification.StateClass.ShouldBe(NotificationStateClass.ApprovalPending);
        alert.Notification.Channel.ShouldBe(NotificationChannel.InApp);
    }

    [Fact]
    public void SerializedAlertContainsNoItemProjectEvidenceOrPiiLeakage()
    {
        // Even with project-shaped item refs and a PII-ish assignee, the aggregate alert leaks none of it.
        List<AdminQueueSummaryProjectionItem> items = Enumerable.Range(0, 26)
            .Select(i => Item("reviewer-a", itemRef: $"project-acme-item-{i}"))
            .ToList();

        IReadOnlyList<ReviewerBacklogAlert> alerts = Evaluate(items, ReviewerBacklogThreshold.SafeDefault);
        string json = JsonSerializer.Serialize(alerts, new JsonSerializerOptions(JsonSerializerDefaults.Web));

        json.ShouldNotContain("project-acme-item-0");
        json.ShouldNotContain("project-", Case.Insensitive);
        json.ShouldNotContain("item-", Case.Insensitive);
        json.ShouldNotContain("secret", Case.Insensitive);
        json.ShouldNotContain("@");
    }

    [Fact]
    public void AggregationIsIsolatedPerReviewerWithinATenant()
    {
        List<AdminQueueSummaryProjectionItem> items =
        [
            .. OpenItems("reviewer-a", 26),  // over threshold
            .. OpenItems("reviewer-b", 5),   // under threshold
        ];

        // Only reviewer-a alerts; reviewer-b's small backlog neither triggers nor inflates reviewer-a's.
        ReviewerBacklogAlert alert = Evaluate(items, ReviewerBacklogThreshold.SafeDefault).ShouldHaveSingleItem();
        alert.ReviewerRef.ShouldBe("reviewer-a");
        alert.BacklogDepth.ShouldBe(26);
    }

    [Fact]
    public void TheResolvedRecipientIsTheTenantAdminAndNeverTheReviewer()
    {
        IReadOnlyList<ReviewerBacklogAlert> alerts = ReviewerBacklogEvaluator.Evaluate(
            OpenItems("reviewer-a", 26),
            [Candidate("reviewer-a", "operations-admin"), Candidate("admin-001", "tenant-admin")],
            "tenant-alpha",
            "01ARZ3NDEKTSV4RRFFQ69G5FAW",
            ReviewerBacklogThreshold.SafeDefault,
            Clock);

        ReviewerBacklogAlert alert = alerts.ShouldHaveSingleItem();
        alert.Notification.RecipientRole.ShouldBe(AdminRole.TenantAdmin);
        alert.Notification.RecipientRef.ShouldBe("admin-001");
        // The reviewer is never alerted about their own backlog, even when present as a (non-tenant-admin) candidate.
        alerts.ShouldNotContain(a => a.Notification.RecipientRef == "reviewer-a");
    }

    [Fact]
    public void OnlyTenantAdminCandidatesReceiveTheAlert()
    {
        // No tenant-admin candidate → no recipient resolves → no alert delivered.
        IReadOnlyList<ReviewerBacklogAlert> alerts = ReviewerBacklogEvaluator.Evaluate(
            OpenItems("reviewer-a", 26),
            [Candidate("operator-001", "operations-admin")],
            "tenant-alpha",
            "01ARZ3NDEKTSV4RRFFQ69G5FAW",
            ReviewerBacklogThreshold.SafeDefault,
            Clock);

        alerts.ShouldBeEmpty();
    }

    [Fact]
    public void ALoweredThresholdAlertsSoonerWhileTheCapStillBounds()
    {
        // A tenant may lower the threshold to be alerted sooner: at threshold 10, an 11-item backlog alerts.
        Evaluate(OpenItems("reviewer-a", 11), new ReviewerBacklogThreshold(10)).ShouldHaveSingleItem().BacklogDepth.ShouldBe(11);
        Evaluate(OpenItems("reviewer-a", 10), new ReviewerBacklogThreshold(10)).ShouldBeEmpty();
    }

    [Fact]
    public void AnOutOfBoundsThresholdFallsBackToTheSafeDefault()
    {
        // An above-cap threshold can never raise the bar — it falls back to the NFR46 maximum (25), so 26 still alerts
        // and 25 still does not.
        ReviewerBacklogThreshold aboveCap = new(1000);
        Evaluate(OpenItems("reviewer-a", 26), aboveCap).ShouldHaveSingleItem().Threshold.ShouldBe(25);
        Evaluate(OpenItems("reviewer-a", 25), aboveCap).ShouldBeEmpty();
    }

    [Fact]
    public void EvaluationIsDeterministicGivenTheInjectedClock()
    {
        IReadOnlyList<ReviewerBacklogAlert> first = Evaluate(OpenItems("reviewer-a", 26), ReviewerBacklogThreshold.SafeDefault);
        IReadOnlyList<ReviewerBacklogAlert> second = Evaluate(OpenItems("reviewer-a", 26), ReviewerBacklogThreshold.SafeDefault);

        JsonSerializer.Serialize(first, new JsonSerializerOptions(JsonSerializerDefaults.Web))
            .ShouldBe(JsonSerializer.Serialize(second, new JsonSerializerOptions(JsonSerializerDefaults.Web)));
    }

    [Fact]
    public void AlertCarriesTheBoundTenantRefNeverAnItemDerivedTenant()
    {
        // The tenant ref comes from the authenticated binding (the evaluator argument), never from item/project refs.
        // Even with project-shaped item refs, the alert's delivery tenant ref is exactly the supplied binding.
        List<AdminQueueSummaryProjectionItem> items = Enumerable.Range(0, 26)
            .Select(i => Item("reviewer-a", itemRef: $"tenant-beta-project-{i}"))
            .ToList();

        IReadOnlyList<ReviewerBacklogAlert> alerts = ReviewerBacklogEvaluator.Evaluate(
            items,
            [Candidate("admin-001", "tenant-admin")],
            "tenant-alpha",
            "01ARZ3NDEKTSV4RRFFQ69G5FAW",
            ReviewerBacklogThreshold.SafeDefault,
            Clock);

        alerts.ShouldHaveSingleItem().Notification.TenantRef.ShouldBe("tenant-alpha");
    }

    [Fact]
    public void MultipleReviewersOverThresholdEachProduceAnIsolatedAlertInDeterministicOrder()
    {
        // Two reviewers both over threshold → two independent alerts, keyed strictly per (tenant × reviewer) and
        // emitted in deterministic reviewer-ref order. One reviewer's depth never inflates or suppresses the other's.
        List<AdminQueueSummaryProjectionItem> items =
        [
            .. OpenItems("reviewer-b", 30),
            .. OpenItems("reviewer-a", 26),
            .. OpenItems("reviewer-c", 5),   // under threshold — no alert
        ];

        IReadOnlyList<ReviewerBacklogAlert> alerts = Evaluate(items, ReviewerBacklogThreshold.SafeDefault);

        alerts.Count.ShouldBe(2);
        alerts.Select(a => a.ReviewerRef).ShouldBe(["reviewer-a", "reviewer-b"]);
        alerts.Single(a => a.ReviewerRef == "reviewer-a").BacklogDepth.ShouldBe(26);
        alerts.Single(a => a.ReviewerRef == "reviewer-b").BacklogDepth.ShouldBe(30);
    }

    private static IReadOnlyList<ReviewerBacklogAlert> Evaluate(
        IReadOnlyList<AdminQueueSummaryProjectionItem> items,
        ReviewerBacklogThreshold threshold)
        => ReviewerBacklogEvaluator.Evaluate(
            items,
            [Candidate("admin-001", "tenant-admin")],
            "tenant-alpha",
            "01ARZ3NDEKTSV4RRFFQ69G5FAW",
            threshold,
            Clock);

    private static List<AdminQueueSummaryProjectionItem> OpenItems(string reviewer, int count)
        => Enumerable.Range(0, count).Select(i => Item(reviewer, itemRef: $"i-{reviewer}-{i}")).ToList();

    private static AdminQueueSummaryProjectionItem Item(
        string? assignee = "reviewer-a",
        string itemRef = "i-1",
        string status = "pending",
        int ageSeconds = 100,
        OperationalQueueFamily family = OperationalQueueFamily.PendingApproval,
        bool isTerminal = false,
        DateTimeOffset? freshness = null)
        => new(
            QueueRef: "queue:approvals",
            ItemRef: itemRef,
            Status: status,
            OwnerClass: "operations",
            Health: ChatBotHealthStatus.Degraded,
            AgeSeconds: ageSeconds,
            QueueFamily: family,
            AssigneeRef: assignee,
            IsTerminal: isTerminal,
            FreshnessTimestampUtc: freshness);

    private static NotificationRecipientCandidate Candidate(string recipientRef, string role)
    {
        List<Claim> claims =
        [
            new Claim("sub", recipientRef),
            new Claim(ParticipantAuthorizationStage.ActorTypeClaim, ParticipantAuthorizationStage.HumanActorValue),
            new Claim(ParticipantAuthorizationStage.TenantRoleClaim, role),
        ];
        return new NotificationRecipientCandidate(recipientRef, new ClaimsPrincipal(new ClaimsIdentity(claims, "test")));
    }

    private sealed class FixedClock(DateTimeOffset now) : ISystemClock
    {
        public DateTimeOffset UtcNow { get; } = now;
    }
}
