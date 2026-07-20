using System.Net;
using System.Net.Sockets;

namespace Hexalith.ChatBot.IntegrationTests;

/// <summary>
/// Starts exclusive wildcard TCP listeners with dual-stack coverage when IPv6 is supported and an IPv4 fallback otherwise.
/// </summary>
internal static class WildcardTcpListener
{
    /// <summary>
    /// Starts an exclusive wildcard listener for the supplied port.
    /// </summary>
    /// <param name="port">The port to bind, or zero to let the operating system select one.</param>
    /// <returns>The started wildcard listener.</returns>
    public static TcpListener Start(int port)
        => Start(port, static (address, candidatePort) => new TcpListener(address, candidatePort), Socket.OSSupportsIPv6);

    /// <summary>
    /// Starts an exclusive wildcard listener through an injectable address-family seam.
    /// </summary>
    /// <param name="port">The port to bind, or zero to let the operating system select one.</param>
    /// <param name="createListener">Creates an unstarted listener for the requested wildcard address and port.</param>
    /// <param name="supportsIpv6">Whether the current allocation attempt should try a dual-mode IPv6 listener first.</param>
    /// <returns>The started wildcard listener.</returns>
    public static TcpListener Start(
        int port,
        Func<IPAddress, int, TcpListener> createListener,
        bool supportsIpv6)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(port);
        ArgumentNullException.ThrowIfNull(createListener);
        if (supportsIpv6)
        {
            TcpListener? ipv6Listener = null;
            try
            {
                ipv6Listener = createListener(IPAddress.IPv6Any, port);
                ipv6Listener.ExclusiveAddressUse = true;
                ipv6Listener.Server.DualMode = true;
                ipv6Listener.Start();
                return ipv6Listener;
            }
            catch (SocketException exception) when (IsUnsupportedAddressFamily(exception.SocketErrorCode))
            {
                ipv6Listener?.Stop();
            }
            catch (NotSupportedException)
            {
                ipv6Listener?.Stop();
            }
            catch
            {
                ipv6Listener?.Stop();
                throw;
            }
        }

        TcpListener ipv4Listener = createListener(IPAddress.Any, port);
        try
        {
            ipv4Listener.ExclusiveAddressUse = true;
            ipv4Listener.Start();
            return ipv4Listener;
        }
        catch
        {
            ipv4Listener.Stop();
            throw;
        }
    }

    /// <summary>
    /// Determines whether a socket error represents collision with an exclusive wildcard bind on the supplied platform.
    /// </summary>
    /// <param name="socketError">The socket error returned by the competing bind.</param>
    /// <param name="isWindows">Whether Windows exclusive-bind semantics apply.</param>
    /// <returns><see langword="true"/> for an address collision; otherwise, <see langword="false"/>.</returns>
    public static bool IsExclusiveBindCollision(SocketError socketError, bool isWindows)
        => socketError == SocketError.AddressAlreadyInUse
            || (isWindows && socketError == SocketError.AccessDenied);

    private static bool IsUnsupportedAddressFamily(SocketError socketError)
        => socketError is SocketError.AddressFamilyNotSupported
            or SocketError.AddressNotAvailable
            or SocketError.OperationNotSupported
            or SocketError.ProtocolNotSupported
            or SocketError.ProtocolOption;
}
