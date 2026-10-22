using Shouldly;

namespace Hexalith.ChatBot.IntegrationTests.Recovery;

/// <summary>Verifies cancellation attribution and diagnostic retention through all three concrete decorators.</summary>
public sealed class RecoveryFailureCaptureTests
{
    /// <summary>Cleanup outliving the old scenario deadline does not reclassify an earlier caller cancellation.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CallerCancellationStopsPendingBudgetWithoutErasingAnExpiredBudget(bool budgetExpiredFirst)
    {
        using CancellationTokenSource caller = new();
        using CancellationTokenSource budget = new(TimeSpan.FromMilliseconds(500));
        using CancellationTokenSource scenario = CancellationTokenSource.CreateLinkedTokenSource(caller.Token, budget.Token);
        using CancellationTokenRegistration stopBudget = RecoveryCancellation.StopBudgetOnCallerCancellation(caller.Token, budget);
        if (budgetExpiredFirst)
        {
            await budget.CancelAsync();
        }

        await caller.CancelAsync();
        OperationCanceledException failure = new(scenario.Token);
        // Independent restoration/cleanup may finish after the original scenario deadline.
        await Task.Delay(TimeSpan.FromMilliseconds(600), TestContext.Current.CancellationToken);

        Exception result = RecoveryCancellation.Normalize(failure, scenario.Token, caller.Token, budget.Token);
        if (budgetExpiredFirst)
        {
            result.ShouldBeSameAs(failure);
        }
        else
        {
            budget.IsCancellationRequested.ShouldBeFalse();
            result.ShouldBeOfType<OperationCanceledException>().CancellationToken.ShouldBe(caller.Token);
        }
    }

    /// <summary>An independent scenario budget remains a failure even if caller cancellation is also requested.</summary>
    [Theory]
    [InlineData(true, false, true)]
    [InlineData(true, true, false)]
    [InlineData(false, true, false)]
    [InlineData(false, false, false)]
    public void LinkedCancellationIsNormalizedOnlyWithoutAnIndependentDeadline(
        bool callerCanceled, bool budgetCanceled, bool normalized)
    {
        using CancellationTokenSource caller = new();
        using CancellationTokenSource budget = new();
        using CancellationTokenSource scenario = CancellationTokenSource.CreateLinkedTokenSource(caller.Token, budget.Token);
        if (callerCanceled)
        {
            caller.Cancel();
        }

        if (budgetCanceled)
        {
            budget.Cancel();
        }

        OperationCanceledException failure = new(scenario.Token);
        Exception result = RecoveryCancellation.Normalize(failure, scenario.Token, caller.Token, budget.Token);
        if (normalized)
        {
            result.ShouldBeOfType<OperationCanceledException>().CancellationToken.ShouldBe(caller.Token);
            result.InnerException.ShouldBeSameAs(failure);
        }
        else
        {
            result.ShouldBeSameAs(failure);
        }
    }

    /// <summary>Only a requested cancellation carrying the exact caller token bypasses diagnostic retention.</summary>
    [Theory]
    [InlineData("continuity", "caller")]
    [InlineData("continuity", "internal")]
    [InlineData("continuity", "simultaneous")]
    [InlineData("continuity", "unattributed")]
    [InlineData("continuity", "ordinary")]
    [InlineData("continuity", "uncanceled-caller")]
    [InlineData("rebuild", "caller")]
    [InlineData("rebuild", "internal")]
    [InlineData("rebuild", "simultaneous")]
    [InlineData("rebuild", "unattributed")]
    [InlineData("rebuild", "ordinary")]
    [InlineData("rebuild", "uncanceled-caller")]
    [InlineData("scoped", "caller")]
    [InlineData("scoped", "internal")]
    [InlineData("scoped", "simultaneous")]
    [InlineData("scoped", "unattributed")]
    [InlineData("scoped", "ordinary")]
    [InlineData("scoped", "uncanceled-caller")]
    public async Task FailureRetentionUsesCancellationTokenIdentity(string driver, string scenario)
    {
        using CancellationTokenSource caller = new();
        using CancellationTokenSource deadline = new();
        await deadline.CancelAsync();
        if (scenario is not ("internal" or "uncanceled-caller"))
        {
            await caller.CancelAsync();
        }

        Exception failure = scenario switch
        {
            "caller" or "uncanceled-caller" => new OperationCanceledException(caller.Token),
            "internal" or "simultaneous" => new OperationCanceledException(deadline.Token),
            "unattributed" => new TaskCanceledException(),
            _ => new InvalidOperationException("private provider detail"),
        };
        FailingRecoveryDriver inner = new(failure);
        Func<Task> run;
        Func<IEnumerable<Exception>> failures;
        switch (driver)
        {
            case "continuity":
                CapturingContinuityDrillScenarioRunner continuity = new(inner);
                run = () => continuity.RunAsync("eventstore-outage", "replay-test:fixture", "correlation", caller.Token).AsTask();
                failures = () => continuity.Failures;
                break;
            case "rebuild":
                CapturingProjectionRebuildDriver rebuild = new(inner);
                run = () => rebuild.RebuildAsync("replay-test:fixture", "dataset", "correlation", caller.Token).AsTask();
                failures = () => rebuild.Failures;
                break;
            default:
                CapturingScopedOutageInjectionDriver scoped = new(inner);
                run = () => scoped.InjectAndMeasureAsync("graph", "replay-test:fixture", "correlation", caller.Token).AsTask();
                failures = () => scoped.Failures.Values;
                break;
        }

        Exception? thrown = await Record.ExceptionAsync(run);
        thrown.ShouldBeSameAs(failure);
        if (scenario == "caller")
        {
            failures().ShouldBeEmpty();
        }
        else
        {
            Exception retained = failures().ShouldHaveSingleItem();
            retained.InnerException.ShouldBeSameAs(failure);
            retained.Message.ShouldNotContain("private provider detail");
        }
    }
}
