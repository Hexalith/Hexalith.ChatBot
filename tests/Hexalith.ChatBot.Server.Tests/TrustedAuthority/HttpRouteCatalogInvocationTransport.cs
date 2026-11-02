using System.Net;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Hexalith.ChatBot.Server.Tests.TrustedAuthority;

/// <summary>Delivers the SDK sender's request to the in-process receiver while retaining its response lifetime.</summary>
internal sealed class HttpRouteCatalogInvocationTransport(HttpContext context, Func<Task> proceed) : HttpMessageHandler
{
    /// <inheritdoc/>
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        context.RequestServices.GetRequiredService<HttpRouteCatalogFixture>().TransformInvocation?.Invoke(request);
        // Only transmitted headers reach the receiver, including removals by the SDK sender or a control.
        context.Request.Headers.Clear();
        foreach (var header in request.Headers)
        {
            context.Request.Headers[header.Key] = header.Value.ToArray();
        }
        if (request.Content is not null)
        {
            foreach (var header in request.Content.Headers)
            {
                context.Request.Headers[header.Key] = header.Value.ToArray();
            }
            context.Request.Body = await request.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        }
        else { context.Request.Body = Stream.Null; }
        await proceed().ConfigureAwait(false);
        return new HttpResponseMessage((HttpStatusCode)context.Response.StatusCode);
    }
}
