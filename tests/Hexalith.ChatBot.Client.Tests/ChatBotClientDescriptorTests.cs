using Hexalith.ChatBot.Client;

using Shouldly;

namespace Hexalith.ChatBot.Client.Tests;

public static class ChatBotClientDescriptorTests
{
    [Fact]
    public static void DefaultDescriptorShouldUseContractIdentifiers()
    {
        ChatBotClientDescriptor.Default.ModuleName.ShouldBe("Hexalith.ChatBot");
        ChatBotClientDescriptor.Default.DaprAppId.ShouldBe("chatbot");
    }
}
