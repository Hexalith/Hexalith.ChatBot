using System.Reflection;

using Hexalith.ChatBot.Client;
using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Server.Operations;

namespace Hexalith.ChatBot.Architecture.Tests.Fitness;

/// <summary>
/// Resolves the COMPILED ChatBot assemblies that the IL-level (NetArchTest/Mono.Cecil) fitness rules inspect.
/// Each assembly is anchored by a STABLE PUBLIC type — never <c>typeof(Program)</c>, because the Blazor/web
/// <c>Program</c> is an implicit, internal, top-level class that is not reliably reachable. The assemblies are
/// guaranteed present in the test output directory by the project references in this test's csproj.
/// </summary>
internal static class FitnessAssemblies
{
    /// <summary>
    /// Gets the surface-adapter module suffixes — the single source of truth shared with the discovery
    /// vacuity guard (<see cref="FitnessDiscoveryTests"/>) so the two can never drift apart. UI/Cli/Mcp/Workers
    /// exist today; any future adapter suffix added here is auto-covered the moment its project is built and
    /// ProjectReferenced — no edit to the fitness rules is required. This is what makes the adapter rules
    /// forward-safe.
    /// </summary>
    internal static IReadOnlyList<string> AdapterModuleSuffixes { get; } = ["UI", "Cli", "Mcp", "Workers"];

    /// <summary>Gets the Contracts assembly (anchored by <see cref="RecordGovernedNote"/>).</summary>
    internal static Assembly Contracts { get; } = typeof(RecordGovernedNote).Assembly;

    /// <summary>Gets the Client facade assembly (anchored by <see cref="ChatBotClient"/>).</summary>
    internal static Assembly Client { get; } = typeof(ChatBotClient).Assembly;

    /// <summary>Gets the Server assembly (anchored by <see cref="GovernedOperationAggregate"/>).</summary>
    internal static Assembly Server { get; } = typeof(GovernedOperationAggregate).Assembly;

    /// <summary>
    /// Gets the dynamically-discovered surface-adapter assemblies present in the test output directory.
    /// </summary>
    internal static IReadOnlyList<Assembly> Adapters { get; } = DiscoverAdapters();

    /// <summary>
    /// Gets every loaded ChatBot assembly EXCEPT Server — the scan surface for the aggregate/projection
    /// placement rule (AC4).
    /// </summary>
    internal static IReadOnlyList<Assembly> NonServerChatBotAssemblies { get; } =
        [Contracts, Client, .. Adapters];

    private static IReadOnlyList<Assembly> DiscoverAdapters()
    {
        List<Assembly> adapters = [];
        foreach (string suffix in AdapterModuleSuffixes)
        {
            string candidate = Path.Combine(AppContext.BaseDirectory, $"Hexalith.ChatBot.{suffix}.dll");
            if (File.Exists(candidate))
            {
                adapters.Add(Assembly.LoadFrom(candidate));
            }
        }

        return adapters;
    }

    /// <summary>
    /// Resolves the repository root by walking up from the test output directory to the solution file.
    /// Shared layout helper for Fitness tests that read <c>src/</c> project files.
    /// </summary>
    /// <returns>The absolute path of the repository root.</returns>
    internal static string RepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Hexalith.ChatBot.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName
            ?? throw new InvalidOperationException("Could not locate repository root from the test output directory.");
    }
}
