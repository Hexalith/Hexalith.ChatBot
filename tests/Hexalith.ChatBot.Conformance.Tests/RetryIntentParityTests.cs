using Hexalith.ChatBot.Conformance.Tests.Harness;

using Shouldly;

namespace Hexalith.ChatBot.Conformance.Tests;

/// <summary>
/// Differential-conformance harness — retry/idempotent-replay intent (FR86 "including the retry intent"). An
/// equivalent duplicate submission through each arm replays the prior outcome without re-dispatching: exactly
/// one dispatch, one coarse-idempotency record, and a durable view that stays at source version 1 — identical
/// across the UI/CLI/MCP arms except the declared surface origin.
/// </summary>
public static class RetryIntentParityTests
{
    private const string NoteId = "01ARZ3NDEKTSV4RRFFQ69G5FAZ";

    [Fact]
    public static async Task EquivalentDuplicateSubmitShouldReplayPriorOutcomeIdenticallyAcrossArms()
    {
        SemanticIntent intent = new(NoteId);

        ArmOutcome ui = await GovernedCommandConformanceHarness.RunRetryReplayAsync(new UiSurfaceArm(), intent, TestContext.Current.CancellationToken);
        ArmOutcome cli = await GovernedCommandConformanceHarness.RunRetryReplayAsync(new CliSurfaceArm(), intent, TestContext.Current.CancellationToken);
        ArmOutcome mcp = await GovernedCommandConformanceHarness.RunRetryReplayAsync(new McpSurfaceArm(), intent, TestContext.Current.CancellationToken);

        foreach (ArmOutcome outcome in new[] { ui, cli, mcp })
        {
            // Exactly one durable effect on replay — read from the dispatcher count, idempotency store, and the
            // projected view, never from the accepted response code.
            outcome.DispatchCount.ShouldBe(1);
            outcome.CoarseIdempotencyRecordCount.ShouldBe(1);
            outcome.DurableView.ShouldNotBeNull();
            outcome.DurableView.SourceVersion.ShouldBe(1);
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
