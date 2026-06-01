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
        source.ShouldContain("accesscontrol.yaml");
    }

    [Fact]
    public static void AppHostShouldWireKeycloakWithHealthyWaitFor()
    {
        string source = File.ReadAllText(Path.Combine(RepositoryRoot(), "src", "Hexalith.ChatBot.AppHost", "Program.cs"));
        string realm = File.ReadAllText(Path.Combine(RepositoryRoot(), "src", "Hexalith.ChatBot.AppHost", "KeycloakRealms", "hexalith-realm.json"));

        source.ShouldContain("AddKeycloak");
        source.ShouldContain("WaitFor(keycloak)");
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
        policy.ShouldContain("appId: chatbot");
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
