using System.Net.Sockets;

namespace Hexalith.ChatBot.IntegrationTests;

/// <summary>
/// Applies exact-port, branch-local address-bind correlation for topology startup failures.
/// </summary>
internal static class TopologyFailureCorrelation
{
    /// <summary>
    /// Determines whether a failure is retryable for one of the exact selected resource-to-port assignments.
    /// </summary>
    /// <param name="exception">The startup failure graph.</param>
    /// <param name="selectedPorts">The exact selected resource-to-port assignments.</param>
    /// <returns><see langword="true"/> only when one non-cancellation branch contains bind and exact-port evidence.</returns>
    public static bool IsSelectedAddressInUse(
        Exception exception,
        IReadOnlyDictionary<string, int> selectedPorts)
    {
        ArgumentNullException.ThrowIfNull(exception);
        ArgumentNullException.ThrowIfNull(selectedPorts);
        if (ContainsCancellation(exception))
        {
            return false;
        }

        int[] ports = selectedPorts.Values.Distinct().ToArray();
        return HasCorrelatedAddressBindBranch(
            exception,
            selectedPorts,
            ports,
            hasBindEvidence: false,
            hasPortEvidence: false);
    }

    /// <summary>
    /// Determines whether one captured log line contains both address-bind wording and the whole exact port.
    /// </summary>
    /// <param name="line">One captured log line.</param>
    /// <param name="expectedPort">The exact selected port.</param>
    /// <returns><see langword="true"/> only when both facts occur in the same line.</returns>
    public static bool IsCorrelatedLogLine(string line, int expectedPort)
    {
        ArgumentNullException.ThrowIfNull(line);
        return ContainsAddressBindEvidence(line) && ContainsWholeNumber(line, expectedPort);
    }

    /// <summary>
    /// Determines whether text contains platform address-bind evidence.
    /// </summary>
    /// <param name="value">The text to inspect.</param>
    /// <returns><see langword="true"/> when the text contains recognized bind-failure wording.</returns>
    public static bool ContainsAddressBindEvidence(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return value.Contains("address already in use", StringComparison.OrdinalIgnoreCase)
            || value.Contains("Only one usage of each socket address", StringComparison.OrdinalIgnoreCase)
            || value.Contains("EADDRINUSE", StringComparison.OrdinalIgnoreCase)
            || value.Contains(
                "An attempt was made to access a socket in a way forbidden by its access permissions",
                StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Determines whether text contains the supplied number delimited by non-digit boundaries.
    /// </summary>
    /// <param name="value">The text to inspect.</param>
    /// <param name="number">The exact number to locate.</param>
    /// <returns><see langword="true"/> when the whole number occurs.</returns>
    public static bool ContainsWholeNumber(string value, int number)
    {
        ArgumentNullException.ThrowIfNull(value);
        string text = number.ToString(System.Globalization.CultureInfo.InvariantCulture);
        int startIndex = 0;
        while ((startIndex = value.IndexOf(text, startIndex, StringComparison.Ordinal)) >= 0)
        {
            int endIndex = startIndex + text.Length;
            bool startsAtBoundary = startIndex == 0 || !char.IsAsciiDigit(value[startIndex - 1]);
            bool endsAtBoundary = endIndex == value.Length || !char.IsAsciiDigit(value[endIndex]);
            if (startsAtBoundary && endsAtBoundary)
            {
                return true;
            }

            startIndex++;
        }

        return false;
    }

    private static bool HasCorrelatedAddressBindBranch(
        Exception exception,
        IReadOnlyDictionary<string, int> selectedPorts,
        IReadOnlyList<int> ports,
        bool hasBindEvidence,
        bool hasPortEvidence)
    {
        if (exception is AggregateException aggregateException)
        {
            return aggregateException.InnerExceptions.Any(innerException =>
                HasCorrelatedAddressBindBranch(
                    innerException,
                    selectedPorts,
                    ports,
                    hasBindEvidence,
                    hasPortEvidence));
        }

        bool branchHasBindEvidence = hasBindEvidence || IsAddressBindEvidence(exception, selectedPorts);
        bool branchHasPortEvidence = hasPortEvidence
            || ports.Any(port => ContainsWholeNumber(exception.Message, port));
        if (exception.InnerException is not null)
        {
            return HasCorrelatedAddressBindBranch(
                exception.InnerException,
                selectedPorts,
                ports,
                branchHasBindEvidence,
                branchHasPortEvidence);
        }

        return branchHasBindEvidence && branchHasPortEvidence;
    }

    private static bool IsAddressBindEvidence(
        Exception exception,
        IReadOnlyDictionary<string, int> selectedPorts)
    {
        if (exception is SelectedEndpointStartupException selectedFailure)
        {
            return selectedFailure.HasCorrelatedBindEvidence
                && selectedPorts.TryGetValue(selectedFailure.ResourceName, out int expectedPort)
                && expectedPort == selectedFailure.Port;
        }

        return exception is SocketException socketException
            ? WildcardTcpListener.IsExclusiveBindCollision(
                socketException.SocketErrorCode,
                OperatingSystem.IsWindows())
            : ContainsAddressBindEvidence(exception.Message);
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
