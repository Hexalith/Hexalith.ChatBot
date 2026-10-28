using Shouldly;

using static Hexalith.ChatBot.UI.Tests.ComponentContractSource;

namespace Hexalith.ChatBot.UI.Tests;

public sealed class WhyProjectPanelContractTests
{
    [Fact]
    public void WhyProjectActionsShouldUseFluentPrimitivesAndPreserveGovernedMarkers()
    {
        string whyPanel = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Governed/ChatBotWhyProjectPanel.razor");
        whyPanel.ShouldContain("<FluentButton", Case.Sensitive);
        whyPanel.ShouldContain("OnClick=\"CloseAsync\"", Case.Sensitive);
        whyPanel.ShouldContain("OnClick=\"OpenSupersedingCorrectionAsync\"", Case.Sensitive);
        whyPanel.ShouldContain("data-chatbot-correction-link");
        whyPanel.ShouldContain("data-chatbot-why-project-panel=\"metadata-only\"");
        whyPanel.ShouldContain("role=\"complementary\"");
        whyPanel.ShouldNotContain("<button", Case.Sensitive);
    }
}
