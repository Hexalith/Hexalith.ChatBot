using Shouldly;

namespace Hexalith.ChatBot.Architecture.Tests.Fitness;

/// <summary>
/// Vacuity guards for the fitness harness itself (AC5 philosophy: a silently no-op rule
/// gives false confidence). The adapter-boundary and adapter-dependency rules iterate
/// <see cref="FitnessAssemblies.Adapters"/>; if that discovery returns nothing, those rules
/// pass with zero assertions. These tests prove the discovery covers every adapter project that
/// exists today so the adapter fitness rules can never be silently vacuous or partial.
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
    /// Every adapter project that exists in <c>src/</c> must be discovered, anchoring the
    /// forward-safe discovery so a dropped ProjectReference fails loudly instead of silently.
    /// </summary>
    [Fact]
    public static void AdapterDiscoveryIncludesEveryPresentAdapterProject()
    {
        string[] expectedAdapterNames = PresentAdapterAssemblyNames();
        string?[] discoveredAdapterNames = FitnessAssemblies.Adapters.Select(static a => a.GetName().Name).ToArray();

        foreach (string expected in expectedAdapterNames)
        {
            discoveredAdapterNames.ShouldContain(
                expected,
                $"{expected} was not discovered (found: {string.Join(", ", discoveredAdapterNames)}). "
                + "The adapter fitness rules would not cover every present adapter project.");
        }
    }

    private static string[] PresentAdapterAssemblyNames()
    {
        string root = FitnessAssemblies.RepositoryRoot();

        // Drive the "expected" set from the SAME suffix list discovery uses, so the guard can never silently
        // omit an adapter the rules actually iterate.
        return FitnessAssemblies.AdapterModuleSuffixes
            .Where(suffix => File.Exists(Path.Combine(
                root,
                "src",
                $"Hexalith.ChatBot.{suffix}",
                $"Hexalith.ChatBot.{suffix}.csproj")))
            .Select(static suffix => $"Hexalith.ChatBot.{suffix}")
            .ToArray();
    }
}
