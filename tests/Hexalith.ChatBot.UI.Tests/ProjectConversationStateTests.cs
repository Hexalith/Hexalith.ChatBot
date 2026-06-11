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
}
