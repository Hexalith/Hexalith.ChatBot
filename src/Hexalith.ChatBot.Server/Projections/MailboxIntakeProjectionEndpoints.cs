using Hexalith.EventStore.DomainService;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Hexalith.ChatBot.Server.Projections;

/// <summary>
/// Projects published mailbox-intake events into the association read model. The chatbot events subscription
/// delivers each event through <see cref="ChatBotProjectionFanOutEndpoints"/>, which runs <see cref="ProjectAsync"/>.
/// </summary>
internal static class MailboxIntakeProjectionEndpoints
{
    /// <summary>The sidecar-guarded route that projects one published mailbox-intake event.</summary>
    public const string MailboxIntakeRoute = "/chatbot/events/mailbox-intakes";

    /// <summary>Maps the sidecar-guarded mailbox-intake projection endpoint.</summary>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <returns>The endpoint route builder, for chaining.</returns>
    public static IEndpointRouteBuilder MapMailboxIntakeProjectionEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        _ = endpoints
            .MapPost(MailboxIntakeRoute, ProjectAsync)
            .RequireEventStoreSidecarChannel();

        return endpoints;
    }

    /// <summary>Projects one published mailbox-intake event.</summary>
    /// <param name="published">The published event envelope.</param>
    /// <param name="handler">The association projection handler.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>An empty 200 result; foreign events are acknowledged without projection.</returns>
    public static async Task<IResult> ProjectAsync(
        PublishedMailboxIntakeEvent published,
        AssociationProjectionHandler handler,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(handler);

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
    }
}
