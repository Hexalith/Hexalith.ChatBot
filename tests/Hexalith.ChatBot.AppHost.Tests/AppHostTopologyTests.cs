using System.Text.Json;

using Shouldly;

namespace Hexalith.ChatBot.AppHost.Tests;

public static class AppHostTopologyTests
{
    [Fact]
    public static void AppHostShouldFailFastWhenDaprAccessControlIsMissing()
    {
        string source = File.ReadAllText(Path.Combine(RepositoryRoot(), "src", "Hexalith.ChatBot.AppHost", "Program.cs"));

        source.ShouldContain("ResolveDaprConfigPath");
        source.ShouldContain("throw new FileNotFoundException");
        source.ShouldContain("accesscontrol.local.yaml");
    }

    [Fact]
    public static void LocalAppHostShimShouldOwnOnlyTopologySpecificDaprWiring()
    {
        string appHost = File.ReadAllText(Path.Combine(RepositoryRoot(), "src", "Hexalith.ChatBot.AppHost", "Program.cs"));
        string module = File.ReadAllText(Path.Combine(RepositoryRoot(), "src", "Hexalith.ChatBot.AppHost", "Aspire", "ChatBotAspireModule.cs"));

        appHost.ShouldContain("local-development umbrella");
        module.ShouldContain("AppId = \"chatbot\"");
        module.ShouldContain("ActorStateStoreComponentName = \"statestore\"");
        module.ShouldContain("StateStoreComponentName = \"chatbot-statestore\"");
        module.ShouldContain("WorkflowStateStoreComponentName = \"chatbot-workflow-statestore\"");
        module.ShouldContain("PubSubComponentName = \"chatbot-pubsub\"");
        module.ShouldContain("PubSubTopicName = \"chatbot.events\"");
        module.ShouldContain("DeadLetterTopicName = \"deadletter.chatbot.events\"");
        module.ShouldContain("ChatBotUiAppId = \"chatbot-ui\"");
    }

    [Fact]
    public static void LocalAppHostShimShouldPreserveDedicatedDaprResourceIsolation()
    {
        string module = File.ReadAllText(Path.Combine(RepositoryRoot(), "src", "Hexalith.ChatBot.AppHost", "Aspire", "ChatBotAspireModule.cs"));

        module.ShouldContain(".WithMetadata(\"actorStateStore\", \"true\")");
        module.ShouldContain(".WithReference(workflowStateStore)");
        module.ShouldContain(".WithReference(stateStore)");
        module.ShouldContain(".WithReference(pubSub)");
        module.ShouldContain("AppChannelAddress = \"127.0.0.1\"");
        module.ShouldContain("PlacementHostAddress");
        module.ShouldContain("SchedulerHostAddress");
        module.ShouldContain("endpoint.IsProxied = false");
        module.ShouldContain("EventStore__Publisher__PubSubName");
        module.ShouldContain("Authentication__DaprInternal__AllowedCallers__0");
        module.ShouldContain("return new HexalithChatBotResources(actorStateStore, stateStore, workflowStateStore, pubSub, eventStore, tenants, chatBot)");
    }

    [Fact]
    public static void AppHostShouldWireKeycloakWithStartWait()
    {
        string source = File.ReadAllText(Path.Combine(RepositoryRoot(), "src", "Hexalith.ChatBot.AppHost", "Program.cs"));
        string realm = File.ReadAllText(Path.Combine(RepositoryRoot(), "src", "Hexalith.ChatBot.AppHost", "KeycloakRealms", "hexalith-realm.json"));

        source.ShouldContain("AddKeycloak");
        source.ShouldContain("WaitForStart(keycloak)");
        source.ShouldNotContain("WaitFor(keycloak)");
        source.ShouldContain("\"hexalith-chatbot\"");
        source.ShouldContain("\"hexalith-eventstore\"");
        source.ShouldContain("\"hexalith-tenants\"");
        realm.ShouldContain("\"clientId\": \"hexalith-chatbot\"");
        realm.ShouldContain("\"clientId\": \"hexalith-eventstore\"");
        realm.ShouldContain("\"clientId\": \"hexalith-tenants\"");
    }

    [Fact]
    public static void DaprAccessControlShouldBeDenyByDefault()
    {
        string policy = File.ReadAllText(Path.Combine(RepositoryRoot(), "src", "Hexalith.ChatBot.AppHost", "DaprComponents", "accesscontrol.yaml"));

        policy.ShouldContain("defaultAction: deny");
        policy.ShouldNotContain("defaultAction: allow");
        policy.ShouldContain("appId: eventstore");
        policy.ShouldContain("appId: chatbot");
        policy.ShouldNotContain("appId: chatbot-ui");
    }

