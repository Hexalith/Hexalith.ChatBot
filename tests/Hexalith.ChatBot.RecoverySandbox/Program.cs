using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;

using Hexalith.ChatBot.Client;
using Hexalith.ChatBot.Client.Generated;
using Hexalith.ChatBot.RecoverySandbox;
using Hexalith.ChatBot.Workers.Mailbox;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

bool enabled = builder.Configuration.GetValue<bool>("Recovery:Enabled");
string tenantRef = builder.Configuration["Recovery:TenantRef"] ?? string.Empty;
string storageTenantRef = builder.Configuration["Recovery:StorageTenantRef"] ?? string.Empty;
string controllerSecret = builder.Configuration["Recovery:ControllerSecret"] ?? string.Empty;
string providerMessageId = builder.Configuration["Recovery:ProviderMessageId"] ?? string.Empty;
string chatBotBaseAddress = builder.Configuration["Recovery:ChatBotBaseAddress"] ?? string.Empty;
if (!enabled || string.IsNullOrWhiteSpace(tenantRef) || string.IsNullOrWhiteSpace(storageTenantRef) ||
    string.IsNullOrWhiteSpace(controllerSecret) || string.IsNullOrWhiteSpace(providerMessageId) ||
    !Uri.TryCreate(chatBotBaseAddress, UriKind.Absolute, out Uri? chatBotUri))
{
    throw new InvalidOperationException("The recovery sandbox requires explicit enablement and complete Tier-3 configuration.");
}

builder.Services.AddSingleton<RecoverySubscriptionSimulatorState>();
builder.Services.AddSingleton<RecoveryScopedOutageState>();
builder.Services.AddSingleton<RecoveryAiAssistanceProvider>();
builder.Services.AddSingleton<RecoveryEventStoreGatewayClient>();
builder.Services.AddSingleton<RecoveryAuditWriter>();
builder.Services.AddSingleton<RecoveryAttachmentContentSource>();
builder.Services.AddSingleton<RecoveryScopeObservationMonitor>();
builder.Services.AddHostedService(static services => services.GetRequiredService<RecoveryScopeObservationMonitor>());
builder.Services.AddSingleton<RecoveryDependencyExercise>();
builder.Services.AddSingleton<ControlledGraphMailboxMessageSource>();

// Pooled rather than one HttpClient per /process request: a repeated sweep allocated (and disposed) a client per
// call, which exhausts sockets through TIME_WAIT under exactly the repeated-run pattern this lane uses.
builder.Services.AddHttpClient("chatbot-forward", client =>
{
    client.BaseAddress = chatBotUri;
    client.Timeout = TimeSpan.FromSeconds(30);
});

WebApplication app = builder.Build();

app.MapGet("/health", () => Results.Ok(new { status = "available" }));

app.MapPost(
    "/recovery/{requestedTenant}/m365-subscription-failure/fault",
    (string requestedTenant, HttpRequest request, RecoverySubscriptionSimulatorState state) =>
    {
        if (!Authorized(request, requestedTenant, tenantRef, controllerSecret))
        {
            return Results.NotFound();
        }

        state.Fault(DateTimeOffset.UtcNow);
        return Results.Ok(state.Snapshot());
    });

app.MapPost(
    "/recovery/{requestedTenant}/m365-subscription-failure/restore",
    (string requestedTenant, HttpRequest request, RecoverySubscriptionSimulatorState state) =>
    {
        if (!Authorized(request, requestedTenant, tenantRef, controllerSecret))
        {
            return Results.NotFound();
        }

        state.Restore(DateTimeOffset.UtcNow);
        return Results.Ok(state.Snapshot());
    });

app.MapPost(
    "/recovery/{requestedTenant}/m365-subscription-failure/process",
    async (
        string requestedTenant,
        HttpRequest request,
        RecoverySubscriptionSimulatorState state,
        ControlledGraphMailboxMessageSource source,
        IHttpClientFactory httpClientFactory,
        CancellationToken cancellationToken) =>
    {
        if (!Authorized(request, requestedTenant, tenantRef, controllerSecret) ||
            request.Headers.Authorization.ToString() is not string authorization ||
            !AuthenticationHeaderValue.TryParse(authorization, out AuthenticationHeaderValue? bearer) ||
            !string.Equals(bearer.Scheme, "Bearer", StringComparison.OrdinalIgnoreCase) ||
            string.IsNullOrWhiteSpace(bearer.Parameter))
        {
            return Results.NotFound();
        }

        string scenarioLane = request.Headers["X-Recovery-Scenario-Lane"].ToString();
        if (scenarioLane is not ("continuity" or "graph"))
        {
            return Results.NotFound();
        }

        HttpClient http = httpClientFactory.CreateClient("chatbot-forward");
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearer.Parameter);
        ChatBotClient client = new(new Client(http));
        GraphMailboxIntakeWorker worker = new(
            storageTenantRef,
            new RecoveryMailboxConfigurationProvider(),
            source,
            client);
        MailboxIntakeWorkerResult result = await worker.ProcessAsync(
            new GraphMailboxNotification(
                "recovery-mailbox-001",
                $"{providerMessageId}-{scenarioLane}",
                OpaqueProviderState: null),
            cancellationToken: cancellationToken).ConfigureAwait(false);
        bool submitted = result.Kind == MailboxIntakeWorkerResultKind.Submitted;
        state.RecordProcessing(submitted);
        return Results.Ok(new
        {
            kind = result.Kind.ToString().ToLowerInvariant(),
            result.ReasonCode,
            result.IntakeId,
            submitted,
            observedAtUtc = DateTimeOffset.UtcNow,
        });
    });

