using System.Text.Json;

using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Identities;
using Hexalith.ChatBot.Contracts.Queries;
using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Authentication;
using Hexalith.ChatBot.Server.Authorization;
using Hexalith.ChatBot.Server.Gateway.Stages;
using Hexalith.ChatBot.Server.Gateway.Status;
using Hexalith.ChatBot.Server.Gateway;
using Hexalith.ChatBot.Server.Governance.AiMediation;
using Hexalith.ChatBot.Server.Lifecycle.Attachments;
using Hexalith.ChatBot.Server.Projections;
using Hexalith.EventStore.Client.Queries;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.EventStore.DomainService;

namespace Hexalith.ChatBot.Server.Queries;

internal sealed class ComplianceAuditSearchQueryHandler(
    ChatBotRequestContextResolver requestContextResolver,
    ChatBotRequestAuthorizer requestAuthorizer,
    IWormAuditStore wormAuditStore)
    : ChatBotReadQueryHandler<ComplianceAuditSearchQuery>(requestContextResolver, requestAuthorizer)
{
    public override string QueryType => ChatBotReadQueryTypes.ComplianceAuditSearch;

    protected override Task<QueryResult> ExecuteAsync(QueryEnvelope query, ComplianceAuditSearchQuery request, ChatBotAuthorityPrincipal principal, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!ComplianceAdministrationSchema.ValidateAuditQueryFilters(request.Filters).IsValid)
        {
            return Task.FromResult(QueryResult.Failure(ChatBotAuthorizationReasonCodes.SafeNotFound));
        }

        IReadOnlyList<AuditEnvelope> envelopes =
            [.. wormAuditStore.EnumerateChain(query.TenantId).Select(static record => record.Envelope)];
        ComplianceAuditSearchResult result = ComplianceAuditReadPolicy.Search(
            principal,
            request.Filters!,
            envelopes,
            DateTimeOffset.UtcNow,
            query.CorrelationId);
        return Task.FromResult(QueryResult.FromPayload(ComplianceAuditHttpResults.SearchJsonElement(result), "chatbot.compliance-audit-search.v1"));
    }

}
