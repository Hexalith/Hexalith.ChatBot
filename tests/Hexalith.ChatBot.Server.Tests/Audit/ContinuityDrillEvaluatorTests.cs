using Hexalith.ChatBot.Server.Audit;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Audit;

/// <summary>
/// Story 9.11 (AC1/AC4) coverage for the pure continuity-drill verdict function: <c>met</c> iff both measured durations
/// are within the single-source <see cref="RecoveryTargets"/> and no data loss; <c>missed</c> when RPO over, RTO over,
/// or data loss (each independently and combined); the deviation list enumerates exactly the breached dimensions in a
/// stable order; and boundary equality (<c>== target</c>) is met. Also pins the A10/NFR56 targets (15 min / 4 hr).
/// </summary>
public sealed class ContinuityDrillEvaluatorTests
{
    [Fact]
    public void RecoveryTargetsArePinnedToTheA10Nfr56Assumption()
    {
        RecoveryTargets.MaxRpo.ShouldBe(TimeSpan.FromMinutes(15));
        RecoveryTargets.MaxRto.ShouldBe(TimeSpan.FromHours(4));
    }

    [Fact]
    public void MetWhenBothDurationsWithinTargetAndNoDataLoss()
    {
        string verdict = ContinuityDrillEvaluator.Evaluate(TimeSpan.FromMinutes(10), TimeSpan.FromHours(3), dataLossDetected: false);

        verdict.ShouldBe(ContinuityDrillVerdicts.Met);
        ContinuityDrillEvaluator.Deviations(TimeSpan.FromMinutes(10), TimeSpan.FromHours(3), false).ShouldBeEmpty();
    }

    [Fact]
    public void BoundaryEqualityIsMet()
    {
        // Exactly at target on both dimensions ⇒ within target (met), no deviations.
        string verdict = ContinuityDrillEvaluator.Evaluate(RecoveryTargets.MaxRpo, RecoveryTargets.MaxRto, dataLossDetected: false);

        verdict.ShouldBe(ContinuityDrillVerdicts.Met);
        ContinuityDrillEvaluator.Deviations(RecoveryTargets.MaxRpo, RecoveryTargets.MaxRto, false).ShouldBeEmpty();
    }

    [Fact]
    public void MissedWhenRpoExceeded()
    {
        TimeSpan rpo = RecoveryTargets.MaxRpo + TimeSpan.FromSeconds(1);

        ContinuityDrillEvaluator.Evaluate(rpo, TimeSpan.FromHours(1), false).ShouldBe(ContinuityDrillVerdicts.Missed);
        ContinuityDrillEvaluator.Deviations(rpo, TimeSpan.FromHours(1), false)
            .ShouldBe([ContinuityDrillEvaluator.RpoExceededDeviation]);
    }

    [Fact]
    public void MissedWhenRtoExceeded()
    {
        TimeSpan rto = RecoveryTargets.MaxRto + TimeSpan.FromSeconds(1);

        ContinuityDrillEvaluator.Evaluate(TimeSpan.FromMinutes(1), rto, false).ShouldBe(ContinuityDrillVerdicts.Missed);
        ContinuityDrillEvaluator.Deviations(TimeSpan.FromMinutes(1), rto, false)
            .ShouldBe([ContinuityDrillEvaluator.RtoExceededDeviation]);
    }

    [Fact]
    public void MissedWhenDataLossDetectedEvenIfDurationsWithinTarget()
    {
        ContinuityDrillEvaluator.Evaluate(TimeSpan.FromMinutes(1), TimeSpan.FromHours(1), dataLossDetected: true)
            .ShouldBe(ContinuityDrillVerdicts.Missed);
        ContinuityDrillEvaluator.Deviations(TimeSpan.FromMinutes(1), TimeSpan.FromHours(1), true)
            .ShouldBe([ContinuityDrillEvaluator.DataLossDeviation]);
    }

    [Fact]
    public void DeviationsEnumerateAllBreachedDimensionsInStableOrder()
    {
        TimeSpan rpo = RecoveryTargets.MaxRpo + TimeSpan.FromMinutes(5);
        TimeSpan rto = RecoveryTargets.MaxRto + TimeSpan.FromHours(1);

        ContinuityDrillEvaluator.Evaluate(rpo, rto, dataLossDetected: true).ShouldBe(ContinuityDrillVerdicts.Missed);
        ContinuityDrillEvaluator.Deviations(rpo, rto, true).ShouldBe(
        [
            ContinuityDrillEvaluator.RpoExceededDeviation,
            ContinuityDrillEvaluator.RtoExceededDeviation,
            ContinuityDrillEvaluator.DataLossDeviation,
        ]);
    }

    [Fact]
    public void EvaluatorIsDeterministicOverRepeatedCalls()
    {
        TimeSpan rpo = RecoveryTargets.MaxRpo + TimeSpan.FromMinutes(1);

        ContinuityDrillEvaluator.Evaluate(rpo, TimeSpan.FromHours(1), false)
            .ShouldBe(ContinuityDrillEvaluator.Evaluate(rpo, TimeSpan.FromHours(1), false));
    }
}
