using System.Buffers.Binary;
using System.Net;

using Dapr.DurableTask.Protobuf;

using Google.Protobuf;

namespace Hexalith.ChatBot.Server.Tests.Lifecycle;

/// <summary>Returns a controlled gRPC response through the production Dapr workflow client.</summary>
internal sealed class WorkflowStateTransport : HttpMessageHandler, IHttpClientFactory
{
    public HttpClient CreateClient(string name) => new(this, disposeHandler: false);
    public GetInstanceResponse Response { get; set; } = new();
    public bool Unavailable { get; set; }
    public string? RequestedPath { get; private set; }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        RequestedPath = request.RequestUri?.AbsolutePath;
        if (Unavailable)
        {
            throw new HttpRequestException("Injected workflow transport outage.");
        }
        byte[] payload = Response.ToByteArray();
        byte[] frame = new byte[payload.Length + 5];
        BinaryPrimitives.WriteInt32BigEndian(frame.AsSpan(1, 4), payload.Length);
        payload.CopyTo(frame, 5);
        HttpResponseMessage response = new(HttpStatusCode.OK) { Version = new Version(2, 0), Content = new ByteArrayContent(frame) };
        response.Content.Headers.ContentType = new("application/grpc");
        response.TrailingHeaders.Add("grpc-status", "0");
        return Task.FromResult(response);
    }
}
