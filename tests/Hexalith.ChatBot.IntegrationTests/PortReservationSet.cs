using System.Net;
using System.Net.Sockets;

namespace Hexalith.ChatBot.IntegrationTests;

/// <summary>
/// Owns simultaneously-held wildcard TCP listeners used to reserve an isolated set of host ports.
/// </summary>
internal sealed class PortReservationSet : IDisposable
{
    private const int DefaultMaximumCandidateCount = 1_024;

    private readonly List<TcpListener> _listeners;

    private PortReservationSet(List<TcpListener> listeners, IReadOnlyList<int> ports)
    {
        _listeners = listeners;
        Ports = ports;
    }

    /// <summary>
    /// Gets a value indicating whether every reservation has been released.
    /// </summary>
    public bool IsReleased { get; private set; }

    /// <summary>
    /// Gets the distinct host ports held by this reservation set.
    /// </summary>
    public IReadOnlyList<int> Ports { get; }

    /// <summary>
    /// Reserves the requested number of distinct wildcard TCP ports while rejecting concrete ports already declared by other endpoints.
    /// </summary>
    /// <param name="count">The number of ports to reserve.</param>
    /// <param name="excludedPorts">Concrete ports that must not be selected.</param>
    /// <param name="startListener">Starts one candidate listener. The default reserves an operating-system-selected wildcard port.</param>
    /// <param name="getCandidatePort">Resolves the bound port from a candidate listener.</param>
    /// <param name="maximumCandidateCount">The maximum number of candidates to inspect before failing allocation.</param>
    /// <returns>A reservation set that holds every selected port until released or disposed.</returns>
    public static PortReservationSet Reserve(
        int count,
        IReadOnlySet<int>? excludedPorts = null,
        Func<TcpListener>? startListener = null,
        Func<TcpListener, int>? getCandidatePort = null,
        int maximumCandidateCount = DefaultMaximumCandidateCount)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(count);
        ArgumentOutOfRangeException.ThrowIfLessThan(maximumCandidateCount, count);
        startListener ??= static () => WildcardTcpListener.Start(0);
        getCandidatePort ??= static listener => ((IPEndPoint)listener.LocalEndpoint).Port;
        List<TcpListener> listeners = [];
        List<TcpListener> rejectedListeners = [];
        List<int> ports = [];
        TcpListener? currentListener = null;
        int candidateCount = 0;
        try
        {
            while (listeners.Count < count)
            {
                if (candidateCount >= maximumCandidateCount)
                {
                    throw new InvalidOperationException(
                        $"Unable to reserve {count} permitted wildcard TCP ports after inspecting {candidateCount} candidates.");
                }

                candidateCount++;
                currentListener = startListener()
                    ?? throw new InvalidOperationException("The reservation candidate factory returned no listener.");
                int port = getCandidatePort(currentListener);
                if (port is <= IPEndPoint.MinPort or > IPEndPoint.MaxPort)
                {
                    throw new InvalidOperationException($"Reservation candidate port {port} is outside the valid TCP range.");
                }

                if (ports.Contains(port))
                {
                    throw new InvalidOperationException($"Reservation candidate port {port} duplicates an already-held candidate.");
                }

                if (excludedPorts?.Contains(port) == true)
                {
                    rejectedListeners.Add(currentListener);
                    currentListener = null;
                    continue;
                }

                listeners.Add(currentListener);
                ports.Add(port);
                currentListener = null;
            }

            PortReservationSet reservations = new(listeners, ports);
            StopAll(rejectedListeners);
            return reservations;
        }
        catch
        {
            currentListener?.Stop();
            StopAll(listeners);
            StopAll(rejectedListeners);

            throw;
        }
    }

    /// <summary>
    /// Releases every held listener. Repeated calls are safe and have no effect.
    /// </summary>
    public void Dispose() => Release();

    /// <summary>
    /// Releases every held listener. Repeated calls are safe and have no effect.
    /// </summary>
    public void Release()
    {
        if (IsReleased)
        {
            return;
        }

        foreach (TcpListener listener in _listeners)
        {
            listener.Stop();
        }

        IsReleased = true;
    }

    private static void StopAll(IEnumerable<TcpListener> listeners)
    {
        foreach (TcpListener listener in listeners)
        {
            listener.Stop();
        }
    }
}