    [Fact]
    public static void AppHostShouldEnableHostedCorrectionPropagationWorkflowRuntime()
    {
        string source = File.ReadAllText(Path.Combine(RepositoryRoot(), "src", "Hexalith.ChatBot.AppHost", "Program.cs"));

        source.ShouldContain("ChatBot__UseDaprWorkflowRuntime");
        source.ShouldContain("ChatBot__Workflow__StateStoreName");
        source.ShouldContain("WorkflowStateStoreComponentName");
        source.ShouldNotContain("chatbot-ui\".WithDaprSidecar");
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

        appHost.ShouldContain("Hexalith_ChatBot_UI");
        appHost.ShouldContain("ChatBotUiAppId");
        appHost.ShouldContain("WaitFor(chatBot)");
        appHost.ShouldContain("WithExternalHttpEndpoints");

        accessControl.ShouldContain("defaultAction: deny");
        accessControl.ShouldNotContain("defaultAction: allow");
        accessControl.ShouldNotContain("chatbot-ui");
    }

    [Fact]
    public static void LocalDaprAccessControlShouldBeExplicitlyScopedToSelfHostedAspireOnly()
    {
        string appHost = File.ReadAllText(Path.Combine(RepositoryRoot(), "src", "Hexalith.ChatBot.AppHost", "Program.cs"));
        string localPolicy = File.ReadAllText(Path.Combine(
            RepositoryRoot(),
            "src",
            "Hexalith.ChatBot.AppHost",
            "DaprComponents",
            "accesscontrol.local.yaml"));

        appHost.ShouldContain("ResolveDaprConfigPath(builder.AppHostDirectory, \"accesscontrol.local.yaml\")");
        localPolicy.ShouldContain("LOCAL DEVELOPMENT ONLY");
        localPolicy.ShouldContain("self-hosted Aspire Tier-3 topology");
        localPolicy.ShouldContain("defaultAction: allow");
        localPolicy.ShouldNotContain("appId: chatbot-ui");
    }

    [Fact]
    public static void KeycloakRealmShouldDeclareLeastPrivilegeServiceAccountClients()
    {
        string realmPath = Path.Combine(RepositoryRoot(), "src", "Hexalith.ChatBot.AppHost", "KeycloakRealms", "hexalith-realm.json");
        using JsonDocument document = JsonDocument.Parse(File.ReadAllText(realmPath));
        JsonElement clients = document.RootElement.GetProperty("clients");
        Dictionary<string, JsonElement> clientsById = clients
            .EnumerateArray()
            .ToDictionary(client => client.GetProperty("clientId").GetString()!, StringComparer.Ordinal);

        string[] required =
        [
            "cli-automation-client",
            "mcp-tool-client",
            "background-worker-client",
            "mailbox-ingestion-client",
            "audit-projection-client",
            "ai-action-execution-client",
        ];

        foreach (string clientId in required)
        {
            clientsById.ContainsKey(clientId).ShouldBeTrue(clientId);
            JsonElement client = clientsById[clientId];
            client.GetProperty("enabled").GetBoolean().ShouldBeTrue(clientId);
            client.GetProperty("serviceAccountsEnabled").GetBoolean().ShouldBeTrue(clientId);
            client.GetProperty("publicClient").GetBoolean().ShouldBeFalse(clientId);
            client.GetProperty("directAccessGrantsEnabled").GetBoolean().ShouldBeFalse(clientId);
            client.GetProperty("fullScopeAllowed").GetBoolean().ShouldBeFalse(clientId);
            client.TryGetProperty("realmRoles", out _).ShouldBeFalse(clientId);
            client.TryGetProperty("defaultRoles", out _).ShouldBeFalse(clientId);

            string mapperText = client.GetProperty("protocolMappers").ToString();
            mapperText.ShouldContain("chatbot:actor-type");
            mapperText.ShouldContain("chatbot:service-client-id");
            mapperText.ShouldContain("chatbot:service-client-class");
            mapperText.ShouldContain("chatbot:service-client-surface");
            mapperText.ShouldContain("eventstore:tenant");
            mapperText.ShouldContain("chatbot:service-client-grant-tenant");
            mapperText.ShouldContain("chatbot:service-client-grant-id");
            mapperText.ShouldContain("chatbot:service-client-grant-expiry");
            mapperText.ShouldContain("chatbot:service-client-scope");
            mapperText.ShouldContain("chatbot:service-client-command");
            mapperText.ShouldContain("chatbot:service-client-command-set-version");
            mapperText.ShouldNotContain("client-secret", Case.Insensitive);
        }

        JsonElement publicClient = clientsById["hexalith-chatbot"];
        publicClient.GetProperty("publicClient").GetBoolean().ShouldBeTrue();
        publicClient.GetProperty("serviceAccountsEnabled").GetBoolean().ShouldBeFalse();

        string mcpMapperText = clientsById["mcp-tool-client"].GetProperty("protocolMappers").ToString();
        mcpMapperText.ShouldContain("\"claim.value\": \"mcp-tool-client\"");
        mcpMapperText.ShouldContain("\"claim.value\": \"mcp-tool\"");
        mcpMapperText.ShouldContain("\"claim.value\": \"mcp\"");
        mcpMapperText.ShouldNotContain("\"claim.value\": \"hexalith-chatbot\"");
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
