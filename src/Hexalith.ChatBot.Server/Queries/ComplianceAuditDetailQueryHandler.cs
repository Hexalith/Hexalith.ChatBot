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
        bool hasPerProjectAuthority = RequestAuthorizer.IsCurrent(principal) && await RequestAuthorizer.HasProjectAuthoritiesAsync(principal.Context, projectRefs, QueryType, cancellationToken,
            () => RequestAuthorizer.IsCurrent(principal)).ConfigureAwait(false) &&
            RequestAuthorizer.IsCurrent(principal);

        ComplianceAuditDetail detail = ComplianceAuditReadPolicy.Detail(envelope, hasPerProjectAuthority);
        return QueryResult.FromPayload(ComplianceAuditHttpResults.DetailJsonElement(detail), "chatbot.compliance-audit-detail.v1");
    }

    /// <summary>Preserves the established restricted-detail refusal if final compliance authority lapses.</summary>
    protected override QueryResult FinalizeResult(QueryResult result, ChatBotAuthorityPrincipal principal)
    {
        if (RequestAuthorizer.IsCurrent(principal) || !result.Success) { return result; }
        Dictionary<string, JsonElement>? wire = result.PayloadBytes is null
            ? null
            : JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(result.PayloadBytes, JsonOptions);
        if (wire is null) { return QueryResult.Failure(ChatBotAuthorizationReasonCodes.SafeNotFound); }
        wire["redactionState"] = JsonSerializer.SerializeToElement("escalation-required");
        wire["escalationStatus"] = JsonSerializer.SerializeToElement("requested");
        wire["visibleMetadataRefs"] = JsonSerializer.SerializeToElement(Array.Empty<string>());
        wire["safeNextAction"] = JsonSerializer.SerializeToElement("request-access");
        wire["redactionReasonCode"] = JsonSerializer.SerializeToElement("restricted-detail");
        return Payload(wire, "chatbot.compliance-audit-detail.v1");
    }
}
