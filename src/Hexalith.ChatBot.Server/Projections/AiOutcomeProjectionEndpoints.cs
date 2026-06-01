using Dapr;

using System.Text.Json;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Hexalith.ChatBot.Server.Projections;

/// <summary>
/// Maps the DAPR pub/sub subscriber endpoint that projects governed AI outcome events into the S1 conversation.
/// </summary>
internal static class AiOutcomeProjectionEndpoints
{
    /// <summary>The route the chatbot events subscription delivers published AI outcome events to.</summary>
    public const string AiOutcomeRecordedRoute = "/chatbot/events/ai-outcomes";

    /// <summary>Maps the AI outcome projection subscriber endpoint and its DAPR topic subscription.</summary>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <param name="pubSubName">The DAPR pub/sub component name.</param>
    /// <param name="topicName">The topic the EventStore publishes chatbot events to.</param>
    /// <returns>The endpoint route builder, for chaining.</returns>
    public static IEndpointRouteBuilder MapAiOutcomeProjectionEndpoints(
        this IEndpointRouteBuilder endpoints,
        string pubSubName,
        string topicName)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        ArgumentException.ThrowIfNullOrWhiteSpace(pubSubName);
        ArgumentException.ThrowIfNullOrWhiteSpace(topicName);

        _ = endpoints
            .MapPost(
                AiOutcomeRecordedRoute,
                static async (
                    JsonElement published,
                    AiOutcomeProjectionHandler handler,
                    CancellationToken cancellationToken) =>
                {
                    _ = await handler.HandleAsync(published, cancellationToken).ConfigureAwait(false);
                    return Results.Ok();
                })
            .WithTopic(pubSubName, topicName);

        return endpoints;
    }
}
