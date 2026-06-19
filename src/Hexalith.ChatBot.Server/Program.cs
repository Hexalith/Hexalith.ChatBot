using System.Text.Json;

using Hexalith.ChatBot.Server.Authentication;
using Hexalith.ChatBot.Server.Gateway;
using Hexalith.ChatBot.Server.Gateway.Correlation;
using Hexalith.ChatBot.Server.Lifecycle.Attachments;
using Hexalith.ChatBot.Server.Lifecycle.Workflows;
using Hexalith.ChatBot.Server.Operations;
using Hexalith.ChatBot.Server.Operations.PeriodicEnforcement;
using Hexalith.ChatBot.Server.Projections;
using Hexalith.EventStore.Client.Registration;
using Hexalith.EventStore.DomainService;

using Microsoft.AspNetCore.DataProtection;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

_ = builder.AddEventStoreDomainService(typeof(GovernedOperationAggregate).Assembly);
_ = builder.Services.AddChatBotCommandGateway();
_ = builder.AddEventStoreDomainTelemetry("chatbot");
_ = builder.Services
    .AddHealthChecks()
    .AddEventStoreDomainStateStoreHealthCheck(
        "chatbot",
        stateStoreName: ChatBotReadModelStoreNames.StateStoreName,
        tags: ["ready", "chatbot"]);
// DataProtection backs two cross-instance tokens: the query-cursor codec and — since Story 11.5 — the
// CommandGateway admission marker that lets the EventStore->/process callback skip re-admission. The marker is
// created on the gateway instance and validated on whichever replica handles the /process callback. A stable
// application name keeps the key derivation consistent across instances; a multi-replica (or restarted)
// deployment MUST additionally persist and share the key ring (e.g. a Dapr/Redis/blob key store) so Unprotect
// succeeds on the second instance — otherwise the marker is rejected and admission re-runs. Single-instance
// dev/test shares an in-process ring. Wiring the shared key store is a deployment / Story 11.6 composition concern.
_ = builder.Services.AddDataProtection().SetApplicationName("Hexalith.ChatBot");
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

app.Run();

public partial class Program
{
    internal static readonly JsonSerializerOptions QueryJsonOptions = new(JsonSerializerDefaults.Web);
}
