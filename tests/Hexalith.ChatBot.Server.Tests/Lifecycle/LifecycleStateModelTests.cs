using Hexalith.ChatBot.Server.Lifecycle.StateModel;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Lifecycle;

public static class LifecycleStateModelTests
{
    [Fact]
    public static void StateVocabularyShouldBeStableAndOrdered()
    {
        LifecycleStates.All.ShouldBe(
            [
                "Received",
                "Proposed",
                "Associated",
                "Rejected",
                "Deferred",
                "NeedsReview",
                "Failed",
                "Skipped",
                "Corrected",
                "Correcting",
                "Correction-delayed",
            ],
            ignoreOrder: false);
        LifecycleSubStates.All.ShouldBe(["Correcting", "Correction-delayed"], ignoreOrder: false);
    }

    [Theory]
    [InlineData("Received", "Proposed")]
    [InlineData("Received", "NeedsReview")]
    [InlineData("Received", "Failed")]
    [InlineData("Received", "Skipped")]
    [InlineData("Proposed", "Associated")]
    [InlineData("Proposed", "Rejected")]
    [InlineData("Proposed", "Deferred")]
    [InlineData("Proposed", "NeedsReview")]
    [InlineData("Proposed", "Failed")]
    [InlineData("Deferred", "Proposed")]
    [InlineData("Deferred", "Rejected")]
    [InlineData("Deferred", "NeedsReview")]
    [InlineData("NeedsReview", "Proposed")]
    [InlineData("NeedsReview", "Associated")]
    [InlineData("NeedsReview", "Rejected")]
    [InlineData("NeedsReview", "Deferred")]
    [InlineData("Associated", "Corrected")]
    [InlineData("Corrected", "Correcting")]
    [InlineData("Correcting", "Corrected")]
    [InlineData("Correcting", "Correction-delayed")]
    [InlineData("Correction-delayed", "Corrected")]
    public static void ValidatorShouldAcceptExplicitEdgesOnly(string from, string to)
    {
        LifecycleTransitionValidation result = LifecycleTransitionValidator.Validate(new LifecycleTransitionDefinition(from, to));

        result.IsValid.ShouldBeTrue();
        result.ReasonCode.ShouldBe(LifecycleTransitionReasonCodes.ValidTransition);
        result.Transition.ToString().ShouldBe($"{from}->{to}");
    }

    [Theory]
    [InlineData("Received", "Associated")]
    [InlineData("Proposed", "Corrected")]
    [InlineData("NeedsReview", "Failed")]
    [InlineData("Corrected", "Associated")]
    [InlineData("Correction-delayed", "Correcting")]
    [InlineData("Rejected", "Proposed")]
    [InlineData("Failed", "Received")]
    [InlineData("Skipped", "Received")]
    public static void ValidatorShouldRejectRepresentativeInvalidEdges(string from, string to)
    {
        LifecycleTransitionValidation result = LifecycleTransitionValidator.Validate(new LifecycleTransitionDefinition(from, to));

        result.IsValid.ShouldBeFalse();
        result.ReasonCode.ShouldBe(LifecycleTransitionReasonCodes.InvalidTransition);
    }

    [Theory]
    [InlineData("Rejected")]
    [InlineData("Failed")]
    [InlineData("Skipped")]
    public static void TerminalStatesShouldNotTransitionInPlaceAndRequireReprocessPlan(string terminalState)
    {
        LifecycleTerminalStates.IsTerminal(terminalState).ShouldBeTrue();
        LifecycleTransitionValidator.Validate(new LifecycleTransitionDefinition(terminalState, "Received")).IsValid.ShouldBeFalse();

        LifecycleReprocessPlan plan = LifecycleReprocessFactory.Create(
            terminalState,
            "workflow-old",
            "workflow-new");

        plan.SupersededWorkflowId.ShouldBe("workflow-old");
        plan.NewWorkflowId.ShouldBe("workflow-new");
        plan.SupersededByAuditLinkName.ShouldBe("superseded_by_workflow");
        plan.SupersedesAuditLinkName.ShouldBe("supersedes_workflow");
    }
}
