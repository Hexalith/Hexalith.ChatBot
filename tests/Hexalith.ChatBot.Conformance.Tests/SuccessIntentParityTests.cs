using Hexalith.ChatBot.Conformance.Tests.Harness;
using Hexalith.ChatBot.Server.Projections;

using Shouldly;

namespace Hexalith.ChatBot.Conformance.Tests;

/// <summary>
/// Differential-conformance harness — success intent. The same semantic intent
/// (<c>RecordGovernedNote</c>) submitted through the UI/CLI/MCP arms must drive the same shared pipeline and
/// produce an identical admission event sequence (AC2) and an identical durable state-store end-state (AC3),
/// with the declared <c>surfaceOrigin</c> as the only permitted delta (AC1). Every assertion reads the captured
/// audit-envelope sequence or the projected view — never a bare HTTP 202 / CLI exit / MCP response code (AC6).
/// </summary>
public static class SuccessIntentParityTests
{
    private const string NoteId = "01ARZ3NDEKTSV4RRFFQ69G5FAZ";

    [Fact]
    public static async Task EachArmAdmissionSequenceShouldBeIdenticalExceptDeclaredSurfaceOrigin()
    {
        SemanticIntent intent = new(NoteId);

        ArmOutcome ui = await GovernedCommandConformanceHarness.RunSuccessAsync(new UiSurfaceArm(), intent, TestContext.Current.CancellationToken);
        ArmOutcome cli = await GovernedCommandConformanceHarness.RunSuccessAsync(new CliSurfaceArm(), intent, TestContext.Current.CancellationToken);
        ArmOutcome mcp = await GovernedCommandConformanceHarness.RunSuccessAsync(new McpSurfaceArm(), intent, TestContext.Current.CancellationToken);

        // Admission event sequence (read from the audit-envelope capture, not a status code): PreCommit then
        // PostCommit, both Received->Proposed, accepted lifecycle Proposed, emitted GovernedNoteRecorded.
        foreach (ArmOutcome outcome in new[] { ui, cli, mcp })
        {
            outcome.AdmissionSequence.Select(static step => step.Phase).ShouldBe(["PreCommit", "PostCommit"]);
            outcome.AdmissionSequence.Select(static step => step.StateTransition).ShouldBe(["Received->Proposed", "Received->Proposed"]);
            outcome.AcceptedLifecycleState.ShouldBe("Proposed");
            outcome.DomainOutcomeIdentity.ShouldBe("GovernedNoteRecorded");
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
        SemanticIntent intent = new(NoteId);

        ArmOutcome ui = await GovernedCommandConformanceHarness.RunSuccessAsync(new UiSurfaceArm(), intent, TestContext.Current.CancellationToken);
        ArmOutcome cli = await GovernedCommandConformanceHarness.RunSuccessAsync(new CliSurfaceArm(), intent, TestContext.Current.CancellationToken);
        ArmOutcome mcp = await GovernedCommandConformanceHarness.RunSuccessAsync(new McpSurfaceArm(), intent, TestContext.Current.CancellationToken);

        // Durable end-state is read from the state store (the projected GovernedOperationView), never inferred
        // from the accepted response. The domain event carries no origin, so the projection is surface-invariant
        // by construction — assert it explicitly so the parity reads as by-construction, not coincidence.
        foreach (ArmOutcome outcome in new[] { ui, cli, mcp })
        {
            outcome.DurableView.ShouldNotBeNull();
            outcome.DurableView.NoteId.ShouldBe(NoteId);
            outcome.DurableView.SourceVersion.ShouldBe(1);
            outcome.DurableView.SourceProvenance.ShouldBe(GovernedOperationView.GovernedCommandProvenance);
            outcome.DurableView.RedactionState.ShouldBe(GovernedOperationView.MetadataOnlyRedactionState);
            outcome.DurableView.SchemaVersion.ShouldBe(GovernedOperationView.CurrentSchemaVersion);
            outcome.DurableView.DerivationKernelVersion.ShouldBe(GovernedOperationView.CurrentDerivationKernelVersion);
            outcome.DurableView.RetentionClass.ShouldBe(GovernedOperationView.GovernedOperationalRetentionClass);
        }

        ui.DurableView.ShouldBe(cli.DurableView);
        ui.DurableView.ShouldBe(mcp.DurableView);
    }
}
