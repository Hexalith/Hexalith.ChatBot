namespace Hexalith.ChatBot.IntegrationTests.Recovery;

/// <summary>Preserves caller cancellation across a known linked scenario token without hiding independent deadlines.</summary>
internal static class RecoveryCancellation
{
    /// <summary>Stops a pending scenario timer on caller cancellation so independent cleanup cannot change its cause.</summary>
    internal static CancellationTokenRegistration StopBudgetOnCallerCancellation(
        CancellationToken callerToken, CancellationTokenSource scenarioBudget)
        => callerToken.Register(static state => ((CancellationTokenSource)state!).CancelAfter(Timeout.InfiniteTimeSpan), scenarioBudget);

    /// <summary>Identifies caller-only cancellation of the known scenario token.</summary>
    internal static bool IsCallerCancellation(
        Exception failure, CancellationToken scenarioToken, CancellationToken callerToken, CancellationToken budgetToken)
        => failure is OperationCanceledException canceled
            && canceled.CancellationToken == scenarioToken
            && callerToken.IsCancellationRequested
            && !budgetToken.IsCancellationRequested;

    /// <summary>Restamps caller-only scenario cancellation; unrelated failures retain their original identity.</summary>
    internal static Exception Normalize(
        Exception failure, CancellationToken scenarioToken, CancellationToken callerToken, CancellationToken budgetToken)
        => IsCallerCancellation(failure, scenarioToken, callerToken, budgetToken)
            ? new OperationCanceledException("The recovery scenario was canceled by its caller.", failure, callerToken)
            : failure;
}
