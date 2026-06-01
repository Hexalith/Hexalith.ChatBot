using Hexalith.ChatBot.Conformance.Tests.Harness;

using Shouldly;

namespace Hexalith.ChatBot.Conformance.Tests;

/// <summary>
/// Differential-conformance harness — retry/idempotent-replay intent (FR86 "including the retry intent"). An
/// equivalent duplicate submission through each arm replays the prior outcome without re-dispatching: exactly
/// one dispatch, one coarse-idempotency record, and a durable status record — identical
/// across the UI/CLI/MCP arms except the declared surface origin.
/// </summary>
public static class RetryIntentParityTests
{
    [Fact]
    public static async Task EquivalentDuplicateSubmitShouldReplayPriorOutcomeIdenticallyAcrossArms()
    {
        SemanticCommandIntent intent = SurfaceIntentCatalog.GatewayCommandIntent;

        ArmOutcome ui = await GovernedCommandConformanceHarness.RunRetryReplayAsync(new UiApiSurfaceArm(), intent, TestContext.Current.CancellationToken);
        ArmOutcome cli = await GovernedCommandConformanceHarness.RunRetryReplayAsync(new CliSurfaceArm(), intent, TestContext.Current.CancellationToken);
        ArmOutcome mcp = await GovernedCommandConformanceHarness.RunRetryReplayAsync(new McpSurfaceArm(), intent, TestContext.Current.CancellationToken);

        foreach (ArmOutcome outcome in new[] { ui, cli, mcp })
        {
            // Exactly one durable effect on replay — read from the dispatcher count, idempotency store, and the
            // operation-status store, never from the accepted response code.
            outcome.DispatchCount.ShouldBe(1);
            outcome.CoarseIdempotencyRecordCount.ShouldBe(1);
            outcome.DurableStatus.ShouldNotBeNull();
            outcome.DurableStatus.DuplicateAttemptCount.ShouldBe(0);
            outcome.AcceptedLifecycleState.ShouldBe("Proposed");
        }

        // The single permitted delta: each arm's audited origin equals its own declared origin.
        ui.AuditedOrigin.ShouldBe("ui");
        cli.AuditedOrigin.ShouldBe("cli");
        mcp.AuditedOrigin.ShouldBe("mcp");

        DifferentialOracle.Compare(ui, cli).AreEqual.ShouldBeTrue();
        DifferentialOracle.Compare(ui, mcp).AreEqual.ShouldBeTrue();
        DifferentialOracle.Compare(cli, mcp).AreEqual.ShouldBeTrue();
    }
}
