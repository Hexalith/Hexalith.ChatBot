using System.Security.Claims;
using System.Text.Json;

using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Identities;
using Hexalith.ChatBot.Contracts.Queries;
using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Gateway;
using Hexalith.ChatBot.Server.Gateway.Status;
using Hexalith.ChatBot.Server.Gateway.Stages;
using Hexalith.ChatBot.Server.Governance.AiMediation;
using Hexalith.ChatBot.Server.Lifecycle.Attachments;
using Hexalith.ChatBot.Server.Projections;
using Hexalith.EventStore.Client.Queries;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.EventStore.DomainService;

namespace Hexalith.ChatBot.Server.Queries;

internal abstract class ChatBotReadQueryHandler<TRequest> : IDomainQueryHandler
{
    protected static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public string Domain => ChatBotReadQueryTypes.Domain;

    public abstract string QueryType { get; }

    public async Task<QueryResult> ExecuteAsync(QueryEnvelope query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        TRequest? request = JsonSerializer.Deserialize<TRequest>(query.Payload, JsonOptions);
        return request is null
            ? QueryResult.Failure(ChatBotAuthorizationReasonCodes.SafeNotFound)
            : await ExecuteAsync(query, request, cancellationToken).ConfigureAwait(false);
    }

    protected static QueryResult Payload<T>(T payload, string projectionType)
        => QueryResult.FromPayload(JsonSerializer.SerializeToElement(payload, JsonOptions), projectionType);

    protected abstract Task<QueryResult> ExecuteAsync(QueryEnvelope query, TRequest request, CancellationToken cancellationToken);
}
