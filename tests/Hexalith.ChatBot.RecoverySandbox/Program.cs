using System.Net.Http.Headers;

using Hexalith.ChatBot.Client;
using Hexalith.ChatBot.Client.Generated;
using Hexalith.ChatBot.RecoverySandbox;
using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Workers.Mailbox;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

bool enabled = builder.Configuration.GetValue<bool>("Recovery:Enabled");
string tenantRef = builder.Configuration["Recovery:TenantRef"] ?? string.Empty;
string storageTenantRef = builder.Configuration["Recovery:StorageTenantRef"] ?? string.Empty;
string controllerSecret = builder.Configuration["Recovery:ControllerSecret"] ?? string.Empty;
string providerMessageId = builder.Configuration["Recovery:ProviderMessageId"] ?? string.Empty;
string chatBotBaseAddress = builder.Configuration["Recovery:ChatBotBaseAddress"] ?? string.Empty;
const string AllowlistedMailboxId = "recovery-mailbox-001";
if (!enabled ||
    !ReplayTenantPolicy.IsTestTenant(tenantRef) ||
    !string.Equals(ReplayTenantPolicy.StorageTenantFor(tenantRef), storageTenantRef, StringComparison.Ordinal) ||
    string.IsNullOrWhiteSpace(controllerSecret) ||
    string.IsNullOrWhiteSpace(providerMessageId) ||
    !Uri.TryCreate(chatBotBaseAddress, UriKind.Absolute, out Uri? chatBotUri))
{
    throw new InvalidOperationException(
        "The recovery sandbox requires explicit enablement, a replay-test tenant with a matching derived storage tenant, and complete Tier-3 configuration.");
}

builder.Services.AddSingleton<RecoverySubscriptionSimulatorState>();
builder.Services.AddSingleton<RecoveryScopedOutageState>();
builder.Services.AddSingleton<RecoveryAiAssistanceProvider>();
builder.Services.AddSingleton<RecoveryEventStoreGatewayClient>();
builder.Services.AddSingleton<RecoveryAuditWriter>();
builder.Services.AddSingleton<RecoveryAttachmentContentSource>();
builder.Services.AddSingleton<RecoveryFolderStore>();
builder.Services.AddSingleton<RecoveryTenantAiPolicySnapshotProvider>();
builder.Services.AddSingleton<Hexalith.ChatBot.Server.Projections.InMemoryProjectConversationProjectionStore>();
builder.Services.AddSingleton<RecoveryScopeObservationMonitor>();
builder.Services.AddHostedService(static services => services.GetRequiredService<RecoveryScopeObservationMonitor>());
builder.Services.AddSingleton<RecoveryDependencyExercise>();
builder.Services.AddSingleton<ControlledGraphMailboxMessageSource>();
builder.Services.AddSingleton(new RecoveryMailboxConfigurationProvider(AllowlistedMailboxId));
builder.Services.AddTransient<RecoveryBearerForwardingHandler>();

// Pooled rather than one HttpClient per /process request: a repeated sweep allocated (and disposed) a client per
// call, which exhausts sockets through TIME_WAIT under exactly the repeated-run pattern this lane uses. Bearer auth
// is applied per request via RecoveryBearerForwardingHandler — never via DefaultRequestHeaders on the pooled client.
builder.Services.AddHttpClient("chatbot-forward", client =>
{
    client.BaseAddress = chatBotUri;
    client.Timeout = TimeSpan.FromSeconds(30);
}).AddHttpMessageHandler<RecoveryBearerForwardingHandler>();

WebApplication app = builder.Build();

app.MapGet("/health", () => Results.Ok(new { status = "available" }));

app.MapPost(
    "/recovery/{requestedTenant}/m365-subscription-failure/fault",
    (string requestedTenant, HttpRequest request, RecoverySubscriptionSimulatorState state) =>
    {
        if (!RecoverySandboxAuthorization.Authorized(
                requestedTenant,
                tenantRef,
                controllerSecret,
                request.Headers["X-Recovery-Controller-Secret"].ToString()))
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
        if (!RecoverySandboxAuthorization.Authorized(
                requestedTenant,
                tenantRef,
                controllerSecret,
                request.Headers["X-Recovery-Controller-Secret"].ToString()))
        {
            return Results.NotFound();
        }

        return Results.Ok(state.Restore(DateTimeOffset.UtcNow));
    });

