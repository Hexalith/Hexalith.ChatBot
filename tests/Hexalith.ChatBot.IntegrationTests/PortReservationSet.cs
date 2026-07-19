using System.Net;
using System.Net.Sockets;

namespace Hexalith.ChatBot.IntegrationTests;

internal sealed class PortReservationSet : IDisposable
{
    private readonly List<TcpListener> _listeners;

    private PortReservationSet(List<TcpListener> listeners)
    {
        _listeners = listeners;
        Ports = listeners
            .Select(static listener => ((IPEndPoint)listener.LocalEndpoint).Port)
            .ToArray();
    }

    public bool IsReleased { get; private set; }

    public IReadOnlyList<int> Ports { get; }

    public static PortReservationSet Reserve(int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(count);
        List<TcpListener> listeners = [];
        try
        {
            for (int index = 0; index < count; index++)
            {
                TcpListener listener = CreateWildcardListener(0);
                listener.Start();
                listeners.Add(listener);
            }

            return new PortReservationSet(listeners);
        }
        catch
        {
            foreach (TcpListener listener in listeners)
            {
                listener.Stop();
            }

            throw;
        }
    }

    public void Dispose() => Release();

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

    private static TcpListener CreateWildcardListener(int port)
    {
        TcpListener listener = new(IPAddress.IPv6Any, port);
        listener.Server.DualMode = true;
        listener.ExclusiveAddressUse = true;
        return listener;
    }
}
