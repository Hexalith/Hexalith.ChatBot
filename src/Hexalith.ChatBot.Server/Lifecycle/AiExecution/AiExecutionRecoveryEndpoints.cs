using System.Security.Claims;

using Hexalith.EventStore.Client.Queries;

namespace Hexalith.ChatBot.Server.Lifecycle.AiExecution;

internal static class AiExecutionRecoveryEndpoints
{
    private const string QueryType = "chatbot-ai-execution-exhausted";

    public static WebApplication MapAiExecutionRecoveryEndpoints(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);
        _ = app.MapGet(
            "/api/v1/operations/ai-executions/exhausted",
            async (
                HttpContext httpContext,
                IAiExecutionWorkStore workStore,
                IQueryCursorCodec cursorCodec,
                string? cursor,
                int? pageSize,
                CancellationToken cancellationToken) =>
            {
                if (!TryAuthenticatedScope(httpContext.User, out string scope))
                {
                    return Results.StatusCode(StatusCodes.Status401Unauthorized);
                }

                if (!cursorCodec.TryDecode(cursor, QueryType, scope, out string? afterKey, out _))
                {
                    return Results.BadRequest(new { code = "invalid_exhausted_work_cursor" });
                }

                int take = Math.Clamp(pageSize ?? 50, 1, 100);
                IReadOnlyList<AiExecutionWorkItem> rows = await workStore
                    .ListExhaustedAsync(afterKey, take + 1, cancellationToken)
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
                IAiExecutionWorkStore workStore,
                AiExecutionRecoveryRequest request,
                CancellationToken cancellationToken) =>
            {
                if (!TryAuthenticatedScope(httpContext.User, out _))
                {
                    return Results.StatusCode(StatusCodes.Status401Unauthorized);
                }

                if (string.IsNullOrWhiteSpace(request.Key))
                {
                    return Results.BadRequest(new { code = "invalid_exhausted_work_identity" });
                }

                bool recovered = await workStore
                    .RecoverExhaustedAsync(request.Key, DateTimeOffset.UtcNow, cancellationToken)
                    .ConfigureAwait(false);
                return recovered
                    ? Results.Ok(new { status = "recovered", key = request.Key })
                    : Results.NotFound(new { code = "exhausted_work_not_found" });
            });

        return app;
    }

    private static bool TryAuthenticatedScope(ClaimsPrincipal principal, out string scope)
    {
        string? actor = principal.FindFirst("sub")?.Value ?? principal.Identity?.Name;
        scope = $"operator:{actor}";
        return principal.Identity?.IsAuthenticated is true && !string.IsNullOrWhiteSpace(actor);
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

internal sealed record AiExecutionRecoveryRequest(string Key);

internal sealed record AiExecutionExhaustedPage(
    IReadOnlyList<AiExecutionExhaustedRow> Items,
    string? NextCursor,
    bool HasMore,
    int PageSize);

internal sealed record AiExecutionExhaustedRow(
    string Key,
    string TenantId,
    string ProjectId,
    string StateOwnerAggregateId,
    string ResponseId,
    string GenerationId,
    long StartedSourceVersion,
    int AttemptCount,
    int TerminalSubmissionAttemptCount,
    string FailureReason,
    DateTimeOffset UpdatedAtUtc);
