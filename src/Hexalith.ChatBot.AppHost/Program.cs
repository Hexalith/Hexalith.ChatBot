using System.Globalization;
using System.Runtime.Versioning;
using System.Security.AccessControl;
using System.Security.Principal;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Hexalith.ChatBot.AppHost.Aspire;
using Hexalith.EventStore.Aspire;
using Microsoft.Extensions.Configuration;

IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);

// ADR exception boundary: this project is only a local-development umbrella for the sibling topology. Reusable
// domain-hosting behavior stays in the EventStore DomainService SDK; ChatBot does not ship a reusable Aspire package.

// The chatbot sidecar loads the LOCAL access-control config: this Aspire topology runs DAPR self-hosted with
// mTLS disabled, where deny-by-default policies cannot match (no verified SPIFFE caller identity). The deployed
// production posture is the deny-by-default accesscontrol.yaml (conformance reference), enforced under mTLS.
string accessControlConfigPath = ResolveDaprConfigPath(builder.AppHostDirectory, "accesscontrol.local.yaml");

string realmImportPath = PrepareKeycloakRealmImport(builder.AppHostDirectory, builder.Configuration);
HexalithEventStoreSecurityResources? security = builder.AddHexalithEventStoreSecurity(
    new HexalithEventStoreSecurityOptions { RealmImportPath = realmImportPath });

IResourceBuilder<ProjectResource> eventStore = builder.AddProject<Projects.Hexalith_EventStore>(
    ChatBotAspireModule.EventStoreServiceName);
IResourceBuilder<ProjectResource> tenants = builder.AddProject<Projects.Hexalith_Tenants>(
    ChatBotAspireModule.TenantsAppId);
IResourceBuilder<ProjectResource> chatBot = builder.AddProject<Projects.Hexalith_ChatBot_Server>(
    ChatBotAspireModule.AppId);

HexalithChatBotResources resources = builder.AddHexalithChatBot(eventStore, tenants, chatBot, accessControlConfigPath);

// Live durable read path: project the governed-operation read model into the DAPR chatbot-statestore, and
// subscribe to the tenant-prefixed topic the EventStore publishes governed events on
// ({tenantId}.chatbot.events). The primary M0 projection remains tenant-alpha. The local realm also carries an
// actor-beta identity so the required Tier-3 release gate can establish real cross-tenant probe coverage; adding a
// second projection subscription remains an M1 concern.
_ = chatBot
    .WithExternalHttpEndpoints()
    .WithEnvironment("ChatBot__UseDaprStateStores", "true")
    .WithEnvironment("ChatBot__UseDaprWorkflowRuntime", "true")
    .WithEnvironment("ChatBot__UsePeriodicEnforcementRuntime", "true")
    .WithEnvironment("ChatBot__PeriodicEnforcement__RunM2AuditRecoverySweeps", "true")

    // Enables /health/chatbot/periodic-enforcement/m2, the fail-closed M2 stop-ship endpoint the release gate polls.
    // Without a token the endpoint is not mapped at all, so the breach state is never anonymously reachable. This is
    // a local topology value; a real deployment supplies it as a secret.
    .WithEnvironment("ChatBot__PeriodicEnforcement__M2ReleaseGateToken", "local-topology-m2-release-gate-token")
    .WithEnvironment("ChatBot__ProjectionChangeNotifications__Enabled", "true")
    .WithEnvironment("ChatBot__Workflow__StateStoreName", ChatBotAspireModule.WorkflowStateStoreComponentName)
    .WithEnvironment("ChatBot__Projection__PubSubName", ChatBotAspireModule.PubSubComponentName)
    .WithEnvironment("ChatBot__Projection__Topic", $"tenant-alpha.{ChatBotAspireModule.PubSubTopicName}")
    .WithEnvironment("ChatBot__Projection__DeadLetterTopic", ChatBotAspireModule.GetTenantDeadLetterTopic("tenant-alpha"));

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
builder.AddEventStoreAdmin(resources, eventStoreAdmin, eventStoreAdminUi, accessControlConfigPath);

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

