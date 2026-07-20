using Aspire.Hosting;

namespace Hexalith.ChatBot.IntegrationTests;

/// <summary>
/// Holds one fresh built Aspire application, its still-held reservations, and the exact selected resource-to-port mapping.
/// </summary>
internal sealed class TopologyStartupAttempt : IAsyncDisposable
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TopologyStartupAttempt"/> class.
    /// </summary>
    /// <param name="application">The freshly built Aspire application.</param>
    /// <param name="reservations">The reservations held through model construction.</param>
    /// <param name="selectedPorts">The exact selected resource-to-port mapping.</param>
    public TopologyStartupAttempt(
        DistributedApplication application,
        PortReservationSet reservations,
        IReadOnlyDictionary<string, int> selectedPorts)
    {
        Application = application;
        Reservations = reservations;
        SelectedPorts = selectedPorts;
    }

    /// <summary>
    /// Gets the freshly built Aspire application.
    /// </summary>
    public DistributedApplication Application { get; }

    /// <summary>
    /// Gets the wildcard port reservations retained through the build.
    /// </summary>
    public PortReservationSet Reservations { get; }

    /// <summary>
    /// Gets the exact selected resource-to-port mapping for this attempt.
    /// </summary>
    public IReadOnlyDictionary<string, int> SelectedPorts { get; }

    /// <summary>
    /// Disposes the Aspire application and releases any remaining reservations.
    /// </summary>
    /// <returns>A task representing asynchronous cleanup.</returns>
    public async ValueTask DisposeAsync()
    {
        try
        {
            await Application.DisposeAsync().ConfigureAwait(false);
        }
        finally
        {
            Reservations.Dispose();
        }
    }
}
