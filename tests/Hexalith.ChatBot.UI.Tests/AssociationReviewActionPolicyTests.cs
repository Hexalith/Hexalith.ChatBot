using Hexalith.ChatBot.UI.State.AssociationReview;

using Shouldly;

namespace Hexalith.ChatBot.UI.Tests;

/// <summary>
/// Executes the association action gate directly. This is a governance-visible decision - which actions an
/// operator may take, and the reason shown when they may not - so it is asserted by running it, not by
/// checking that its source text exists.
/// </summary>
public sealed class AssociationReviewActionPolicyTests
{
    [Fact]
    public void TerminalAssociationDisablesEveryDecisionAction()
        => AssociationReviewActionPolicy.ResolveDecisionDisabledReasonCode(
            isTerminal: true,
            isSubmitting: false,
            requiresCandidate: false,
            hasSelectedCandidate: true,
            [])
            .ShouldBe("terminal-state");

    [Fact]
    public void CommandInFlightDisablesDecisionActionsSoOneClickCannotBecomeTwoCommands()
        => AssociationReviewActionPolicy.ResolveDecisionDisabledReasonCode(
            isTerminal: false,
            isSubmitting: true,
            requiresCandidate: false,
            hasSelectedCandidate: true,
            [])
            .ShouldBe("submit-in-flight");

    [Fact]
    public void ActionRequiringACandidateIsDisabledWithoutOne()
        => AssociationReviewActionPolicy.ResolveDecisionDisabledReasonCode(
            isTerminal: false,
            isSubmitting: false,
            requiresCandidate: true,
            hasSelectedCandidate: false,
            [])
            .ShouldBe("candidate-required");

    [Theory]
    [InlineData("not-authorized")]
    [InlineData("target-unauthorized")]
    [InlineData("policy-blocked")]
    [InlineData("already-corrected")]
    [InlineData("audit-unavailable")]
    [InlineData("projection-invalidation-unavailable")]
    public void ServerBlockedReasonsDisableDecisionActions(string reason)
        => AssociationReviewActionPolicy.ResolveDecisionDisabledReasonCode(
            isTerminal: false,
            isSubmitting: false,
            requiresCandidate: false,
            hasSelectedCandidate: true,
            [reason])
            .ShouldBe(reason);

    [Fact]
    public void AuthorizationOutranksALaterReasonWhenSeveralApply()
        => AssociationReviewActionPolicy.ResolveDecisionDisabledReasonCode(
            isTerminal: false,
            isSubmitting: false,
            requiresCandidate: false,
            hasSelectedCandidate: true,
            ["projection-pending", "not-authorized"])
            .ShouldBe("not-authorized");

    [Fact]
    public void NoReasonMeansEnabled()
        => AssociationReviewActionPolicy.ResolveDecisionDisabledReasonCode(
            isTerminal: false,
            isSubmitting: false,
            requiresCandidate: true,
            hasSelectedCandidate: true,
            [])
            .ShouldBeEmpty();

    [Fact]
    public void CorrectionIsDisabledWhenTheLifecycleDoesNotAdmitIt()
        => AssociationReviewActionPolicy.ResolveCorrectionDisabledReasonCode(
            canCorrect: false,
            isSubmitting: false,
            hasSelectedCandidate: true,
            [])
            .ShouldBe("correction-invalid-lifecycle");

    [Fact]
    public void CorrectionRequiresATarget()
        => AssociationReviewActionPolicy.ResolveCorrectionDisabledReasonCode(
            canCorrect: true,
            isSubmitting: false,
            hasSelectedCandidate: false,
            [])
            .ShouldBe("correction-target-required");

    [Theory]
    [InlineData("delayed", null, AssociationCorrectionStatus.Delayed)]
    [InlineData("DELAYED", null, AssociationCorrectionStatus.Delayed)]
    [InlineData(null, "pending", AssociationCorrectionStatus.Pending)]
    [InlineData(null, "preview-only", AssociationCorrectionStatus.Partial)]
    public void CorrectionStatusIsResolvedCaseInsensitivelyFromServerTokens(
        string? propagationStatus,
        string? downstreamStatus,
        AssociationCorrectionStatus expected)
        => AssociationReviewActionPolicy.ResolveCorrectionStatus(
            [],
            propagationStatus,
            downstreamStatus,
            correctedProjectId: "01ARZ3NDEKTSV4RRFFQ69G5FBC",
            isCorrectedContextStale: false)
            .ShouldBe(expected);

    [Fact]
    public void BlockedReasonsOutrankEveryOtherCorrectionStatus()
        => AssociationReviewActionPolicy.ResolveCorrectionStatus(
            ["target-unauthorized"],
            "delayed",
            "pending",
            correctedProjectId: "01ARZ3NDEKTSV4RRFFQ69G5FBC",
            isCorrectedContextStale: true)
            .ShouldBe(AssociationCorrectionStatus.Blocked);

    [Fact]
    public void AnUnrecognizedDownstreamStatusIsNotReportedAsACompletedCorrection()
        => AssociationReviewActionPolicy.ResolveCorrectionStatus(
            [],
            propagationStatus: null,
            downstreamImpactStatus: "some-future-token",
            correctedProjectId: "01ARZ3NDEKTSV4RRFFQ69G5FBC",
            isCorrectedContextStale: false)
            .ShouldNotBe(AssociationCorrectionStatus.Success);
}
