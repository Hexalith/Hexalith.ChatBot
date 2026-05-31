using Shouldly;

namespace Hexalith.ChatBot.Architecture.Tests.Fitness;

/// <summary>
/// Vacuity guards for the fitness harness itself (AC5 philosophy: a silently no-op rule
/// gives false confidence). The adapter-boundary and adapter-dependency rules iterate
/// <see cref="FitnessAssemblies.Adapters"/>; if that discovery returns nothing, those rules
/// pass with zero assertions. These tests prove the discovery is non-empty today so the
/// adapter fitness rules can never be silently vacuous.
/// </summary>
public static class FitnessDiscoveryTests
{
    /// <summary>
    /// Adapter discovery must find at least one adapter assembly so the adapter fitness rules
    /// run against real input rather than passing vacuously.
    /// </summary>
    [Fact]
    public static void AdapterDiscoveryIsNotVacuous()
    {
        FitnessAssemblies.Adapters.ShouldNotBeEmpty(
            "Adapter discovery found no assemblies — the adapter-boundary and adapter-dependency fitness rules "
            + "would pass vacuously. Ensure at least one adapter (Hexalith.ChatBot.UI) is ProjectReferenced so its "
            + "DLL is copied to the test output directory (AppContext.BaseDirectory).");
    }

    /// <summary>
    /// The UI adapter (the only adapter that exists today) must be discovered, anchoring the
    /// forward-safe discovery so a dropped ProjectReference fails loudly instead of silently.
    /// </summary>
    [Fact]
    public static void AdapterDiscoveryIncludesTheUiAdapter()
    {
        string?[] adapterNames = FitnessAssemblies.Adapters.Select(static a => a.GetName().Name).ToArray();

        adapterNames.ShouldContain(
            "Hexalith.ChatBot.UI",
            $"The UI adapter was not discovered (found: {string.Join(", ", adapterNames)}). "
            + "The adapter fitness rules would not cover UI.");
    }
}
