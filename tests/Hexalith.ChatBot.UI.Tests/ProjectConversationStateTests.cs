using Hexalith.ChatBot.UI.State.ProjectConversation;

using Shouldly;

namespace Hexalith.ChatBot.UI.Tests;

public sealed class ProjectConversationStateTests
{
    [Fact]
    public void LoadAndFailureReducersShouldClearPriorProjectConversation()
    {
        ProjectConversationModel priorConversation = new(
            "project-alpha",
            "Project Alpha",
            null,
            "Current",
            "Associated",
            [],
            null,
            false,
            25,
            "m365-mailbox-intake",
            "metadata_only",
            "collaboration_input",
            "chatbot.project-conversation-response.v1",
            "01ARZ3NDEKTSV4RRFFQ69G5FAX",
            "none");
        ProjectConversationState state = new(false, priorConversation, null);

        // Switching to a DIFFERENT project drops the prior conversation.
        ProjectConversationState switching = ProjectConversationReducers.ReduceLoad(
            state,
            new LoadProjectConversationAction("project-beta"));
        ProjectConversationState failed = ProjectConversationReducers.ReduceFailed(
            state,
            new ProjectConversationFailedAction("authorization_denied"));

        switching.IsLoading.ShouldBeTrue();
        switching.Conversation.ShouldBeNull();
        failed.IsLoading.ShouldBeFalse();
        failed.Conversation.ShouldBeNull();
        failed.ErrorCode.ShouldBe("authorization_denied");
    }

    [Fact]
    public void SameProjectReloadShouldKeepTheRenderedConversationAndOpenWhyPanel()
    {
        // A nudge / reconnect / post-submit / post-cancel re-query re-loads the conversation already on screen. Blanking
        // it there unmounted the stream, the composer and the Stop control mid-generation, destroying focus and any
        // typed draft, and silently closed the reviewer's open Why panel. Only a genuine project switch may clear.
        ProjectConversationModel conversation = Conversation("project-alpha");
        ProjectConversationState state = new(false, conversation, null)
        {
            WhyPanelProjectId = "project-alpha",
            WhyPanelAssociationId = "association-001",
            LastAcceptedAiResponseNudge = null,
        };

        ProjectConversationState reloading = ProjectConversationReducers.ReduceLoad(
            state,
            new LoadProjectConversationAction("project-alpha"));

        reloading.IsLoading.ShouldBeTrue();
        reloading.Conversation.ShouldBe(conversation);
        reloading.WhyPanelProjectId.ShouldBe("project-alpha");
        reloading.WhyPanelAssociationId.ShouldBe("association-001");
    }

    [Fact]
    public void CrossProjectNudgeShouldFailClosedWhileNoConversationIsLoaded()
    {
        // The cross-project guard used to be skipped whenever Conversation was null, so a foreign-project nudge was
        // accepted for the whole window in which a reload was in flight.
        ProjectConversationState noConversation = new(false, null, null);
        ProjectConversationAiResponseNudgeModel foreign = new(
            "project-OTHER",
            "conversation-001",
            "response-001",
            "generation-001",
            "01ARZ3NDEKTSV4RRFFQ69G5FAX",
            11,
            5,
            "rendering",
            "metadata_only",
            "metadata_only");

        ProjectConversationState result = ProjectConversationReducers.ReduceAiResponseNudge(
            noConversation,
            new ProjectConversationAiResponseNudgeReceivedAction(foreign));

        result.LastAcceptedAiResponseNudge.ShouldBeNull();
        result.StreamingErrorCode.ShouldBe("ai-response-nudge-unsafe");
    }

    private static ProjectConversationModel Conversation(string projectId) => new(
        projectId,
        "Project Alpha",
        null,
        "Current",
        "Associated",
        [],
        null,
        false,
        25,
        "m365-mailbox-intake",
        "metadata_only",
        "collaboration_input",
        "chatbot.project-conversation-response.v1",
        "01ARZ3NDEKTSV4RRFFQ69G5FAX",
        "none");

