using System.Net;

namespace Hexalith.ChatBot.IntegrationTests.Recovery;

/// <summary>Small deterministic HTTP response sequencer for durable-state probe contract tests.</summary>
internal sealed class SequenceHttpMessageHandler(Func<int, HttpRequestMessage, HttpResponseMessage> responseFactory)
    : HttpMessageHandler
{
    private int _requests;

    public int Requests => Volatile.Read(ref _requests);

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        int requestNumber = Interlocked.Increment(ref _requests);
        HttpResponseMessage response = responseFactory(requestNumber, request);
        response.RequestMessage ??= request;
        return Task.FromResult(response);
    }

    public static HttpResponseMessage Json(string json)
        => new(HttpStatusCode.OK)
        {
            Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json"),
        };
}
