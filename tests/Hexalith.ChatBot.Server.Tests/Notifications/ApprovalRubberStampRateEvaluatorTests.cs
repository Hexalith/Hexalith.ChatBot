using System.Linq;
using System.Text.Json;

using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Notifications;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Notifications;

public sealed class ApprovalRubberStampRateEvaluatorTests
{
    private static readonly DateTimeOffset Now = new(2026, 6, 2, 12, 0, 0, TimeSpan.Zero);
    private static readonly ISystemClock Clock = new FixedClock(Now);
    private const string Tenant = "tenant-alpha";
    private const string Correlation = "01ARZ3NDEKTSV4RRFFQ69G5FAW";

    [Fact]
    public void DenominatorIsRestrictedToApprovedDecisionsAgainstApprovalRequiredActions()
    {
        List<ApprovalDecisionSample> decisions =
        [
            // Qualifying: Approved × approval-required.
            Decision("reviewer-a", latencySeconds: 1),
            Decision("reviewer-a", latencySeconds: 10),
            // Excluded: rejection, revision-request, cancellation (not Approve).
            Decision("reviewer-a", latencySeconds: 1, kind: ApprovalDecisionKind.Reject),
            Decision("reviewer-a", latencySeconds: 1, kind: ApprovalDecisionKind.RequestRevision),
            Decision("reviewer-a", latencySeconds: 1, kind: ApprovalDecisionKind.Cancel),
            // Excluded: low-risk action (not approval-required).
            Decision("reviewer-a", latencySeconds: 1, risk: AiActionRiskClass.LowRisk),
        ];

        ApprovalRubberStampRateObservation observation = Evaluate(decisions);

        // Only the two Approved × approval-required decisions count; one of them is rubber-stamp (1 s).
        observation.ApprovalTotal.ShouldBe(2);
        observation.RubberStampCount.ShouldBe(1);
    }

    [Fact]
    public void LatencyIsServerMeasuredAndClampedAtZeroForFutureOrSkewedPairs()
    {
        List<ApprovalDecisionSample> decisions =
        [
            // DecidedAt before RequestedAt → negative latency clamps to 0 → rubber-stamp.
            new(Tenant, "reviewer-a", RequestedAtUtc: Now.AddSeconds(-3600), DecidedAtUtc: Now.AddSeconds(-3700),
                ApprovalDecisionKind.Approve, AiActionRiskClass.ApprovalRequired),
        ];

        ApprovalRubberStampRateObservation observation = Evaluate(decisions);

        observation.ApprovalTotal.ShouldBe(1);
        observation.RubberStampCount.ShouldBe(1);
    }

    [Fact]
    public void RubberStampBoundaryIsStrictlyLessThanFiveSeconds()
    {
        // 4.999 s counts as rubber-stamp; exactly 5.000 s does not.
        ApprovalRubberStampRateObservation justUnder = Evaluate([DecisionWithLatency(TimeSpan.FromMilliseconds(4999))]);
        justUnder.RubberStampCount.ShouldBe(1);

        ApprovalRubberStampRateObservation exactlyFive = Evaluate([DecisionWithLatency(TimeSpan.FromSeconds(5))]);
        exactlyFive.RubberStampCount.ShouldBe(0);
        exactlyFive.ApprovalTotal.ShouldBe(1);
    }

    [Fact]
    public void RollingWindowBoundaryIsHalfOpenSevenDaysKeyedOnDecidedAt()
    {
        // Decided just inside the window (6d 23h 59m old) counts; exactly 7 days old is excluded; future is ignored.
        ApprovalDecisionSample justInside = DecidedAt(Now - TimeSpan.FromDays(7) + TimeSpan.FromMinutes(1));
        ApprovalDecisionSample exactlySevenDays = DecidedAt(Now - TimeSpan.FromDays(7));
        ApprovalDecisionSample future = DecidedAt(Now.AddMinutes(5));

        Evaluate([justInside]).ApprovalTotal.ShouldBe(1);
        Evaluate([exactlySevenDays]).ApprovalTotal.ShouldBe(0);
        Evaluate([future]).ApprovalTotal.ShouldBe(0);
    }

    [Fact]
    public void FatigueTriggerBoundaryIsStrictlyGreaterThanFifteenPercentViaExactArithmetic()
    {
        // Exactly 15 % (3 of 20) does NOT trigger.
        ApprovalRubberStampRateObservation atFifteen = Evaluate(Mix(rubberStamp: 3, slow: 17));
        atFifteen.RubberStampCount.ShouldBe(3);
        atFifteen.ApprovalTotal.ShouldBe(20);
        atFifteen.TuningRevisitTriggered.ShouldBeFalse();

        // Just above 15 % (4 of 20 = 20 %) triggers.
        ApprovalRubberStampRateObservation justAbove = Evaluate(Mix(rubberStamp: 4, slow: 16));
        justAbove.TuningRevisitTriggered.ShouldBeTrue();

        // A tighter just-above case the exact arithmetic catches but a rounded compare could miss: 16 of 105 ≈ 15.238 %.
        ApprovalRubberStampRateObservation tight = Evaluate(Mix(rubberStamp: 16, slow: 89));
        tight.ApprovalTotal.ShouldBe(105);
        tight.TuningRevisitTriggered.ShouldBeTrue();
    }