app.MapPost(
    "/recovery/{requestedTenant}/m365-subscription-failure/process",
    async (
        string requestedTenant,
        HttpRequest request,
        RecoverySubscriptionSimulatorState state,
        ControlledGraphMailboxMessageSource source,
        RecoveryMailboxConfigurationProvider mailboxConfiguration,
        IHttpClientFactory httpClientFactory,
        CancellationToken cancellationToken) =>
    {
        if (!RecoverySandboxAuthorization.Authorized(
                requestedTenant,
                tenantRef,
                controllerSecret,
                request.Headers["X-Recovery-Controller-Secret"].ToString()) ||
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

        string notificationPhase = request.Headers[RecoveryNotificationIdentity.HeaderName].ToString();
        string notificationIdentity;
        try
        {
            notificationIdentity = RecoveryNotificationIdentity.Compose(
                providerMessageId,
                scenarioLane,
                notificationPhase);
        }
        catch (InvalidOperationException)
        {
            return Results.NotFound();
        }

        HttpClient http = httpClientFactory.CreateClient("chatbot-forward");
        using (RecoveryBearerForwardingHandler.Use(bearer.Parameter))
        {
            ChatBotClient client = new(new Client(http));
            GraphMailboxIntakeWorker worker = new(
                storageTenantRef,
                mailboxConfiguration,
                source,
                client);
            MailboxIntakeWorkerResult result = await worker.ProcessAsync(
                new GraphMailboxNotification(
                    AllowlistedMailboxId,
                    notificationIdentity,
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
        }
    });

app.MapGet(
    "/recovery/{requestedTenant}/m365-subscription-failure/status",
    (string requestedTenant, HttpRequest request, RecoverySubscriptionSimulatorState state) =>
        RecoverySandboxAuthorization.Authorized(
            requestedTenant,
            tenantRef,
            controllerSecret,
            request.Headers["X-Recovery-Controller-Secret"].ToString())
            ? Results.Ok(state.Snapshot())
            : Results.NotFound());

app.MapPost(
    "/recovery/{requestedTenant}/scoped-outage/{dependency}/fault",
    (string requestedTenant, string dependency, HttpRequest request, RecoveryScopedOutageState state) =>
    {
        if (!RecoverySandboxAuthorization.Authorized(
                requestedTenant,
                tenantRef,
                controllerSecret,
                request.Headers["X-Recovery-Controller-Secret"].ToString()) ||
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
        if (!RecoverySandboxAuthorization.Authorized(
                requestedTenant,
                tenantRef,
                controllerSecret,
                request.Headers["X-Recovery-Controller-Secret"].ToString()) ||
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
        if (!RecoverySandboxAuthorization.Authorized(
                requestedTenant,
                tenantRef,
                controllerSecret,
                request.Headers["X-Recovery-Controller-Secret"].ToString()) ||
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
            recoverable = result.FaultObserved || !result.SilentDataLossDetected,
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
        if (!RecoverySandboxAuthorization.Authorized(
                requestedTenant,
                tenantRef,
                controllerSecret,
                request.Headers["X-Recovery-Controller-Secret"].ToString()) ||
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
        string faultSignalCode,
        HttpRequest request,
        RecoveryScopeObservationMonitor monitor,
        CancellationToken cancellationToken) =>
    {
        if (!RecoverySandboxAuthorization.Authorized(
                requestedTenant,
                tenantRef,
                controllerSecret,
                request.Headers["X-Recovery-Controller-Secret"].ToString()) ||
            dependency is not ("graph" or "identity") ||
            correlationId.Length != 26 ||
            !RecoveryScopeObservationMonitor.IsKnownFaultSignal(faultSignalCode))
        {
            return Results.NotFound();
        }

        // The caller supplies the real signal it independently observed (the Graph simulator's own reason code,
        // or the genuine failed token-acquisition outcome) — the monitor never derives scope from `dependency`.
        RecoveryScopeObservation observation = await monitor.RecordAsync(
            new RecoveryDependencyFailure(dependency, correlationId, DateTimeOffset.UtcNow, faultSignalCode),
            cancellationToken).ConfigureAwait(false);
        return Results.Ok(observation);
    });

app.Run();
