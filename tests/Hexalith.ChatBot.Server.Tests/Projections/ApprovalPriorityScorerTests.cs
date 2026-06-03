using System.Text.Json;

using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Contracts.Queries;
using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Projections;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Projections;

public sealed class ApprovalPriorityScorerTests
{
    private static readonly DateTimeOffset Now = new(2026, 6, 2, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void ScoreShouldRankHigherRiskAuthorityAndAgeFirst()
    {
        ApprovalPriorityWeights weights = ApprovalPriorityWeights.SafeDefaults;
        decimal high = ApprovalPriorityScorer.Score(RiskClasses.Rank(RiskClass.High), SenderAuthorityClasses.Rank(SenderAuthorityClass.SendOnBehalf), 3600, weights);
        decimal low = ApprovalPriorityScorer.Score(RiskClasses.Rank(RiskClass.Low), SenderAuthorityClasses.Rank(SenderAuthorityClass.AuthenticatedUserSend), 60, weights);
        high.ShouldBeGreaterThan(low);
    }

    [Fact]
    public void ExactlyEqualDimensionsShouldProduceEqualScores()
    {
        ApprovalEventView a = Pending("approval-a", RiskClass.High, "send-on-behalf", Now.AddHours(-1), sourceVersion: 4);
        ApprovalEventView b = Pending("approval-b", RiskClass.High, "send-on-behalf", Now.AddHours(-1), sourceVersion: 9);

        ApprovalPriorityResult resultA = ApprovalPriorityScorer.Evaluate(a, ApprovalPriorityWeights.SafeDefaults, Now);
        ApprovalPriorityResult resultB = ApprovalPriorityScorer.Evaluate(b, ApprovalPriorityWeights.SafeDefaults, Now);
        resultA.Score.ShouldBe(resultB.Score);
    }

    [Fact]
    public void WeightsShouldBeHonoredAndZeroWeightShouldDropDimension()
    {
        int riskRank = RiskClasses.Rank(RiskClass.High);
        int authorityRank = SenderAuthorityClasses.Rank(SenderAuthorityClass.SendOnBehalf);

        decimal weighted = ApprovalPriorityScorer.Score(riskRank, authorityRank, 3600, new ApprovalPriorityWeights(2.0, 2.0, 2.0));
        decimal defaultWeighted = ApprovalPriorityScorer.Score(riskRank, authorityRank, 3600, ApprovalPriorityWeights.SafeDefaults);
        weighted.ShouldBeGreaterThan(defaultWeighted);

        // Zero risk-weight collapses the risk factor to 1 — the dimension no longer contributes.
        decimal riskDropped = ApprovalPriorityScorer.Score(riskRank, authorityRank, 3600, new ApprovalPriorityWeights(0.0, 1.0, 1.0));
        decimal riskZeroRank = ApprovalPriorityScorer.Score(0, authorityRank, 3600, ApprovalPriorityWeights.SafeDefaults);
        riskDropped.ShouldBe(riskZeroRank);
    }

    [Fact]
    public void OutOfBoundsWeightsShouldFallBackToSafeDefaults()
    {
        int riskRank = RiskClasses.Rank(RiskClass.High);
        decimal rejected = ApprovalPriorityScorer.Score(riskRank, 2, 3600, new ApprovalPriorityWeights(-5.0, 1.0, 1.0));
        decimal defaults = ApprovalPriorityScorer.Score(riskRank, 2, 3600, ApprovalPriorityWeights.SafeDefaults);
        rejected.ShouldBe(defaults);
    }

    [Fact]
    public void TimeInQueueShouldBeServerMeasuredAndFutureRequestClampsToZero()
    {
        // A request timestamped in the future (client/item-supplied skew) must never inflate priority.
        ApprovalEventView future = Pending("approval-future", RiskClass.Medium, "send-on-behalf", Now.AddHours(1), sourceVersion: 1);
        ApprovalEventView nowish = Pending("approval-now", RiskClass.Medium, "send-on-behalf", Now, sourceVersion: 1);

        ApprovalPriorityResult futureResult = ApprovalPriorityScorer.Evaluate(future, ApprovalPriorityWeights.SafeDefaults, Now);
        ApprovalPriorityResult nowResult = ApprovalPriorityScorer.Evaluate(nowish, ApprovalPriorityWeights.SafeDefaults, Now);
        futureResult.Explanation.ShouldContain("age:0s");
        futureResult.Score.ShouldBe(nowResult.Score);
    }

    [Fact]
    public void GroupKeyShouldMergeOnlyOnIdenticalRequesterCommandProjectWithinTenant()
    {
        string baseKey = ApprovalPriorityScorer.GroupKey("tenant-alpha", "requester-1", "Project.AppendConversationMessage", "project-1");

        baseKey.ShouldStartWith("sha256:");
        // Identical triple within the same tenant ⇒ identical group.
        baseKey.ShouldBe(ApprovalPriorityScorer.GroupKey("tenant-alpha", "requester-1", "Project.AppendConversationMessage", "project-1"));
        // Any differing dimension ⇒ different group.
        baseKey.ShouldNotBe(ApprovalPriorityScorer.GroupKey("tenant-alpha", "requester-2", "Project.AppendConversationMessage", "project-1"));
        baseKey.ShouldNotBe(ApprovalPriorityScorer.GroupKey("tenant-alpha", "requester-1", "Other.Command", "project-1"));
        baseKey.ShouldNotBe(ApprovalPriorityScorer.GroupKey("tenant-alpha", "requester-1", "Project.AppendConversationMessage", "project-2"));
        // Different tenant ⇒ never merged.
        baseKey.ShouldNotBe(ApprovalPriorityScorer.GroupKey("tenant-beta", "requester-1", "Project.AppendConversationMessage", "project-1"));
    }

    [Fact]
    public void DecidedAndTerminalApprovalsShouldBeExcludedFromTheQueue()
    {
        foreach (ApprovalStatus terminal in new[]
                 {
                     ApprovalStatus.Approved, ApprovalStatus.Rejected, ApprovalStatus.RevisionRequested,
                     ApprovalStatus.Cancelled, ApprovalStatus.Executed, ApprovalStatus.Failed,
                 })
        {
            ApprovalEventView view = Pending("approval-x", RiskClass.High, "send-on-behalf", Now.AddHours(-1), 1) with { Status = terminal };
            ApprovalPriorityScorer.IsPending(view).ShouldBeFalse();
            ApprovalQueueItemBuilder.TryBuild(view, ApprovalPriorityWeights.SafeDefaults, new FixedClock(Now)).ShouldBeNull();
        }
    }

    [Fact]
    public void BuiltQueueRowsShouldOrderHighestFirstStablyAndStayMetadataOnly()
    {
        FixedClock clock = new(Now);
        AdminQueueSummaryProjectionItem[] items =
        [
            ApprovalQueueItemBuilder.TryBuild(Pending("approval-low", RiskClass.Low, "authenticated-user send", Now.AddMinutes(-1), 2), ApprovalPriorityWeights.SafeDefaults, clock)!,
            ApprovalQueueItemBuilder.TryBuild(Pending("approval-high", RiskClass.High, "send-on-behalf", Now.AddHours(-2), 8), ApprovalPriorityWeights.SafeDefaults, clock)!,
            ApprovalQueueItemBuilder.TryBuild(Pending("approval-mid", RiskClass.Medium, "shared-mailbox send", Now.AddMinutes(-30), 5), ApprovalPriorityWeights.SafeDefaults, clock)!,
        ];

        OperationalQueueSearchResult result = AdminQueueSummaryProjector.Search(
            new SearchOperationalQueueItems(
                OperationalQueueFamily.PendingApproval,
                PageSize: 100,
                PageToken: null,
                OperationalQueueSortKey.Priority,
                SortDescending: true,
                new OperationalQueueFilter()),
            items,
            "correlation-alpha");

        result.Rows.Count.ShouldBe(3);
        result.Rows[0].ItemRef.ShouldContain("approval-high");
        result.Rows.Select(static row => row.PriorityScore).ShouldBeInOrder(SortDirection.Descending);
        result.Rows.ShouldAllBe(static row => row.GroupKey != null && row.GroupKey.StartsWith("sha256:"));
        // Priority explanation is a single safe token (no spaces) — never a free-form sentence carrying content.
        result.Rows.ShouldAllBe(static row => !row.PriorityExplanation.Contains(' '));
        result.Rows.ShouldAllBe(static row => row.RedactionState == "metadata_only");

        string json = JsonSerializer.Serialize(result, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        json.ShouldNotContain("secret", Case.Insensitive);
    }

    [Fact]
    public void EqualScoreRowsShouldTieBreakBySourceVersionThenItemRefDeterministically()
    {
        // Two pending items with identical risk/authority/age ⇒ exactly-equal priority score (the AC9 boundary case).
        // The queue must still produce a stable total order via the projector's tie-break (source version desc → item ref).
        FixedClock clock = new(Now);
        ApprovalEventView lowerVersion = Pending("approval-tie", RiskClass.High, "send-on-behalf", Now.AddHours(-1), sourceVersion: 4);
        ApprovalEventView higherVersion = Pending("approval-tie", RiskClass.High, "send-on-behalf", Now.AddHours(-1), sourceVersion: 9);

        AdminQueueSummaryProjectionItem lower = ApprovalQueueItemBuilder.TryBuild(lowerVersion, ApprovalPriorityWeights.SafeDefaults, clock)!;
        AdminQueueSummaryProjectionItem higher = ApprovalQueueItemBuilder.TryBuild(higherVersion, ApprovalPriorityWeights.SafeDefaults, clock)!;
        lower.PriorityScore.ShouldBe(higher.PriorityScore);

        OperationalQueueSearchResult result = AdminQueueSummaryProjector.Search(
            new SearchOperationalQueueItems(
                OperationalQueueFamily.PendingApproval,
                PageSize: 100,
                PageToken: null,
                OperationalQueueSortKey.Priority,
                SortDescending: true,
                new OperationalQueueFilter()),
            [lower, higher],
            "correlation-alpha");

        // Equal score ⇒ higher source version sorts first; ordering is deterministic, not insertion-dependent.
        result.Rows.Count.ShouldBe(2);
        result.Rows[0].SourceVersion.ShouldBe(9);
        result.Rows[1].SourceVersion.ShouldBe(4);

        // Re-running with the inputs reversed yields the identical order (stable across pages/insertion order).
        OperationalQueueSearchResult reversed = AdminQueueSummaryProjector.Search(
            new SearchOperationalQueueItems(
                OperationalQueueFamily.PendingApproval,
                PageSize: 100,
                PageToken: null,
                OperationalQueueSortKey.Priority,
                SortDescending: true,
                new OperationalQueueFilter()),
            [higher, lower],
            "correlation-alpha");
        reversed.Rows.Select(static row => row.SourceVersion).ShouldBe(result.Rows.Select(static row => row.SourceVersion));
    }

    [Fact]
    public void VeryOldRequestShouldClampTimeInQueueToTheBoundedMaximum()
    {
        // Server-measured age is clamped at the upper bound so an ancient (or skewed) request cannot inflate the score
        // without bound — the boundary partner to the future→0 lower clamp.
        long beyondMax = ApprovalPriorityScorer.MaxTimeInQueueSeconds + (10L * 24 * 60 * 60);
        ApprovalEventView ancient = Pending("approval-ancient", RiskClass.Medium, "send-on-behalf", Now.AddSeconds(-beyondMax), sourceVersion: 1);
        ApprovalEventView exactlyMax = Pending("approval-max", RiskClass.Medium, "send-on-behalf", Now.AddSeconds(-ApprovalPriorityScorer.MaxTimeInQueueSeconds), sourceVersion: 1);

        ApprovalPriorityResult ancientResult = ApprovalPriorityScorer.Evaluate(ancient, ApprovalPriorityWeights.SafeDefaults, Now);
        ApprovalPriorityResult maxResult = ApprovalPriorityScorer.Evaluate(exactlyMax, ApprovalPriorityWeights.SafeDefaults, Now);

        ancientResult.Explanation.ShouldContain($"age:{ApprovalPriorityScorer.MaxTimeInQueueSeconds}s");
        ancientResult.Score.ShouldBe(maxResult.Score);
    }

    [Fact]
    public void ExplanationAndGroupKeyShouldNotLeakRequesterCommandOrProjectPlaintext()
    {
        // NFR2/AC5: the priority explanation and group fingerprint are metadata-only and must never carry the raw
        // requester id, command name, or project id as recoverable plaintext.
        ApprovalEventView view = Pending("approval-secretcheck", RiskClass.High, "send-on-behalf", Now.AddHours(-1), sourceVersion: 1);
        ApprovalPriorityResult result = ApprovalPriorityScorer.Evaluate(view, ApprovalPriorityWeights.SafeDefaults, Now);

        result.Explanation.ShouldNotContain("requester-1");
        result.Explanation.ShouldNotContain("project-1");
        result.Explanation.ShouldNotContain("Project.AppendConversationMessage");
        // The group key is an opaque sha256 fingerprint — no canonical-triple plaintext survives in it.
        result.GroupKey.ShouldStartWith("sha256:");
        result.GroupKey.ShouldNotContain("requester-1");
        result.GroupKey.ShouldNotContain("project-1");
        result.GroupKey.ShouldNotContain("Project.AppendConversationMessage");
    }

    [Fact]
    public void TryBuildShouldPopulateRunbookRealDiagnosticContextFromTheApprovalView()
    {
        // Story 8.5 AC7: the sole AdminQueueSummaryProjectionItem construction site must carry the new NFR44
        // diagnostic fields (correlation, tenant, last-transition triple) from the genuinely-carried approval/spine
        // context — proving the view→item→diagnostic chain (not just hand-built items) yields a runbook-complete
        // diagnostic with no stubs and no fabricated values.
        DateTimeOffset requestedAt = Now.AddMinutes(-15);
        ApprovalEventView view = Pending("approval-runbook", RiskClass.High, "send-on-behalf", requestedAt, sourceVersion: 3);

        AdminQueueSummaryProjectionItem item = ApprovalQueueItemBuilder.TryBuild(view, ApprovalPriorityWeights.SafeDefaults, new FixedClock(Now))!;

        item.CorrelationId.ShouldBe("correlation-alpha");
        item.TenantRef.ShouldBe("tenant-alpha");
        item.LastTransitionFromState.ShouldBe("request");
        item.LastTransitionActor.ShouldBe("requester-1");
        item.LastTransitionTimestampUtc.ShouldBe(requestedAt);

        // End-to-end: the projector turns the wired context into a runbook-complete per-item diagnostic.
        OperationalQueueSearchResult result = AdminQueueSummaryProjector.Search(
            new SearchOperationalQueueItems(
                OperationalQueueFamily.PendingApproval,
                PageSize: 100,
                PageToken: null,
                OperationalQueueSortKey.Priority,
                SortDescending: true,
                new OperationalQueueFilter()),
            [item],
            "correlation-alpha");

        OperationalQueueDiagnostics diagnostics = result.Rows.Single().Diagnostics;
        diagnostics.CorrelationId.ShouldBe("correlation-alpha");
        diagnostics.TenantRef.ShouldBe("tenant-alpha");
        diagnostics.LastTransition.ShouldBe($"from:request|actor:requester-1|at:{requestedAt.ToUnixTimeSeconds()}");
        RunbookDiagnosticCompletenessValidator.IsComplete(diagnostics).ShouldBeTrue();
    }

    private static ApprovalEventView Pending(
        string approvalId,
        RiskClass riskClass,
        string senderAuthorityClass,
        DateTimeOffset requestedAtUtc,
        long sourceVersion)
        => new(
            TenantId: "tenant-alpha",
            ProjectId: "project-1",
            ApprovalId: approvalId,
            EventKind: ApprovalEventKind.Request,
            Status: ApprovalStatus.Pending,
            OccurredAtUtc: requestedAtUtc,
            SourceVersion: sourceVersion,
            CorrelationId: "correlation-alpha",
            RequesterId: "requester-1",
            RequestedAtUtc: requestedAtUtc,
            CommandName: "Project.AppendConversationMessage",
            RiskClass: riskClass,
            SenderAuthorityClass: senderAuthorityClass);

    private sealed class FixedClock(DateTimeOffset now) : ISystemClock
    {
        public DateTimeOffset UtcNow { get; } = now;
    }
}
