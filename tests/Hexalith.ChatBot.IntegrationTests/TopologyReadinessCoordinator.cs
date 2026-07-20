using System.Runtime.ExceptionServices;

namespace Hexalith.ChatBot.IntegrationTests;

/// <summary>
/// Validates all selected resources concurrently and cancels sibling work on the first failure.
/// </summary>
internal static class TopologyReadinessCoordinator
{
    /// <summary>
    /// Runs concurrent initial validation followed by a concurrent final recheck under one caller-owned deadline.
    /// </summary>
    /// <param name="selectedPorts">The exact selected resource-to-port mapping.</param>
    /// <param name="validateResourceAsync">Validates one selected resource until it becomes ready or fails.</param>
    /// <param name="recheckResourceAsync">Rechecks one selected resource immediately before success.</param>
    /// <param name="cancellationToken">The shared attempt deadline and external cancellation token.</param>
    /// <returns>A task representing both four-resource validation phases.</returns>
    public static async Task ValidateAsync(
        IReadOnlyDictionary<string, int> selectedPorts,
        Func<string, int, CancellationToken, Task> validateResourceAsync,
        Func<string, int, CancellationToken, Task> recheckResourceAsync,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(selectedPorts);
        ArgumentNullException.ThrowIfNull(validateResourceAsync);
        ArgumentNullException.ThrowIfNull(recheckResourceAsync);

        await RunConcurrentPhaseAsync(selectedPorts, validateResourceAsync, cancellationToken).ConfigureAwait(false);
        await RunConcurrentPhaseAsync(selectedPorts, recheckResourceAsync, cancellationToken).ConfigureAwait(false);
    }

    private static async Task RunConcurrentPhaseAsync(
        IReadOnlyDictionary<string, int> selectedPorts,
        Func<string, int, CancellationToken, Task> validateResourceAsync,
        CancellationToken cancellationToken)
    {
        using CancellationTokenSource siblingSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        List<Task> remaining = selectedPorts
            .Select(pair => InvokeValidationAsync(
                validateResourceAsync,
                pair.Key,
                pair.Value,
                siblingSource.Token))
            .ToList();
        Exception? firstFailure = null;
        while (remaining.Count > 0 && firstFailure is null)
        {
            Task completed = await Task.WhenAny(remaining).ConfigureAwait(false);
            remaining.Remove(completed);
            try
            {
                await completed.ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                firstFailure = exception;
                await siblingSource.CancelAsync().ConfigureAwait(false);
            }
        }

        if (firstFailure is null)
        {
            return;
        }

        List<Exception> siblingFailures = [];
        foreach (Task task in remaining)
        {
            try
            {
                await task.ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (siblingSource.IsCancellationRequested)
            {
                // The first failure deliberately canceled sibling validation.
            }
            catch (Exception siblingException)
            {
                siblingFailures.Add(siblingException);
            }
        }

        if (siblingFailures.Count > 0)
        {
            firstFailure.Data["TopologySiblingValidationExceptions"] = new AggregateException(siblingFailures);
        }

        ExceptionDispatchInfo.Capture(firstFailure).Throw();
    }

    private static async Task InvokeValidationAsync(
        Func<string, int, CancellationToken, Task> validateResourceAsync,
        string resourceName,
        int expectedPort,
        CancellationToken cancellationToken)
        => await validateResourceAsync(resourceName, expectedPort, cancellationToken).ConfigureAwait(false);
}
