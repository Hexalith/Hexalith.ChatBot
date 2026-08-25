using System.Text.Json;

using Hexalith.ChatBot.Server.Authentication;
using Hexalith.ChatBot.Server.Gateway;
using Hexalith.ChatBot.Server.Gateway.Correlation;
using Hexalith.ChatBot.Server.Lifecycle.Attachments;
using Hexalith.ChatBot.Server.Lifecycle.Workflows;
using Hexalith.ChatBot.Server.Observability;
using Hexalith.ChatBot.Server.Operations;
using Hexalith.ChatBot.Server.Operations.PeriodicEnforcement;
using Hexalith.ChatBot.Server.Projections;
using Hexalith.EventStore.Client.Registration;
using Hexalith.EventStore.DomainService;

using Microsoft.AspNetCore.DataProtection;

using OpenTelemetry.Metrics;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

_ = builder.AddEventStoreDomainService(typeof(GovernedOperationAggregate).Assembly);

// Resolve the identity EventStore negotiates named projections against. The SDK derives an unconfigured AppId
// from DAPR_APP_ID, falling back to the .NET application name ("Hexalith.ChatBot.Server"), which is never the DAPR
// app id EventStore registers and invokes this service under - so the SDK answers 400 UnsupportedCapability on
// every operational-index refresh. Post-cutover that is not a lost optimisation: the projection checkpoint can then
// only advance through the named fenced completion, so it never advances and the poller re-delivers forever.
//
// Precedence matters: an unconditional override would silently kill both EventStore:DomainService binding and
// DAPR_APP_ID, so any per-environment prefix, canary or multi-instance deployment would reintroduce the very
// mismatch this exists to prevent. Explicit configuration wins, then the DAPR-supplied app id, and the pinned
// constant is only a last-resort fallback for hosts that supply neither.
_ = builder.Services
    .AddOptions<DomainProjectionIdentityOptions>()
    .PostConfigure(options =>
    {
        options.AppId = ChatBotDomainServiceIdentity.ResolveAppId(
            builder.Configuration[$"{ChatBotDomainServiceIdentity.ConfigurationSection}:AppId"],
            builder.Configuration["EventStore:DomainService:AppId"],
            Environment.GetEnvironmentVariable("DAPR_APP_ID"));
        options.ServiceVersion = ChatBotDomainServiceIdentity.ResolveServiceVersion(
            builder.Configuration[$"{ChatBotDomainServiceIdentity.ConfigurationSection}:ServiceVersion"],
            builder.Configuration["EventStore:DomainService:ServiceVersion"]);
    })
    .Validate(
        static options => ChatBotDomainServiceIdentity.IsUsableIdentityComponent(options.AppId)
            && ChatBotDomainServiceIdentity.IsUsableIdentityComponent(options.ServiceVersion),
        "The ChatBot projection identity requires an AppId and ServiceVersion that are safe stable identifiers; "
        + "EventStore compares them verbatim and refuses the whole named-projection capability on any mismatch.")
    .ValidateOnStart();
_ = builder.Services.AddChatBotCommandGateway();
_ = builder.AddEventStoreDomainTelemetry("chatbot");
_ = builder.Services.ConfigureOpenTelemetryMeterProvider(metrics => metrics.AddMeter(ChatBotMetrics.MeterName));
_ = builder.Services
    .AddHealthChecks()
    .AddEventStoreDomainStateStoreHealthCheck(
        "chatbot",
        stateStoreName: ChatBotReadModelStoreNames.StateStoreName,
        tags: ["ready", "chatbot"]);
ConfigureChatBotDataProtection(builder);
_ = builder.Services.AddEventStoreQueryCursorCodec("Hexalith.ChatBot.QueryCursor.v1");
_ = builder.Services.Configure<PeriodicEnforcementOptions>(builder.Configuration.GetSection("ChatBot:PeriodicEnforcement"));

bool jwtAuthentication = ChatBotJwtAuthentication.IsConfigured(builder.Configuration);
_ = builder.Services.AddChatBotJwtAuthentication(builder.Configuration);

