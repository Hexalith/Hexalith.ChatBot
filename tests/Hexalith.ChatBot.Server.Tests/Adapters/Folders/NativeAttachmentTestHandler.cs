namespace Hexalith.ChatBot.Server.Tests.Adapters.Folders;
internal sealed class NativeAttachmentTestHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send) : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => send(request, cancellationToken);
    internal Task<HttpResponseMessage> ForwardAsync(HttpRequestMessage request, CancellationToken token) => base.SendAsync(request, token);
}
