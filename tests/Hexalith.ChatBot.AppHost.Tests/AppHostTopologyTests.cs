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
        string appHostProject = File.ReadAllText(Path.Combine(
            RepositoryRoot(),
            "src",
            "Hexalith.ChatBot.AppHost",
            "Hexalith.ChatBot.AppHost.csproj"));
        appHostProject.ShouldContain("<IsPublishable>false</IsPublishable>");
        appHostProject.ShouldNotContain("<IsPublishable>true</IsPublishable>");
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
        (module.Split(".WithMetadata(\"keyPrefix\", \"none\")", StringSplitOptions.None).Length - 1).ShouldBe(1);
        (module.Split(".WithMetadata(\"keyPrefix\", \"name\")", StringSplitOptions.None).Length - 1).ShouldBe(2);
    }

    [Fact]
    public static void AppHostShouldInitializeSecurityThroughEventStoreAspireHelpers()
    {
        string source = File.ReadAllText(Path.Combine(RepositoryRoot(), "src", "Hexalith.ChatBot.AppHost", "Program.cs"));
        string csproj = File.ReadAllText(Path.Combine(RepositoryRoot(), "src", "Hexalith.ChatBot.AppHost", "Hexalith.ChatBot.AppHost.csproj"));
        string realm = File.ReadAllText(Path.Combine(RepositoryRoot(), "src", "Hexalith.ChatBot.AppHost", "KeycloakRealms", "hexalith-realm.json"));

        csproj.ShouldContain("Hexalith.EventStore.Aspire.csproj");
        csproj.ShouldContain("IsAspireProjectResource=\"false\"");
        source.ShouldContain("AddHexalithEventStoreSecurity");
        source.ShouldContain("PrepareKeycloakRealmImport");
        source.ShouldContain("ChatBotServiceGrants:ExpiresAtUtc");
        source.ShouldContain("Service-client grants expire too soon");
        source.ShouldContain("WithJwtBearerSecurity(security");
        source.ShouldContain("WithEventStoreClientCredentials(");
        source.ShouldNotContain("builder.AddKeycloak");
        source.ShouldNotContain("static void ConfigureJwt");
        source.ShouldContain("\"hexalith-chatbot\"");
        source.ShouldContain("\"hexalith-eventstore\"");
        source.ShouldContain("\"hexalith-tenants\"");
        realm.ShouldContain("\"clientId\": \"hexalith-chatbot\"");
        realm.ShouldContain("\"clientId\": \"hexalith-eventstore\"");
        realm.ShouldContain("\"clientId\": \"hexalith-tenants\"");
    }

    [Fact]
    public static void AppHostShouldEmitSafeCanonicalUtcServiceGrantExpiryTokens()
    {
        string source = File.ReadAllText(Path.Combine(RepositoryRoot(), "src", "Hexalith.ChatBot.AppHost", "Program.cs"));

        source.ShouldContain("expiresAt.UtcDateTime.ToString(\"O\", CultureInfo.InvariantCulture)");
        source.ShouldNotContain("expiresAt.ToUniversalTime().ToString(\"O\", CultureInfo.InvariantCulture)");
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
        localPolicy.ShouldContain("name: HotReload");
        localPolicy.ShouldContain("enabled: false");
        localPolicy.ShouldNotContain("appId: chatbot-ui");

        string module = File.ReadAllText(Path.Combine(
            RepositoryRoot(),
            "src",
            "Hexalith.ChatBot.AppHost",
            "Aspire",
            "ChatBotAspireModule.cs"));
        module.ShouldContain("SidecarOptions(builder, EventStoreServiceName, daprConfigPath)");
        module.ShouldContain("SidecarOptions(builder, TenantsAppId, daprConfigPath)");
        module.ShouldContain("SidecarOptions(builder, EventStoreAdminAppId, daprConfigPath)");
        module.ShouldContain("SidecarOptions(builder, EventStoreAdminUiAppId, daprConfigPath)");
    }

    [Fact]
    public static void LocalAppHostShouldAllowEachDaprInternalGrpcPortToBeIsolated()
    {
        string module = File.ReadAllText(Path.Combine(
            RepositoryRoot(),
            "src",
            "Hexalith.ChatBot.AppHost",
            "Aspire",
            "ChatBotAspireModule.cs"));

        module.ShouldContain("Dapr:InternalGrpcPorts:");
        module.ShouldContain("DaprInternalGrpcPort = internalGrpcPort");
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
            "recovery-validation-mailbox-client",
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
            JsonElement audienceMapper = client.GetProperty("protocolMappers").EnumerateArray()
                .Single(mapper => mapper.GetProperty("protocolMapper").GetString() == "oidc-audience-mapper");
            audienceMapper.GetProperty("config").GetProperty("included.client.audience").GetString()
                .ShouldBe("hexalith-chatbot", clientId);
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
            mapperText.ShouldContain("__HEXALITH_CHATBOT_SERVICE_GRANT_EXPIRES_AT__");
            mapperText.ShouldNotContain("2026-12-31T23:59:59Z");
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

    [Fact]
    public static void AppHostShouldWireSubscriberDeadLetterRoutingIntoDaprDiscovery()
    {
        string root = RepositoryRoot();
        string appHost = File.ReadAllText(Path.Combine(root, "src", "Hexalith.ChatBot.AppHost", "Program.cs"));
        string endpoints = File.ReadAllText(Path.Combine(
            root,
            "src",
            "Hexalith.ChatBot.Server",
            "Gateway",
            "ChatBotCompatibilityEndpointExtensions.cs"));
        string governedSubscription = File.ReadAllText(Path.Combine(
            root,
            "src",
            "Hexalith.ChatBot.Server",
            "Projections",
            "GovernedOperationProjectionEndpoints.cs"));

        appHost.ShouldContain("ChatBot__Projection__DeadLetterTopic");
        appHost.ShouldContain("GetTenantDeadLetterTopic(\"tenant-alpha\")");
        endpoints.ShouldContain("ChatBot:Projection:DeadLetterTopic");
        endpoints.ShouldContain("deadLetterTopic");
        governedSubscription.ShouldContain("DeadLetterTopic = deadLetterTopic");
    }

    [Fact]
    public static void AppHostShouldWireEventStoreAdminServerAndAdminUi()
    {
        string appHost = File.ReadAllText(Path.Combine(RepositoryRoot(), "src", "Hexalith.ChatBot.AppHost", "Program.cs"));
        string module = File.ReadAllText(Path.Combine(RepositoryRoot(), "src", "Hexalith.ChatBot.AppHost", "Aspire", "ChatBotAspireModule.cs"));
        string csproj = File.ReadAllText(Path.Combine(RepositoryRoot(), "src", "Hexalith.ChatBot.AppHost", "Hexalith.ChatBot.AppHost.csproj"));

        // Project references generate the typed Projects.* metadata the AppHost adds.
        csproj.ShouldContain("Hexalith.EventStore.Admin.Server.Host.csproj");
        csproj.ShouldContain("Hexalith.EventStore.Admin.UI.csproj");

        // Canonical app-ids match the Hexalith.EventStore AppHost so Admin.Server option defaults
        // (StateStoreName "statestore", EventStoreAppId "eventstore", TenantServiceAppId "tenants") resolve.
        module.ShouldContain("EventStoreAdminAppId = \"eventstore-admin\"");
        module.ShouldContain("EventStoreAdminUiAppId = \"eventstore-admin-ui\"");

        // Admin.Server references the shared actor state store + the EventStore project; Admin.UI is a
        // sidecar-backed, externally-exposed, service-invocation-only surface.
        module.ShouldContain("AddEventStoreAdmin");
        module.ShouldContain(".WithReference(resources.EventStore)");
        module.ShouldContain(".WithReference(resources.EventStoreService)");
        module.ShouldContain("WithExternalHttpEndpoints");

        // Program.cs adds both resources, invokes the wiring helper, and surfaces the Admin.Server swagger link.
        appHost.ShouldContain("Projects.Hexalith_EventStore_Admin_Server_Host");
        appHost.ShouldContain("Projects.Hexalith_EventStore_Admin_UI");
        appHost.ShouldContain("AddEventStoreAdmin(resources, eventStoreAdmin, eventStoreAdminUi, accessControlConfigPath)");
        appHost.ShouldContain("EventStore__AdminServer__SwaggerUrl");
    }

    [Fact]
    public static void AppHostShouldAuthenticateEventStoreAdminThroughKeycloak()
    {
        string appHost = File.ReadAllText(Path.Combine(RepositoryRoot(), "src", "Hexalith.ChatBot.AppHost", "Program.cs"));
        string realmPath = Path.Combine(RepositoryRoot(), "src", "Hexalith.ChatBot.AppHost", "KeycloakRealms", "hexalith-realm.json");
        using JsonDocument document = JsonDocument.Parse(File.ReadAllText(realmPath));

        // Admin.Server validates the operator JWT (audience hexalith-eventstore) via OIDC; Admin.UI acquires its
        // token through the EventStore Aspire client-credentials helper as the realm's global-admin operator.
        appHost.ShouldContain("eventStoreAdmin.WithJwtBearerSecurity(security,");
        appHost.ShouldContain("WithEventStoreClientCredentials(");
        appHost.ShouldContain("security,");
        appHost.ShouldContain("username: \"admin-user\"");
        appHost.ShouldContain("password: \"admin-pass\"");

        JsonElement root = document.RootElement;

        // The realm declares the global-admin user the Admin.UI authenticates as.
        JsonElement adminUser = root.GetProperty("users").EnumerateArray()
            .Single(u => u.GetProperty("username").GetString() == "admin-user");
        adminUser.GetProperty("attributes").GetProperty("global_admin")[0].GetString().ShouldBe("true");

        // The hexalith-eventstore client maps the audience + global_admin claims so the ROPC token authorizes
        // against Admin.Server's claims-transformation policy.
        JsonElement eventStoreClient = root.GetProperty("clients").EnumerateArray()
            .Single(c => c.GetProperty("clientId").GetString() == "hexalith-eventstore");
        string mappers = eventStoreClient.GetProperty("protocolMappers").ToString();
        mappers.ShouldContain("audience-mapper");
        mappers.ShouldContain("included.client.audience");
        mappers.ShouldContain("global-admin-mapper");
        mappers.ShouldContain("global_admin");
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
