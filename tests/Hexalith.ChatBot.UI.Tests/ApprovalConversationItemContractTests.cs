using Shouldly;

using static Hexalith.ChatBot.UI.Tests.ComponentContractSource;

namespace Hexalith.ChatBot.UI.Tests;

public sealed class ApprovalConversationItemContractTests
{
    [Fact]
    public void ApprovalActionsShouldUseFluentPrimitivesAndPreserveGovernedMarkers()
    {
        string approval = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Governed/ChatBotApprovalConversationItem.razor");
        approval.ShouldContain("<FluentButton", Case.Sensitive);
        approval.ShouldContain("Type=\"ButtonType.Button\"", Case.Sensitive);
        approval.ShouldContain("OnClick=\"ApproveAsync\"", Case.Sensitive);
        approval.ShouldContain("OnClick=\"RejectAsync\"", Case.Sensitive);
        approval.ShouldContain("OnClick=\"RequestRevisionAsync\"", Case.Sensitive);
        approval.ShouldContain("OnClick=\"CancelAsync\"", Case.Sensitive);
        approval.ShouldContain("aria-disabled=\"@ApproveAriaDisabled\"");
        approval.ShouldContain("aria-describedby=\"@(!CanApprove ? ApproveReasonId : null)\"");
        approval.ShouldContain("BlockApproveAsync");
        approval.ShouldContain("DecisionLiveRegion = \"assertive\"");
        approval.ShouldNotContain("<button", Case.Sensitive);
    }
}
