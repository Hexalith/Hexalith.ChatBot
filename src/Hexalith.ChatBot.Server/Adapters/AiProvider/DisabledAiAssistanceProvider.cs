using Hexalith.ChatBot.Contracts.Queries;
using Hexalith.ChatBot.Server.Audit;

namespace Hexalith.ChatBot.Server.Adapters.AiProvider;

internal sealed class DisabledAiAssistanceProvider(ISystemClock clock) : IAiAssistanceProvider
{
    private readonly ISystemClock _clock = clock ?? throw new ArgumentNullException(nameof(clock));

    public ValueTask<LowRiskAiAssistanceExecutionRecord> ExecuteAsync(
        AiAssistanceProviderRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        return ValueTask.FromResult(new LowRiskAiAssistanceExecutionRecord(
            request.ExecutionId,
            request.ProposalId,
            request.AssistanceKind,
            "failed",
            "disabled",
            "disabled",
            _clock.UtcNow,
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
            "review-ai-action",
            FailureCode: "ai_provider_disabled",
            Retryability: "retryable"));
    }
}
