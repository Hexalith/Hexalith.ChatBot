using System.Reflection;

using Hexalith.ChatBot.Architecture.Tests.Fitness;

using NetArchTest.Rules;

using Shouldly;

using NetArchTestResult = NetArchTest.Rules.TestResult;

namespace Hexalith.ChatBot.Architecture.Tests;

/// <summary>
/// Assembly/IL-level (NetArchTest/Mono.Cecil) dependency-direction fitness tests (AC2).
/// These assert the inherited module rule — Contracts ← Client ← Server, adapters → Client only — by
/// inspecting the COMPILED assemblies' IL references, catching transitive/indirect edges that the
/// complementary source-text/XML <c>ScaffoldArchitectureTests</c> cannot see. Each rule names the forbidden
/// edge on failure (FR86). The exact allowed-<c>ProjectReference</c> set stays asserted by the existing XML
/// tests; here we assert only the forbidden (negative) edges, for which NetArchTest is the right tool.
/// </summary>
public static class DependencyDirectionFitnessTests
{
    [Fact]
    public static void ContractsHasNoDependencyOnAnyOtherChatBotAssembly()
    {
        NetArchTestResult result = Types.InAssembly(FitnessAssemblies.Contracts)
            .Should()
            .NotHaveDependencyOnAny("Hexalith.ChatBot.Client", "Hexalith.ChatBot.Server")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(FitnessRule.Describe(result));
    }

    [Fact]
    public static void ClientDoesNotDependOnServer()
    {
        NetArchTestResult result = Types.InAssembly(FitnessAssemblies.Client)
            .Should()
            .NotHaveDependencyOnAny("Hexalith.ChatBot.Server")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(FitnessRule.Describe(result));
    }

    [Fact]
    public static void ServerDoesNotDependOnAdapterOrHostingAssemblies()
    {
        NetArchTestResult result = Types.InAssembly(FitnessAssemblies.Server)
            .Should()
            .NotHaveDependencyOnAny(
                "Hexalith.ChatBot.UI",
                "Hexalith.ChatBot.AppHost")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(FitnessRule.Describe(result));
    }

    [Fact]
    public static void EveryAdapterAssemblyDoesNotDependOnServer()
    {
        FitnessAssemblies.Adapters.ShouldNotBeEmpty(
            "at least the UI surface-adapter assembly must be present in the test output directory");

        foreach (Assembly adapter in FitnessAssemblies.Adapters)
        {
            NetArchTestResult result = Types.InAssembly(adapter)
                .Should()
                .NotHaveDependencyOnAny("Hexalith.ChatBot.Server")
                .GetResult();

            result.IsSuccessful.ShouldBeTrue($"{adapter.GetName().Name}: {FitnessRule.Describe(result)}");
        }
    }
}
