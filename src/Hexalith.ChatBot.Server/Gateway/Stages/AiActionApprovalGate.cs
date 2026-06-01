using System.Text.Json;

using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Contracts.Queries;
using Hexalith.ChatBot.Server.Governance.AiMediation;

namespace Hexalith.ChatBot.Server.Gateway.Stages;

internal sealed class AiActionApprovalGate(IAiActionPolicyEvaluator policyEvaluator) : IApprovalGate
{
    private static readonly JsonSerializerOptions ReadOptions = new(JsonSerializerDefaults.Web);
    private readonly IAiActionPolicyEvaluator _policyEvaluator = policyEvaluator ?? throw new ArgumentNullException(nameof(policyEvaluator));

    public async ValueTask<ChatBotApprovalResult> EvaluateAsync(ChatBotGatewayContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();

        if (!string.Equals(context.Submission.Request.CommandType, nameof(ExecuteLowRiskAIAssistance), StringComparison.Ordinal))
        {
            return ChatBotApprovalResult.Approved;
        }

        ExecuteLowRiskAIAssistance command = ReadExecutionCommand(context);
        AiActionRiskClassificationRecord? risk = context.RiskClassification?.Record;
        if (risk is null || risk.Rejected)
        {
            return ChatBotApprovalResult.Blocked("risk_classification_unavailable");
        }

        AiActionPolicyDecision decision = await _policyEvaluator
            .EvaluateAsync(
                new AiActionPolicyEvaluationRequest(
                    context.TenantBinding.TenantId,
                    command.ProjectId,
                    command.ProposalId,
                    command.ContextPackageId,
                    command.ContextPackageVersion,
                    command.PolicySnapshotId,
                    risk.RiskClass,
                    risk.RiskActionClasses.Select(static value => value.ToString()).ToArray(),
                    risk.InputTuple.EffectSurface ?? "unavailable",
                    AssistanceKindToken(command.AssistanceKind),
                    HasProjectAuthorization: true),
                cancellationToken)
            .ConfigureAwait(false);

        return decision.Kind switch
        {
            AiActionPolicyDecisionKind.LowRiskExecuteAllowed => ChatBotApprovalResult.AllowedLowRiskExecution(decision.PolicySnapshotId, decision.ReasonCode),
            AiActionPolicyDecisionKind.LowRiskRoutedToApproval => ChatBotApprovalResult.RoutedToApproval(decision.PolicySnapshotId, decision.ReasonCode),
            _ => ChatBotApprovalResult.Blocked(decision.ReasonCode),
        };
    }

    internal static string AssistanceKindToken(LowRiskAiAssistanceKind kind)
        => kind switch
        {
            LowRiskAiAssistanceKind.SummarizeVisibleContext => "summarize-visible-context",
            LowRiskAiAssistanceKind.ExplainVisibleEvidence => "explain-visible-evidence",
            _ => "unknown",
        };

    private static ExecuteLowRiskAIAssistance ReadExecutionCommand(ChatBotGatewayContext context)
    {
        JsonElement command = context.Submission.Request.Command is JsonElement element
            ? element
            : JsonSerializer.SerializeToElement(context.Submission.Request.Command, ReadOptions);

        return command.Deserialize<ExecuteLowRiskAIAssistance>(ReadOptions)
            ?? throw new InvalidOperationException("The low-risk AI assistance execution command payload could not be read.");
    }
}
