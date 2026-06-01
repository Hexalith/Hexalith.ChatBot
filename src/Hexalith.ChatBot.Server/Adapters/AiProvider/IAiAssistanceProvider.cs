using Hexalith.ChatBot.Contracts.Queries;

namespace Hexalith.ChatBot.Server.Adapters.AiProvider;

internal interface IAiAssistanceProvider
{
    ValueTask<LowRiskAiAssistanceExecutionRecord> ExecuteAsync(
        AiAssistanceProviderRequest request,
        CancellationToken cancellationToken);
}

internal sealed record AiAssistanceProviderRequest(
    string TenantId,
    string ProjectId,
    string RequesterId,
    string ProposalId,
    string ExecutionId,
    string AssistanceKind,
    string ContextPackageId,
    string ContextPackageVersion,
    string ContextRedactionState,
    string RetentionClass,
    string ProviderReuseSetting,
    IReadOnlyList<string> SourceEvidenceReferences,
    IReadOnlyList<string> AuthorizedContextReferences,
    IReadOnlyList<string> ExcludedContextReasons,
    string PolicySnapshotId,
    string PolicyReasonCode,
    string CorrelationId,
    string AuditOperationId);
