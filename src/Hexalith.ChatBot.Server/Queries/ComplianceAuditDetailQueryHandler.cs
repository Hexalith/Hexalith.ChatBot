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

internal sealed class ComplianceAuditDetailQueryHandler(
    ChatBotRequestContextResolver requestContextResolver,
    ChatBotRequestAuthorizer requestAuthorizer,
    IWormAuditStore wormAuditStore)
    : ChatBotReadQueryHandler<ComplianceAuditDetailQuery>(requestContextResolver, requestAuthorizer)
{
    public override string QueryType => ChatBotReadQueryTypes.ComplianceAuditDetail;

    protected override async Task<QueryResult> ExecuteAsync(QueryEnvelope query, ComplianceAuditDetailQuery request, ChatBotAuthorityPrincipal principal, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!ComplianceAdministrationSchema.IsSafeComplianceToken(request.AuditRecordRef))
        {
            return QueryResult.Failure(ChatBotAuthorizationReasonCodes.SafeNotFound);
        }

        AuditEnvelope? envelope = wormAuditStore.EnumerateChain(query.TenantId)
            .Select(static record => record.Envelope)
            .Where(static candidate => !AuditReplayExclusion.IsReplayEnvelope(candidate))
            .FirstOrDefault(candidate =>
                AuditMetadata.IsSafeStableIdentifier(candidate.ResourceId) &&
                string.Equals(candidate.ResourceId, request.AuditRecordRef, StringComparison.Ordinal));

        if (envelope is null)
        {
            return QueryResult.Failure(ChatBotAuthorizationReasonCodes.SafeNotFound);
        }

        string[] projectRefs = envelope.SourceEvidenceRefs
            .Where(static reference => reference.StartsWith("project:", StringComparison.Ordinal))
            .Select(static reference => reference["project:".Length..])
            .Where(AuditMetadata.IsSafeStableIdentifier)
            .Distinct(StringComparer.Ordinal).ToArray();
        bool hasPerProjectAuthority = projectRefs.Length > 0;
        foreach (string project in projectRefs)
        {
            if (!await RequestAuthorizer.HasProjectAuthorityAsync(principal.Context, project, QueryType, cancellationToken).ConfigureAwait(false))
            {
                hasPerProjectAuthority = false;
                break;
            }
        }

        ComplianceAuditDetail detail = ComplianceAuditReadPolicy.Detail(envelope, hasPerProjectAuthority);
        return QueryResult.FromPayload(ComplianceAuditHttpResults.DetailJsonElement(detail), "chatbot.compliance-audit-detail.v1");
    }
}
