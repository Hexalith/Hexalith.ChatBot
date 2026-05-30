using Hexalith.ChatBot.Contracts;

using Shouldly;

namespace Hexalith.ChatBot.IntegrationTests;

public static class IntegrationPlaceholderTests
{
    [Fact]
    public static void IntegrationLaneShouldBindToChatBotModuleIdentity()
    {
        // Reserves the Aspire/DAPR integration lane for Story 1.2+ while asserting a real, stable
        // scaffold invariant (the module's published identity) instead of a self-referential constant.
        ChatBotModuleInfo.ModuleName.ShouldBe("Hexalith.ChatBot");
        ChatBotModuleInfo.DaprAppId.ShouldBe("chatbot");
    }
}
