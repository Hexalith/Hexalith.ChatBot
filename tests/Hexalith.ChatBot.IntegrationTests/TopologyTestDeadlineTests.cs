using Shouldly;

namespace Hexalith.ChatBot.IntegrationTests;

/// <summary>Checks whole-test cancellation and completion of the topology cleanup path.</summary>
public sealed class TopologyTestDeadlineTests
{
    /// <summary>A hung inner phase consumes the shared budget and still completes cleanup.</summary>
    [Fact]
    public async Task DeadlineShouldCancelNestedWorkAndWaitForCleanup()
    {
        bool cleanedUp = false;
        TopologyTestDeadline deadline = new(TimeSpan.FromMilliseconds(100));
        TimeoutException exception = await Should.ThrowAsync<TimeoutException>(() => deadline.RunAsync(
            async token =>
            {
                try
                {
                    await Task.Delay(Timeout.InfiniteTimeSpan, token).ConfigureAwait(false);
                }
                finally
                {
                    await Task.Yield();
                    cleanedUp = true;
                }
            },
            TestContext.Current.CancellationToken));

        cleanedUp.ShouldBeTrue();
        exception.Message.ShouldContain("execution budget");
        bool moreWorkStarted = false;
        await Should.ThrowAsync<TimeoutException>(() => deadline.RunAsync(_ =>
        {
            moreWorkStarted = true;
            return Task.CompletedTask;
        }, TestContext.Current.CancellationToken));
        moreWorkStarted.ShouldBeFalse();
    }

    /// <summary>Caller cancellation retains its token and does not become a topology timeout.</summary>
    [Fact]
    public async Task CallerCancellationShouldRemainDistinctFromDeadline()
    {
        using CancellationTokenSource caller = new();
        OperationCanceledException exception = await Should.ThrowAsync<OperationCanceledException>(() =>
            new TopologyTestDeadline().RunAsync(async token =>
            {
                await caller.CancelAsync().ConfigureAwait(false);
                token.ThrowIfCancellationRequested();
            }, caller.Token));

        exception.CancellationToken.ShouldBe(caller.Token);
    }
}
