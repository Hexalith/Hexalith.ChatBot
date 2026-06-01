using Dapr;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Hexalith.ChatBot.Server.Projections;

internal static class MailboxIntakeProjectionEndpoints
{
    public const string MailboxIntakeRoute = "/chatbot/events/mailbox-intakes";

    public static IEndpointRouteBuilder MapMailboxIntakeProjectionEndpoints(
        this IEndpointRouteBuilder endpoints,
        string pubSubName,
        string topicName)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        ArgumentException.ThrowIfNullOrWhiteSpace(pubSubName);
        ArgumentException.ThrowIfNullOrWhiteSpace(topicName);

        _ = endpoints
            .MapPost(
                MailboxIntakeRoute,
                static async (
                    PublishedMailboxIntakeEvent published,
                    AssociationProjectionHandler handler,
                    CancellationToken cancellationToken) =>
                {
                    MailboxIntakeProjectionNotification? notification =
                        MailboxIntakeProjectionTranslator.TryCreateNotification(published);
                    if (notification is null)
                    {
                        return Results.Ok();
                    }

                    _ = await handler
                        .HandleAsync(
                            notification.Captured,
                            notification.TenantId,
                            notification.SourceVersion,
                            notification.CorrelationId,
                            cancellationToken)
                        .ConfigureAwait(false);
                    return Results.Ok();
                })
            .WithTopic(pubSubName, topicName);

        return endpoints;
    }
}
