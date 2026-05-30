using Hexalith.ChatBot.Testing;

using Shouldly;

namespace Hexalith.ChatBot.Testing.Tests;

public static class ChatBotTestConstantsTests
{
    [Fact]
    public static void TestingConstantsShouldMirrorContractIdentifiers()
    {
        ChatBotTestConstants.ModuleName.ShouldBe("Hexalith.ChatBot");
        ChatBotTestConstants.DaprAppId.ShouldBe("chatbot");
    }
}
