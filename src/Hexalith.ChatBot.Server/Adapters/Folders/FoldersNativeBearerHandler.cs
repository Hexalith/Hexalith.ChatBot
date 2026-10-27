using System.Net.Http.Headers;
using Hexalith.ChatBot.Server.Adapters.OwnerServices;

namespace Hexalith.ChatBot.Server.Adapters.Folders;

/// <summary>Sends the dedicated Folders credential only to its configured native HTTP boundary.</summary>
internal sealed class FoldersNativeBearerHandler(Uri endpoint, string tenant, string token) : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        Uri? destination = request.RequestUri;
        string nativeBase = endpoint.AbsolutePath.TrimEnd('/') + "/api/v1/folders/";
        if (destination is null || !destination.IsAbsoluteUri || !OwnerServiceAuthenticationOptions.SameOrigin(endpoint, destination)
            || !string.IsNullOrEmpty(destination.UserInfo) || !string.IsNullOrEmpty(destination.Fragment)
            || !string.IsNullOrEmpty(destination.Query) || !destination.AbsolutePath.StartsWith(nativeBase, StringComparison.Ordinal)
            || !destination.AbsolutePath.Contains("/native/", StringComparison.Ordinal)
            || request.Headers.TryGetValues("X-Hexalith-Tenant-Id", out var supplied) && supplied.Any(value => value != tenant))
            throw new InvalidOperationException("folders_request_destination_denied");
        request.Headers.Remove("X-Hexalith-Tenant-Id"); request.Headers.Add("X-Hexalith-Tenant-Id", tenant);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return base.SendAsync(request, cancellationToken);
    }
}
