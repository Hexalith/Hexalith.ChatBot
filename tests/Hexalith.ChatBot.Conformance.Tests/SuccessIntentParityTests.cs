using Hexalith.ChatBot.Conformance.Tests.Harness;
using Shouldly;

namespace Hexalith.ChatBot.Conformance.Tests;

/// <summary>
/// Differential-conformance harness — success intent. The same semantic intent
/// (<c>RequestFailedWorkflowRetry</c>) submitted through the UI/API, CLI, and MCP arms must drive the same shared pipeline and
/// produce an identical admission event sequence (AC2) and an identical durable state-store end-state (AC3),
/// with the declared <c>surfaceOrigin</c> as the only permitted delta (AC1). Every assertion reads the captured
/// audit-envelope sequence or operation-status store — never a bare HTTP 202 / CLI exit / MCP response code (AC6).
/// </summary>
public static class SuccessIntentParityTests
{
    [Fact]
    public static async Task EachArmAdmissionSequenceShouldBeIdenticalExceptDeclaredSurfaceOrigin()
    {
        SemanticCommandIntent intent = SurfaceIntentCatalog.GatewayCommandIntent;

        ArmOutcome ui = await GovernedCommandConformanceHarness.RunSuccessAsync(new UiApiSurfaceArm(), intent, TestContext.Current.CancellationToken);
        ArmOutcome cli = await GovernedCommandConformanceHarness.RunSuccessAsync(new CliSurfaceArm(), intent, TestContext.Current.CancellationToken);
        ArmOutcome mcp = await GovernedCommandConformanceHarness.RunSuccessAsync(new McpSurfaceArm(), intent, TestContext.Current.CancellationToken);

        // Admission event sequence (read from the audit-envelope capture, not a status code): PreCommit then
        // PostCommit, both Received->Proposed, accepted lifecycle Proposed, submitted retry command.
        foreach (ArmOutcome outcome in new[] { ui, cli, mcp })
        {
            outcome.AdmissionSequence.Select(static step => step.Phase).ShouldBe(["PreCommit", "PostCommit"]);
            outcome.AdmissionSequence.Select(static step => step.StateTransition).ShouldBe(["Received->Proposed", "Received->Proposed"]);
            outcome.AcceptedLifecycleState.ShouldBe("Proposed");
            outcome.DomainOutcomeIdentity.ShouldBe("RequestFailedWorkflowRetry");
            outcome.DispatchCount.ShouldBe(1);
        }

        // The single permitted delta: each arm's audited origin equals its own declared origin.
        ui.AuditedOrigin.ShouldBe("ui");
        cli.AuditedOrigin.ShouldBe("cli");
        mcp.AuditedOrigin.ShouldBe("mcp");
        new[] { ui.AuditedOrigin, cli.AuditedOrigin, mcp.AuditedOrigin }.Distinct().Count().ShouldBe(3);

        // Everything except surfaceOrigin/ids/timestamps is identical under the oracle.
        DifferentialOracle.Compare(ui, cli).AreEqual.ShouldBeTrue();
        DifferentialOracle.Compare(ui, mcp).AreEqual.ShouldBeTrue();
        DifferentialOracle.Compare(cli, mcp).AreEqual.ShouldBeTrue();
    }

    [Fact]
    public static async Task EachArmDurableStateStoreEndStateShouldBeIdentical()
    {
        SemanticCommandIntent intent = SurfaceIntentCatalog.GatewayCommandIntent;

        ArmOutcome ui = await GovernedCommandConformanceHarness.RunSuccessAsync(new UiApiSurfaceArm(), intent, TestContext.Current.CancellationToken);
        ArmOutcome cli = await GovernedCommandConformanceHarness.RunSuccessAsync(new CliSurfaceArm(), intent, TestContext.Current.CancellationToken);
        ArmOutcome mcp = await GovernedCommandConformanceHarness.RunSuccessAsync(new McpSurfaceArm(), intent, TestContext.Current.CancellationToken);

        // Durable end-state is read from the operation-status state store, never inferred from the accepted
        // response. The stored status is surface-invariant by construction — assert it explicitly.
        foreach (ArmOutcome outcome in new[] { ui, cli, mcp })
        {
            outcome.DurableStatus.ShouldNotBeNull();
            outcome.DurableStatus.OperationId.ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAX");
            outcome.DurableStatus.CommandId.ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAY");
            outcome.DurableStatus.CorrelationId.ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAW");
            outcome.DurableStatus.LifecycleState.ShouldBe("Proposed");
            outcome.DurableStatus.CompletionStatus.ShouldBe("accepted-projection-pending");
            outcome.DurableStatus.AuditStatus.ShouldBe("committed");
            outcome.DurableStatus.DuplicateAttemptCount.ShouldBe(0);
        }

        ui.DurableStatus.ShouldBe(cli.DurableStatus);
        ui.DurableStatus.ShouldBe(mcp.DurableStatus);
    }
}
