using System.Xml.Linq;

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

    [Fact]
    public static void StoryElevenSixTierThreeLaunchPathShouldUseTheThinLocalAppHostShim()
    {
        string root = RepositoryRoot();
        string integrationProjectPath = Path.Combine(root, "tests", "Hexalith.ChatBot.IntegrationTests", "Hexalith.ChatBot.IntegrationTests.csproj");
        string e2eTests = File.ReadAllText(Path.Combine(root, "tests", "Hexalith.ChatBot.IntegrationTests", "TrivialGovernedCommandAspireE2eTests.cs"));
        string appHostProject = File.ReadAllText(Path.Combine(root, "src", "Hexalith.ChatBot.AppHost", "Hexalith.ChatBot.AppHost.csproj"));
        string appHostProgram = File.ReadAllText(Path.Combine(root, "src", "Hexalith.ChatBot.AppHost", "Program.cs"));

        XDocument integrationProject = XDocument.Load(integrationProjectPath);
        XElement appHostReference = integrationProject.Descendants("ProjectReference")
            .Single(reference => string.Equals(
                reference.Attribute("Include")?.Value,
                "..\\..\\src\\Hexalith.ChatBot.AppHost\\Hexalith.ChatBot.AppHost.csproj",
                StringComparison.Ordinal));

        appHostReference.Element("IsAspireProjectResource")?.Value.ShouldBe("false");
        e2eTests.ShouldContain("CreateAsync<global::Projects.Hexalith_ChatBot_AppHost>");
        e2eTests.ShouldContain("WaitForChatBotDaprSidecarAsync");
        e2eTests.ShouldContain("CorrectionPropagationWorkflowRuntimeShouldBeHealthyInRealDaprTopology");

        appHostProject.ShouldContain("$(HexalithEventStoreRoot)\\src\\Hexalith.EventStore\\Hexalith.EventStore.csproj");
        appHostProject.ShouldContain("$(HexalithTenantsRoot)\\src\\Hexalith.Tenants\\Hexalith.Tenants.csproj");
        appHostProject.ShouldNotContain("Hexalith.ChatBot.Aspire");
        appHostProject.ShouldNotContain("Hexalith.ChatBot.ServiceDefaults");
        appHostProgram.ShouldContain("local-development umbrella");
        appHostProgram.ShouldContain("Hexalith_EventStore");
        appHostProgram.ShouldContain("Hexalith_Tenants");
    }

    [Fact]
    public static void StoryElevenSixThinShimShouldPreserveDedicatedDaprResourcesForTierThreeE2e()
    {
        string root = RepositoryRoot();
        string appHost = File.ReadAllText(Path.Combine(root, "src", "Hexalith.ChatBot.AppHost", "Program.cs"));
        string module = File.ReadAllText(Path.Combine(root, "src", "Hexalith.ChatBot.AppHost", "Aspire", "ChatBotAspireModule.cs"));
        string resources = File.ReadAllText(Path.Combine(root, "src", "Hexalith.ChatBot.AppHost", "Aspire", "HexalithChatBotResources.cs"));

        module.ShouldContain("public const string AppId = \"chatbot\"");
        module.ShouldContain("public const string ChatBotUiAppId = \"chatbot-ui\"");
        module.ShouldContain("public const string EventStoreServiceName = \"eventstore\"");
        module.ShouldContain("public const string TenantsAppId = \"tenants\"");
        module.ShouldContain("public const string ActorStateStoreComponentName = \"statestore\"");
        module.ShouldContain("public const string StateStoreComponentName = \"chatbot-statestore\"");
        module.ShouldContain("public const string WorkflowStateStoreComponentName = \"chatbot-workflow-statestore\"");
        module.ShouldContain("public const string PubSubComponentName = \"chatbot-pubsub\"");
        module.ShouldContain("public const string PubSubTopicName = \"chatbot.events\"");
        module.ShouldContain("public const string DeadLetterTopicName = \"deadletter.chatbot.events\"");

        module.ShouldContain(".WithMetadata(\"actorStateStore\", \"true\")");
        module.ShouldContain(".WithReference(workflowStateStore)");
        module.ShouldContain(".WithReference(stateStore)");
        module.ShouldContain(".WithReference(pubSub)");
        module.ShouldContain("endpoint.IsProxied = false");
        module.ShouldContain("EventStore__Publisher__PubSubName");
        module.ShouldContain("Authentication__DaprInternal__AllowedCallers__0");

        appHost.ShouldContain("ChatBot__UseDaprStateStores");
        appHost.ShouldContain("ChatBot__UseDaprWorkflowRuntime");
        appHost.ShouldContain("ChatBot__Workflow__StateStoreName");
        appHost.ShouldContain("ChatBot__Projection__PubSubName");
        appHost.ShouldContain("ChatBot__Projection__Topic");
        appHost.ShouldContain("WaitFor(chatBot)");
        appHost.ShouldContain("WithExternalHttpEndpoints");
        appHost.ShouldNotContain("chatbot-ui\".WithDaprSidecar");

        resources.ShouldContain("IResourceBuilder<IDaprComponentResource> WorkflowStateStore");
        resources.ShouldContain("IResourceBuilder<ProjectResource> ChatBotService");
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
