using Hexalith.ChatBot.Conformance.Tests.Harness;

using Shouldly;

namespace Hexalith.ChatBot.Conformance.Tests;

/// <summary>
/// Non-vacuity meta-test for the differential oracle (AC6). A too-broad exclude set would make
/// <see cref="DifferentialOracle.Compare"/> a silent always-pass (a vacuous oracle giving false confidence).
/// These committed, non-destructive tests perturb one in-scope field of a captured outcome and assert the
/// oracle reports inequality AND names the diverging field — proving the equality is genuinely discriminating,
/// not silently no-op. No real cross-surface divergence is introduced in production code.
/// </summary>
public static class DifferentialOracleNonVacuityTests
{
    private static ArmOutcome Baseline(string armName, string declaredOrigin)
        => new(
            armName,
            declaredOrigin,
            declaredOrigin,
            [new AdmissionStep("PreCommit", "Received->Proposed", "accept", "admitted", "accepted", "metadata_only"),
             new AdmissionStep("PostCommit", "Received->Proposed", "accept", "committed", "accepted", "metadata_only")],
            "Proposed",
            "GovernedNoteRecorded",
            DispatchCount: 1,
            CoarseIdempotencyRecordCount: 1,
            new DurableStatusFacts(
                "01ARZ3NDEKTSV4RRFFQ69G5FAX",
                "01ARZ3NDEKTSV4RRFFQ69G5FAY",
                "01ARZ3NDEKTSV4RRFFQ69G5FAW",
                "Proposed",
                RetryCount: 0,
                "accepted-projection-pending",
                "reconciling",
                "command-execution",
                DuplicateAttemptCount: 0),
            new DurableViewFacts(
                "01ARZ3NDEKTSV4RRFFQ69G5FAZ",
                "chatbot.governed-operation-view.v1",
                "governed-command",
                "chatbot.derivation-kernel.v1",
                "metadata_only",
                "governed-operational",
                SourceVersion: 1));

    [Fact]
    public static void IdenticalOutcomesDifferingOnlyInSurfaceOriginShouldCompareEqual()
    {
        // Positive control: surfaceOrigin is excluded, so two outcomes that differ ONLY in declared origin must
        // still compare equal — this is what makes the negative results below meaningful.
        OracleVerdict verdict = DifferentialOracle.Compare(Baseline("ui", "ui"), Baseline("cli", "cli"));

        verdict.AreEqual.ShouldBeTrue();
        verdict.DivergingField.ShouldBeNull();
    }

    [Fact]
    public static void PerturbedDurableSourceVersionShouldFailAndNameTheDivergingField()
    {
        ArmOutcome baseline = Baseline("ui", "ui");
        ArmOutcome perturbed = baseline with
        {
            DurableView = baseline.DurableView! with { SourceVersion = 2 },
        };

        OracleVerdict verdict = DifferentialOracle.Compare(baseline, perturbed);

        verdict.AreEqual.ShouldBeFalse();
        verdict.DivergingField.ShouldBe("view.sourceVersion");
        verdict.LeftValue.ShouldBe("1");
        verdict.RightValue.ShouldBe("2");
    }

    [Fact]
    public static void PerturbedDurableNoteIdShouldFailAndNameTheDivergingField()
    {
        ArmOutcome baseline = Baseline("ui", "ui");
        ArmOutcome perturbed = baseline with
        {
            DurableView = baseline.DurableView! with { NoteId = "01ARZ3NDEKTSV4RRFFQ69G5FBZ" },
        };

        OracleVerdict verdict = DifferentialOracle.Compare(baseline, perturbed);

        verdict.AreEqual.ShouldBeFalse();
        verdict.DivergingField.ShouldBe("view.noteId");
    }

    [Fact]
    public static void PerturbedAdmissionStateTransitionShouldFailAndNameTheDivergingField()
    {
        ArmOutcome baseline = Baseline("ui", "ui");
        ArmOutcome perturbed = baseline with
        {
            AdmissionSequence =
            [
                baseline.AdmissionSequence[0] with { StateTransition = "Received->Associated" },
                baseline.AdmissionSequence[1],
            ],
        };

        OracleVerdict verdict = DifferentialOracle.Compare(baseline, perturbed);

        verdict.AreEqual.ShouldBeFalse();
        verdict.DivergingField.ShouldBe("admission[0].stateTransition");
    }

    [Fact]
    public static void InjectedExtraEventShouldFailOnTheAdmissionCount()
    {
        ArmOutcome baseline = Baseline("ui", "ui");
        ArmOutcome perturbed = baseline with
        {
            AdmissionSequence =
            [
                baseline.AdmissionSequence[0],
                baseline.AdmissionSequence[1],
                new AdmissionStep("PostCommit", "Received->Proposed", "accept", "committed", "accepted", "metadata_only"),
            ],
        };

        OracleVerdict verdict = DifferentialOracle.Compare(baseline, perturbed);

        verdict.AreEqual.ShouldBeFalse();
        verdict.DivergingField.ShouldBe("admission.count");
    }

