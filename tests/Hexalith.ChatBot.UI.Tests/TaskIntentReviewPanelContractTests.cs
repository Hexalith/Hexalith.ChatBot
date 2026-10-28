using Shouldly;

using static Hexalith.ChatBot.UI.Tests.ComponentContractSource;

namespace Hexalith.ChatBot.UI.Tests;

public sealed class TaskIntentReviewPanelContractTests
{
    [Fact]
    public void TaskIntentActionsShouldUseFluentPrimitivesAndPreserveGovernedMarkers()
    {
        string taskIntent = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Governed/ChatBotTaskIntentReviewPanel.razor");
        taskIntent.ShouldContain("<FluentButton", Case.Sensitive);
        taskIntent.ShouldContain("<FluentLabel", Case.Sensitive);
        taskIntent.ShouldContain("<FluentTextInput", Case.Sensitive);
        taskIntent.ShouldContain("role=\"toolbar\"");
        taskIntent.ShouldContain("aria-label=\"Task intent actions\"");
        taskIntent.ShouldContain("aria-disabled=\"@AriaDisabled(transition)\"");
        taskIntent.ShouldContain("aria-describedby=\"@DisabledReasonReferenceId(transition)\"");
        taskIntent.ShouldContain("ValueChanged=\"OnPredecessorChanged\"");
        taskIntent.ShouldContain("predecessor_task_intent_required");
        taskIntent.ShouldContain("TaskIntentTransitionSelectionModel");
        taskIntent.ShouldNotContain("<button", Case.Sensitive);
        taskIntent.ShouldNotContain("<input", Case.Sensitive);
        taskIntent.ShouldNotContain("<label", Case.Sensitive);
    }
}
