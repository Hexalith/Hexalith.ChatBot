using Hexalith.EventStore.DomainService;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Hexalith.ChatBot.Server.Projections;

/// <summary>
/// Projects published EventStore governed-note and governed-control events into the tenant-partitioned read models.
/// The chatbot events subscription delivers each event through <see cref="ChatBotProjectionFanOutEndpoints"/>, which
/// runs <see cref="ProjectAsync"/>; the event arrives as the CloudEvent <c>data</c> (after <c>UseCloudEvents</c>).
/// Tenant and source version are derived from the <b>verified</b> envelope metadata (M1/M2), never from a
/// caller-supplied body, and the route is reachable only by the chatbot sidecar under the deny-by-default DAPR
/// access-control policy.
/// </summary>
internal static class GovernedOperationProjectionEndpoints
{
    /// <summary>The sidecar-guarded route that projects one published governed-operation event.</summary>
    public const string GovernedNoteRecordedRoute = "/chatbot/events/governed-operations";

    /// <summary>Maps the sidecar-guarded governed operation projection endpoint.</summary>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <returns>The endpoint route builder, for chaining.</returns>
    public static IEndpointRouteBuilder MapGovernedOperationProjectionEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        _ = endpoints
            .MapPost(GovernedNoteRecordedRoute, ProjectAsync)
            .RequireEventStoreSidecarChannel();

        return endpoints;
    }

    /// <summary>Projects one published governed-note or governed-control event.</summary>
    /// <param name="published">The published event envelope.</param>
    /// <param name="operationHandler">The governed operation projection handler.</param>
    /// <param name="controlHandler">The governed control state projection handler.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>An empty 200 result; foreign events are acknowledged without projection.</returns>
    public static async Task<IResult> ProjectAsync(
        PublishedGovernedOperationEvent published,
        GovernedOperationProjectionHandler operationHandler,
        GovernedControlStateProjectionHandler controlHandler,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(operationHandler);
        ArgumentNullException.ThrowIfNull(controlHandler);

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
    }
}
