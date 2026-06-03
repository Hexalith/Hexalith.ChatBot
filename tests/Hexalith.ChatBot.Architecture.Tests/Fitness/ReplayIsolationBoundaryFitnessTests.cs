using Hexalith.ChatBot.Architecture.Tests.Fitness;

using Shouldly;

namespace Hexalith.ChatBot.Architecture.Tests;

/// <summary>
/// Story 9.4 (boundary, NetArchTest-enforced) fitness test: every replay-isolation seam introduced by the story lives
/// <c>internal</c> to <c>Hexalith.ChatBot.Server</c>. Because the types are non-public, a surface adapter (.UI/.Cli/.Mcp)
/// literally cannot compile a direct reference to them — the compiler is the first enforcer; this pins the invariant so a
/// future accidental <c>public</c> is caught. A future replay-driver reaches replay only through the gateway/client seam
/// (the optional replay-run marker on the submission), never these internals.
/// </summary>
public static class ReplayIsolationBoundaryFitnessTests
{
    private static readonly string[] InternalReplayIsolationTypeNames =
    [
        "ReplayTenantPolicy",
        "IOutboundTraceStore",
        "InMemoryOutboundTraceStore",
        "OutboundTraceRecord",
        "TestModeOutboundMailboxSender",
        "ReplayAwareOutboundMailboxSender",
        "ReplayIsolationVerifier",
        "ReplayIsolationVerificationResult",
        "ReplayIsolationProbeCoordinator",
        "ReplayIsolationProbeOutcome",
        "AuditEnvelope",
    ];

    [Fact]
    public static void EveryReplayIsolationSeamIsInternalToServer()
    {
        Type[] all = FitnessAssemblies.Server.GetTypes();

        foreach (string typeName in InternalReplayIsolationTypeNames)
        {
            Type type = all.SingleOrDefault(candidate => candidate.Name == typeName)
                ?? throw new InvalidOperationException($"Expected type '{typeName}' was not found in the Server assembly.");

            // A top-level internal type reports IsPublic == false (IsNotPublic == true). None of these may be public.
            type.IsPublic.ShouldBeFalse($"{typeName} must remain internal to Hexalith.ChatBot.Server (no .UI/.Cli/.Mcp reference).");
        }
    }
}
