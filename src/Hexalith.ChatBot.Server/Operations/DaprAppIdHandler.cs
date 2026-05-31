namespace Hexalith.ChatBot.Server.Operations;

/// <summary>
/// Tags an outbound EventStore gateway request with the <c>dapr-app-id</c> header so it is routed by the
/// caller's own DAPR sidecar to the <c>eventstore</c> app via DAPR service invocation. The receiving EventStore
/// sidecar then injects the verified <c>dapr-caller-app-id: chatbot</c> header, which EventStore's DaprInternal
/// authentication scheme validates against its allow-list — so the chatbot submits commands without forging a
/// user JWT (it is a trusted domain service behind the EventStore auth boundary, exactly like <c>tenants</c>).
/// Mirrors the canonical <c>DaprAppIdHandler</c> in the EventStore sample/admin UIs (which is internal to those
/// assemblies and not exported by any published package).
/// </summary>
internal sealed class DaprAppIdHandler(string appId, string? apiToken) : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        _ = request.Headers.TryAddWithoutValidation("dapr-app-id", appId);
        if (!string.IsNullOrEmpty(apiToken))
        {
            _ = request.Headers.TryAddWithoutValidation("dapr-api-token", apiToken);
        }

        return base.SendAsync(request, cancellationToken);
    }
}
