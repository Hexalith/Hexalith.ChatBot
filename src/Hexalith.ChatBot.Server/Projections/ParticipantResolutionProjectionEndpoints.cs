using Hexalith.EventStore.DomainService;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Hexalith.ChatBot.Server.Projections;

/// <summary>
/// Projects published participant-resolution events into their read model. The chatbot events subscription delivers
/// each event through <see cref="ChatBotProjectionFanOutEndpoints"/>, which runs <see cref="ProjectAsync"/>.
/// </summary>
internal static class ParticipantResolutionProjectionEndpoints
{
    /// <summary>The sidecar-guarded route that projects one published participant-resolution event.</summary>
    public const string ParticipantResolutionRoute = "/chatbot/events/participant-resolutions";

    /// <summary>Maps the sidecar-guarded participant-resolution projection endpoint.</summary>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <returns>The endpoint route builder, for chaining.</returns>
    public static IEndpointRouteBuilder MapParticipantResolutionProjectionEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        _ = endpoints
            .MapPost(ParticipantResolutionRoute, ProjectAsync)
            .RequireEventStoreSidecarChannel();

        return endpoints;
    }

    /// <summary>Projects one published participant-resolution event.</summary>
    /// <param name="published">The published event envelope.</param>
    /// <param name="handler">The participant-resolution projection handler.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>An empty 200 result; foreign events are acknowledged without projection.</returns>
    public static async Task<IResult> ProjectAsync(
        PublishedParticipantResolutionEvent published,
        ParticipantResolutionProjectionHandler handler,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(handler);

        ParticipantResolutionNotification? notification =
            ParticipantResolutionProjectionTranslator.TryCreateNotification(published);
        if (notification is null)
        {
            return Results.Ok();
        }

        _ = await handler.HandleAsync(notification, cancellationToken).ConfigureAwait(false);
        return Results.Ok();
    }
}
