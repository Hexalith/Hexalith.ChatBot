using Hexalith.ChatBot.Server.Lifecycle.Workflows;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Lifecycle.Workflows;

/// <summary>
/// Story 9.6 (AC2, NFR17a) coverage for the define-once <see cref="CorrectionPropagationSlo"/>: the M2 deadline is
/// startedAt + 60 min, the M0/M1 deadline is startedAt + 10 min, <see cref="CorrectionPropagationSlo.IsBreached"/> is
/// true iff now is strictly past the deadline, and the boundary (now == deadline) is not breached.
/// </summary>
public sealed class CorrectionPropagationSloTests
{
    private static readonly DateTimeOffset StartedAt = new(2026, 6, 3, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public void M2DeadlineIsSixtyMinutesAfterStart()
    {
        CorrectionPropagationSlo.M2P95Target.ShouldBe(TimeSpan.FromMinutes(60));
        CorrectionPropagationSlo.DeadlineFor(CorrectionPropagationScope.M2, StartedAt)
            .ShouldBe(StartedAt.AddMinutes(60));
    }

    [Fact]
    public void M0M1DeadlineIsTenMinutesAfterStart()
    {
        CorrectionPropagationSlo.M0M1P95Target.ShouldBe(TimeSpan.FromMinutes(10));
        CorrectionPropagationSlo.DeadlineFor(CorrectionPropagationScope.M0M1, StartedAt)
            .ShouldBe(StartedAt.AddMinutes(10));
    }

    [Fact]
    public void IsBreachedIsTrueOnlyStrictlyAfterTheDeadline()
    {
        DateTimeOffset deadline = CorrectionPropagationSlo.DeadlineFor(CorrectionPropagationScope.M2, StartedAt);

        CorrectionPropagationSlo.IsBreached(deadline, deadline.AddSeconds(1)).ShouldBeTrue();
        CorrectionPropagationSlo.IsBreached(deadline, deadline.AddMinutes(-1)).ShouldBeFalse();
    }

    [Fact]
    public void TheDeadlineBoundaryIsNotBreached()
    {
        DateTimeOffset deadline = CorrectionPropagationSlo.DeadlineFor(CorrectionPropagationScope.M2, StartedAt);

        // now == deadline is on-time, not a breach.
        CorrectionPropagationSlo.IsBreached(deadline, deadline).ShouldBeFalse();
    }
}
