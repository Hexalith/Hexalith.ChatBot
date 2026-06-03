using Hexalith.ChatBot.Architecture.Tests.Fitness;

using Shouldly;

namespace Hexalith.ChatBot.Architecture.Tests;

/// <summary>
/// Story 9.5 (boundary, NetArchTest-enforced) fitness test: every derived-store isolation seam introduced by the story
/// lives <c>internal</c> to <c>Hexalith.ChatBot.Server</c>. Because the types are non-public, a surface adapter
/// (.UI/.Cli/.Mcp) literally cannot compile a direct reference — the compiler is the first enforcer; this pins the
/// invariant so a future accidental <c>public</c> is caught. The M2 live Redis-Vector/FalkorDB binding plugs in behind
/// the same internal <c>IDerivedStore</c> seam. Mirrors <c>ReplayIsolationBoundaryFitnessTests</c>.
/// </summary>
public static class DerivedStoreIsolationBoundaryFitnessTests
{
    private static readonly string[] InternalDerivedStoreIsolationTypeNames =
    [
        "DerivedStorePartition",
        "DerivedStoreClass",
        "IDerivedStore",
        "InMemoryDerivedStore",
        "DerivedStoreEntry",
        "DerivedStoreIsolationVerifier",
        "DerivedStoreIsolationVerificationResult",
        "DerivedStoreIsolationStatus",
        "DerivedStoreIsolationProbeCoordinator",
        "DerivedStoreIsolationProbeOutcome",
    ];

    [Fact]
    public static void EveryDerivedStoreIsolationSeamIsInternalToServer()
    {
        Type[] all = FitnessAssemblies.Server.GetTypes();

        foreach (string typeName in InternalDerivedStoreIsolationTypeNames)
        {
            Type type = all.SingleOrDefault(candidate => candidate.Name == typeName)
                ?? throw new InvalidOperationException($"Expected type '{typeName}' was not found in the Server assembly.");

            // A top-level internal type reports IsPublic == false. None of these may be public.
            type.IsPublic.ShouldBeFalse($"{typeName} must remain internal to Hexalith.ChatBot.Server (no .UI/.Cli/.Mcp reference).");
        }
    }
}
