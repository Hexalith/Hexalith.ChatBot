using Hexalith.ChatBot.Server.Governance.AiMediation;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Governance.AiMediation;

public sealed class ApprovedAiActionCommandAllowlistTests
{
    [Fact]
    public void M0AllowlistShouldContainOnlyAppendConversationMessage()
    {
        ApprovedAiActionCommandAllowlist allowlist = new();

        allowlist.CurrentVersion.ShouldBe(AiActionCommandMetadataProvider.M0AllowlistVersion);
        allowlist.IsAllowed(AiActionCommandMetadataProvider.AppendConversationMessageCommandName, AiActionCommandMetadataProvider.M0AllowlistVersion).ShouldBeTrue();
        allowlist.IsAllowed(AiActionCommandMetadataProvider.ExecuteLowRiskAssistanceCommandName, AiActionCommandMetadataProvider.M0AllowlistVersion).ShouldBeFalse();
        allowlist.IsAllowed("Project.SendEmail", AiActionCommandMetadataProvider.M0AllowlistVersion).ShouldBeFalse();
        allowlist.IsAllowed(AiActionCommandMetadataProvider.AppendConversationMessageCommandName, "ai-action-command-allowlist.m1").ShouldBeFalse();
    }
}
