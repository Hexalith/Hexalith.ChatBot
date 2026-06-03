using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Gateway;
using Hexalith.ChatBot.Server.Observability;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Observability;

public sealed class AuthorizationFailureSpikeEvaluatorTests
{
    private static readonly DateTimeOffset Now = new(2026, 6, 3, 12, 0, 0, TimeSpan.Zero);
    private const int Baseline = AuthorizationFailureSpikeEvaluator.DefaultAuthFailureBaselineCount;

    [Fact]
    public void FiresWhenCountStrictlyExceedsBaselineWithSafeScopeOnly()
    {
        IReadOnlyList<OperationalAlertPayload> alerts = AuthorizationFailureSpikeEvaluator.Evaluate(
            [new AuthorizationFailureReading("tenant-alpha", Baseline + 1, Now)], "01ARZ3NDEKTSV4RRFFQ69G5FAW", Now);

        alerts.ShouldHaveSingleItem();
        alerts[0].AlertKind.ShouldBe(OperatorAlertKind.AuthorizationFailureSpike);
        alerts[0].ReasonCode.ShouldBe("authorization_failure_spike_detected");
        alerts[0].OwnerRole.ShouldBe("tenant-admin");
        alerts[0].NextSafeAction.ShouldBe("investigate-authorization-failures");
        // No actor/command/project detail leaks — only the tenant scope token.
        alerts[0].AffectedScope.ShouldBe("tenant:tenant-alpha");
        OperationalAlertPayload.IsValid(alerts[0]).ShouldBeTrue();
    }

    [Fact]
    public void SuppressesAtExactlyBaseline()
        => AuthorizationFailureSpikeEvaluator.Evaluate(
            [new AuthorizationFailureReading("tenant-alpha", Baseline, Now)], "corr-1", Now)
            .ShouldBeEmpty();

    [Fact]
    public void FiresPerTenantInDeterministicOrder()
    {
        IReadOnlyList<OperationalAlertPayload> alerts = AuthorizationFailureSpikeEvaluator.Evaluate(
            [
                new AuthorizationFailureReading("tenant-bravo", Baseline + 5, Now),
                new AuthorizationFailureReading("tenant-alpha", Baseline + 5, Now),
                new AuthorizationFailureReading("tenant-charlie", Baseline - 1, Now),
            ],
            "corr-1",
            Now);

        alerts.Count.ShouldBe(2);
        alerts[0].TenantRef.ShouldBe("tenant-alpha");
        alerts[1].TenantRef.ShouldBe("tenant-bravo");
    }
}

public sealed class InMemoryAuthorizationFailureCounterTests
{
    private static readonly DateTimeOffset T0 = new(2026, 6, 3, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void CountsInWindowFailuresPerTenant()
    {
        MutableClock clock = new(T0);
        InMemoryAuthorizationFailureCounter counter = new(clock);

        for (int i = 0; i < 11; i++)
        {
            counter.Record("tenant-alpha", T0);
        }

        AuthorizationFailureReading reading = counter.ReadAndReset().ShouldHaveSingleItem();
        reading.TenantId.ShouldBe("tenant-alpha");
        reading.FailureCount.ShouldBe(11);
        reading.WindowStartUtc.ShouldBe(T0);
    }

    [Fact]
    public void PrunesEventsOlderThanWindowOnRead()
    {
        MutableClock clock = new(T0);
        InMemoryAuthorizationFailureCounter counter = new(clock, windowSeconds: 600);

        for (int i = 0; i < 11; i++)
        {
            counter.Record("tenant-alpha", T0);
        }

        // Advance past the window: all T0 events are now outside [now-600, now] and must be excluded.
        clock.Set(T0.AddSeconds(700));
        counter.ReadAndReset().ShouldBeEmpty();
    }

    [Fact]
    public void SlidingWindowRetainsRecentEvents()
    {
        MutableClock clock = new(T0);
        InMemoryAuthorizationFailureCounter counter = new(clock, windowSeconds: 600);

        counter.Record("tenant-alpha", T0);
        counter.Record("tenant-alpha", T0.AddSeconds(550));

        // At T0+550 both are in-window; the older one is still within 600s of the read reference.
        clock.Set(T0.AddSeconds(550));
        counter.ReadAndReset().ShouldHaveSingleItem().FailureCount.ShouldBe(2);

        // At T0+700 only the second event remains in-window.
        clock.Set(T0.AddSeconds(700));
        counter.ReadAndReset().ShouldHaveSingleItem().FailureCount.ShouldBe(1);
    }

    [Fact]
    public void IgnoresBlankTenant()
    {
        MutableClock clock = new(T0);
        InMemoryAuthorizationFailureCounter counter = new(clock);

        counter.Record("  ", T0);
        counter.ReadAndReset().ShouldBeEmpty();
    }

    private sealed class MutableClock(DateTimeOffset now) : ISystemClock
    {
        private DateTimeOffset _now = now;

        public DateTimeOffset UtcNow => _now;

        public void Set(DateTimeOffset now) => _now = now;
    }
}
