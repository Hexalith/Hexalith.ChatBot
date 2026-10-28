using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Gateway.Stages;
using Hexalith.ChatBot.Server.Projections.DerivedStores;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Audit;

/// <summary>Verifies bounded cleanup independently of shutdown and uncooperative stores.</summary>
public sealed class DerivedStoreIsolationCleanupTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 28, 0, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CleanupDeadlineBoundsTheWholePairAndFailsClosed(bool ignoresCancellation)
    {
        TaskCompletionSource release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        CleanupControlledDerivedStore store = new()
        {
            BeforeCleanup = token => ignoresCancellation ? release.Task : Task.Delay(Timeout.InfiniteTimeSpan, token),
        };
        InMemoryAuditWriter audit = new();
        InMemoryOperatorAlertSink alerts = new();
        DerivedStoreIsolationProbeCoordinator coordinator = new(
            store, audit, alerts, new WormAuditTestData.FixedClock(Now),
            cleanupTimeout: TimeSpan.FromMilliseconds(50));

        try
        {
            DerivedStoreIsolationVerificationResult result = await coordinator.ProbePairAndAlertAsync(
                "tenant-alpha", "tenant-beta", "cleanup-deadline", TestContext.Current.CancellationToken)
                .AsTask().WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

            result.Status.ShouldBe(DerivedStoreIsolationStatus.Unknown);
            result.IsBreach.ShouldBeTrue();
            result.ReasonCode.ShouldBe(DerivedStoreIsolationVerificationResult.ProbeIncompleteReasonCode);
            store.CleanupTokens.ShouldHaveSingleItem().IsCancellationRequested.ShouldBeTrue();
            audit.Envelopes.ShouldHaveSingleItem();
            alerts.Alerts.ShouldHaveSingleItem();
        }
        finally
        {
            release.TrySetResult();
        }
    }

    [Fact]
    public async Task CleanupSurvivesCallerCancellationAndPreservesItsIdentity()
    {
        using CancellationTokenSource caller = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        CleanupControlledDerivedStore store = new() { BeforeRead = () => caller.Cancel() };
        InMemoryOperatorAlertSink alerts = new();
        DerivedStoreIsolationProbeCoordinator coordinator = new(
            store, new InMemoryAuditWriter(), alerts, new WormAuditTestData.FixedClock(Now));

        OperationCanceledException exception = await Should.ThrowAsync<OperationCanceledException>(
            () => coordinator.ProbePairAndAlertAsync("tenant-alpha", "tenant-beta", "cancel-cleanup", caller.Token).AsTask());

        exception.CancellationToken.ShouldBe(caller.Token);
        CancellationToken cleanupToken = store.CleanupTokens.ShouldHaveSingleItem();
        cleanupToken.ShouldNotBe(caller.Token);
        cleanupToken.IsCancellationRequested.ShouldBeFalse();
        foreach (DerivedStoreClass cls in DerivedStorePartition.AllClasses)
        {
            store.EnumerateResourceIds(cls, "tenant-alpha").ShouldBeEmpty();
        }

        alerts.Alerts.ShouldBeEmpty();
    }

    [Fact]
    public async Task CancelledCallerStillGetsItsOriginalCancellationAfterHangingCleanupExpires()
    {
        using CancellationTokenSource caller = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        TaskCompletionSource release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        CleanupControlledDerivedStore store = new()
        {
            BeforeRead = () => caller.Cancel(),
            BeforeCleanup = _ => release.Task,
        };
        DerivedStoreIsolationProbeCoordinator coordinator = new(
            store, new InMemoryAuditWriter(), new InMemoryOperatorAlertSink(), new WormAuditTestData.FixedClock(Now),
            cleanupTimeout: TimeSpan.FromMilliseconds(50));

        try
        {
            OperationCanceledException exception = await Should.ThrowAsync<OperationCanceledException>(
                () => coordinator.ProbePairAndAlertAsync("tenant-alpha", "tenant-beta", "cancel-hanging-cleanup", caller.Token)
                    .AsTask().WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken));

            exception.CancellationToken.ShouldBe(caller.Token);
            store.CleanupTokens.ShouldHaveSingleItem().IsCancellationRequested.ShouldBeTrue();
        }
        finally
        {
            release.TrySetResult();
        }
    }

    [Fact]
    public async Task SuccessiveInvalidationsShareOneTotalCleanupBudget()
    {
        CleanupControlledDerivedStore store = new()
        {
            BeforeCleanup = token => Task.Delay(TimeSpan.FromMilliseconds(600), token),
        };
        DerivedStoreIsolationProbeCoordinator coordinator = new(
            store, new InMemoryAuditWriter(), new InMemoryOperatorAlertSink(), new WormAuditTestData.FixedClock(Now),
            cleanupTimeout: TimeSpan.FromSeconds(1));

        DerivedStoreIsolationVerificationResult result = await coordinator.ProbePairAndAlertAsync(
            "tenant-alpha", "tenant-beta", "total-cleanup-budget", TestContext.Current.CancellationToken)
            .AsTask().WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

        result.Status.ShouldBe(DerivedStoreIsolationStatus.Unknown);
        store.CleanupTokens.Count.ShouldBe(2);
        store.CleanupTokens[0].ShouldBe(store.CleanupTokens[1]);
    }

    [Fact]
    public async Task ObservedBreachSurvivesCleanupDeadlineExpiry()
    {
        CleanupControlledDerivedStore store = new()
        {
            LeakReads = true,
            BeforeCleanup = token => Task.Delay(Timeout.InfiniteTimeSpan, token),
        };
        DerivedStoreIsolationProbeCoordinator coordinator = new(
            store, new InMemoryAuditWriter(), new InMemoryOperatorAlertSink(), new WormAuditTestData.FixedClock(Now),
            cleanupTimeout: TimeSpan.FromMilliseconds(50));

        DerivedStoreIsolationVerificationResult result = await coordinator.ProbePairAndAlertAsync(
            "tenant-alpha", "tenant-beta", "breach-cleanup-deadline", TestContext.Current.CancellationToken);

        result.Status.ShouldBe(DerivedStoreIsolationStatus.Breach);
        result.ReasonCode.ShouldBe(DerivedStoreIsolationVerificationResult.BreachReasonCode);
        result.FirstOffenderLocator.ShouldNotBeNull().ShouldStartWith("derived-store-sentinel:iso-probe:");
    }

    [Fact]
    public async Task LateUncooperativeCleanupCannotEraseTheNextInvocationsSentinel()
    {
        TaskCompletionSource release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        CleanupControlledDerivedStore store = new()
        {
            IgnoreCleanupCancellation = true,
            BeforeCleanup = _ => release.Task,
        };
        DerivedStoreIsolationProbeCoordinator coordinator = new(
            store, new InMemoryAuditWriter(), new InMemoryOperatorAlertSink(), new WormAuditTestData.FixedClock(Now),
            cleanupTimeout: TimeSpan.FromMilliseconds(50));

        try
        {
            DerivedStoreIsolationVerificationResult first = await coordinator.ProbePairAndAlertAsync(
                "tenant-alpha", "tenant-beta", "same-correlation", TestContext.Current.CancellationToken);
            first.Status.ShouldBe(DerivedStoreIsolationStatus.Unknown);

            store.LeakReads = true;
            store.BeforeReadAsync = async token =>
            {
                release.TrySetResult();
                await store.FirstCleanupCompleted.WaitAsync(token).ConfigureAwait(false);
            };
            DerivedStoreIsolationVerificationResult second = await coordinator.ProbePairAndAlertAsync(
                "tenant-alpha", "tenant-beta", "same-correlation", TestContext.Current.CancellationToken)
                .AsTask().WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

            second.Status.ShouldBe(DerivedStoreIsolationStatus.Breach);
        }
        finally
        {
            release.TrySetResult();
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void NonpositiveCleanupBudgetIsRejected(int milliseconds)
        => Should.Throw<ArgumentOutOfRangeException>(() => new DerivedStoreIsolationProbeCoordinator(
            new InMemoryDerivedStore(), new InMemoryAuditWriter(), new InMemoryOperatorAlertSink(),
            new WormAuditTestData.FixedClock(Now), cleanupTimeout: TimeSpan.FromMilliseconds(milliseconds)));

    [Fact]
    public async Task FastCleanupFailuresAlsoPreventACleanVerdict()
    {
        CleanupControlledDerivedStore store = new()
        {
            BeforeCleanup = _ => Task.FromException(new InvalidOperationException("cleanup unavailable")),
        };
        InMemoryOperatorAlertSink alerts = new();
        DerivedStoreIsolationProbeCoordinator coordinator = new(
            store, new InMemoryAuditWriter(), alerts, new WormAuditTestData.FixedClock(Now));

        DerivedStoreIsolationVerificationResult result = await coordinator.ProbePairAndAlertAsync(
            "tenant-alpha", "tenant-beta", "fast-cleanup-failure", TestContext.Current.CancellationToken);

        result.Status.ShouldBe(DerivedStoreIsolationStatus.Unknown);
        result.IsBreach.ShouldBeTrue();
        store.CleanupTokens.Count.ShouldBe(DerivedStorePartition.AllClasses.Count);
        alerts.Alerts.ShouldHaveSingleItem();
    }
}
