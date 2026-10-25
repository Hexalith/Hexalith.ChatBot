using System.Net.Http.Headers;

namespace Hexalith.ChatBot.Server.Adapters.OwnerServices;

/// <summary>Attaches an owner credential only inside its configured transport boundary.</summary>
internal sealed class OwnerServiceBearerHandler(OwnerServiceAuthenticationOptions options, OwnerServiceTokenProvider? tokens) : DelegatingHandler
{
    /// <inheritdoc />
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();
        Uri? target = request.RequestUri;
        string basePath = options.Endpoint.AbsolutePath.TrimEnd('/');
        if (target is null || !target.IsAbsoluteUri || !string.IsNullOrEmpty(target.UserInfo)
            || !string.IsNullOrEmpty(target.Fragment) || !OwnerServiceAuthenticationOptions.SameOrigin(options.Endpoint, target)
            || !(target.AbsolutePath == basePath || target.AbsolutePath.StartsWith(basePath + "/", StringComparison.Ordinal)))
            throw new InvalidOperationException("owner_request_destination_denied");
        string? token = options.ApiToken ?? await tokens!.AcquireAsync(cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(token)) throw new InvalidOperationException("owner_token_unavailable");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
    }
}
