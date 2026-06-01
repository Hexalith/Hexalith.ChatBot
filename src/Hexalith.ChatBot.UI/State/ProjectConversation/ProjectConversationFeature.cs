using Fluxor;

namespace Hexalith.ChatBot.UI.State.ProjectConversation;

public sealed class ProjectConversationFeature : Feature<ProjectConversationState>
{
    public override string GetName() => "ProjectConversation";

    protected override ProjectConversationState GetInitialState()
        => new(IsLoading: false, Conversation: null, ErrorCode: null);
}
