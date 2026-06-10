using Hexalith.ChatBot.Conformance.Tests.Harness;

using Shouldly;

namespace Hexalith.ChatBot.Conformance.Tests;

/// <summary>
/// Story 1.11 regression pin for the original differential-conformance target:
/// <c>RecordGovernedNote</c>. Later adapter-parity stories exercise broader production
/// CLI/MCP intent catalogs; these tests keep the M0 governed-note harness from drifting away from
/// the story's success, rejection, and retry acceptance criteria.
/// </summary>
public static class Story11RecordGovernedNoteParityTests
{
    private const string NoteId = "01ARZ3NDEKTSV4RRFFQ69G5FAZ";

    [Fact]
    public static async Task RecordGovernedNoteSuccessShouldMatchAdmissionSequenceAndDurableViewAcrossArms()
    {
        SemanticIntent intent = new(NoteId);

        ArmOutcome ui = await GovernedCommandConformanceHarness.RunSuccessAsync(new UiApiSurfaceArm(), intent, TestContext.Current.CancellationToken);
        ArmOutcome cli = await GovernedCommandConformanceHarness.RunSuccessAsync(new CliSurfaceArm(), intent, TestContext.Current.CancellationToken);
        ArmOutcome mcp = await GovernedCommandConformanceHarness.RunSuccessAsync(new McpSurfaceArm(), intent, TestContext.Current.CancellationToken);

        foreach (ArmOutcome outcome in new[] { ui, cli, mcp })
        {
            outcome.AdmissionSequence.Select(static step => step.Phase).ShouldBe(["PreCommit", "PostCommit"]);
            outcome.AdmissionSequence.Select(static step => step.StateTransition).ShouldBe(["Received->Proposed", "Received->Proposed"]);
            outcome.AcceptedLifecycleState.ShouldBe("Proposed");
            outcome.DomainOutcomeIdentity.ShouldBe("GovernedNoteRecorded");
            outcome.DispatchCount.ShouldBe(1);
            outcome.CoarseIdempotencyRecordCount.ShouldBe(1);
            outcome.DurableView.ShouldNotBeNull();
            outcome.DurableView.NoteId.ShouldBe(NoteId);
            outcome.DurableView.SourceVersion.ShouldBe(1);
            outcome.DurableView.RedactionState.ShouldBe("metadata_only");
            outcome.DurableView.SourceProvenance.ShouldBe("governed-command");
        }

        ui.AuditedOrigin.ShouldBe("ui");
        cli.AuditedOrigin.ShouldBe("cli");
        mcp.AuditedOrigin.ShouldBe("mcp");

        DifferentialOracle.Compare(ui, cli).AreEqual.ShouldBeTrue();
        DifferentialOracle.Compare(ui, mcp).AreEqual.ShouldBeTrue();
        DifferentialOracle.Compare(cli, mcp).AreEqual.ShouldBeTrue();
    }

    [Fact]
    public static async Task RecordGovernedNoteReRecordShouldRejectAsFirstClassEventAndKeepDurableViewUnchangedAcrossArms()
    {
        SemanticIntent intent = new(NoteId);

        ArmOutcome ui = await GovernedCommandConformanceHarness.RunGovernedNoteReRecordRejectionAsync(new UiApiSurfaceArm(), intent, TestContext.Current.CancellationToken);
        ArmOutcome cli = await GovernedCommandConformanceHarness.RunGovernedNoteReRecordRejectionAsync(new CliSurfaceArm(), intent, TestContext.Current.CancellationToken);
        ArmOutcome mcp = await GovernedCommandConformanceHarness.RunGovernedNoteReRecordRejectionAsync(new McpSurfaceArm(), intent, TestContext.Current.CancellationToken);

        foreach (ArmOutcome outcome in new[] { ui, cli, mcp })
        {
            outcome.DomainOutcomeIdentity.ShouldBe("GovernedNoteAlreadyRecordedRejection");
            outcome.DispatchCount.ShouldBe(0);
            outcome.CoarseIdempotencyRecordCount.ShouldBe(0);
            outcome.DurableStatus.ShouldBeNull();
            outcome.DurableView.ShouldNotBeNull();
            outcome.DurableView.NoteId.ShouldBe(NoteId);
            outcome.DurableView.SourceVersion.ShouldBe(1);
        }

        // This re-record rejection is modelled at the pure-aggregate level (no gateway admission audit), so the
        // single per-arm delta is the declared origin rather than an audited one.
        ui.DeclaredOrigin.ShouldBe("ui");
        cli.DeclaredOrigin.ShouldBe("cli");
        mcp.DeclaredOrigin.ShouldBe("mcp");

        DifferentialOracle.Compare(ui, cli).AreEqual.ShouldBeTrue();
        DifferentialOracle.Compare(ui, mcp).AreEqual.ShouldBeTrue();
        DifferentialOracle.Compare(cli, mcp).AreEqual.ShouldBeTrue();
    }

    [Fact]
    public static async Task RecordGovernedNoteDuplicateReplayShouldKeepOneDurableEffectAcrossArms()
    {
        SemanticIntent intent = new(NoteId);

        ArmOutcome ui = await GovernedCommandConformanceHarness.RunRetryReplayAsync(new UiApiSurfaceArm(), intent, TestContext.Current.CancellationToken);
        ArmOutcome cli = await GovernedCommandConformanceHarness.RunRetryReplayAsync(new CliSurfaceArm(), intent, TestContext.Current.CancellationToken);
        ArmOutcome mcp = await GovernedCommandConformanceHarness.RunRetryReplayAsync(new McpSurfaceArm(), intent, TestContext.Current.CancellationToken);

        foreach (ArmOutcome outcome in new[] { ui, cli, mcp })
        {
            outcome.DomainOutcomeIdentity.ShouldBe("GovernedNoteRecorded");
            outcome.DispatchCount.ShouldBe(1);
            outcome.CoarseIdempotencyRecordCount.ShouldBe(1);
            outcome.AcceptedLifecycleState.ShouldBe("Proposed");
            outcome.DurableStatus.ShouldNotBeNull();
            outcome.DurableStatus.DuplicateAttemptCount.ShouldBe(0);
            outcome.DurableView.ShouldNotBeNull();
            outcome.DurableView.SourceVersion.ShouldBe(1);
        }

        ui.AuditedOrigin.ShouldBe("ui");
        cli.AuditedOrigin.ShouldBe("cli");
        mcp.AuditedOrigin.ShouldBe("mcp");

        DifferentialOracle.Compare(ui, cli).AreEqual.ShouldBeTrue();
        DifferentialOracle.Compare(ui, mcp).AreEqual.ShouldBeTrue();
        DifferentialOracle.Compare(cli, mcp).AreEqual.ShouldBeTrue();
    }
}
