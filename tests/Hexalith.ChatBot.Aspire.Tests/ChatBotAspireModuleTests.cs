using Hexalith.ChatBot.Aspire;

using Shouldly;

namespace Hexalith.ChatBot.Aspire.Tests;

public static class ChatBotAspireModuleTests
{
    [Fact]
    public static void AspireModuleShouldExposeRequiredDaprNames()
    {
        ChatBotAspireModule.AppId.ShouldBe("chatbot");
        ChatBotAspireModule.ActorStateStoreComponentName.ShouldBe("statestore");
        ChatBotAspireModule.StateStoreComponentName.ShouldBe("chatbot-statestore");
        ChatBotAspireModule.PubSubComponentName.ShouldBe("chatbot-pubsub");
        ChatBotAspireModule.PubSubTopicName.ShouldBe("chatbot.events");
        ChatBotAspireModule.DeadLetterTopicName.ShouldBe("deadletter.chatbot.events");
        ChatBotAspireModule.ChatBotUiAppId.ShouldBe("chatbot-ui");
    }

    [Fact]
    public static void AppHostShouldWireTheUiSurfaceWithoutADaprSidecarOrAclChange()
    {
        string appHost = File.ReadAllText(Path.Combine(RepositoryRoot(), "src", "Hexalith.ChatBot.AppHost", "Program.cs"));
        string accessControl = File.ReadAllText(Path.Combine(
            RepositoryRoot(),
            "src",
            "Hexalith.ChatBot.AppHost",
            "DaprComponents",
            "accesscontrol.yaml"));

        // The UI joins the topology, references the server, and waits for it — over HTTP only. It is wired with
        // the typed Aspire project resource (Projects.Hexalith_ChatBot_UI) so its dapr/endpoint metadata aligns.
        appHost.ShouldContain("Hexalith_ChatBot_UI");
        appHost.ShouldContain("ChatBotUiAppId");
        appHost.ShouldContain("WaitFor(chatBot)");
        appHost.ShouldContain("WithExternalHttpEndpoints");

        // The UI carries no DAPR sidecar, so the deny-by-default policy grants chatbot-ui nothing.
        accessControl.ShouldContain("defaultAction: deny");
        accessControl.ShouldNotContain("defaultAction: allow");
        accessControl.ShouldNotContain("chatbot-ui");
    }

    private static string RepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Hexalith.ChatBot.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("Could not locate repository root.");
    }
}
