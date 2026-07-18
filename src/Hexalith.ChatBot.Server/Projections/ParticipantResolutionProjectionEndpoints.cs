using Dapr;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Hexalith.ChatBot.Server.Projections;

internal static class ParticipantResolutionProjectionEndpoints
{
    public const string ParticipantResolutionRoute = "/chatbot/events/participant-resolutions";

    public static IEndpointRouteBuilder MapParticipantResolutionProjectionEndpoints(
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
                ParticipantResolutionRoute,
                static async (
                    PublishedParticipantResolutionEvent published,
                    ParticipantResolutionProjectionHandler handler,
                    CancellationToken cancellationToken) =>
                {
                    ParticipantResolutionNotification? notification =
                        ParticipantResolutionProjectionTranslator.TryCreateNotification(published);
                    if (notification is null)
                    {
                        return Results.Ok();
                    }

                    _ = await handler.HandleAsync(notification, cancellationToken).ConfigureAwait(false);
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
