namespace Hexalith.ChatBot.Server.Governance.AiMediation;

internal interface IAiActionPolicyEvaluator
{
    ValueTask<AiActionPolicyDecision> EvaluateAsync(
        AiActionPolicyEvaluationRequest request,
        CancellationToken cancellationToken);
}