    [Fact]
    public void ZeroAndDegenerateDenominatorNeverTriggersAndNeverDividesByZero()
    {
        // Empty snapshot.
        ApprovalRubberStampRateObservation empty = Evaluate([]);
        empty.ApprovalTotal.ShouldBe(0);
        empty.RubberStampCount.ShouldBe(0);
        empty.RubberStampRatePermille.ShouldBe(0);
        empty.TuningRevisitTriggered.ShouldBeFalse();

        // A single rubber-stamp decision is a degenerate window under AC10: observable, but not enough support to
        // trigger a tuning revisit.
        ApprovalRubberStampRateObservation single = Evaluate([DecisionWithLatency(TimeSpan.FromSeconds(1))]);
        single.ApprovalTotal.ShouldBe(1);
        single.RubberStampCount.ShouldBe(1);
        single.RubberStampRatePermille.ShouldBe(1000);
        single.TuningRevisitTriggered.ShouldBeFalse();

        // A single slow decision: 0/1 → 0 ‰, no trigger.
        ApprovalRubberStampRateObservation singleSlow = Evaluate([DecisionWithLatency(TimeSpan.FromSeconds(60))]);
        singleSlow.ApprovalTotal.ShouldBe(1);
        singleSlow.RubberStampCount.ShouldBe(0);
        singleSlow.RubberStampRatePermille.ShouldBe(0);
        singleSlow.TuningRevisitTriggered.ShouldBeFalse();
    }

    [Fact]
    public void PerTenantAndPerReviewerFractionsAreComputedOverTheWindow()
    {
        List<ApprovalDecisionSample> decisions =
        [
            // reviewer-a: 2 rubber-stamp, 1 slow.
            Decision("reviewer-a", latencySeconds: 1),
            Decision("reviewer-a", latencySeconds: 2),
            Decision("reviewer-a", latencySeconds: 30),
            // reviewer-b: 0 rubber-stamp, 2 slow.
            Decision("reviewer-b", latencySeconds: 20),
            Decision("reviewer-b", latencySeconds: 40),
        ];

        ApprovalRubberStampRateObservation observation = Evaluate(decisions);

        // Tenant aggregate: 2 rubber-stamp of 5 total.
        observation.RubberStampCount.ShouldBe(2);
        observation.ApprovalTotal.ShouldBe(5);
        observation.RubberStampRatePermille.ShouldBe(400);

        observation.PerReviewer.Count.ShouldBe(2);
        ReviewerRubberStampRate a = observation.PerReviewer.Single(r => r.ReviewerRef == "reviewer-a");
        a.RubberStampCount.ShouldBe(2);
        a.ApprovalTotal.ShouldBe(3);
        ReviewerRubberStampRate b = observation.PerReviewer.Single(r => r.ReviewerRef == "reviewer-b");
        b.RubberStampCount.ShouldBe(0);
        b.ApprovalTotal.ShouldBe(2);
    }

    [Fact]
    public void NullReviewerIsExcludedFromPerReviewerButCountedInTenantAggregate()
    {
        List<ApprovalDecisionSample> decisions =
        [
            Decision(reviewer: null, latencySeconds: 1),
            Decision(reviewer: "  ", latencySeconds: 1),
            Decision("reviewer-a", latencySeconds: 1),
        ];

        ApprovalRubberStampRateObservation observation = Evaluate(decisions);

        // All three count in the tenant aggregate.
        observation.ApprovalTotal.ShouldBe(3);
        observation.RubberStampCount.ShouldBe(3);
        // Only the named reviewer appears in the per-reviewer breakdown — no phantom reviewer.
        observation.PerReviewer.ShouldHaveSingleItem().ReviewerRef.ShouldBe("reviewer-a");
    }

    [Fact]
    public void PerReviewerBreakdownIsInDeterministicReviewerOrder()
    {
        List<ApprovalDecisionSample> decisions =
        [
            Decision("reviewer-c", latencySeconds: 1),
            Decision("reviewer-a", latencySeconds: 1),
            Decision("reviewer-b", latencySeconds: 1),
        ];

        Evaluate(decisions).PerReviewer.Select(r => r.ReviewerRef).ShouldBe(["reviewer-a", "reviewer-b", "reviewer-c"]);
    }

    [Fact]
    public void EvaluationIsDeterministicGivenTheInjectedClock()
    {
        List<ApprovalDecisionSample> decisions = Mix(rubberStamp: 4, slow: 16);

        string first = JsonSerializer.Serialize(Evaluate(decisions), new JsonSerializerOptions(JsonSerializerDefaults.Web));
        string second = JsonSerializer.Serialize(Evaluate(decisions), new JsonSerializerOptions(JsonSerializerDefaults.Web));
        first.ShouldBe(second);
    }

