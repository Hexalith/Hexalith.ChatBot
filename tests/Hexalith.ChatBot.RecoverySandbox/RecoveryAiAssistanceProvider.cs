using Hexalith.ChatBot.Contracts.Queries;
using Hexalith.ChatBot.Server.Adapters.AiProvider;

namespace Hexalith.ChatBot.RecoverySandbox;

/// <summary>Controllable provider adapter used to exercise the real ChatBot AI-provider contract.</summary>
internal sealed class RecoveryAiAssistanceProvider(RecoveryScopedOutageState state) : IAiAssistanceProvider
{
    /// <inheritdoc />
    public ValueTask<LowRiskAiAssistanceExecutionRecord> ExecuteAsync(
        AiAssistanceProviderRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();
        bool faulted = state.IsFaulted("ai-provider");
        if (faulted)
        {
            state.RecordFaultObservation("ai-provider");
        }
        else
        {
            _ = state.RecordEffect("ai-provider", request.TenantId, request.CorrelationId);
        }

        return ValueTask.FromResult(new LowRiskAiAssistanceExecutionRecord(
            request.ExecutionId,
            request.ProposalId,
            request.AssistanceKind,
            faulted ? "failed" : "succeeded",
            faulted ? "unavailable" : "available",
            faulted ? "not-invoked" : "recovery-provider",
            DateTimeOffset.UtcNow,
            request.SourceEvidenceReferences,
            request.ContextPackageId,
            request.ContextPackageVersion,
            request.ContextRedactionState,
            request.PolicySnapshotId,
            request.PolicyReasonCode,
            request.AuditOperationId,
            "available",
            request.CorrelationId,
            "metadata_only",
            "metadata_only",
            faulted ? "retry-ai-action" : "none",
            FailureCode: faulted ? "ai_provider_unavailable" : null,
            Retryability: faulted ? "retryable" : null,
            RetentionClass: request.RetentionClass));
    }
}