app.MapGet(
    "/recovery/{requestedTenant}/m365-subscription-failure/status",
    (string requestedTenant, HttpRequest request, RecoverySubscriptionSimulatorState state) =>
        Authorized(request, requestedTenant, tenantRef, controllerSecret)
            ? Results.Ok(state.Snapshot())
            : Results.NotFound());

app.MapPost(
    "/recovery/{requestedTenant}/scoped-outage/{dependency}/fault",
    (string requestedTenant, string dependency, HttpRequest request, RecoveryScopedOutageState state) =>
    {
        if (!Authorized(request, requestedTenant, tenantRef, controllerSecret) ||
            !RecoveryScopedOutageState.Contains(dependency))
        {
            return Results.NotFound();
        }

        return Results.Ok(state.Fault(dependency, DateTimeOffset.UtcNow));
    });

app.MapPost(
    "/recovery/{requestedTenant}/scoped-outage/{dependency}/restore",
    (string requestedTenant, string dependency, HttpRequest request, RecoveryScopedOutageState state) =>
    {
        if (!Authorized(request, requestedTenant, tenantRef, controllerSecret) ||
            !RecoveryScopedOutageState.Contains(dependency))
        {
            return Results.NotFound();
        }

        return Results.Ok(state.Restore(dependency, DateTimeOffset.UtcNow));
    });

app.MapPost(
    "/recovery/{requestedTenant}/scoped-outage/{dependency}/process/{correlationId}",
    async (
        string requestedTenant,
        string dependency,
        string correlationId,
        HttpRequest request,
        RecoveryScopedOutageState state,
        RecoveryDependencyExercise exercise,
        CancellationToken cancellationToken) =>
    {
        if (!Authorized(request, requestedTenant, tenantRef, controllerSecret) ||
            !RecoveryScopedOutageState.Contains(dependency) ||
            correlationId.Length != 26)
        {
            return Results.NotFound();
        }

        (RecoveryDependencyExerciseResult result, RecoveryScopeObservation? scope) = await exercise
            .ProcessAsync(dependency, requestedTenant, correlationId, cancellationToken)
            .ConfigureAwait(false);
        return Results.Ok(new
        {
            faulted = result.FaultObserved,
            observedAtUtc = result.ObservedAtUtc,
            scopeRecordedAtUtc = scope?.ScopeRecordedAtUtc,
            observedScope = scope?.ObservedScope,
            recoverable = result.FaultObserved || result.EffectCount == 1,
            result.CrossTenantLeakageDetected,
            result.UnauthorizedMutationDetected,
            result.SilentDataLossDetected,
            result.DuplicateSideEffectDetected,
            result.EffectCount,
            outcome = result.FaultObserved ? "recoverable-failure" : "completed",
        });
    });

app.MapGet(
    "/recovery/{requestedTenant}/scoped-outage/{dependency}/status",
    (string requestedTenant, string dependency, HttpRequest request, RecoveryScopedOutageState state) =>
    {
        if (!Authorized(request, requestedTenant, tenantRef, controllerSecret) ||
            !RecoveryScopedOutageState.Contains(dependency))
        {
            return Results.NotFound();
        }

        return Results.Ok(state.Snapshot(dependency));
    });

app.MapPost(
    "/recovery/{requestedTenant}/scope-observation/{dependency}/{correlationId}",
    async (
        string requestedTenant,
        string dependency,
        string correlationId,
        HttpRequest request,
        RecoveryScopeObservationMonitor monitor,
        CancellationToken cancellationToken) =>
    {
        if (!Authorized(request, requestedTenant, tenantRef, controllerSecret) ||
            dependency is not ("graph" or "identity") ||
            correlationId.Length != 26)
        {
            return Results.NotFound();
        }

        RecoveryScopeObservation observation = await monitor.RecordAsync(
            new RecoveryDependencyFailure(dependency, correlationId, DateTimeOffset.UtcNow),
            cancellationToken).ConfigureAwait(false);
        return Results.Ok(observation);
    });

app.Run();

static bool Authorized(HttpRequest request, string requestedTenant, string configuredTenant, string configuredSecret)
{
    if (!string.Equals(requestedTenant, configuredTenant, StringComparison.Ordinal) ||
        request.Headers["X-Recovery-Controller-Secret"].ToString() is not string presented ||
        string.IsNullOrWhiteSpace(presented))
    {
        return false;
    }

    byte[] configuredHash = SHA256.HashData(Encoding.UTF8.GetBytes(configuredSecret));
    byte[] presentedHash = SHA256.HashData(Encoding.UTF8.GetBytes(presented));
    return CryptographicOperations.FixedTimeEquals(configuredHash, presentedHash);
}
