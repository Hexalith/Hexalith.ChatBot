using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

using Hexalith.ChatBot.Aspire;

IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);

string accessControlConfigPath = ResolveDaprConfigPath(builder.AppHostDirectory, "accesscontrol.yaml");

IResourceBuilder<KeycloakResource>? keycloak = null;
ReferenceExpression? realmUrl = null;
if (!string.Equals(builder.Configuration["EnableKeycloak"], "false", StringComparison.OrdinalIgnoreCase))
{
    keycloak = builder.AddKeycloak("keycloak", 8180)
        .WithRealmImport("./KeycloakRealms");
    EndpointReference keycloakEndpoint = keycloak.GetEndpoint("http");
    realmUrl = ReferenceExpression.Create($"{keycloakEndpoint}/realms/hexalith");
}

IResourceBuilder<ProjectResource> eventStore = builder.AddProject(
    ChatBotAspireModule.EventStoreServiceName,
    RootProjectPath(builder.AppHostDirectory, "Hexalith.EventStore", "src", "Hexalith.EventStore", "Hexalith.EventStore.csproj"));
IResourceBuilder<ProjectResource> tenants = builder.AddProject(
    ChatBotAspireModule.TenantsAppId,
    RootProjectPath(builder.AppHostDirectory, "Hexalith.Tenants", "src", "Hexalith.Tenants", "Hexalith.Tenants.csproj"));
IResourceBuilder<ProjectResource> chatBot = builder.AddProject(
    ChatBotAspireModule.AppId,
    RootProjectPath(builder.AppHostDirectory, "src", "Hexalith.ChatBot.Server", "Hexalith.ChatBot.Server.csproj"));

_ = builder.AddHexalithChatBot(eventStore, tenants, chatBot, accessControlConfigPath);

if (keycloak is not null && realmUrl is not null)
{
    ConfigureJwt(eventStore, keycloak, realmUrl, "hexalith-eventstore");
    ConfigureJwt(tenants, keycloak, realmUrl, "hexalith-tenants");
    ConfigureJwt(chatBot, keycloak, realmUrl, "hexalith-chatbot");
}

builder.Build().Run();

static void ConfigureJwt(
    IResourceBuilder<ProjectResource> resource,
    IResourceBuilder<KeycloakResource> keycloak,
    ReferenceExpression realmUrl,
    string audience)
{
    _ = resource
        .WithReference(keycloak)
        .WaitFor(keycloak)
        .WithEnvironment("Authentication__JwtBearer__Authority", realmUrl)
        .WithEnvironment("Authentication__JwtBearer__Issuer", realmUrl)
        .WithEnvironment("Authentication__JwtBearer__Audience", audience)
        .WithEnvironment("Authentication__JwtBearer__RequireHttpsMetadata", "false")
        .WithEnvironment("Authentication__JwtBearer__SigningKey", string.Empty);
}

static string ResolveDaprConfigPath(string appHostDirectory, string fileName)
{
    string configPath = Path.Combine(appHostDirectory, "DaprComponents", fileName);
    if (File.Exists(configPath))
    {
        return configPath;
    }

    configPath = Path.Combine(Directory.GetCurrentDirectory(), "DaprComponents", fileName);
    if (File.Exists(configPath))
    {
        return configPath;
    }

    throw new FileNotFoundException(
        "DAPR access control configuration not found. "
        + $"Ensure {fileName} exists in the DaprComponents directory.",
        configPath);
}

static string RootProjectPath(string appHostDirectory, params string[] pathParts)
{
    string repositoryRoot = Path.GetFullPath(Path.Combine(appHostDirectory, "..", ".."));
    string projectPath = Path.Combine([repositoryRoot, .. pathParts]);
    if (File.Exists(projectPath))
    {
        return projectPath;
    }

    throw new FileNotFoundException(
        "Required root-level project reference not found. Ensure root submodules were initialized non-recursively.",
        projectPath);
}
