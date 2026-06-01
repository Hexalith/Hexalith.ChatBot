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
}
