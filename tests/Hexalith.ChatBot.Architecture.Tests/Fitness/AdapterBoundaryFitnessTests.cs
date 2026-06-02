using System.Reflection;

using Hexalith.ChatBot.Architecture.Tests.Fitness;

using NetArchTest.Rules;

using Shouldly;

using NetArchTestResult = NetArchTest.Rules.TestResult;

namespace Hexalith.ChatBot.Architecture.Tests;

/// <summary>
/// Assembly/IL-level (NetArchTest/Mono.Cecil) adapter-boundary fitness tests (AC3 / FR81a).
/// FR81a forbids a surface adapter from replicating any pipeline stage. The four governance seams
/// (<c>IRiskClassifier</c>/<c>IApprovalGate</c>/<c>IAuditWriter</c>/<c>IIdempotencyStore</c>) are already
/// <c>internal</c> to <c>.Server</c>, so an adapter literally cannot compile a direct reference to them — the
/// compiler is the first enforcer. The meaningful IL invariant is therefore the NAMESPACE-dependency edge: no
/// adapter type may depend on a <c>…Server.Gateway</c>, <c>…Server.Gateway.Stages</c>, or
/// <c>…Server.Governance.Outbound</c> namespace (this also
/// catches transitive/indirect leaks a source-token scan misses). These COMPLEMENT — never replace — the
/// existing source-token guards in <c>ScaffoldArchitectureTests</c>, which additionally cover not-yet-compiled
/// future projects.
/// </summary>
public static class AdapterBoundaryFitnessTests
{
    [Fact]
    public static void NoAdapterTypeDependsOnServerGatewayNamespaces()
    {
        FitnessAssemblies.Adapters.ShouldNotBeEmpty(
            "at least the UI surface-adapter assembly must be present in the test output directory");

        foreach (Assembly adapter in FitnessAssemblies.Adapters)
        {
            NetArchTestResult result = Types.InAssembly(adapter)
                .Should()
                .NotHaveDependencyOnAny(
                    "Hexalith.ChatBot.Server.Gateway",
                    "Hexalith.ChatBot.Server.Gateway.Stages",
                    "Hexalith.ChatBot.Server.Governance.Outbound")
                .GetResult();

            result.IsSuccessful.ShouldBeTrue($"{adapter.GetName().Name}: {FitnessRule.Describe(result)}");
        }
    }
}
