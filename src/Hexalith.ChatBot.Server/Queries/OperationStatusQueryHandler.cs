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

internal sealed class OperationStatusQueryHandler(IOperationStatusStore statusStore)
    : ChatBotReadQueryHandler<OperationStatusQuery>
{
    public override string QueryType => ChatBotReadQueryTypes.OperationStatus;

    protected override async Task<QueryResult> ExecuteAsync(QueryEnvelope query, OperationStatusQuery request, CancellationToken cancellationToken)
    {
        if (!ChatBotIdentity.IsValidUlid(request.OperationId))
        {
            return QueryResult.Failure(ChatBotAuthorizationReasonCodes.SafeNotFound);
        }

        OperationStatusRecord? record = await statusStore
            .TryGetAsync(query.TenantId, request.OperationId, cancellationToken)
            .ConfigureAwait(false);

        return record is null
            ? QueryResult.Failure(ChatBotAuthorizationReasonCodes.SafeNotFound)
            : QueryResult.FromPayload(OperationStatusHttpResults.ToJsonElement(record), "chatbot.operation-status.v1");
    }
}
