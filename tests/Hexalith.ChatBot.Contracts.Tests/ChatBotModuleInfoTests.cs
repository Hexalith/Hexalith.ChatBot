using Hexalith.ChatBot.Contracts;

using Shouldly;

namespace Hexalith.ChatBot.Contracts.Tests;

public static class ChatBotModuleInfoTests
{
    [Fact]
    public static void ModuleInfoShouldExposeStableBootstrapIdentifiers()
    {
        ChatBotModuleInfo.ModuleName.ShouldBe("Hexalith.ChatBot");
        ChatBotModuleInfo.DaprAppId.ShouldBe("chatbot");
    }
}
