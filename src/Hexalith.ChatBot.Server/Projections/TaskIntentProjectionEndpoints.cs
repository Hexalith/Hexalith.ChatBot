using Dapr;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Hexalith.ChatBot.Server.Projections;

internal static class TaskIntentProjectionEndpoints
{
    public const string TaskIntentCapturedRoute = "/chatbot/events/task-intents";

    public static IEndpointRouteBuilder MapTaskIntentProjectionEndpoints(
        this IEndpointRouteBuilder endpoints,
        string pubSubName,
        string topicName,
        string deadLetterTopic)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        ArgumentException.ThrowIfNullOrWhiteSpace(pubSubName);
        ArgumentException.ThrowIfNullOrWhiteSpace(topicName);
        ArgumentException.ThrowIfNullOrWhiteSpace(deadLetterTopic);

        _ = endpoints
            .MapPost(
                TaskIntentCapturedRoute,
                static async (
                    PublishedTaskIntentEvent published,
                    TaskIntentProjectionHandler handler,
                    CancellationToken cancellationToken) =>
                {
                    _ = await handler.HandleAsync(published, cancellationToken).ConfigureAwait(false);
                    return Results.Ok();
                })
            .WithTopic(new TopicOptions
            {
                PubsubName = pubSubName,
                Name = topicName,
                DeadLetterTopic = deadLetterTopic,
            });

        return endpoints;
    }
}
