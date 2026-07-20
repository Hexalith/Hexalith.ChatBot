namespace Hexalith.ChatBot.IntegrationTests;

/// <summary>
/// Preserves a selected resource's terminal state and safe derived bind-correlation evidence.
/// </summary>
internal sealed class SelectedEndpointStartupException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SelectedEndpointStartupException"/> class.
    /// </summary>
    /// <param name="resourceName">The selected Aspire resource name.</param>
    /// <param name="port">The reserved HTTP port assigned to the selected resource.</param>
    /// <param name="terminalState">The terminal Aspire state.</param>
    /// <param name="healthStatus">The terminal health status, when reported.</param>
    /// <param name="isTerminal">Whether the reported state is terminal rather than runtime-unhealthy.</param>
    public SelectedEndpointStartupException(
        string resourceName,
        int port,
        string? terminalState,
        string? healthStatus,
        bool isTerminal)
        : base(
            $"Selected resource '{resourceName}' reached state '{terminalState}' with health '{healthStatus}' "
            + $"before accepting TCP connections on assigned HTTP port {port}.")
    {
        ResourceName = resourceName;
        Port = port;
        TerminalState = terminalState;
        HealthStatus = healthStatus;
        IsTerminal = isTerminal;
    }

    /// <summary>
    /// Gets a value indicating whether safe exact-resource, same-line bind-and-port evidence was captured.
    /// </summary>
    public bool HasCorrelatedBindEvidence { get; private set; }

    /// <summary>
    /// Gets the health status reported with the failure, when available.
    /// </summary>
    public string? HealthStatus { get; }

    /// <summary>
    /// Gets a value indicating whether the reported Aspire state was terminal.
    /// </summary>
    public bool IsTerminal { get; }

    /// <summary>
    /// Gets the reserved HTTP port correlated with the failure.
    /// </summary>
    public int Port { get; }

    /// <summary>
    /// Gets the selected Aspire resource name correlated with the failure.
    /// </summary>
    public string ResourceName { get; }

    /// <summary>
    /// Gets the terminal Aspire state reported for the selected resource.
    /// </summary>
    public string? TerminalState { get; }

    /// <summary>
    /// Records the safe result of draining the exact selected resource's pre-started log watcher.
    /// </summary>
    /// <param name="hasCorrelatedBindEvidence">Whether one line contained bind wording and the whole selected port.</param>
    public void RecordCorrelatedBindEvidence(bool hasCorrelatedBindEvidence)
        => HasCorrelatedBindEvidence = IsTerminal && hasCorrelatedBindEvidence;
}
