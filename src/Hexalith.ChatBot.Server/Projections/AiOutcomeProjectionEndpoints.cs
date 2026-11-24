using System.Text.Json;
using Hexalith.EventStore.DomainService;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Hexalith.ChatBot.Server.Projections;

/// <summary>
/// Projects governed AI outcome events into the S1 conversation. The chatbot events subscription delivers each event
/// through <see cref="ChatBotProjectionFanOutEndpoints"/>, which runs <see cref="ProjectAsync"/>.
/// </summary>
internal static class AiOutcomeProjectionEndpoints
{
    /// <summary>The sidecar-guarded route that projects one published AI outcome event.</summary>
    public const string AiOutcomeRecordedRoute = "/chatbot/events/ai-outcomes";

    /// <summary>Maps the sidecar-guarded AI outcome projection endpoint.</summary>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <returns>The endpoint route builder, for chaining.</returns>
    public static IEndpointRouteBuilder MapAiOutcomeProjectionEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        _ = endpoints
            .MapPost(AiOutcomeRecordedRoute, ProjectAsync)
            .RequireEventStoreSidecarChannel();

        return endpoints;
    }

    /// <summary>Projects one published AI outcome or AI action execution event.</summary>
    /// <param name="published">The published event body.</param>
    /// <param name="handler">The AI outcome projection handler.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>An empty 200 result; foreign events are acknowledged without projection.</returns>
    public static async Task<IResult> ProjectAsync(
        JsonElement published,
        AiOutcomeProjectionHandler handler,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(handler);

        _ = await handler.HandleAsync(published, cancellationToken).ConfigureAwait(false);
        return Results.Ok();
    }
}
