using System.Text.Json;

using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Gateway;
using Hexalith.ChatBot.Server.Gateway.Correlation;
using Hexalith.ChatBot.Server.Gateway.Stages;

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
    // Denial auditing never needs an entire command payload. Larger or malformed submissions retain the denial
    // with unknown command metadata, rather than spending unbounded resources to describe an unauthenticated call.
    private const int DeniedCommandMetadataLimit = 16 * 1024;
    private static readonly TimeSpan DeniedCommandMetadataReadDeadline = TimeSpan.FromMilliseconds(250);
    private static readonly ChatBotSpineCommandAllowlist CanonicalCommandNames = new();
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
        if (context.GetEndpoint() is RouteEndpoint endpoint &&
            string.Equals(endpoint.RoutePattern.RawText, "/api/v1/commands", StringComparison.OrdinalIgnoreCase) &&
            HttpMethods.IsPost(context.Request.Method))
        {
            // A missing audit writer is a host misconfiguration, not an audit outage, so it is resolved before the
            // catch below can answer it as a retryable dependency-unavailable outcome.
            IAuditWriter auditWriter = context.RequestServices.GetRequiredService<IAuditWriter>();

            // Only validated transport identity can name the caller. Body identity, resource and origin fields are
            // never authority, and a challenge must stay anonymous even if another identity was attached upstream.
            ChatBotRequestContext? actor = null;
            if (!authorizeResult.Challenged)
            {
                _ = ChatBotRequestContextResolver.TryResolve(context.User, ChatBotSurfaceOrigin.Api, out actor, out _);
            }

            string commandName = await ReadDeniedCommandNameAsync(context.Request, context.RequestAborted).ConfigureAwait(false);
            string tenantId = actor?.TenantId ?? "unavailable";
            try
            {
                await auditWriter
                    .RecordAuthorizationFailureAsync(new ChatBotAuthorizationFailureAuditFact(
                        tenantId,
                        actor?.SubjectId ?? "anonymous",
                        commandName,
                        reasonCode,
                        correlation.CorrelationId,
                        correlation.TaskId,
                        "api"), context.RequestAborted)
                    .ConfigureAwait(false);
            }
            catch (Exception exception) when (exception is not OperationCanceledException || !context.RequestAborted.IsCancellationRequested)
            {
                // A request abort propagates as the request's cancellation, whatever the audit store raised for it
                // (cancellation or a transport failure), so nothing is written to the aborted response. Any other
                // failure, including the store's own cancellation, is an unavailable audit path that must not become
                // an untyped host failure: answer the typed, retryable dependency-unavailable outcome, as the gateway's
                // admission path does for a non-cancellation audit failure. The denied request has no effect.
                context.RequestAborted.ThrowIfCancellationRequested();
                await CommandGatewayHttpResults.ToHttpResult(ChatBotGatewayResult.Denied(
                        problemDetailsFactory.CreateDependencyUnavailable(correlation.CorrelationId, correlation.TaskId)))
                    .ExecuteAsync(context)
                    .ConfigureAwait(false);
                return;
            }

            // The NFR43 spike counter is fed from the same path as the denial fact, with the same tenant value.
            if (context.RequestServices.GetService<IAuthorizationFailureCounter>() is { } failureCounter)
            {
                failureCounter.Record(tenantId, context.RequestServices.GetRequiredService<ISystemClock>().UtcNow);
            }
        }

        await CommandGatewayHttpResults.ToHttpResult(ChatBotGatewayResult.Denied(
                problemDetailsFactory.CreateAuthorizationProblem(reasonCode, correlation.CorrelationId, correlation.TaskId)))
            .ExecuteAsync(context)
            .ConfigureAwait(false);
    }

    private static bool RequiresChatBotBoundary(Endpoint? endpoint)
        => endpoint?.Metadata.GetOrderedMetadata<IAuthorizeData>()
            .Any(static data => string.Equals(data.Policy, ChatBotApplicationBoundaryPolicy.Name, StringComparison.Ordinal)) == true;

    private static async Task<string> ReadDeniedCommandNameAsync(HttpRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (request.ContentLength > DeniedCommandMetadataLimit ||
            !string.Equals(request.ContentType?.Split(';', 2)[0].Trim(), "application/json", StringComparison.OrdinalIgnoreCase))
        {
            return AuditMetadata.UnknownCommandName;
        }

        using CancellationTokenSource deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        // One deadline covers the whole best-effort read. WaitAsync also bounds a transport that ignores its token;
        // the private buffer is never reused or parsed after expiry. Audit always uses the original request token.
        deadline.CancelAfter(DeniedCommandMetadataReadDeadline);
        byte[] metadata = new byte[DeniedCommandMetadataLimit + 1];
        int length = 0;
        try
        {
            while (length < metadata.Length)
            {
                int read = await request.Body.ReadAsync(metadata.AsMemory(length), deadline.Token).AsTask()
                    .WaitAsync(deadline.Token).ConfigureAwait(false);
                if (read == 0)
                {
                    break;
                }

                length += read;
            }
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return AuditMetadata.UnknownCommandName;
        }
        catch (Exception exception) when (exception is IOException or BadHttpRequestException or InvalidDataException)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return AuditMetadata.UnknownCommandName;
        }

        if (length > DeniedCommandMetadataLimit)
        {
            return AuditMetadata.UnknownCommandName;
        }

        try
        {
            using JsonDocument document = JsonDocument.Parse(metadata.AsMemory(0, length));
            if (document.RootElement.ValueKind == JsonValueKind.Object)
            {
                JsonProperty[] names = document.RootElement.EnumerateObject()
                    .Where(static property => string.Equals(property.Name, "commandType", StringComparison.OrdinalIgnoreCase)).ToArray();
                if (names.Length == 1 && names[0].NameEquals("commandType") && names[0].Value.ValueKind == JsonValueKind.String)
                {
                    string name = AuditMetadata.SafeCommandName(names[0].Value.GetString());
                    return CanonicalCommandNames.IsAllowed(name) ? name : AuditMetadata.UnknownCommandName;
                }
            }
        }
        catch (Exception exception) when (exception is JsonException or InvalidOperationException)
        {
            // Authentication denial takes precedence over request validation. No payload detail enters the fact.
        }

        return AuditMetadata.UnknownCommandName;
    }
}
