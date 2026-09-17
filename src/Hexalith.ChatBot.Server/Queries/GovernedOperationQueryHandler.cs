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

internal sealed class GovernedOperationQueryHandler(IGovernedOperationProjectionStore projectionStore)
    : ChatBotReadQueryHandler<GovernedOperationQuery>
{
    public override string QueryType => ChatBotReadQueryTypes.GovernedOperation;

    protected override async Task<QueryResult> ExecuteAsync(QueryEnvelope query, GovernedOperationQuery request, CancellationToken cancellationToken)
    {
        if (!ChatBotIdentity.IsValidUlid(request.NoteId))
        {
            return QueryResult.Failure(ChatBotAuthorizationReasonCodes.SafeNotFound);
        }

        GovernedOperationView? view = await projectionStore
            .GetAsync(query.TenantId, request.NoteId, cancellationToken)
            .ConfigureAwait(false);

        return view is null
            ? QueryResult.Failure(ChatBotAuthorizationReasonCodes.SafeNotFound)
            : Payload(GovernedOperationViewResponse.From(view), "chatbot.governed-operation.v1");
    }
}
