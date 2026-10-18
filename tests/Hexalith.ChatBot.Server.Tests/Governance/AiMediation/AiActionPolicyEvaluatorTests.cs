using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Contracts.Messages;
using Hexalith.ChatBot.Server.Governance.AiMediation;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Governance.AiMediation;

public sealed class AiActionPolicyEvaluatorTests
{
    [Fact]
    public async Task AllowedPolicyShouldPermitLowRiskReadOnlyAssistance()
    {
        DefaultAiActionPolicyEvaluator evaluator = new(new FixedProvider(new(
            "tenant-alpha",
            1,
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
        decision.TenantId.ShouldBe("tenant-alpha");
        decision.PolicySnapshotId.ShouldBe("policy-snap-001");
        decision.PolicyVersion.ShouldBe(1);
    }

    [Theory]
    [InlineData(false, true, true, "low_risk_policy_false")]
    [InlineData(true, false, true, ChatBotRefusalReasonCodes.PolicySnapshotUnavailable)]
    [InlineData(true, true, false, ChatBotRefusalReasonCodes.PolicySnapshotUnavailable)]
    public async Task UnsafePolicyStatesShouldNotAllowLowRiskExecution(bool allowed, bool fresh, bool valid, string expectedReason)
    {
        DefaultAiActionPolicyEvaluator evaluator = new(new FixedProvider(new(
            "tenant-alpha",
            1,
            "policy-snap-001",
            allowed,
            "read-only",
            ["summarize-visible-context"],
            fresh,
            valid)));

        AiActionPolicyDecision decision = await evaluator.EvaluateAsync(Request(), TestContext.Current.CancellationToken);

        decision.Kind.ShouldBe(fresh && valid ? AiActionPolicyDecisionKind.LowRiskRoutedToApproval : AiActionPolicyDecisionKind.Blocked);
        decision.ReasonCode.ShouldBe(expectedReason);
        decision.SafeNextAction.ShouldBe(fresh && valid ? "review-ai-action" : "none");
    }

    [Fact]
    public async Task MissingPackageOrRiskyClassificationShouldRouteToApproval()
    {
        DefaultAiActionPolicyEvaluator evaluator = new(new FixedProvider(new(
            "tenant-alpha",
            1,
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
        foreach (AiActionPolicyDecision decision in new[] { missingPackage, risky, missingAuthorization })
        {
            decision.PolicySnapshotId.ShouldBe("unavailable");
            decision.TenantId.ShouldBeNull();
            decision.PolicyVersion.ShouldBeNull();
        }
    }

    [Fact]
    public async Task PerActionClassPolicyShouldRequireEveryRequestedClassToBeAllowed()
    {
        DefaultAiActionPolicyEvaluator evaluator = new(new FixedProvider(new(
            "tenant-alpha",
            1,
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
        denied.TenantId.ShouldBe("tenant-alpha");
        denied.PolicyVersion.ShouldBe(1);
        denied.PolicySnapshotId.ShouldBe("policy-snap-001");
    }

    [Fact]
    public async Task UnavailableOrUnknownActionClassPolicyShouldRouteToApproval()
    {
        DefaultAiActionPolicyEvaluator unavailable = new(new FixedProvider(null));

        AiActionPolicyDecision missingSnapshot = await unavailable.EvaluateAsync(
            Request(),
            TestContext.Current.CancellationToken);

        missingSnapshot.Kind.ShouldBe(AiActionPolicyDecisionKind.Blocked);
        missingSnapshot.ReasonCode.ShouldBe(ChatBotRefusalReasonCodes.PolicySnapshotUnavailable);
        missingSnapshot.PolicySnapshotId.ShouldBe("unavailable");

        DefaultAiActionPolicyEvaluator unknownClass = new(new FixedProvider(new(
            "tenant-alpha",
            1,
            "policy-snap-001",
            LowRiskAllowed: true,
            "read-only",
            ["summarize-visible-context"],
            IsFresh: true,
            IsValid: true,
            LowRiskAllowedByActionClass: TenantPolicyAllowedByClass())));

        AiActionPolicyDecision decision = await unknownClass.EvaluateAsync(
            Request(actionClasses: ["custom-class"]),
            TestContext.Current.CancellationToken);

        decision.Kind.ShouldBe(AiActionPolicyDecisionKind.LowRiskRoutedToApproval);
        decision.ReasonCode.ShouldBe("low_risk_policy_false");
        decision.TenantId.ShouldBe("tenant-alpha");
        decision.PolicyVersion.ShouldBe(1);
        decision.PolicySnapshotId.ShouldBe("policy-snap-001");
    }

    [Fact]
    public async Task SurfaceMismatchMustKeepTheExactResolvedReference()
    {
        DefaultAiActionPolicyEvaluator evaluator = new(new FixedProvider(new("tenant-alpha", 1, "policy-snap-001", true, "read-only", ["other-kind"], true, true)));

        AiActionPolicyDecision decision = await evaluator.EvaluateAsync(Request(), TestContext.Current.CancellationToken);

        decision.ReasonCode.ShouldBe("policy_surface_mismatch");
        decision.TenantId.ShouldBe("tenant-alpha");
        decision.PolicyVersion.ShouldBe(1);
        decision.PolicySnapshotId.ShouldBe("policy-snap-001");
    }

    [Theory]
    [InlineData("bad/kind", "read-only")]
    [InlineData("summarize-visible-context", "bad/surface")]
    public async Task SyntheticUnsafeSnapshotTokensMustNotPermitExecution(string kind, string surface)
    {
        DefaultAiActionPolicyEvaluator evaluator = new(new FixedProvider(new("tenant-alpha", 1, "policy-snap-001", true, surface, [kind], true, true)));

        AiActionPolicyDecision decision = await evaluator.EvaluateAsync(Request(), TestContext.Current.CancellationToken);

        decision.Kind.ShouldBe(AiActionPolicyDecisionKind.Blocked);
        decision.ReasonCode.ShouldBe(ChatBotRefusalReasonCodes.PolicySnapshotUnavailable);
        decision.PolicySnapshotId.ShouldBe("unavailable");
    }

    [Fact]
    public async Task SyntheticMalformedRequestTenantMustNotBeAcceptedWhenProviderEchoesIt()
    {
        DefaultAiActionPolicyEvaluator evaluator = new(new FixedProvider(new("bad/tenant", 1, "policy-snap-001", true, "read-only", ["summarize-visible-context"], true, true)));

        AiActionPolicyDecision decision = await evaluator.EvaluateAsync(Request() with { TenantId = "bad/tenant" }, TestContext.Current.CancellationToken);

        decision.Kind.ShouldBe(AiActionPolicyDecisionKind.Blocked);
        decision.ReasonCode.ShouldBe(ChatBotRefusalReasonCodes.PolicySnapshotUnavailable);
        decision.PolicySnapshotId.ShouldBe("unavailable");
        decision.TenantId.ShouldBeNull();
        decision.PolicyVersion.ShouldBeNull();
    }

    [Theory]
    [InlineData("malformed/id")]
    [InlineData("")]
    public async Task SyntheticMalformedRequestSnapshotIdMustNotBeAcceptedWhenProviderEchoesIt(string id)
    {
        DefaultAiActionPolicyEvaluator evaluator = new(new FixedProvider(new("tenant-alpha", 1, id, true, "read-only", ["summarize-visible-context"], true, true)));

        AiActionPolicyDecision decision = await evaluator.EvaluateAsync(Request() with { RequestedPolicySnapshotId = id }, TestContext.Current.CancellationToken);

        decision.Kind.ShouldBe(AiActionPolicyDecisionKind.Blocked);
        decision.ReasonCode.ShouldBe(ChatBotRefusalReasonCodes.PolicySnapshotUnavailable);
        decision.PolicySnapshotId.ShouldBe("unavailable");
        decision.TenantId.ShouldBeNull();
        decision.PolicyVersion.ShouldBeNull();
    }

    [Theory]
    [InlineData("foreign-tenant", "restricted-policy", 7)]
    [InlineData("tenant-alpha", "restricted-policy", 7)]
    [InlineData("tenant-alpha", "policy-snap-001", 0)]
    public async Task SyntheticUnresolvedReferencesMustNotPermitExecutionOrRevealProviderMetadata(string tenant, string id, long version)
    {
        DefaultAiActionPolicyEvaluator evaluator = new(new FixedProvider(new(tenant, version, id, true, "read-only", ["summarize-visible-context"], true, true)));
        AiActionPolicyDecision decision = await evaluator.EvaluateAsync(Request(), TestContext.Current.CancellationToken);
        decision.Kind.ShouldBe(AiActionPolicyDecisionKind.Blocked);
        decision.PolicySnapshotId.ShouldBe("unavailable");
        decision.TenantId.ShouldBeNull();
        decision.PolicyVersion.ShouldBeNull();
        decision.ReasonCode.ShouldBe(ChatBotRefusalReasonCodes.PolicySnapshotUnavailable);
        System.Text.Json.JsonSerializer.Serialize(decision).ShouldNotContain("restricted-policy");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("malformed/id")]
    public async Task SyntheticMissingOrMalformedReferencesMustNeverSelectADefault(string? id)
    {
        DefaultAiActionPolicyEvaluator evaluator = new(new FixedProvider(new("tenant-alpha", 1, "policy-snap-001", true, "read-only", ["summarize-visible-context"], true, true)));
        AiActionPolicyDecision decision = await evaluator.EvaluateAsync(Request() with { RequestedPolicySnapshotId = id }, TestContext.Current.CancellationToken);
        decision.AllowsLowRiskExecution.ShouldBeFalse();
        decision.PolicySnapshotId.ShouldBe("unavailable");
        decision.TenantId.ShouldBeNull();
        decision.PolicyVersion.ShouldBeNull();
    }

    [Fact]
    public async Task SyntheticUnavailableOrMalformedProviderMustNotLeakItsFailureOrGrantPermission()
    {
        foreach (DefaultAiActionPolicyEvaluator evaluator in new DefaultAiActionPolicyEvaluator[]
        {
            new(new FixedProvider(null, fails: true)),
            new(new FixedProvider(new("tenant-alpha", 1, "policy-snap-001", true, "read-only", null!, true, true))),
        })
        {
            AiActionPolicyDecision decision = await evaluator.EvaluateAsync(Request(), TestContext.Current.CancellationToken);
            decision.Kind.ShouldBe(AiActionPolicyDecisionKind.Blocked);
            decision.PolicySnapshotId.ShouldBe("unavailable");
            decision.TenantId.ShouldBeNull();
            decision.PolicyVersion.ShouldBeNull();
            System.Text.Json.JsonSerializer.Serialize(decision).ShouldNotContain("restricted-provider-detail");
        }
    }

    [Fact]
    public async Task IndependentProviderCancellationMustReturnRedactedUnavailableWhileCallerRemainsActive()
    {
        using CancellationTokenSource provider = new();
        provider.Cancel();
        DefaultAiActionPolicyEvaluator evaluator = new(new FixedProvider(null,
            failure: new OperationCanceledException("restricted-provider-timeout", provider.Token)));

        AiActionPolicyDecision decision = await evaluator.EvaluateAsync(Request(), TestContext.Current.CancellationToken);

        TestContext.Current.CancellationToken.IsCancellationRequested.ShouldBeFalse();
        decision.Kind.ShouldBe(AiActionPolicyDecisionKind.Blocked);
        decision.ReasonCode.ShouldBe(ChatBotRefusalReasonCodes.PolicySnapshotUnavailable);
        decision.PolicySnapshotId.ShouldBe("unavailable");
        decision.TenantId.ShouldBeNull();
        decision.PolicyVersion.ShouldBeNull();
        System.Text.Json.JsonSerializer.Serialize(decision).ShouldNotContain("restricted-provider-timeout");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ActualCallerCancellationDuringProviderResolutionMustPropagate(bool providerThrows)
    {
        using CancellationTokenSource caller = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        DefaultAiActionPolicyEvaluator evaluator = new(new FixedProvider(null,
            failure: providerThrows ? new OperationCanceledException(caller.Token) : null, onRead: caller.Cancel));

        OperationCanceledException canceled = await Should.ThrowAsync<OperationCanceledException>(() => evaluator.EvaluateAsync(Request(), caller.Token).AsTask());

        canceled.CancellationToken.ShouldBe(caller.Token);
    }

    [Fact]
    public async Task CallerCancellationMustWinOverANonCancellationProviderFailure()
    {
        using CancellationTokenSource caller = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        DefaultAiActionPolicyEvaluator evaluator = new(new FixedProvider(null, fails: true, onRead: caller.Cancel));

        await Should.ThrowAsync<OperationCanceledException>(() => evaluator.EvaluateAsync(Request(), caller.Token).AsTask());
    }

    [Fact]
    public async Task ProductionPolicyProviderMustRemainUnavailable()
    {
        (await new UnavailableTenantAiPolicySnapshotProvider().TryGetAsync("tenant-alpha", "project-001", "policy-snap-001", TestContext.Current.CancellationToken)).ShouldBeNull();
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

    private static IReadOnlyDictionary<AiActionRiskActionClass, bool> TenantPolicyAllowedByClass()
        => new Dictionary<AiActionRiskActionClass, bool>
        {
            [AiActionRiskActionClass.ModifiesState] = true,
            [AiActionRiskActionClass.ExposesFiles] = true,
            [AiActionRiskActionClass.SendsExternal] = true,
            [AiActionRiskActionClass.CreatesTasks] = true,
            [AiActionRiskActionClass.InvokesTools] = true,
            [AiActionRiskActionClass.ActsOnBehalf] = true,
        };

    private sealed class FixedProvider(TenantAiPolicySnapshot? snapshot, bool fails = false, Exception? failure = null, Action? onRead = null) : ITenantAiPolicySnapshotProvider
    {
        public ValueTask<TenantAiPolicySnapshot?> TryGetAsync(
            string tenantId,
            string projectId,
            string? requestedPolicySnapshotId,
            CancellationToken cancellationToken)
        {
            onRead?.Invoke();
            if (failure is not null) { throw failure; }
            return fails ? throw new IOException("restricted-provider-detail") : ValueTask.FromResult(snapshot);
        }
    }
}
