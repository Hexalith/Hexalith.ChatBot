using System.Net.Http.Headers;

namespace Hexalith.ChatBot.Server.Adapters.Projects;

/// <summary>Adds the operator-supplied service bearer token to authorization-filtered Projects reads.</summary>
internal sealed class ProjectsBearerTokenHandler(string apiToken) : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        request.Headers.Authorization ??= new AuthenticationHeaderValue("Bearer", apiToken);
        return base.SendAsync(request, cancellationToken);
    }
}
