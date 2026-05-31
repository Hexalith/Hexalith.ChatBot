using Hexalith.ChatBot.Architecture.Tests.Fitness;

using Hexalith.ChatBot.Server.Operations;

using NetArchTest.Rules;

using Shouldly;

using NetArchTestResult = NetArchTest.Rules.TestResult;

namespace Hexalith.ChatBot.Architecture.Tests;

/// <summary>
/// Proves the fitness-rule MACHINERY actually works (AC5 / FR86: a failure is an invariant violation, and the
/// failure must identify the forbidden edge). Without these, a silently-misconfigured rule (e.g. a typo'd
/// namespace that matches nothing) would always "pass" and give false confidence.
/// </summary>
public static class FitnessRuleMachineryTests
{
    [Fact]
    public static void NetArchTestParsesNet10AssembliesWithoutCecilFailure()
    {
        // Gating-risk canary: Mono.Cecil 0.11.6 (via NetArchTest.eNhancedEdition 1.4.5) must parse net10.0 IL.
        // A trivially-true rule that still forces a full Cecil load + inspection of a ChatBot assembly.
        NetArchTestResult result = Types.InAssembly(FitnessAssemblies.Server)
            .Should()
            .NotHaveDependencyOnAny("This.Namespace.Intentionally.Does.Not.Exist")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(FitnessRule.Describe(result));
    }

    [Fact]
    public static void FitnessRuleFailureSurfacesTheForbiddenEdge()
    {
        // NON-DESTRUCTIVE detection proof: feed the machinery a deliberately-FALSE rule against a TRUE fact.
        // Server's GovernedOperationAggregate genuinely derives from EventStoreAggregate<> (in Hexalith.EventStore),
        // so "Server.Operations must NOT depend on Hexalith.EventStore" MUST fail — and MUST name the offending
        // type. This proves FailingTypes surfaces the forbidden edge WITHOUT committing a real architecture
        // violation or introducing a skipped/quarantined assembly.
        NetArchTestResult result = Types.InAssembly(FitnessAssemblies.Server)
            .That()
            .ResideInNamespace("Hexalith.ChatBot.Server.Operations")
            .Should()
            .NotHaveDependencyOnAny("Hexalith.EventStore")
            .GetResult();

        result.IsSuccessful.ShouldBeFalse(
            "the deliberately-false rule must fail so rule detection is proven, not assumed");

        FitnessRule.FailingTypeNames(result)
            .ShouldContain(
                name => name != null && name.Contains(nameof(GovernedOperationAggregate), StringComparison.Ordinal),
                "the failure must IDENTIFY the forbidden edge by naming GovernedOperationAggregate");
    }
}
