using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Messages;

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

        if (!request.HasProjectAuthorization)
        {
            return AiActionPolicyDecision.Routed("unavailable", "missing_project_authorization");
        }

        if (request.RiskClass is not AiActionRiskClass.LowRisk ||
            !string.Equals(request.EffectSurface, "read-only", StringComparison.Ordinal))
        {
            return AiActionPolicyDecision.Routed("unavailable", "risk_not_low_risk");
        }

        if (string.IsNullOrWhiteSpace(request.ContextPackageId) ||
            string.IsNullOrWhiteSpace(request.ContextPackageVersion))
        {
            return AiActionPolicyDecision.Routed("unavailable", "missing_context_package");
        }

        if (!TenantPolicySchema.IsSafePolicyToken(request.TenantId) ||
            !TenantPolicySchema.IsSafePolicyToken(request.RequestedPolicySnapshotId))
        {
            return Unavailable();
        }

        TenantAiPolicySnapshot? snapshot;
        try
        {
            snapshot = await _policySnapshots
                .TryGetAsync(request.TenantId, request.ProjectId, request.RequestedPolicySnapshotId, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            return Unavailable();
        }

        cancellationToken.ThrowIfCancellationRequested();
        if (snapshot is null || !snapshot.IsValid || !snapshot.IsFresh || snapshot.PolicyVersion < 1 ||
            snapshot.TenantId != request.TenantId || snapshot.PolicySnapshotId != request.RequestedPolicySnapshotId ||
            snapshot.AssistanceKinds is null || snapshot.AssistanceKinds.Any(static kind => !TenantPolicySchema.IsSafePolicyToken(kind)) ||
            !TenantPolicySchema.IsSafePolicyToken(snapshot.EffectSurface))
        {
            return Unavailable();
        }

        if (!LowRiskAllowedForEveryActionClass(snapshot, request.RiskActionClasses))
        {
            return AiActionPolicyDecision.Routed(snapshot, "low_risk_policy_false");
        }

        if (!string.Equals(snapshot.EffectSurface, request.EffectSurface, StringComparison.Ordinal) ||
            !snapshot.AssistanceKinds.Contains(request.AssistanceKind, StringComparer.Ordinal))
        {
            return AiActionPolicyDecision.Routed(snapshot, "policy_surface_mismatch");
        }

        return AiActionPolicyDecision.Allowed(snapshot);
    }

    private static AiActionPolicyDecision Unavailable()
        => AiActionPolicyDecision.Blocked("unavailable", ChatBotRefusalReasonCodes.PolicySnapshotUnavailable);

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
