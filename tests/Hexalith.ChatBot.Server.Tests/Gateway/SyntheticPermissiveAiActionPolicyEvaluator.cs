using Hexalith.ChatBot.Server.Governance.AiMediation;

namespace Hexalith.ChatBot.Server.Tests.Gateway;

/// <summary>Adversarial synthetic policy that permits everything if the mandatory effect gate incorrectly reaches it.</summary>
internal sealed class SyntheticPermissiveAiActionPolicyEvaluator : IAiActionPolicyEvaluator
{
    /// <summary>Gets the number of policy evaluations.</summary>
    public int Calls { get; private set; }
    /// <inheritdoc />
    public ValueTask<AiActionPolicyDecision> EvaluateAsync(AiActionPolicyEvaluationRequest request, CancellationToken cancellationToken)
    {
        Calls++;
        return ValueTask.FromResult(AiActionPolicyDecision.Allowed(new TenantAiPolicySnapshot(request.TenantId, 1,
            request.RequestedPolicySnapshotId ?? "synthetic-policy", true, "read-only", [request.AssistanceKind], true, true)));
    }
}
