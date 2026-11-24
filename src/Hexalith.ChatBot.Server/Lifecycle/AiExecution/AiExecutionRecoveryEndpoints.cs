using Hexalith.ChatBot.Server.Authentication;
using Hexalith.ChatBot.Server.Authorization;
using Hexalith.ChatBot.Server.Gateway;
using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Gateway.Correlation;
using Hexalith.ChatBot.Server.Gateway.Stages;

using Hexalith.EventStore.Client.Queries;

namespace Hexalith.ChatBot.Server.Lifecycle.AiExecution;

internal static class AiExecutionRecoveryEndpoints
{
    private const string QueryType = AiExecutionRecoveryOperations.List;

    public static WebApplication MapAiExecutionRecoveryEndpoints(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);
        _ = app.MapGet(
            "/api/v1/operations/ai-executions/exhausted",
            async (
                HttpContext httpContext,
                ChatBotRequestContextResolver contextResolver,
                ChatBotRequestAuthorizer authorizer,
                IAiExecutionWorkStore workStore,
                IQueryCursorCodec cursorCodec,
                IChatBotProblemDetailsFactory problemDetailsFactory,
                string? cursor,
                int? pageSize,
                CancellationToken cancellationToken) =>
            {
                ChatBotRequestContext? context = contextResolver.ResolveCurrent();
                ChatBotAuthorityDecision? decision = context is null ? null : await authorizer.AuthorizeAsync(context, QueryType, true, new { }, cancellationToken).ConfigureAwait(false);
                if (context is null || decision?.IsAllowed != true)
                {
                    return SafeNotFound(httpContext, problemDetailsFactory);
                }

                string scope = $"tenant:{context.TenantId}:actor:{context.SubjectId}";

                if (!cursorCodec.TryDecode(cursor, QueryType, scope, out string? afterKey, out _))
                {
                    return InvalidCursor(httpContext, problemDetailsFactory);
                }

                int take = Math.Clamp(pageSize ?? 50, 1, 100);
                IReadOnlyList<AiExecutionWorkItem> rows;
                try
                {
                    rows = await workStore.ListExhaustedAsync(afterKey, take + 1, cancellationToken, context.TenantId,
                        () => authorizer.IsCurrent(decision.Principal!)).ConfigureAwait(false);
                }
                catch (ChatBotAuthorityLapsedException) { return SafeNotFound(httpContext, problemDetailsFactory); }
                if (!authorizer.IsCurrent(decision.Principal!)) { return SafeNotFound(httpContext, problemDetailsFactory); }
                bool hasMore = rows.Count > take;
                AiExecutionWorkItem[] visible = rows.Take(take).ToArray();
                string? nextCursor = hasMore
                    ? cursorCodec.Encode(QueryType, scope, visible[^1].Key)
                    : null;
                if (!authorizer.IsCurrent(decision.Principal!)) { return SafeNotFound(httpContext, problemDetailsFactory); }
                return Results.Ok(new AiExecutionExhaustedPage(
                    visible.Select(ToOperatorRow).ToArray(),
                    nextCursor,
                    hasMore,
                    take));
            })
            .RequireAuthorization(ChatBotApplicationBoundaryPolicy.Name);

        _ = app.MapPost(
            "/api/v1/operations/ai-executions/exhausted/recover",
            async (
                HttpContext httpContext,
                ChatBotRequestContextResolver contextResolver,
                ChatBotRequestAuthorizer authorizer,
                IAiExecutionWorkStore workStore,
                AiExecutionRecoveryRequest request,
                ISystemClock clock,
                IChatBotProblemDetailsFactory problemDetailsFactory,
                CancellationToken cancellationToken) =>
            {
                if (string.IsNullOrWhiteSpace(request.Key))
                {
                    return SafeNotFound(httpContext, problemDetailsFactory);
                }

                ChatBotRequestContext? context = contextResolver.ResolveCurrent();
                ChatBotAuthorityDecision? decision = context is null ? null : await authorizer.AuthorizeAsync(context, AiExecutionRecoveryOperations.Recover, false, request, cancellationToken).ConfigureAwait(false);
                if (context is null || decision?.IsAllowed != true)
                {
                    return SafeNotFound(httpContext, problemDetailsFactory);
                }

                bool recovered;
                try
                {
                    recovered = await workStore.RecoverExhaustedAsync(request.Key, clock.UtcNow, cancellationToken, context.TenantId,
                        () => authorizer.IsCurrent(decision.Principal!)).ConfigureAwait(false);
                }
                catch (ChatBotAuthorityLapsedException) { return SafeNotFound(httpContext, problemDetailsFactory); }
                return recovered
                    ? Results.Ok(new { status = "recovered", key = request.Key })
                    : SafeNotFound(httpContext, problemDetailsFactory);
            })
            .RequireAuthorization(ChatBotApplicationBoundaryPolicy.Name);

        return app;
    }

    /// <summary>
    /// Returns the catalog-backed safe-not-found problem that the read routes return, so an unauthorized,
    /// blank, unknown or lapsed recovery request stays indistinguishable from any other hidden resource.
    /// </summary>
    /// <param name="httpContext">The current request, which carries the response correlation.</param>
    /// <param name="problemDetailsFactory">The versioned problem catalog factory.</param>
    /// <returns>The redacted problem+json result.</returns>
    private static IResult SafeNotFound(HttpContext httpContext, IChatBotProblemDetailsFactory problemDetailsFactory)
    {
        ChatBotCorrelationContext correlationContext = httpContext.GetCorrelationContext();
        return CommandGatewayHttpResults.ToHttpResult(ChatBotGatewayResult.Denied(
            problemDetailsFactory.CreateAuthorizationProblem(
                ChatBotAuthorizationReasonCodes.SafeNotFound,
                correlationContext.CorrelationId,
                correlationContext.TaskId)));
    }

    /// <summary>Returns the catalog-backed validation problem for an exhausted-work cursor that cannot be decoded.</summary>
    /// <param name="httpContext">The current request, which carries the response correlation.</param>
    /// <param name="problemDetailsFactory">The versioned problem catalog factory.</param>
    /// <returns>The redacted problem+json result.</returns>
    private static IResult InvalidCursor(HttpContext httpContext, IChatBotProblemDetailsFactory problemDetailsFactory)
    {
        ChatBotCorrelationContext correlationContext = httpContext.GetCorrelationContext();
        return CommandGatewayHttpResults.ToHttpResult(ChatBotGatewayResult.Denied(
            problemDetailsFactory.CreateValidationProblem(correlationContext.CorrelationId, correlationContext.TaskId)));
    }

    private static AiExecutionExhaustedRow ToOperatorRow(AiExecutionWorkItem item)
        => new(
            item.Key,
            item.TenantId,
            item.ProjectId,
            item.ConversationId,
            item.ResponseId,
            item.GenerationId,
            item.StartedSourceVersion,
            item.AttemptCount,
            item.TerminalSubmissionAttemptCount,
            item.FailureReason ?? "attempts-exhausted",
            item.UpdatedAtUtc);
}
