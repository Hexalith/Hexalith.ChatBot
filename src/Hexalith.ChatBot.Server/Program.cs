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

if (string.Equals(builder.Configuration["ChatBot:UsePeriodicEnforcementRuntime"], "true", StringComparison.OrdinalIgnoreCase))
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