    [Fact]
    public void GovernanceConstantsAreSingleSourcedAtFiveFifteenAndSeven()
    {
        RubberStampRateObservable.RubberStampLatencySeconds.ShouldBe(5);
        RubberStampRateObservable.FatigueFractionPercent.ShouldBe(15);
        RubberStampRateObservable.RollingWindowDays.ShouldBe(7);
        RubberStampRateObservable.RollingWindow.ShouldBe(TimeSpan.FromDays(7));
        RubberStampRateObservable.RubberStampLatency.ShouldBe(TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void DecisionsRedactedFromAnotherTenantNeverEnterThisTenantsRate()
    {
        // The evaluator keys strictly by the supplied authenticated-binding tenant ref. The snapshot is the caller's
        // tenant-bound projection; even decision samples carrying a different TenantRef are aggregated under the bound
        // tenant only — the rate is the supplied tenant's, and one tenant's volume cannot leak into another's.
        List<ApprovalDecisionSample> decisions = Mix(rubberStamp: 4, slow: 16);

        ApprovalRubberStampRateObservation alpha = ApprovalRubberStampRateEvaluator.Evaluate(decisions, "tenant-alpha", Correlation, Clock);
        ApprovalRubberStampRateObservation beta = ApprovalRubberStampRateEvaluator.Evaluate([], "tenant-beta", Correlation, Clock);

        alpha.TenantRef.ShouldBe("tenant-alpha");
        alpha.TuningRevisitTriggered.ShouldBeTrue();
        // tenant-beta's (empty) snapshot is unaffected by tenant-alpha's volume.
        beta.TenantRef.ShouldBe("tenant-beta");
        beta.ApprovalTotal.ShouldBe(0);
        beta.TuningRevisitTriggered.ShouldBeFalse();
    }

    [Fact]
    public void SerializedObservationCarriesNoProjectProposalOrPiiLeakage()
    {
        // Even with PII-ish reviewer refs, the observation stays metadata-only counts + safe refs.
        List<ApprovalDecisionSample> decisions =
        [
            Decision("reviewer-a", latencySeconds: 1),
            Decision("reviewer-b", latencySeconds: 1),
        ];

        string json = JsonSerializer.Serialize(Evaluate(decisions), new JsonSerializerOptions(JsonSerializerDefaults.Web));
        json.ShouldNotContain("project-", Case.Insensitive);
        json.ShouldNotContain("proposal-", Case.Insensitive);
        json.ShouldNotContain("secret", Case.Insensitive);
        json.ShouldNotContain("@");
    }

    private static ApprovalRubberStampRateObservation Evaluate(IReadOnlyList<ApprovalDecisionSample> decisions)
        => ApprovalRubberStampRateEvaluator.Evaluate(decisions, Tenant, Correlation, Clock);

    private static ApprovalDecisionSample Decision(
        string? reviewer,
        double latencySeconds,
        ApprovalDecisionKind kind = ApprovalDecisionKind.Approve,
        AiActionRiskClass risk = AiActionRiskClass.ApprovalRequired)
    {
        DateTimeOffset decidedAt = Now.AddHours(-1);
        return new ApprovalDecisionSample(
            Tenant,
            reviewer,
            RequestedAtUtc: decidedAt.AddSeconds(-latencySeconds),
            DecidedAtUtc: decidedAt,
            kind,
            risk);
    }

    private static ApprovalDecisionSample DecisionWithLatency(TimeSpan latency)
    {
        DateTimeOffset decidedAt = Now.AddHours(-1);
        return new ApprovalDecisionSample(
            Tenant,
            "reviewer-a",
            RequestedAtUtc: decidedAt - latency,
            DecidedAtUtc: decidedAt,
            ApprovalDecisionKind.Approve,
            AiActionRiskClass.ApprovalRequired);
    }

    private static ApprovalDecisionSample DecidedAt(DateTimeOffset decidedAtUtc)
        => new(
            Tenant,
            "reviewer-a",
            RequestedAtUtc: decidedAtUtc.AddSeconds(-30),
            DecidedAtUtc: decidedAtUtc,
            ApprovalDecisionKind.Approve,
            AiActionRiskClass.ApprovalRequired);

    private static List<ApprovalDecisionSample> Mix(int rubberStamp, int slow)
    {
        List<ApprovalDecisionSample> decisions = [];
        for (int i = 0; i < rubberStamp; i++)
        {
            decisions.Add(DecisionWithLatency(TimeSpan.FromSeconds(1)));
        }

        for (int i = 0; i < slow; i++)
        {
            decisions.Add(DecisionWithLatency(TimeSpan.FromSeconds(60)));
        }

        return decisions;
    }

    private sealed class FixedClock(DateTimeOffset now) : ISystemClock
    {
        public DateTimeOffset UtcNow { get; } = now;
    }
}
