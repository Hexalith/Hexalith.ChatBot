using System.Diagnostics;
using System.Runtime.ExceptionServices;

namespace Hexalith.ChatBot.IntegrationTests;

/// <summary>
/// Runs at most two entirely fresh topology attempts and retries only failures approved by the supplied correlation policy.
/// </summary>
internal static class TopologyStartupOrchestrator
{
    private const int MaximumAttempts = 2;

    /// <summary>
    /// Creates, starts, and validates a topology attempt, with one permitted fresh retry for correlated failures.
    /// </summary>
    /// <typeparam name="TAttempt">The attempt state type.</typeparam>
    /// <param name="createAttemptAsync">Creates a completely fresh attempt for the supplied one-based attempt number.</param>
    /// <param name="startAttemptAsync">Starts the attempt.</param>
    /// <param name="validateAttemptAsync">Validates the started attempt's child-resource readiness.</param>
    /// <param name="shouldRetry">Determines whether a startup failure is correlated and retryable.</param>
    /// <param name="disposeAttemptAsync">Cleans up a failed attempt.</param>
    /// <param name="cancellationToken">Cancels attempt creation, startup, or validation.</param>
    /// <returns>The first successfully started and validated attempt.</returns>
    public static async Task<TAttempt> StartAsync<TAttempt>(
        Func<int, CancellationToken, Task<TAttempt>> createAttemptAsync,
        Func<TAttempt, CancellationToken, Task> startAttemptAsync,
        Func<TAttempt, CancellationToken, Task> validateAttemptAsync,
        Func<TAttempt, Exception, bool> shouldRetry,
        Func<TAttempt, ValueTask> disposeAttemptAsync,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(createAttemptAsync);
        ArgumentNullException.ThrowIfNull(startAttemptAsync);
        ArgumentNullException.ThrowIfNull(validateAttemptAsync);
        ArgumentNullException.ThrowIfNull(shouldRetry);
        ArgumentNullException.ThrowIfNull(disposeAttemptAsync);

        Exception? firstCorrelatedFailure = null;
        for (int attemptNumber = 1; attemptNumber <= MaximumAttempts; attemptNumber++)
        {
            TAttempt attempt;
            try
            {
                attempt = await createAttemptAsync(attemptNumber, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception creationException)
            {
                if (firstCorrelatedFailure is not null)
                {
                    creationException.Data["TopologyFirstAttemptException"] = firstCorrelatedFailure;
                }

                ExceptionDispatchInfo.Capture(creationException).Throw();
                throw new UnreachableException();
            }

            try
            {
                await startAttemptAsync(attempt, cancellationToken).ConfigureAwait(false);
                await validateAttemptAsync(attempt, cancellationToken).ConfigureAwait(false);
                return attempt;
            }
            catch (Exception startupException)
            {
                if (firstCorrelatedFailure is not null)
                {
                    startupException.Data["TopologyFirstAttemptException"] = firstCorrelatedFailure;
                }

                bool retry = false;
                bool retryClassificationFailed = false;
                if (firstCorrelatedFailure is null
                    && attemptNumber < MaximumAttempts
                    && !cancellationToken.IsCancellationRequested
                    && !ContainsCancellation(startupException))
                {
                    try
                    {
                        retry = shouldRetry(attempt, startupException);
                    }
                    catch (Exception classificationException)
                    {
                        retryClassificationFailed = true;
                        startupException.Data["TopologyRetryClassificationException"] = classificationException;
                    }
                }

                bool cleanupFailed = false;
                try
                {
                    await disposeAttemptAsync(attempt).ConfigureAwait(false);
                }
                catch (Exception cleanupException)
                {
                    cleanupFailed = true;
                    startupException.Data["TopologyCleanupException"] = cleanupException;
                }

                if (firstCorrelatedFailure is not null
                    || !retry
                    || retryClassificationFailed
                    || cleanupFailed
                    || cancellationToken.IsCancellationRequested)
                {
                    ExceptionDispatchInfo.Capture(startupException).Throw();
                }

                firstCorrelatedFailure = startupException;
            }
        }

        throw new UnreachableException();
    }

    private static bool ContainsCancellation(Exception exception)
    {
        if (exception is OperationCanceledException)
        {
            return true;
        }

        if (exception is AggregateException aggregateException)
        {
            return aggregateException.InnerExceptions.Any(ContainsCancellation);
        }

        return exception.InnerException is not null && ContainsCancellation(exception.InnerException);
    }
}
