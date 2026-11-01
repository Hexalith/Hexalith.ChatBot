using Hexalith.ChatBot.Server.Gateway;
using Hexalith.ChatBot.Server.Gateway.Correlation;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;

namespace Hexalith.ChatBot.Server.Authentication;

/// <summary>
/// Turns a refused ChatBot boundary policy into ChatBot's typed, metadata-only denial instead of the framework's bare
/// challenge or forbid, so the boundary keeps the published problem-details contract. Every other policy keeps the
/// framework result.
/// </summary>
internal sealed class ChatBotApplicationBoundaryResultHandler : IAuthorizationMiddlewareResultHandler
{
    private readonly AuthorizationMiddlewareResultHandler _frameworkHandler = new();

    /// <inheritdoc />
    public async Task HandleAsync(
        RequestDelegate next,
        HttpContext context,
        AuthorizationPolicy policy,
        PolicyAuthorizationResult authorizeResult)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(authorizeResult);

        if (authorizeResult.Succeeded || !RequiresChatBotBoundary(context.GetEndpoint()))
        {
            await _frameworkHandler.HandleAsync(next, context, policy, authorizeResult).ConfigureAwait(false);
            return;
        }

        // A challenge means no human bearer credential authenticated; a forbid means one did but is not a human
        // credential kind. Neither outcome names the route's resource, matching the guards behind the boundary.
        string reasonCode = authorizeResult.Challenged
            ? ChatBotAuthorizationReasonCodes.AuthenticationDenied
            : ChatBotAuthorizationReasonCodes.AuthorizationDenied;

        // Authorization runs ahead of the ChatBot correlation middleware, so the denial sets the correlation headers
        // that middleware would otherwise add, keeping them equal to the body's metadata.
        ChatBotCorrelationContext correlation = context.GetCorrelationContext();
        context.Response.Headers["X-Correlation-Id"] = correlation.CorrelationId;
        if (correlation.TaskId is not null)
        {
            context.Response.Headers["X-Hexalith-Task-Id"] = correlation.TaskId;
        }

        IChatBotProblemDetailsFactory problemDetailsFactory = context.RequestServices.GetRequiredService<IChatBotProblemDetailsFactory>();
        await CommandGatewayHttpResults.ToHttpResult(ChatBotGatewayResult.Denied(
                problemDetailsFactory.CreateAuthorizationProblem(reasonCode, correlation.CorrelationId, correlation.TaskId)))
            .ExecuteAsync(context)
            .ConfigureAwait(false);
    }

    private static bool RequiresChatBotBoundary(Endpoint? endpoint)
        => endpoint?.Metadata.GetOrderedMetadata<IAuthorizeData>()
            .Any(static data => string.Equals(data.Policy, ChatBotApplicationBoundaryPolicy.Name, StringComparison.Ordinal)) == true;
}
