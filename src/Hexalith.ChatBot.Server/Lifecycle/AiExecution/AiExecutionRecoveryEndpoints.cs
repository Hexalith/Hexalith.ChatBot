using Hexalith.ChatBot.Server.Authentication;
using Hexalith.ChatBot.Server.Authorization;
using Hexalith.ChatBot.Server.Gateway;
using Hexalith.ChatBot.Server.Audit;

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
                string? cursor,
                int? pageSize,
                CancellationToken cancellationToken) =>
            {
                ChatBotRequestContext? context = contextResolver.ResolveCurrent();
                if (context is null || !(await authorizer.AuthorizeAsync(context, QueryType, true, new { }, cancellationToken).ConfigureAwait(false)).IsAllowed)
                {
                    return SafeNotFound();
                }

                string scope = $"tenant:{context.TenantId}:actor:{context.SubjectId}";

                if (!cursorCodec.TryDecode(cursor, QueryType, scope, out string? afterKey, out _))
                {
                    return Results.BadRequest(new { code = "invalid_exhausted_work_cursor" });
                }

                int take = Math.Clamp(pageSize ?? 50, 1, 100);
                IReadOnlyList<AiExecutionWorkItem> rows = await workStore
                    .ListExhaustedAsync(afterKey, take + 1, cancellationToken, context.TenantId)
                    .ConfigureAwait(false);
                bool hasMore = rows.Count > take;
                AiExecutionWorkItem[] visible = rows.Take(take).ToArray();
                string? nextCursor = hasMore
                    ? cursorCodec.Encode(QueryType, scope, visible[^1].Key)
                    : null;
                return Results.Ok(new AiExecutionExhaustedPage(
                    visible.Select(ToOperatorRow).ToArray(),
                    nextCursor,
                    hasMore,
                    take));
            });

        _ = app.MapPost(
            "/api/v1/operations/ai-executions/exhausted/recover",
            async (
                HttpContext httpContext,
                ChatBotRequestContextResolver contextResolver,
                ChatBotRequestAuthorizer authorizer,
                IAiExecutionWorkStore workStore,
                AiExecutionRecoveryRequest request,
                ISystemClock clock,
                CancellationToken cancellationToken) =>
            {
                if (string.IsNullOrWhiteSpace(request.Key))
                {
                    return SafeNotFound();
                }

                ChatBotRequestContext? context = contextResolver.ResolveCurrent();
                if (context is null || !(await authorizer.AuthorizeAsync(context, AiExecutionRecoveryOperations.Recover, false, request, cancellationToken).ConfigureAwait(false)).IsAllowed)
                {
                    return SafeNotFound();
                }

                bool recovered = await workStore
                    .RecoverExhaustedAsync(request.Key, clock.UtcNow, cancellationToken, context.TenantId)
                    .ConfigureAwait(false);
                return recovered
                    ? Results.Ok(new { status = "recovered", key = request.Key })
                    : SafeNotFound();
            });

        return app;
    }

    private static IResult SafeNotFound() => Results.NotFound(new { code = ChatBotAuthorizationReasonCodes.SafeNotFound });

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
