using Hexalith.EventStore.DomainService;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Hexalith.ChatBot.Server.Projections;

/// <summary>
/// Projects published task-intent and conversation-message events into the project conversation. The chatbot events
/// subscription delivers each event through <see cref="ChatBotProjectionFanOutEndpoints"/>, which runs
/// <see cref="ProjectAsync"/>.
/// </summary>
internal static class TaskIntentProjectionEndpoints
{
    /// <summary>The sidecar-guarded route that projects one published task-intent event.</summary>
    public const string TaskIntentCapturedRoute = "/chatbot/events/task-intents";

    /// <summary>Maps the sidecar-guarded task-intent projection endpoint.</summary>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <returns>The endpoint route builder, for chaining.</returns>
    public static IEndpointRouteBuilder MapTaskIntentProjectionEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        _ = endpoints
            .MapPost(TaskIntentCapturedRoute, ProjectAsync)
            .RequireEventStoreSidecarChannel();

        return endpoints;
    }

    /// <summary>Projects one published task-intent event.</summary>
    /// <param name="published">The published event envelope.</param>
    /// <param name="handler">The task-intent projection handler.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>An empty 200 result; foreign events are acknowledged without projection.</returns>
    public static async Task<IResult> ProjectAsync(
        PublishedTaskIntentEvent published,
        TaskIntentProjectionHandler handler,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(handler);

        _ = await handler.HandleAsync(published, cancellationToken).ConfigureAwait(false);
        return Results.Ok();
    }
}
