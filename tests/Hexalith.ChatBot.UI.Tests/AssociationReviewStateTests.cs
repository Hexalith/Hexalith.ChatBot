using Hexalith.ChatBot.UI.Design;
using Hexalith.ChatBot.UI.State.AssociationReview;

using Shouldly;

namespace Hexalith.ChatBot.UI.Tests;

/// <summary>Executes the association reducers and model gates directly.</summary>
public sealed class AssociationReviewStateTests
{
    [Fact]
    public void SuccessfulTerminalOutcomesAreNotFailures()
    {
        Review("Associated").IsTerminalSuccess.ShouldBeTrue();
        Review("Associated").IsTerminalFailure.ShouldBeFalse();
        Review("Corrected").IsTerminalSuccess.ShouldBeTrue();
        Review("Rejected").IsTerminalFailure.ShouldBeTrue();
        Review("Failed").IsTerminalFailure.ShouldBeTrue();
        Review("Skipped").IsTerminalFailure.ShouldBeTrue();
    }

    /// <summary>
    /// The shipped consequence copy promises a deferred item "remains visible for later review", so Deferred
    /// must stay decidable. This pins the behavior that was previously only accidental.
    /// </summary>
    [Fact]
    public void DeferredIsNotTerminalSoTheItemStaysReDecidable()
    {
        Review("Deferred").IsDeferred.ShouldBeTrue();
        Review("Deferred").IsTerminal.ShouldBeFalse();
    }

    [Fact]
    public void CorrectionLifecycleGateMatchesTheEffectExactly()
    {
        AssociationReviewModelExtensions.CanCorrect("Associated").ShouldBeTrue();
        AssociationReviewModelExtensions.CanCorrect("Corrected").ShouldBeTrue();
        AssociationReviewModelExtensions.CanCorrect("Correcting").ShouldBeFalse();
        AssociationReviewModelExtensions.CanCorrect("Correction-delayed").ShouldBeFalse();
        AssociationReviewModelExtensions.CanCorrect("NeedsReview").ShouldBeFalse();
    }

    [Fact]
    public void LoadingADifferentAssociationClearsTheOneBeingLeft()
    {
        AssociationReviewState state = Loaded() with
        {
            SelectedCandidateId = "project-a",
            DecisionNote = "note written for association A",
        };

        AssociationReviewState next = AssociationReviewReducers.ReduceLoad(
            state,
            new LoadAssociationReviewAction("association-B"));

        next.Review.ShouldBeNull();
        next.SelectedCandidateId.ShouldBeNull();
        next.DecisionNote.ShouldBeEmpty();
        next.IsLoading.ShouldBeTrue();
    }

    [Fact]
    public void ReloadingTheSameAssociationKeepsTheReviewersWorkInProgress()
    {
        AssociationReviewState state = Loaded() with { DecisionNote = "still typing" };

        AssociationReviewState next = AssociationReviewReducers.ReduceLoad(
            state,
            new LoadAssociationReviewAction("association-A"));

        next.DecisionNote.ShouldBe("still typing");
        next.Review.ShouldNotBeNull();
    }

    [Fact]
    public void ASingleIncompleteCandidateIsNotAutoSelected()
    {
        AssociationReviewState next = AssociationReviewReducers.ReduceLoaded(
            Empty(),
            new AssociationReviewLoadedAction(Review("NeedsReview", Candidate("project-a", complete: false))));

        next.SelectedCandidateId.ShouldBeNull();
    }

    [Fact]
    public void ASingleDecidableCandidateIsAutoSelected()
    {
        AssociationReviewState next = AssociationReviewReducers.ReduceLoaded(
            Empty(),
            new AssociationReviewLoadedAction(Review("NeedsReview", Candidate("project-a"))));

        next.SelectedCandidateId.ShouldBe("project-a");
    }

    [Fact]
    public void ASelectionThatDisappearedFromTheRefreshIsDropped()
    {
        AssociationReviewState state = Empty() with { SelectedCandidateId = "project-gone" };

        AssociationReviewState next = AssociationReviewReducers.ReduceLoaded(
            state,
            new AssociationReviewLoadedAction(Review("NeedsReview", Candidate("project-a"), Candidate("project-b"))));

        next.SelectedCandidateId.ShouldBeNull();
    }

    [Fact]
    public void RequestingADecisionOpensConfirmationWithoutStartingASubmission()
    {
        AssociationReviewState next = AssociationReviewReducers.ReduceDecisionRequested(
            Loaded(),
            new RequestAssociationDecisionAction("reject-all"));

        next.PendingDecisionCode.ShouldBe("reject-all");
        next.HasPendingDecision.ShouldBeTrue();
        next.IsSubmitting.ShouldBeFalse();
    }

    [Fact]
    public void CancellingADecisionLeavesNothingPending()
        => AssociationReviewReducers
            .ReduceDecisionCancelled(Loaded() with { PendingDecisionCode = "defer" })
            .PendingDecisionCode.ShouldBeNull();

    [Fact]
    public void RecordingADecisionClearsTheNoteAndKeepsTheOperationIdentity()
    {
        AssociationReviewState state = Loaded() with
        {
            DecisionNote = "note for the decision just recorded",
            PendingDecisionCode = "defer",
            IsSubmitting = true,
        };

        AssociationReviewState next = AssociationReviewReducers.ReduceSubmitted(
            state,
            new AssociationDecisionSubmittedAction(new AssociationDecisionSubmitResult(
                "command-1",
                "correlation-1",
                "task-1",
                "Deferred",
                Review("Deferred", Candidate("project-a")))));

        next.DecisionNote.ShouldBeEmpty();
        next.PendingDecisionCode.ShouldBeNull();
        next.IsSubmitting.ShouldBeFalse();
        next.LastAcceptedOperation.ShouldNotBeNull().CommandId.ShouldBe("command-1");
        next.LastAcceptedOperation.TaskId.ShouldBe("task-1");
    }

    [Fact]
    public void AFailedReadIsScopedAsALoadFailureNotASubmission()
        => AssociationReviewReducers
            .ReduceFailed(Loaded(), new AssociationReviewFailedAction("authorization_denied"))
            .ErrorScope.ShouldBe(AssociationReviewErrorScope.Load);

    [Fact]
    public void AFailedSubmitIsScopedAsASubmission()
        => AssociationReviewReducers
            .ReduceSubmitFailed(Loaded(), new AssociationDecisionSubmitFailedAction("conflict"))
            .ErrorScope.ShouldBe(AssociationReviewErrorScope.Submit);

    private static AssociationReviewState Empty()
        => new(false, false, null, null, string.Empty, string.Empty, null, null);

    private static AssociationReviewState Loaded()
        => Empty() with { Review = Review("NeedsReview", Candidate("project-a")) };

    private static AssociationCandidateModel Candidate(string projectId, bool complete = true)
        => new(projectId, projectId, 0.6, 1, [], [Evidence()], complete);

    private static AssociationEvidenceModel Evidence()
        => new("reference", "fingerprint", "kind", ChatBotEvidenceState.Available, string.Empty);

    private static AssociationReviewModel Review(string lifecycleState, params AssociationCandidateModel[] candidates)
        => new(
            "association-A",
            "intake",
            "mailbox",
            "conversation",
            null,
            lifecycleState,
            "ambiguous",
            "within",
            0.6,
            [],
            candidates,
            [Evidence()],
            [],
            [],
            "policy.v1",
            "kernel.v1",
            DateTimeOffset.UnixEpoch,
            "m365-mailbox-intake",
            "metadata-only",
            "collaboration-input",
            "schema.v1",
            1,
            "correlation");
}
