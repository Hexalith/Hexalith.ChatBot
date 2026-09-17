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

internal sealed class ComplianceAuditDetailQueryHandler(IWormAuditStore wormAuditStore)
    : ChatBotReadQueryHandler<ComplianceAuditDetailQuery>
{
    public override string QueryType => ChatBotReadQueryTypes.ComplianceAuditDetail;

    protected override Task<QueryResult> ExecuteAsync(QueryEnvelope query, ComplianceAuditDetailQuery request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!request.CanSearchTenantAudit || !ComplianceAdministrationSchema.IsSafeComplianceToken(request.AuditRecordRef))
        {
            return Task.FromResult(QueryResult.Failure(ChatBotAuthorizationReasonCodes.SafeNotFound));
        }

        AuditEnvelope? envelope = wormAuditStore.EnumerateChain(query.TenantId)
            .Select(static record => record.Envelope)
            .Where(static candidate => !AuditReplayExclusion.IsReplayEnvelope(candidate))
            .FirstOrDefault(candidate =>
                AuditMetadata.IsSafeStableIdentifier(candidate.ResourceId) &&
                string.Equals(candidate.ResourceId, request.AuditRecordRef, StringComparison.Ordinal));

        if (envelope is null)
        {
            return Task.FromResult(QueryResult.Failure(ChatBotAuthorizationReasonCodes.SafeNotFound));
        }

        bool hasPerProjectAuthority = envelope.SourceEvidenceRefs
            .Where(static reference => reference.StartsWith("project:", StringComparison.Ordinal))
            .Select(static reference => reference["project:".Length..])
            .Where(AuditMetadata.IsSafeStableIdentifier)
            .Any(projectRef => request.ExplicitProjectGrants.Contains(projectRef, StringComparer.Ordinal));
        ComplianceAuditDetail detail = ComplianceAuditReadPolicy.Detail(envelope, hasPerProjectAuthority);
        return Task.FromResult(QueryResult.FromPayload(ComplianceAuditHttpResults.DetailJsonElement(detail), "chatbot.compliance-audit-detail.v1"));
    }
}
