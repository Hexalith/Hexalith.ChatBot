using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

using Hexalith.ChatBot.AppHost.Aspire;
using Hexalith.EventStore.Aspire;

IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);

// ADR exception boundary: this project is only a local-development umbrella for the sibling topology. Reusable
// domain-hosting behavior stays in the EventStore DomainService SDK; ChatBot does not ship a reusable Aspire package.

// The chatbot sidecar loads the LOCAL access-control config: this Aspire topology runs DAPR self-hosted with
// mTLS disabled, where deny-by-default policies cannot match (no verified SPIFFE caller identity). The deployed
// production posture is the deny-by-default accesscontrol.yaml (conformance reference), enforced under mTLS.
string accessControlConfigPath = ResolveDaprConfigPath(builder.AppHostDirectory, "accesscontrol.local.yaml");

HexalithEventStoreSecurityResources? security = builder.AddHexalithEventStoreSecurity();

IResourceBuilder<ProjectResource> eventStore = builder.AddProject<Projects.Hexalith_EventStore>(
    ChatBotAspireModule.EventStoreServiceName);
IResourceBuilder<ProjectResource> tenants = builder.AddProject<Projects.Hexalith_Tenants>(
    ChatBotAspireModule.TenantsAppId);
IResourceBuilder<ProjectResource> chatBot = builder.AddProject<Projects.Hexalith_ChatBot_Server>(
    ChatBotAspireModule.AppId);

HexalithChatBotResources resources = builder.AddHexalithChatBot(eventStore, tenants, chatBot, accessControlConfigPath);

// Live durable read path: project the governed-operation read model into the DAPR chatbot-statestore, and
// subscribe to the tenant-prefixed topic the EventStore publishes governed events on
// ({tenantId}.chatbot.events). M0 runs the single tenant-alpha, so the subscription topic is tenant-prefixed
// here without baking a tenant into source; M1's second tenant is additive.
_ = chatBot
    .WithEnvironment("ChatBot__UseDaprStateStores", "true")
    .WithEnvironment("ChatBot__UseDaprWorkflowRuntime", "true")
    .WithEnvironment("ChatBot__UsePeriodicEnforcementRuntime", "true")
    .WithEnvironment("ChatBot__ProjectionChangeNotifications__Enabled", "true")
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

// EventStore Admin operator console (Admin REST API + Admin Blazor UI), mirroring the canonical
// Hexalith.EventStore AppHost. The Admin.Server inspects the chatbot spine's events/streams/projections by
// reading the shared EventStore actor state store directly; the Admin.UI invokes it over DAPR service
// invocation. See ChatBotAspireModule.AddEventStoreAdmin for the sidecar/reference wiring.
IResourceBuilder<ProjectResource> eventStoreAdmin = builder.AddProject<Projects.Hexalith_EventStore_Admin_Server_Host>(
    ChatBotAspireModule.EventStoreAdminAppId);
IResourceBuilder<ProjectResource> eventStoreAdminUi = builder.AddProject<Projects.Hexalith_EventStore_Admin_UI>(
    ChatBotAspireModule.EventStoreAdminUiAppId);
builder.AddEventStoreAdmin(resources, eventStoreAdmin, eventStoreAdminUi);

// The Admin.UI surfaces a hyperlink to the Admin.Server Swagger page; the AppHost owns the resolved endpoint.
// This topology selects each project's "http" launch profile (DAPR app-ports are http — the Admin.Server is
// served on :8090), so eventstore-admin only exposes an "http" endpoint here. The standalone Hexalith.EventStore
// AppHost runs the "https" profiles, hence its GetEndpoint("https"); resolving "https" in this http-only topology
// throws "endpoint https is not defined" and fails the Admin.UI. Resolve against the endpoint that exists here.
EndpointReference adminServerHttp = eventStoreAdmin.GetEndpoint("http");
ReferenceExpression adminSwaggerUrl = ReferenceExpression.Create($"{adminServerHttp}/swagger/index.html");

if (security is not null)
{
    _ = eventStore.WithJwtBearerSecurity(security, "hexalith-eventstore");
    _ = tenants.WithJwtBearerSecurity(security, "hexalith-tenants");
    _ = chatBot.WithJwtBearerSecurity(security, "hexalith-chatbot");

    // Admin.Server validates the operator JWT the same way as the EventStore service (audience
    // hexalith-eventstore, OIDC discovery against the Keycloak realm).
    _ = eventStoreAdmin.WithJwtBearerSecurity(security, "hexalith-eventstore");

    // Admin.UI acquires its bearer token server-side via the Keycloak direct-access (password) grant on the
    // hexalith-eventstore client, logging in as the realm's global-admin operator. The realm's
    // hexalith-eventstore client carries the audience + global_admin protocol mappers so the issued token
    // authorizes against Admin.Server's claims policy.
    _ = eventStoreAdminUi
        .WithEventStoreClientCredentials(
            security,
            clientId: "hexalith-eventstore",
            username: "admin-user",
            password: "admin-pass")
        .WithEnvironment("EventStore__AdminServer__SwaggerUrl", adminSwaggerUrl);
}
else
{
    // Keycloak disabled: the Admin.UI falls back to a development HS256 token (its appsettings default a
    // GlobalAdmin dev identity) validated by the Admin.Server's symmetric dev signing key.
    _ = eventStoreAdminUi.WithEnvironment("EventStore__AdminServer__SwaggerUrl", adminSwaggerUrl);
}

builder.Build().Run();

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
