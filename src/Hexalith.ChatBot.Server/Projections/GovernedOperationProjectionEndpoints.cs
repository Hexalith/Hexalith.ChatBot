using Dapr;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Hexalith.ChatBot.Server.Projections;

/// <summary>
/// Maps the DAPR pub/sub subscriber endpoint that projects published EventStore governed-note events into the
/// tenant-partitioned read model. The endpoint subscribes (via <c>WithTopic</c> + <c>MapSubscribeHandler</c>)
/// to the chatbot events topic on the chatbot pub/sub component; the EventStore-stamped envelope arrives as the
/// CloudEvent <c>data</c> (after <c>UseCloudEvents</c>). Tenant and source version are derived from the
/// <b>verified</b> envelope metadata (M1/M2), never from a caller-supplied body, and the route is reachable
/// only by the chatbot sidecar under the deny-by-default DAPR access-control policy.
/// </summary>
internal static class GovernedOperationProjectionEndpoints
{
    /// <summary>The route the chatbot events subscription delivers published governed-operation events to.</summary>
    public const string GovernedNoteRecordedRoute = "/chatbot/events/governed-operations";

    /// <summary>Maps the governed operation projection subscriber endpoint and its DAPR topic subscription.</summary>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <param name="pubSubName">The DAPR pub/sub component name (e.g. <c>chatbot-pubsub</c>).</param>
    /// <param name="topicName">The topic the EventStore publishes chatbot events to.</param>
    /// <returns>The endpoint route builder, for chaining.</returns>
    public static IEndpointRouteBuilder MapGovernedOperationProjectionEndpoints(
        this IEndpointRouteBuilder endpoints,
        string pubSubName,
        string topicName)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        ArgumentException.ThrowIfNullOrWhiteSpace(pubSubName);
        ArgumentException.ThrowIfNullOrWhiteSpace(topicName);

        _ = endpoints
            .MapPost(
                GovernedNoteRecordedRoute,
                static async (
                    PublishedGovernedOperationEvent published,
                    GovernedOperationProjectionHandler operationHandler,
                    GovernedControlStateProjectionHandler controlHandler,
                    CancellationToken cancellationToken) =>
                {
                    GovernedNoteRecordedNotification? notification =
                        GovernedOperationProjectionTranslator.TryCreateNotification(published);
                    if (notification is not null)
                    {
                        // The outcome (applied vs ignored-as-duplicate) is intentionally not surfaced: both are an
                        // idempotent success to the at-least-once publisher, and the response stays metadata-free so
                        // nothing leaks to a redelivery path.
                        _ = await operationHandler.HandleAsync(notification, cancellationToken).ConfigureAwait(false);
                        return Results.Ok();
                    }

                    GovernedControlStateProjectionNotification? controlNotification =
                        GovernedControlStateProjectionTranslator.TryCreateNotification(published);
                    if (controlNotification is not null)
                    {
                        _ = await controlHandler.HandleAsync(controlNotification, cancellationToken).ConfigureAwait(false);
                    }

                    return Results.Ok();
                })
            .WithTopic(pubSubName, topicName);

        return endpoints;
    }
}
