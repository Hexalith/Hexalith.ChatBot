using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Server.Governance.AiMediation;

internal sealed class DefaultAiActionPolicyEvaluator(ITenantAiPolicySnapshotProvider policySnapshots) : IAiActionPolicyEvaluator
{
    private readonly ITenantAiPolicySnapshotProvider _policySnapshots = policySnapshots ?? throw new ArgumentNullException(nameof(policySnapshots));

    public async ValueTask<AiActionPolicyDecision> EvaluateAsync(
        AiActionPolicyEvaluationRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        string requestedSnapshot = string.IsNullOrWhiteSpace(request.RequestedPolicySnapshotId)
            ? "unavailable"
            : request.RequestedPolicySnapshotId;
        if (!request.HasProjectAuthorization)
        {
            return AiActionPolicyDecision.Routed(requestedSnapshot, "missing_project_authorization");
        }

        if (request.RiskClass is not AiActionRiskClass.LowRisk ||
            !string.Equals(request.EffectSurface, "read-only", StringComparison.Ordinal))
        {
            return AiActionPolicyDecision.Routed(requestedSnapshot, "risk_not_low_risk");
        }

        if (string.IsNullOrWhiteSpace(request.ContextPackageId) ||
            string.IsNullOrWhiteSpace(request.ContextPackageVersion))
        {
            return AiActionPolicyDecision.Routed(requestedSnapshot, "missing_context_package");
        }

        TenantAiPolicySnapshot? snapshot = await _policySnapshots
            .TryGetAsync(request.TenantId, request.ProjectId, request.RequestedPolicySnapshotId, cancellationToken)
            .ConfigureAwait(false);
        if (snapshot is null)
        {
            return AiActionPolicyDecision.Routed(requestedSnapshot, "policy_unavailable");
        }

        if (!snapshot.IsValid)
        {
            return AiActionPolicyDecision.Routed(snapshot.PolicySnapshotId, "policy_invalid");
        }

        if (!snapshot.IsFresh)
        {
            return AiActionPolicyDecision.Routed(snapshot.PolicySnapshotId, "policy_stale");
        }

        if (!LowRiskAllowedForEveryActionClass(snapshot, request.RiskActionClasses))
        {
            return AiActionPolicyDecision.Routed(snapshot.PolicySnapshotId, "low_risk_policy_false");
        }

        if (!string.Equals(snapshot.EffectSurface, request.EffectSurface, StringComparison.Ordinal) ||
            !snapshot.AssistanceKinds.Contains(request.AssistanceKind, StringComparer.Ordinal))
        {
            return AiActionPolicyDecision.Routed(snapshot.PolicySnapshotId, "policy_surface_mismatch");
        }

        return AiActionPolicyDecision.Allowed(snapshot.PolicySnapshotId);
    }

    private static bool LowRiskAllowedForEveryActionClass(TenantAiPolicySnapshot snapshot, IReadOnlyList<string> requestedActionClasses)
    {
        if (requestedActionClasses.Count == 0)
        {
            return snapshot.LowRiskAllowed;
        }

        if (snapshot.LowRiskAllowedByActionClass is null)
        {
            return snapshot.LowRiskAllowed;
        }

        foreach (string requested in requestedActionClasses)
        {
            if (!TryParseActionClass(requested, out AiActionRiskActionClass actionClass) ||
                !snapshot.LowRiskAllowedByActionClass.TryGetValue(actionClass, out bool allowed) ||
                !allowed)
            {
                return false;
            }
        }

        return true;
    }

    private static bool TryParseActionClass(string value, out AiActionRiskActionClass actionClass)
    {
        actionClass = value switch
        {
            "modifies-state" => AiActionRiskActionClass.ModifiesState,
            "exposes-files" => AiActionRiskActionClass.ExposesFiles,
            "sends-external" => AiActionRiskActionClass.SendsExternal,
            "creates-tasks" => AiActionRiskActionClass.CreatesTasks,
            "invokes-tools" => AiActionRiskActionClass.InvokesTools,
            "acts-on-behalf" => AiActionRiskActionClass.ActsOnBehalf,
            _ => default,
        };
        return value is "modifies-state" or "exposes-files" or "sends-external" or "creates-tasks" or "invokes-tools" or "acts-on-behalf";
    }
}
