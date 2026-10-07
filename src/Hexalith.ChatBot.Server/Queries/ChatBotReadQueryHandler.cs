using System.Text.Json;

using Hexalith.ChatBot.Server.Authentication;
using Hexalith.ChatBot.Server.Authorization;
using Hexalith.ChatBot.Server.Gateway;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.EventStore.DomainService;

namespace Hexalith.ChatBot.Server.Queries;

/// <summary>Mandatory authorization boundary shared by HTTP and SDK reads.</summary>
internal abstract class ChatBotReadQueryHandler<TRequest>(ChatBotRequestContextResolver contextResolver, ChatBotRequestAuthorizer authorizer) : IDomainQueryHandler
{
    /// <summary>Canonical query payload serializer.</summary>
    protected static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    /// <summary>The mandatory shared authority boundary for project-specific detail.</summary>
    protected ChatBotRequestAuthorizer RequestAuthorizer => authorizer;
    /// <inheritdoc/>
    public string Domain => ChatBotReadQueryTypes.Domain;
    /// <inheritdoc/>
    public abstract string QueryType { get; }

    /// <inheritdoc/>
    public async Task<QueryResult> ExecuteAsync(QueryEnvelope query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        ChatBotRequestContext? context = contextResolver.ResolveQuery(query);
        if (context is null || query.Domain != Domain || query.QueryType != QueryType || query.TenantId != context.TenantId || query.UserId != context.SubjectId)
        {
            return QueryResult.Failure(ChatBotAuthorizationReasonCodes.SafeNotFound);
        }

        TRequest? request;
        JsonElement original;
        try
        {
            using JsonDocument document = JsonDocument.Parse(query.Payload);
            original = document.RootElement.Clone();
            request = original.Deserialize<TRequest>(JsonOptions);
        }
        catch (JsonException)
        {
            return QueryResult.Failure(ChatBotAuthorizationReasonCodes.SafeNotFound);
        }

        if (request is null)
        {
            return QueryResult.Failure(ChatBotAuthorizationReasonCodes.SafeNotFound);
        }

        ChatBotAuthorityDecision decision = await authorizer.AuthorizeAsync(context, QueryType, true, original, cancellationToken).ConfigureAwait(false);
        if (!decision.IsAllowed)
        {
            return QueryResult.Failure(ChatBotAuthorizationReasonCodes.SafeNotFound);
        }

        QueryEnvelope bound = new(context.TenantId!, Domain, query.AggregateId, QueryType, query.Payload, query.CorrelationId, context.SubjectId);
        return await ExecuteAsync(bound, request, decision.Principal!, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Serializes a successfully authorized response.</summary>
    protected static QueryResult Payload<T>(T payload, string projectionType)
        => QueryResult.FromPayload(JsonSerializer.SerializeToElement(payload, JsonOptions), projectionType);
    /// <summary>Executes only after trusted binding and current owner authorization.</summary>
    protected abstract Task<QueryResult> ExecuteAsync(QueryEnvelope query, TRequest request, ChatBotAuthorityPrincipal principal, CancellationToken cancellationToken);
}
