using Hexalith.ChatBot.Aspire;

using Shouldly;

namespace Hexalith.ChatBot.Aspire.Tests;

public static class ChatBotAspireModuleTests
{
    [Fact]
    public static void AspireModuleShouldExposeRequiredDaprNames()
    {
        ChatBotAspireModule.AppId.ShouldBe("chatbot");
        ChatBotAspireModule.EventStoreResourceName.ShouldBe("chatbot-eventstore");
        ChatBotAspireModule.StateStoreComponentName.ShouldBe("chatbot-statestore");
        ChatBotAspireModule.PubSubComponentName.ShouldBe("chatbot-pubsub");
        ChatBotAspireModule.PubSubTopicName.ShouldBe("chatbot.events");
        ChatBotAspireModule.DeadLetterTopicName.ShouldBe("deadletter.chatbot.events");
    }
}