static string PrepareKeycloakRealmImport(string appHostDirectory, IConfiguration configuration)
{
    const string expiryPlaceholder = "__HEXALITH_CHATBOT_SERVICE_GRANT_EXPIRES_AT__";
    const string recoveryClientSecretPlaceholder = "__HEXALITH_CHATBOT_RECOVERY_CLIENT_SECRET__";
    const int expectedServiceGrantCount = 7;
    const int defaultLifetimeDays = 90;
    const int defaultMinimumRemainingDays = 30;

    string sourceDirectory = Path.Combine(appHostDirectory, "KeycloakRealms");
    string sourcePath = Path.Combine(sourceDirectory, "hexalith-realm.json");
    if (!File.Exists(sourcePath))
    {
        sourceDirectory = Path.Combine(Directory.GetCurrentDirectory(), "KeycloakRealms");
        sourcePath = Path.Combine(sourceDirectory, "hexalith-realm.json");
    }

    if (!File.Exists(sourcePath))
    {
        throw new FileNotFoundException(
            "Keycloak realm template not found. Ensure KeycloakRealms/hexalith-realm.json exists.",
            sourcePath);
    }

    int minimumRemainingDays = configuration.GetValue<int?>("ChatBotServiceGrants:MinimumRemainingDays")
        ?? defaultMinimumRemainingDays;
    if (minimumRemainingDays < 1)
    {
        throw new InvalidOperationException("ChatBotServiceGrants:MinimumRemainingDays must be at least 1.");
    }

    DateTimeOffset now = DateTimeOffset.UtcNow;
    string? configuredExpiry = configuration["ChatBotServiceGrants:ExpiresAtUtc"];
    DateTimeOffset expiresAt;
    if (string.IsNullOrWhiteSpace(configuredExpiry))
    {
        int lifetimeDays = configuration.GetValue<int?>("ChatBotServiceGrants:LifetimeDays") ?? defaultLifetimeDays;
        if (lifetimeDays < minimumRemainingDays)
        {
            throw new InvalidOperationException(
                "ChatBotServiceGrants:LifetimeDays must be greater than or equal to the pre-expiry minimum.");
        }

        expiresAt = now.AddDays(lifetimeDays);
    }
    else if (!DateTimeOffset.TryParse(
        configuredExpiry,
        CultureInfo.InvariantCulture,
        DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
        out expiresAt))
    {
        throw new InvalidOperationException("ChatBotServiceGrants:ExpiresAtUtc must be a valid UTC timestamp.");
    }

    if (expiresAt < now.AddDays(minimumRemainingDays))
    {
        throw new InvalidOperationException(
            $"Service-client grants expire too soon. Provision an expiry at least {minimumRemainingDays} days in the future.");
    }

    string realm = File.ReadAllText(sourcePath);
    int placeholderCount = realm.Split(expiryPlaceholder, StringSplitOptions.None).Length - 1;
    if (placeholderCount != expectedServiceGrantCount)
    {
        throw new InvalidOperationException(
            $"Expected {expectedServiceGrantCount} service-grant expiry placeholders but found {placeholderCount}.");
    }

    realm = realm.Replace(expiryPlaceholder, expiresAt.UtcDateTime.ToString("O", CultureInfo.InvariantCulture), StringComparison.Ordinal);
    int recoverySecretPlaceholderCount = realm.Split(recoveryClientSecretPlaceholder, StringSplitOptions.None).Length - 1;
    if (recoverySecretPlaceholderCount != 1)
    {
        throw new InvalidOperationException(
            $"Expected one recovery client-secret placeholder but found {recoverySecretPlaceholderCount}.");
    }

    // Deliberately NOT the controller secret. The ADR requires the controller secret to stay out of the realm, and
    // reusing it would make one compromised fault-injection header also mint CaptureMailboxMessageIntake
    // service-client tokens. The secret is required, fail-closed configuration with no generated fallback: an
    // auto-generated value never round-trips back to whatever process needs to authenticate as the mailbox client,
    // so silently minting one only hid a missing secret until the mailbox admission probe failed for an unrelated
    // reason. `ChatBot:LiveRecoveryValidation:MailboxClientSecret` is preferred (aligned with the Server section
    // binding); `LiveRecoveryValidation:MailboxClientSecret` remains supported as the legacy AppHost-only key.
    string? configuredClientSecret = configuration["ChatBot:LiveRecoveryValidation:MailboxClientSecret"];
    if (string.IsNullOrWhiteSpace(configuredClientSecret))
    {
        configuredClientSecret = configuration["LiveRecoveryValidation:MailboxClientSecret"];
    }

    if (string.IsNullOrWhiteSpace(configuredClientSecret))
    {
        throw new InvalidOperationException(
            "A recovery mailbox client secret must be supplied via "
            + "ChatBot:LiveRecoveryValidation:MailboxClientSecret (preferred) or "
            + "LiveRecoveryValidation:MailboxClientSecret (legacy AppHost key). No secret was configured.");
    }

    string recoveryClientSecret = configuredClientSecret;
    if (recoveryClientSecret.Length < 32)
    {
        throw new InvalidOperationException(
            "The recovery mailbox client secret configured via ChatBot:LiveRecoveryValidation:MailboxClientSecret "
            + "or LiveRecoveryValidation:MailboxClientSecret must be at least 32 characters long.");
    }

    // The placeholder sits inside a JSON string literal, so the value is substituted verbatim into the document. A
    // secret carrying a quote or backslash would corrupt the realm; one shaped like `x","publicClient":true,"a":"`
    // would inject attributes into the recovery client's definition. Restrict it to characters that cannot escape the
    // literal rather than escaping them, so a mis-supplied secret fails loudly instead of silently reshaping the realm.
    if (!recoveryClientSecret.All(static character => char.IsAsciiLetterOrDigit(character) || character is '-' or '_'))
    {
        throw new InvalidOperationException(
            "ChatBot:LiveRecoveryValidation:MailboxClientSecret (or the legacy LiveRecoveryValidation:MailboxClientSecret) "
            + "must contain only ASCII letters, digits, '-', or '_'.");
    }

    realm = realm.Replace(recoveryClientSecretPlaceholder, recoveryClientSecret, StringComparison.Ordinal);

    // Created 0700 by CreateTempSubdirectory rather than 0755-then-chmod, and with a random name rather than the
    // process id. The previous shape lost the race it existed to win: CreateDirectory and WriteAllText both completed
    // at the default umask before SetUnixFileMode narrowed them, leaving the literal client secret world-readable on
    // exactly the shared build host the comment below worries about. The predictable {temp}/hexalith-chatbot-keycloak/
    // {pid} path also let a pre-created symlink redirect the write, and a recycled pid whose 0700 directory survived
    // under another owner failed topology startup outright.
    string generatedDirectory = Directory.CreateTempSubdirectory("hexalith-chatbot-keycloak-").FullName;
    string generatedRealmPath = Path.Combine(generatedDirectory, "hexalith-realm.json");

    // This rendered realm is the only artifact that carries a literal client secret, so it must never exist at
    // world-readable permissions even briefly, and it must not outlive the process that needed it. CreateNew makes the
    // write fail rather than follow a pre-existing path, and UnixCreateMode applies the mode at creation.
    FileStreamOptions realmFileOptions = new()
    {
        Mode = FileMode.CreateNew,
        Access = FileAccess.Write,
        Share = FileShare.None,
    };
    if (!OperatingSystem.IsWindows())
    {
        realmFileOptions.UnixCreateMode = UnixFileMode.UserRead | UnixFileMode.UserWrite;
    }

    using (StreamWriter realmWriter = new(new FileStream(generatedRealmPath, realmFileOptions)))
    {
        realmWriter.Write(realm);
    }

    if (OperatingSystem.IsWindows())
    {
        // UnixCreateMode has no Windows equivalent, so the file above was created inheriting the temp directory's
        // ACL, which is not necessarily owner-only on a shared build host. Apply an explicit owner-only ACL so the
        // client secret embedded in this rendered realm cannot be read by another account.
        ApplyWindowsRealmFileAcl(generatedRealmPath);
    }

    AppDomain.CurrentDomain.ProcessExit += (_, _) =>
    {
        try
        {
            Directory.Delete(generatedDirectory, recursive: true);
        }
        catch (IOException)
        {
            // Best effort: a locked or already-removed directory must never fail the topology teardown.
        }
        catch (UnauthorizedAccessException)
        {
            // Same rationale as above.
        }
    };

    return generatedDirectory;
}

// Windows has no UnixFileMode equivalent, so a file created on Windows inherits its parent directory's ACL instead
// of an owner-only mode. Since the rendered realm carries a literal client secret, this ACL must be applied
// explicitly rather than relying on the temp directory's default permissions, which are not guaranteed owner-only
// on every Windows host (e.g. a shared build agent).
[SupportedOSPlatform("windows")]
static void ApplyWindowsRealmFileAcl(string path)
{
    SecurityIdentifier owner = WindowsIdentity.GetCurrent().User
        ?? throw new InvalidOperationException("The current Windows identity has no security identifier.");
    FileSecurity security = new();
    security.SetOwner(owner);
    security.SetAccessRuleProtection(isProtected: true, preserveInheritance: false);
    security.AddAccessRule(new FileSystemAccessRule(owner, FileSystemRights.FullControl, AccessControlType.Allow));
    new FileInfo(path).SetAccessControl(security);
}
