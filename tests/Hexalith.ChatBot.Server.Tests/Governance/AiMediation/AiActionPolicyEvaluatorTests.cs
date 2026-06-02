using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Server.Governance.AiMediation;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Governance.AiMediation;

public sealed class AiActionPolicyEvaluatorTests
{
    [Fact]
    public async Task AllowedPolicyShouldPermitLowRiskReadOnlyAssistance()
    {
        DefaultAiActionPolicyEvaluator evaluator = new(new FixedProvider(new(
            "policy-snap-001",
            LowRiskAllowed: true,
            "read-only",
            ["summarize-visible-context"],
            IsFresh: true,
            IsValid: true)));

        AiActionPolicyDecision decision = await evaluator.EvaluateAsync(Request(), TestContext.Current.CancellationToken);

        decision.Kind.ShouldBe(AiActionPolicyDecisionKind.LowRiskExecuteAllowed);
        decision.ReasonCode.ShouldBe("low-risk-execute-allowed");
        decision.SafeNextAction.ShouldBe("none");
    }

    [Theory]
    [InlineData(false, true, true, "low_risk_policy_false")]
    [InlineData(true, false, true, "policy_stale")]
    [InlineData(true, true, false, "policy_invalid")]
    public async Task UnsafePolicyStatesShouldRouteToApproval(bool allowed, bool fresh, bool valid, string expectedReason)
    {
        DefaultAiActionPolicyEvaluator evaluator = new(new FixedProvider(new(
            "policy-snap-001",
            allowed,
            "read-only",
            ["summarize-visible-context"],
            fresh,
            valid)));

        AiActionPolicyDecision decision = await evaluator.EvaluateAsync(Request(), TestContext.Current.CancellationToken);

        decision.Kind.ShouldBe(AiActionPolicyDecisionKind.LowRiskRoutedToApproval);
        decision.ReasonCode.ShouldBe(expectedReason);
        decision.SafeNextAction.ShouldBe("review-ai-action");
    }

    [Fact]
    public async Task MissingPackageOrRiskyClassificationShouldRouteToApproval()
    {
        DefaultAiActionPolicyEvaluator evaluator = new(new FixedProvider(new(
            "policy-snap-001",
            LowRiskAllowed: true,
            "read-only",
            ["summarize-visible-context"],
            IsFresh: true,
            IsValid: true)));

        AiActionPolicyDecision missingPackage = await evaluator.EvaluateAsync(
            Request(contextPackageId: string.Empty),
            TestContext.Current.CancellationToken);
        AiActionPolicyDecision risky = await evaluator.EvaluateAsync(
            Request(riskClass: AiActionRiskClass.ApprovalRequired),
            TestContext.Current.CancellationToken);
        AiActionPolicyDecision missingAuthorization = await evaluator.EvaluateAsync(
            Request(hasProjectAuthorization: false),
            TestContext.Current.CancellationToken);

        missingPackage.ReasonCode.ShouldBe("missing_context_package");
        risky.ReasonCode.ShouldBe("risk_not_low_risk");
        missingAuthorization.ReasonCode.ShouldBe("missing_project_authorization");
    }

    [Fact]
    public async Task PerActionClassPolicyShouldRequireEveryRequestedClassToBeAllowed()
    {
        DefaultAiActionPolicyEvaluator evaluator = new(new FixedProvider(new(
            "policy-snap-001",
            LowRiskAllowed: false,
            "read-only",
            ["summarize-visible-context"],
            IsFresh: true,
            IsValid: true,
            LowRiskAllowedByActionClass: new Dictionary<AiActionRiskActionClass, bool>
            {
                [AiActionRiskActionClass.ModifiesState] = true,
                [AiActionRiskActionClass.ExposesFiles] = false,
                [AiActionRiskActionClass.SendsExternal] = false,
                [AiActionRiskActionClass.CreatesTasks] = false,
                [AiActionRiskActionClass.InvokesTools] = false,
                [AiActionRiskActionClass.ActsOnBehalf] = false,
            })));

        AiActionPolicyDecision allowed = await evaluator.EvaluateAsync(
            Request(actionClasses: ["modifies-state"]),
            TestContext.Current.CancellationToken);
        AiActionPolicyDecision denied = await evaluator.EvaluateAsync(
            Request(actionClasses: ["modifies-state", "exposes-files"]),
            TestContext.Current.CancellationToken);

        allowed.Kind.ShouldBe(AiActionPolicyDecisionKind.LowRiskExecuteAllowed);
        denied.Kind.ShouldBe(AiActionPolicyDecisionKind.LowRiskRoutedToApproval);
        denied.ReasonCode.ShouldBe("low_risk_policy_false");
    }

    private static AiActionPolicyEvaluationRequest Request(
        string contextPackageId = "context-package-001",
        AiActionRiskClass riskClass = AiActionRiskClass.LowRisk,
        bool hasProjectAuthorization = true,
        IReadOnlyList<string>? actionClasses = null)
        => new(
            "tenant-alpha",
            "project-001",
            "proposal-001",
            contextPackageId,
            "v1",
            "policy-snap-001",
            riskClass,
            actionClasses ?? [],
            "read-only",
            "summarize-visible-context",
            hasProjectAuthorization);

    private sealed class FixedProvider(TenantAiPolicySnapshot? snapshot) : ITenantAiPolicySnapshotProvider
    {
        public ValueTask<TenantAiPolicySnapshot?> TryGetAsync(
            string tenantId,
            string projectId,
            string? requestedPolicySnapshotId,
            CancellationToken cancellationToken)
            => ValueTask.FromResult(snapshot);
    }
}
