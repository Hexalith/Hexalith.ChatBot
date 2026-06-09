using Shouldly;

namespace Hexalith.ChatBot.IntegrationTests;

public static class ScaffoldTopologySmokeTests
{
    [Fact]
    public static void AppHostScaffoldShouldBindChatBotToDaprStateAndTenantScopedProjectionTopic()
    {
        string appHost = File.ReadAllText(Path.Combine(RepositoryRoot(), "src", "Hexalith.ChatBot.AppHost", "Program.cs"));
        string productionAccessControl = File.ReadAllText(Path.Combine(
            RepositoryRoot(),
            "src",
            "Hexalith.ChatBot.AppHost",
            "DaprComponents",
            "accesscontrol.yaml"));
        string localAccessControl = File.ReadAllText(Path.Combine(
            RepositoryRoot(),
            "src",
            "Hexalith.ChatBot.AppHost",
            "DaprComponents",
            "accesscontrol.local.yaml"));

        appHost.ShouldContain("ChatBot__UseDaprStateStores");
        appHost.ShouldContain("ChatBot__Projection__PubSubName");
        appHost.ShouldContain("ChatBot__Projection__Topic");
        appHost.ShouldContain("tenant-alpha.{ChatBotAspireModule.PubSubTopicName}");
        appHost.ShouldContain("accesscontrol.local.yaml");

        productionAccessControl.ShouldContain("defaultAction: deny");
        productionAccessControl.ShouldContain("appId: eventstore");
        productionAccessControl.ShouldContain("appId: chatbot");
        productionAccessControl.ShouldNotContain("appId: chatbot-ui");

        localAccessControl.ShouldContain("LOCAL DEVELOPMENT ONLY");
        localAccessControl.ShouldContain("defaultAction: allow");
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
