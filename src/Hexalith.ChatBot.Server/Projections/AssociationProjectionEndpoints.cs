using Dapr;
using System.Security.Cryptography;
using System.Text;

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
        string topicName,
        string deadLetterTopic)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        ArgumentException.ThrowIfNullOrWhiteSpace(pubSubName);
        ArgumentException.ThrowIfNullOrWhiteSpace(topicName);
        ArgumentException.ThrowIfNullOrWhiteSpace(deadLetterTopic);

        _ = endpoints
            .MapPost(
                AssociationRoute,
                static async (
                    HttpContext context,
                    IConfiguration configuration,
                    PublishedAssociationEvent published,
                    AssociationProjectionHandler handler,
                    CancellationToken cancellationToken) =>
                {
                    string? expected = configuration["APP_API_TOKEN"];
                    Microsoft.Extensions.Primitives.StringValues presented = context.Request.Headers["dapr-api-token"];
                    if (string.IsNullOrWhiteSpace(expected) || presented.Count != 1 || string.IsNullOrWhiteSpace(presented[0]) ||
                        !CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(expected), Encoding.UTF8.GetBytes(presented[0]!)))
                    {
                        return Results.StatusCode(StatusCodes.Status401Unauthorized);
                    }

                    AssociationNotification? notification =
                        AssociationProjectionTranslator.TryCreateNotification(published);
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
