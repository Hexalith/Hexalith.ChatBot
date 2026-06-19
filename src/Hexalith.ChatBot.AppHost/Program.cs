using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

using Hexalith.ChatBot.AppHost.Aspire;

IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);

// ADR exception boundary: this project is only a local-development umbrella for the sibling topology. Reusable
// domain-hosting behavior stays in the EventStore DomainService SDK; ChatBot does not ship a reusable Aspire package.

// The chatbot sidecar loads the LOCAL access-control config: this Aspire topology runs DAPR self-hosted with
// mTLS disabled, where deny-by-default policies cannot match (no verified SPIFFE caller identity). The deployed
// production posture is the deny-by-default accesscontrol.yaml (conformance reference), enforced under mTLS.
string accessControlConfigPath = ResolveDaprConfigPath(builder.AppHostDirectory, "accesscontrol.local.yaml");

IResourceBuilder<KeycloakResource>? keycloak = null;
ReferenceExpression? realmUrl = null;
if (!string.Equals(builder.Configuration["EnableKeycloak"], "false", StringComparison.OrdinalIgnoreCase))
{
    keycloak = builder.AddKeycloak("keycloak", 8180)
        .WithRealmImport("./KeycloakRealms");
    EndpointReference keycloakEndpoint = keycloak.GetEndpoint("http");
    realmUrl = ReferenceExpression.Create($"{keycloakEndpoint}/realms/hexalith");
}

IResourceBuilder<ProjectResource> eventStore = builder.AddProject<Projects.Hexalith_EventStore>(
    ChatBotAspireModule.EventStoreServiceName);
IResourceBuilder<ProjectResource> tenants = builder.AddProject<Projects.Hexalith_Tenants>(
    ChatBotAspireModule.TenantsAppId);
IResourceBuilder<ProjectResource> chatBot = builder.AddProject<Projects.Hexalith_ChatBot_Server>(
    ChatBotAspireModule.AppId);

_ = builder.AddHexalithChatBot(eventStore, tenants, chatBot, accessControlConfigPath);

// Live durable read path: project the governed-operation read model into the DAPR chatbot-statestore, and
// subscribe to the tenant-prefixed topic the EventStore publishes governed events on
// ({tenantId}.chatbot.events). M0 runs the single tenant-alpha, so the subscription topic is tenant-prefixed
// here without baking a tenant into source; M1's second tenant is additive.
_ = chatBot
    .WithEnvironment("ChatBot__UseDaprStateStores", "true")
    .WithEnvironment("ChatBot__UseDaprWorkflowRuntime", "true")
    .WithEnvironment("ChatBot__UsePeriodicEnforcementRuntime", "true")
    .WithEnvironment("ChatBot__Workflow__StateStoreName", ChatBotAspireModule.WorkflowStateStoreComponentName)
    .WithEnvironment("ChatBot__Projection__PubSubName", ChatBotAspireModule.PubSubComponentName)
    .WithEnvironment("ChatBot__Projection__Topic", $"tenant-alpha.{ChatBotAspireModule.PubSubTopicName}");

// The minimal UI core-operations surface joins the topology and reaches the ChatBot server over HTTP via
// service discovery (it submits only through IChatBotClient). It carries no DAPR sidecar, so the
// deny-by-default DAPR access-control policy is unchanged (no chatbot-ui appId is granted any operation).
IResourceBuilder<ProjectResource> chatBotUi = builder.AddProject<Projects.Hexalith_ChatBot_UI>(
    ChatBotAspireModule.ChatBotUiAppId);
_ = chatBotUi
    .WithReference(chatBot)
    .WaitFor(chatBot)
    .WithExternalHttpEndpoints();

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
