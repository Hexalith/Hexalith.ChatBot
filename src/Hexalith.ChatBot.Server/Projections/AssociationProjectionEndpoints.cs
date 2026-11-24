using Hexalith.EventStore.DomainService;
using System.Security.Cryptography;
using System.Text;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Hexalith.ChatBot.Server.Projections;

/// <summary>
/// Projects published association events into the association read model. The chatbot events subscription delivers
/// each event through <see cref="ChatBotProjectionFanOutEndpoints"/>, which runs <see cref="ProjectAsync"/>.
/// </summary>
internal static class AssociationProjectionEndpoints
{
    /// <summary>The sidecar-guarded route that projects one published association event.</summary>
    public const string AssociationRoute = "/chatbot/events/associations";

    /// <summary>Maps the sidecar-guarded association projection endpoint.</summary>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <returns>The endpoint route builder, for chaining.</returns>
    public static IEndpointRouteBuilder MapAssociationProjectionEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        _ = endpoints
            .MapPost(AssociationRoute, VerifyChannelAndProjectAsync)
            .RequireEventStoreSidecarChannel();

        return endpoints;
    }

    /// <summary>
    /// Verifies the application-channel credential, then projects one published association event.
    /// </summary>
    /// <param name="context">The delivery request context.</param>
    /// <param name="configuration">The host configuration holding the application-channel token.</param>
    /// <param name="published">The published event envelope.</param>
    /// <param name="handler">The association projection handler.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>401 without a single matching channel credential; otherwise an empty 200 result.</returns>
    public static async Task<IResult> VerifyChannelAndProjectAsync(
        HttpContext context,
        IConfiguration configuration,
        PublishedAssociationEvent published,
        AssociationProjectionHandler handler,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(configuration);

        string? expected = configuration["APP_API_TOKEN"];
        Microsoft.Extensions.Primitives.StringValues presented = context.Request.Headers["dapr-api-token"];
        if (string.IsNullOrWhiteSpace(expected) || presented.Count != 1 || string.IsNullOrWhiteSpace(presented[0]) ||
            !CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(expected), Encoding.UTF8.GetBytes(presented[0]!)))
        {
            return Results.StatusCode(StatusCodes.Status401Unauthorized);
        }

        return await ProjectAsync(published, handler, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Projects one published association event whose application channel the caller already verified, as the
    /// sidecar-guarded fan-out route does.
    /// </summary>
    /// <param name="published">The published event envelope.</param>
    /// <param name="handler">The association projection handler.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>An empty 200 result; foreign events are acknowledged without projection.</returns>
    public static async Task<IResult> ProjectAsync(
        PublishedAssociationEvent published,
        AssociationProjectionHandler handler,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(handler);

        AssociationNotification? notification =
            AssociationProjectionTranslator.TryCreateNotification(published);
        if (notification is null)
        {
            return Results.Ok();
        }

        _ = await handler.HandleAsync(notification, cancellationToken).ConfigureAwait(false);
        return Results.Ok();
    }
}