    [Fact]
    public void ComposerReducersShouldTrackSubmittingAcceptedAndValidationStates()
    {
        ProjectConversationState state = new(false, null, null);

        ProjectConversationState submitting = ProjectConversationReducers.ReduceSubmitComposer(
            state,
            new SubmitProjectConversationComposerAction("project-001", ProjectConversationComposerMode.AskAi, "request", "en-US", 8));
        ProjectConversationSubmissionReceiptModel receipt = new(
            ProjectConversationComposerMode.AskAi,
            "command-001",
            "correlation-001",
            "task-001",
            "Proposed",
            new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero),
            "wait-for-projection");

        ProjectConversationState accepted = ProjectConversationReducers.ReduceSubmissionAccepted(
            submitting,
            new ProjectConversationSubmissionAcceptedAction(receipt));
        ProjectConversationState invalid = ProjectConversationReducers.ReduceComposerValidationFailed(
            accepted,
            new ProjectConversationComposerValidationFailedAction("composer_input_required"));

        submitting.IsSubmitting.ShouldBeTrue();
        submitting.ComposerMode.ShouldBe(ProjectConversationComposerMode.AskAi);
        accepted.IsSubmitting.ShouldBeFalse();
        accepted.PendingSubmission.ShouldBe(receipt);
        invalid.ComposerValidationErrorCode.ShouldBe("composer_input_required");
    }

    [Fact]
    public void StreamingReducersShouldAcceptOnlyNewerMatchingMetadataOnlyNudges()
    {
        ProjectConversationState state = new(
            false,
            Conversation("project-001"),
            null,
            LastAcceptedAiResponseNudge: new ProjectConversationAiResponseNudgeModel(
                "project-001",
                "conversation-001",
                "response-001",
                "generation-001",
                "correlation-001",
                10,
                3,
                "rendering",
                "metadata_only",
                "metadata_only"));
        ProjectConversationAiResponseNudgeModel accepted = new(
            "project-001",
            "conversation-001",
            "response-001",
            "generation-001",
            "correlation-001",
            11,
            4,
            "rendering",
            "metadata_only",
            "metadata_only");
        ProjectConversationAiResponseNudgeModel stale = accepted with { Sequence = 2 };
        ProjectConversationAiResponseNudgeModel mismatched = accepted with { ProjectId = "project-other" };

        ProjectConversationState afterAccepted = ProjectConversationReducers.ReduceAiResponseNudge(
            state,
            new ProjectConversationAiResponseNudgeReceivedAction(accepted));
        ProjectConversationState afterStale = ProjectConversationReducers.ReduceAiResponseNudge(
            state,
            new ProjectConversationAiResponseNudgeReceivedAction(stale));
        ProjectConversationState afterMismatch = ProjectConversationReducers.ReduceAiResponseNudge(
            state,
            new ProjectConversationAiResponseNudgeReceivedAction(mismatched));

        afterAccepted.LastAcceptedAiResponseNudge.ShouldBe(accepted);
        afterAccepted.StreamingErrorCode.ShouldBeNull();
        afterStale.StreamingErrorCode.ShouldBe("ai-response-nudge-unsafe");
        afterMismatch.StreamingErrorCode.ShouldBe("ai-response-nudge-unsafe");
    }

    [Fact]
    public void ReconnectReducerShouldSurfaceLocalizedNoticeClearedByFreshActivity()
    {
        ProjectConversationState state = new(false, Conversation("project-001"), null);

        ProjectConversationState reconnected = ProjectConversationReducers.ReduceAiResponseReconnect(state);
        ProjectConversationState afterSubmit = ProjectConversationReducers.ReduceSubmitComposer(
            reconnected,
            new SubmitProjectConversationComposerAction("project-001", ProjectConversationComposerMode.AskAi, "hi", "en-US", 1));

        reconnected.StreamingNotice.ShouldBe("reconnected");
        reconnected.StreamingErrorCode.ShouldBeNull();
        afterSubmit.StreamingNotice.ShouldBeNull();
    }

    private static ProjectConversationModel Conversation(string projectId)
        => new(
            projectId,
            "Project",
            null,
            "Current",
            "Associated",
            [],
            null,
            false,
            25,
            "m365-mailbox-intake",
            "metadata_only",
            "collaboration_input",
            "chatbot.project-conversation-response.v1",
            "correlation-001",
            "none");
}
