using Hexalith.ChatBot.Server.Operations.PeriodicEnforcement;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Operations.PeriodicEnforcement;

/// <summary>Verifies that clock regressions cannot rewind scheduler success evidence.</summary>
public sealed class InMemoryPeriodicEnforcementStatusStoreTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 28, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void OverallSuccessDoesNotMoveBackwardsAndStillCompletesTheCurrentAttempt()
    {
        InMemoryPeriodicEnforcementStatusStore store = new();
        store.RecordSucceeded(Now, TimeSpan.FromSeconds(1));
        store.RecordStarted(Now.AddHours(-1), "older");
        store.RecordSucceeded(Now.AddHours(-1), TimeSpan.FromSeconds(2));

        store.Read().LastSucceededAtUtc.ShouldBe(Now);
        store.Read().IsRunning.ShouldBeFalse();

        store.RecordSucceeded(Now.AddHours(1), TimeSpan.FromSeconds(3));
        store.Read().LastSucceededAtUtc.ShouldBe(Now.AddHours(1));
    }

    [Fact]
    public void M2ClockRegressionPreservesTheWholeSuccessSnapshotAndLatestAttempt()
    {
        const string job = M2SweepJobs.WormAuditChain;
        InMemoryPeriodicEnforcementStatusStore store = new();
        store.RecordM2SweepRan(job, Now, "newer");
        store.RecordM2SweepSucceeded(job, Now, "newer", breaches: 2, coverage: 3, population: 4);
        store.RecordM2SweepRan(job, Now.AddDays(-1), "older");
        store.Read().M2SweepStatuses[job].LastAttemptCompletedSuccessfully.ShouldBeFalse();
        store.RecordM2SweepSucceeded(job, Now.AddDays(-1), "older", breaches: 0, coverage: 30, population: 40);

        PeriodicEnforcementM2SweepStatus status = store.Read().M2SweepStatuses[job];
        status.LastSucceededAtUtc.ShouldBe(Now);
        status.LastSuccessCorrelationId.ShouldBe("newer");
        status.LastBreaches.ShouldBe(2);
        status.LastCoverage.ShouldBe(3);
        status.LastPopulation.ShouldBe(4);
        status.LastRanAtUtc.ShouldBe(Now.AddDays(-1));
        status.LastRunCorrelationId.ShouldBe("older");
        status.LastAttemptCompletedSuccessfully.ShouldBeFalse();

        store.RecordM2SweepRan(job, Now.AddHours(1), "latest");
        store.RecordM2SweepSucceeded(job, Now.AddHours(1), "latest", breaches: 0, coverage: 5, population: 6);
        status = store.Read().M2SweepStatuses[job];
        status.LastSucceededAtUtc.ShouldBe(Now.AddHours(1));
        status.LastSuccessCorrelationId.ShouldBe("latest");
        status.LastBreaches.ShouldBe(0);
        status.LastCoverage.ShouldBe(5);
        status.LastPopulation.ShouldBe(6);
    }

    [Theory]
    [InlineData(1, 2)]
    [InlineData(0, 0)]
    public void RegressedCompletionCannotReleaseAnOlderCleanSnapshot(int breaches, int coverage)
    {
        InMemoryPeriodicEnforcementStatusStore store = new();
        foreach (string job in M2SweepJobs.All)
        {
            store.RecordM2SweepRan(job, Now, "clean");
            store.RecordM2SweepSucceeded(job, Now, "clean", breaches: 0, coverage: 2, population: 2);
        }

        PeriodicEnforcementM2ReleaseGateResponse.From(store.Read(), true, Now, TimeSpan.FromDays(1)).IsStopShip.ShouldBeFalse();
        store.RecordM2SweepRan(M2SweepJobs.WormAuditChain, Now.AddHours(-1), "clock-regressed");
        store.RecordM2SweepSucceeded(M2SweepJobs.WormAuditChain, Now.AddHours(-1), "clock-regressed", breaches, coverage, population: 2);

        PeriodicEnforcementM2ReleaseGateResponse gate = PeriodicEnforcementM2ReleaseGateResponse.From(
            store.Read(), true, Now.AddHours(1), TimeSpan.FromDays(1));

        gate.IsStopShip.ShouldBeTrue();
        gate.StopShipReasons.ShouldContain($"{M2SweepJobs.WormAuditChain}:latest_attempt_incomplete");
    }
}
