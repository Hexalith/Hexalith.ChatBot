using System.Net.Http.Headers;

namespace Hexalith.ChatBot.RecoverySandbox;

/// <summary>
/// Forwards a per-call bearer onto each outbound <see cref="HttpRequestMessage"/> without mutating a pooled
/// <see cref="HttpClient"/>'s <c>DefaultRequestHeaders</c> (which races under concurrent Graph <c>/process</c> calls).
/// </summary>
internal sealed class RecoveryBearerForwardingHandler : DelegatingHandler
{
    private static readonly AsyncLocal<string?> BearerToken = new();

    /// <summary>Sets the bearer used by outbound requests on the current async flow until disposed.</summary>
    public static IDisposable Use(string bearerToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(bearerToken);
        string? previous = BearerToken.Value;
        BearerToken.Value = bearerToken;
        return new Revert(previous);
    }

    /// <inheritdoc />
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (BearerToken.Value is string bearer)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearer);
        }

        return base.SendAsync(request, cancellationToken);
    }

    private sealed class Revert(string? previous) : IDisposable
    {
        private bool _disposed;

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            BearerToken.Value = previous;
        }
    }
}
