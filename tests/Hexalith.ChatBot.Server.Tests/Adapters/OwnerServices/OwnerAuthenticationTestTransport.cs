namespace Hexalith.ChatBot.Server.Tests.Adapters.OwnerServices;

internal sealed class OwnerAuthenticationTestTransport(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send)
    : HttpMessageHandler, IHttpClientFactory
{
    private readonly List<HttpClient> _clients = [];
    public HttpClient CreateClient(string name)
    {
        HttpClient client = new(this, disposeHandler: false);
        _clients.Add(client);
        return client;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => send(request, cancellationToken);

    protected override void Dispose(bool disposing)
    {
        if (disposing) foreach (HttpClient client in _clients) client.Dispose();
        base.Dispose(disposing);
    }
}
