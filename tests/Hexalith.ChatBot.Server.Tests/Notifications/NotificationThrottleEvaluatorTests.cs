using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Notifications;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Notifications;

public sealed class NotificationThrottleEvaluatorTests
{
    private static readonly DateTimeOffset Now = new(2026, 6, 2, 12, 0, 0, TimeSpan.Zero);
    private static readonly ISystemClock Clock = new FixedClock(Now);
    private static readonly NotificationThrottleCeilings Ceilings = NotificationThrottleCeilings.SafeDefaults; // 8/hr, 30/day

    [Fact]
    public void UnderBothCeilingsShouldDeliver()
        => NotificationThrottleEvaluator.Decide(PushesWithinLastMinutes(7), Ceilings, Clock)
            .ShouldBe(NotificationThrottleDecision.Deliver);

    [Fact]
    public void EighthDeliveryInTheHourShouldDeliverAndNinthShouldThrottle()
    {
        // 7 prior pushes in the hour → the 8th (prior count 7 < 8) delivers.
        NotificationThrottleEvaluator.Decide(PushesWithinLastMinutes(7), Ceilings, Clock)
            .ShouldBe(NotificationThrottleDecision.Deliver);

        // 8 prior pushes in the hour → the 9th (prior count 8, not < 8) throttles, even though under the daily ceiling.
        NotificationThrottleEvaluator.Decide(PushesWithinLastMinutes(8), Ceilings, Clock)
            .ShouldBe(NotificationThrottleDecision.ThrottleToDigest);
    }

    [Fact]
    public void DailyCeilingShouldThrottleEvenWhenUnderTheHourlyCeiling()
    {
        // 30 pushes spread across the last 23 hours (≤ 1 in the most recent hour) — under hourly, at the daily ceiling.
        List<DateTimeOffset> pushes = [];
        for (int i = 0; i < 30; i++)
        {
            pushes.Add(Now.AddMinutes(-((i * 45) + 90))); // all older than an hour, within 24h
        }

        NotificationThrottleEvaluator.CountInTrailingWindow(pushes, Now, NotificationThrottleEvaluator.HourWindow).ShouldBe(0);
        NotificationThrottleEvaluator.CountInTrailingWindow(pushes, Now, NotificationThrottleEvaluator.DayWindow).ShouldBe(30);
        NotificationThrottleEvaluator.Decide(pushes, Ceilings, Clock).ShouldBe(NotificationThrottleDecision.ThrottleToDigest);
    }

    [Fact]
    public void DeliveryExactlyAtTheWindowEdgeShouldBeOutsideTheWindow()
    {
        // A delivery exactly 3600s old has aged out of the hourly window (strictly-less-than).
        NotificationThrottleEvaluator.CountInTrailingWindow([Now.AddHours(-1)], Now, NotificationThrottleEvaluator.HourWindow).ShouldBe(0);
        NotificationThrottleEvaluator.CountInTrailingWindow([Now.AddSeconds(-3599)], Now, NotificationThrottleEvaluator.HourWindow).ShouldBe(1);

        // A delivery exactly 86400s old has aged out of the daily window.
        NotificationThrottleEvaluator.CountInTrailingWindow([Now.AddHours(-24)], Now, NotificationThrottleEvaluator.DayWindow).ShouldBe(0);
        NotificationThrottleEvaluator.CountInTrailingWindow([Now.AddSeconds(-86399)], Now, NotificationThrottleEvaluator.DayWindow).ShouldBe(1);
    }

    [Fact]
    public void WindowIsServerMeasuredSoFutureTimestampsAreIgnored()
    {
        // Item/client-supplied timestamps in the future (clock skew / spoofing) never count toward the ceiling.
        NotificationThrottleEvaluator.CountInTrailingWindow([Now.AddMinutes(5)], Now, NotificationThrottleEvaluator.HourWindow).ShouldBe(0);
    }

    [Fact]
    public void DecisionIsPureGivenTheInjectedClock()
    {
        IReadOnlyList<DateTimeOffset> pushes = PushesWithinLastMinutes(8);
        NotificationThrottleDecision first = NotificationThrottleEvaluator.Decide(pushes, Ceilings, Clock);
        NotificationThrottleDecision second = NotificationThrottleEvaluator.Decide(pushes, Ceilings, Clock);
        first.ShouldBe(second);
        first.ShouldBe(NotificationThrottleDecision.ThrottleToDigest);
    }

    [Fact]
    public void OutOfBoundsCeilingSetFailsClosedToTheNfr46Maximums()
    {
        // An above-maximum ceiling set never raises the cap — it falls back to 8/30, so the 9th hourly push still throttles.
        NotificationThrottleCeilings rogue = new(9999, 9999);
        rogue.IsWithinBounds.ShouldBeFalse();
        NotificationThrottleEvaluator.Decide(PushesWithinLastMinutes(8), rogue, Clock)
            .ShouldBe(NotificationThrottleDecision.ThrottleToDigest);
    }

    [Fact]
    public void ATenantLoweredCeilingThrottlesSooner()
    {
        // A tenant may lower the ceiling: at hourly 3, the 4th delivery (prior count 3) throttles.
        NotificationThrottleCeilings lowered = new(3, 30);
        NotificationThrottleEvaluator.Decide(PushesWithinLastMinutes(2), lowered, Clock).ShouldBe(NotificationThrottleDecision.Deliver);
        NotificationThrottleEvaluator.Decide(PushesWithinLastMinutes(3), lowered, Clock).ShouldBe(NotificationThrottleDecision.ThrottleToDigest);
    }

    private static IReadOnlyList<DateTimeOffset> PushesWithinLastMinutes(int count)
    {
        List<DateTimeOffset> pushes = [];
        for (int i = 0; i < count; i++)
        {
            pushes.Add(Now.AddMinutes(-(i + 1))); // all within the trailing hour
        }

        return pushes;
    }

    private sealed class FixedClock(DateTimeOffset now) : ISystemClock
    {
        public DateTimeOffset UtcNow { get; } = now;
    }
}
