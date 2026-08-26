using System.Diagnostics;
using System.Runtime.CompilerServices;

using Aspire.Hosting.ApplicationModel;

using Shouldly;

namespace Hexalith.ChatBot.IntegrationTests.Recovery;

/// <summary>Metadata-only and bounded-capture tests for recovery startup diagnostics.</summary>
public sealed class RecoveryResourceLogTailTests
{
    [Fact]
    public async Task RenderRetainsOnlyTheBoundedMetadataAllowlist()
    {
        string oversizedCategory = new('A', 129);
        RecoveryResourceLogTail tail = await RecoveryResourceLogTail.StartAsync(
            "chatbot",
            LogBatchesAsync(oversizedCategory, TestContext.Current.CancellationToken),
            maximumLines: 3,
            TestContext.Current.CancellationToken).ConfigureAwait(true);
        await using ConfiguredAsyncDisposable tailScope = tail.ConfigureAwait(true);

        string rendered = await WaitForRenderAsync(tail, "capturedLines=3").ConfigureAwait(true);

        rendered.ShouldContain("levels=[fail|info|warn]");
        rendered.ShouldContain("categories=[Safe.Category|Second.Category]");
        rendered.ShouldNotContain("tenant-secret-payload");
        rendered.ShouldNotContain("oldest");
        rendered.ShouldNotContain(oversizedCategory);
    }

    [Fact]
    public async Task CaptureFaultIsReducedToAnExceptionTypeRatherThanReplacingTheRealFailure()
    {
        RecoveryResourceLogTail tail = await RecoveryResourceLogTail.StartAsync(
            "eventstore",
            FaultingLogBatchesAsync(),
            maximumLines: 2,
            TestContext.Current.CancellationToken).ConfigureAwait(true);
        await using ConfiguredAsyncDisposable tailScope = tail.ConfigureAwait(true);

        string rendered = await WaitForRenderAsync(tail, "captureFault=InvalidOperationException")
            .ConfigureAwait(true);

        rendered.ShouldBe("[eventstore] no captured log lines. captureFault=InvalidOperationException");
        rendered.ShouldNotContain("sensitive-capture-message");
    }

    [Fact]
    public async Task DisposalBoundsADiagnosticEnumeratorThatIgnoresCancellation()
    {
        RecoveryResourceLogTail tail = await RecoveryResourceLogTail.StartAsync(
            "eventstore",
            NonCancellingLogBatchesAsync(),
            maximumLines: 1,
            TestContext.Current.CancellationToken).ConfigureAwait(true);
        _ = await WaitForRenderAsync(tail, "capturedLines=1").ConfigureAwait(true);
        Stopwatch stopwatch = Stopwatch.StartNew();

        await tail.DisposeAsync().AsTask()
            .WaitAsync(TimeSpan.FromSeconds(6), TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        stopwatch.Elapsed.ShouldBeLessThan(TimeSpan.FromSeconds(6));
    }

    private static async Task<string> WaitForRenderAsync(RecoveryResourceLogTail tail, string expected)
    {
        using CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(
            TestContext.Current.CancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(2));
        while (true)
        {
            string rendered = tail.Render();
            if (rendered.Contains(expected, StringComparison.Ordinal))
            {
                return rendered;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(10), timeout.Token).ConfigureAwait(false);
        }
    }

    private static async IAsyncEnumerable<IReadOnlyList<LogLine>> LogBatchesAsync(
        string oversizedCategory,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);
        yield return
        [
            new LogLine(1, "info: Old.Category[1] oldest", IsErrorMessage: false),
            new LogLine(2, "warn: Safe.Category[2] tenant-secret-payload", IsErrorMessage: false),
            new LogLine(3, "fail: Second.Category[3] another private body", IsErrorMessage: true),
            new LogLine(4, $"info: {oversizedCategory}[4] private body", IsErrorMessage: false),
        ];

        await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken).ConfigureAwait(false);
    }

    private static async IAsyncEnumerable<IReadOnlyList<LogLine>> FaultingLogBatchesAsync()
    {
        await Task.CompletedTask.ConfigureAwait(false);
        throw new InvalidOperationException("sensitive-capture-message");
#pragma warning disable CS0162 // Required to give the deliberately faulting async iterator an element type.
        yield return [];
#pragma warning restore CS0162
    }

    private static async IAsyncEnumerable<IReadOnlyList<LogLine>> NonCancellingLogBatchesAsync()
    {
        yield return [new LogLine(1, "info: Safe.Category[1] body", IsErrorMessage: false)];
        await Task.Delay(Timeout.InfiniteTimeSpan).ConfigureAwait(false);
    }
}