    [Fact]
    public static void ProjectionShouldExposeEveryIncludedFieldSoNoFieldCanBeSilentlyExcluded()
    {
        // Coverage guard against the dev-notes-flagged "single biggest risk": a too-broad exclude set silently
        // dropping an included field would make the corresponding parity assertion vacuous. Pinning the exact
        // ordered include-set means any field accidentally removed from (or added to) Project() flips this test,
        // forcing a matching perturbation below — so vacuity cannot be introduced unnoticed.
        IReadOnlyList<string> projectedFields = DifferentialOracle.Project(Baseline("ui", "ui"))
            .Select(static field => field.Key)
            .ToArray();

        projectedFields.ShouldBe(
        [
            "lifecycle",
            "domainOutcome",
            "dispatchCount",
            "coarseIdempotencyRecordCount",
            "status.present",
            "status.operationId",
            "status.commandId",
            "status.correlationId",
            "status.lifecycleState",
            "status.retryCount",
            "status.completionStatus",
            "status.auditStatus",
            "status.operationClass",
            "status.duplicateAttemptCount",
            "admission.count",
            "admission[0].phase",
            "admission[0].stateTransition",
            "admission[0].decision",
            "admission[0].reasonCode",
            "admission[0].outcome",
            "admission[0].redactionDecision",
            "admission[1].phase",
            "admission[1].stateTransition",
            "admission[1].decision",
            "admission[1].reasonCode",
            "admission[1].outcome",
            "admission[1].redactionDecision",
            "view.present",
            "view.noteId",
            "view.schemaVersion",
            "view.sourceProvenance",
            "view.derivationKernelVersion",
            "view.redactionState",
            "view.retentionClass",
            "view.sourceVersion",
        ]);
    }

    [Fact]
    public static void PerturbedDomainOutcomeShouldFailAndNameTheDivergingField()
    {
        // domainOutcome backs AC2 (GovernedNoteRecorded) and AC4 (GovernedNoteAlreadyRecordedRejection / problem
        // identity) — if it were silently excluded, the rejection parity test would pass vacuously.
        ArmOutcome baseline = Baseline("ui", "ui");
        ArmOutcome perturbed = baseline with { DomainOutcomeIdentity = "GovernedNoteAlreadyRecordedRejection" };

        OracleVerdict verdict = DifferentialOracle.Compare(baseline, perturbed);

        verdict.AreEqual.ShouldBeFalse();
        verdict.DivergingField.ShouldBe("domainOutcome");
        verdict.LeftValue.ShouldBe("GovernedNoteRecorded");
        verdict.RightValue.ShouldBe("GovernedNoteAlreadyRecordedRejection");
    }

    [Fact]
    public static void PerturbedDispatchCountShouldFailAndNameTheDivergingField()
    {
        // dispatchCount backs AC5 (retry/replay must show exactly one dispatch) — a silent exclude would let a
        // double-dispatch divergence pass unnoticed across arms.
        ArmOutcome baseline = Baseline("ui", "ui");
        ArmOutcome perturbed = baseline with { DispatchCount = 2 };

        OracleVerdict verdict = DifferentialOracle.Compare(baseline, perturbed);

        verdict.AreEqual.ShouldBeFalse();
        verdict.DivergingField.ShouldBe("dispatchCount");
        verdict.LeftValue.ShouldBe("1");
        verdict.RightValue.ShouldBe("2");
    }

    [Fact]
    public static void PerturbedCoarseIdempotencyRecordCountShouldFailAndNameTheDivergingField()
    {
        // coarseIdempotencyRecordCount backs AC5 (exactly one coarse-idempotency record on replay).
        ArmOutcome baseline = Baseline("ui", "ui");
        ArmOutcome perturbed = baseline with { CoarseIdempotencyRecordCount = 2 };

        OracleVerdict verdict = DifferentialOracle.Compare(baseline, perturbed);

        verdict.AreEqual.ShouldBeFalse();
        verdict.DivergingField.ShouldBe("coarseIdempotencyRecordCount");
    }

    [Fact]
    public static void DroppedDurableViewShouldFailOnViewPresence()
    {
        // view.present backs AC4 fail-closed ("no durable view"): an outcome that materialized a view must never
        // compare equal to one that did not, even before any view-field comparison.
        ArmOutcome baseline = Baseline("ui", "ui");
        ArmOutcome perturbed = baseline with { DurableView = null };

        OracleVerdict verdict = DifferentialOracle.Compare(baseline, perturbed);

        verdict.AreEqual.ShouldBeFalse();
        verdict.DivergingField.ShouldBe("view.present");
        verdict.LeftValue.ShouldBe("True");
        verdict.RightValue.ShouldBe("False");
    }

    [Fact]
    public static void PerturbedStatusLifecycleShouldFailAndNameTheDivergingField()
    {
        ArmOutcome baseline = Baseline("ui-api", "ui");
        ArmOutcome perturbed = baseline with
        {
            DurableStatus = baseline.DurableStatus! with { LifecycleState = "Failed" },
        };

        OracleVerdict verdict = DifferentialOracle.Compare(baseline, perturbed);

        verdict.AreEqual.ShouldBeFalse();
        verdict.DivergingField.ShouldBe("status.lifecycleState");
    }
}