if (string.Equals(builder.Configuration["ChatBot:UseDaprWorkflowRuntime"], "true", StringComparison.OrdinalIgnoreCase))
{
    _ = builder.Services.AddChatBotCorrectionPropagationWorkflow();
}

// Both spellings enable the hosted runtime. The top-level key is what the Aspire topology sets; the nested key is the
// one an operator naturally reaches for, since every other periodic-enforcement setting lives under that section and
// binds to the identically-named option. Honouring only the top-level key made the nested spelling a silent no-op:
// IOptions reported UsePeriodicEnforcementRuntime = true while the hosted service was never registered, so nothing ran
// and — because the health check lives inside that loop — nothing reported that nothing ran.
if (string.Equals(builder.Configuration["ChatBot:UsePeriodicEnforcementRuntime"], "true", StringComparison.OrdinalIgnoreCase) ||
    string.Equals(builder.Configuration["ChatBot:PeriodicEnforcement:UsePeriodicEnforcementRuntime"], "true", StringComparison.OrdinalIgnoreCase))
{
    _ = builder.Services.Configure<PeriodicEnforcementOptions>(
        options => options.UsePeriodicEnforcementRuntime = true);
    _ = builder.Services.AddChatBotPeriodicEnforcementHostedService();
}

if (string.Equals(builder.Configuration["ChatBot:UseDaprStateStores"], "true", StringComparison.OrdinalIgnoreCase))
{
    _ = builder.Services.AddChatBotDaprStateStores();
}

// Story 10.6b transport: ChatBot-owned SignalR hub. When enabled, server-verified AI response progress changes are
// broadcast to subscribed UI clients (which re-query the typed read state) over the colocated ChatBot hub. Off by
// default (no-op publisher, no hub mapped); the host (Aspire topology / deployment) sets
// ChatBot:ProjectionChangeNotifications:Enabled=true.
bool projectionChangeNotificationsEnabled = string.Equals(
    builder.Configuration["ChatBot:ProjectionChangeNotifications:Enabled"],
    "true",
    StringComparison.OrdinalIgnoreCase);
if (projectionChangeNotificationsEnabled)
{
    _ = builder.Services.AddChatBotProjectionChangeNotifications();
}

WebApplication app = builder.Build();

if (jwtAuthentication)
{
    _ = app.UseAuthentication();
}

_ = app.UseChatBotCorrelation();
_ = app.UseCloudEvents();
_ = app.UseEventStoreDomainService();
_ = app.MapChatBotProjectionSubscriptionCompatibilityEndpoints();
_ = app.MapChatBotCompatibilityEndpoints();

if (projectionChangeNotificationsEnabled)
{
    _ = app.MapHub<ChatBotProjectConversationHub>(ChatBotProjectConversationHub.HubPath);
}

app.Run();

static void ConfigureChatBotDataProtection(WebApplicationBuilder builder)
{
    IDataProtectionBuilder dataProtection = builder.Services.AddDataProtection().SetApplicationName("Hexalith.ChatBot");
    string? keyRingPath = builder.Configuration["ChatBot:DataProtection:KeyRingPath"];
    if (!string.IsNullOrWhiteSpace(keyRingPath))
    {
        _ = dataProtection.PersistKeysToFileSystem(new DirectoryInfo(keyRingPath));
        return;
    }

    bool singleReplicaOnly = string.Equals(
        builder.Configuration["ChatBot:DataProtection:SingleReplicaOnly"],
        "true",
        StringComparison.OrdinalIgnoreCase);
    if (builder.Environment.IsProduction() && !singleReplicaOnly)
    {
        throw new InvalidOperationException(
            "Production ChatBot deployments must configure ChatBot:DataProtection:KeyRingPath or explicitly set "
            + "ChatBot:DataProtection:SingleReplicaOnly=true. The admission marker and query cursor key ring "
            + "cannot be ephemeral for multi-replica or restart-surviving topology claims.");
    }
}

public partial class Program
{
    internal static readonly JsonSerializerOptions QueryJsonOptions = new(JsonSerializerDefaults.Web);
}
