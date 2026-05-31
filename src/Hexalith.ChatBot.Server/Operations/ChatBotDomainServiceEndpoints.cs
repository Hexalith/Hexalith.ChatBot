using Hexalith.EventStore.Contracts.Commands;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Hexalith.ChatBot.Server.Operations;

/// <summary>
/// Maps the EventStore domain-service endpoint for the <c>chatbot</c> domain. The EventStore aggregate actor
/// resolves this app by convention (domain <c>chatbot</c> → DAPR app id <c>chatbot</c>, method <c>process</c>)
/// and invokes <see cref="ProcessRoute"/> with a <c>DomainServiceRequest</c>, expecting a
/// <c>DomainServiceWireResult</c>. The route is reachable only by the EventStore app under the deny-by-default
/// DAPR access-control policy.
/// </summary>
internal static class ChatBotDomainServiceEndpoints
{
    /// <summary>The domain-service process route the EventStore aggregate actor invokes (method <c>process</c>).</summary>
    public const string ProcessRoute = "/process";

    /// <summary>Maps the chatbot domain-service <c>/process</c> endpoint.</summary>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <returns>The endpoint route builder, for chaining.</returns>
    public static IEndpointRouteBuilder MapChatBotDomainServiceEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        _ = endpoints
            .MapPost(
                ProcessRoute,
                static async (DomainServiceRequest request, ChatBotDomainServiceRequestHandler handler)
                    => await handler.ProcessAsync(request).ConfigureAwait(false))
            .WithName("ProcessChatBotDomainCommand");

        return endpoints;
    }
}
