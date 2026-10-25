namespace Hexalith.ChatBot.Server.Tests.Adapters.Folders;

/// <summary>Supplies HTTP responses to exercise the generated Folders client.</summary>
/// <param name="response">The response for each request.</param>
internal sealed class FoldersResponseTransport(Func<HttpRequestMessage, HttpResponseMessage> response) : HttpMessageHandler
{
    /// <inheritdoc />
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(response(request));
    }
}
