using Dapr;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Hexalith.ChatBot.Server.Projections;

internal static class AssociationProjectionEndpoints
{
    public const string AssociationRoute = "/chatbot/events/associations";

    public static IEndpointRouteBuilder MapAssociationProjectionEndpoints(
        this IEndpointRouteBuilder endpoints,
        string pubSubName,
        string topicName)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        ArgumentException.ThrowIfNullOrWhiteSpace(pubSubName);
        ArgumentException.ThrowIfNullOrWhiteSpace(topicName);

        _ = endpoints
            .MapPost(
                AssociationRoute,
                static async (
                    PublishedAssociationEvent published,
                    AssociationProjectionHandler handler,
                    CancellationToken cancellationToken) =>
                {
                    AssociationNotification? notification =
                        AssociationProjectionTranslator.TryCreateNotification(published);
                    if (notification is null)
                    {
                        return Results.Ok();
                    }

                    _ = await handler.HandleAsync(notification, cancellationToken).ConfigureAwait(false);
                    return Results.Ok();
                })
            .WithTopic(pubSubName, topicName);

        return endpoints;
    }
}
