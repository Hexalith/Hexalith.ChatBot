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

        ProjectConversationState loading = ProjectConversationReducers.ReduceLoad(state);
        ProjectConversationState failed = ProjectConversationReducers.ReduceFailed(
            state,
            new ProjectConversationFailedAction("authorization_denied"));

        loading.IsLoading.ShouldBeTrue();
        loading.Conversation.ShouldBeNull();
        failed.IsLoading.ShouldBeFalse();
        failed.Conversation.ShouldBeNull();
        failed.ErrorCode.ShouldBe("authorization_denied");
    }

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
