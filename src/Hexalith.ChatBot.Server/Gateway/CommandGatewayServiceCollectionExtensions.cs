using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Gateway.Idempotency;
using Hexalith.ChatBot.Server.Gateway.Redaction;
using Hexalith.ChatBot.Server.Gateway.Status;
using Hexalith.ChatBot.Server.Gateway.Stages;
using Hexalith.ChatBot.Server.Lifecycle.StateModel;
using Hexalith.ChatBot.Server.Operations;
using Hexalith.ChatBot.Server.Projections;
using Hexalith.EventStore.Client.Registration;

using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Hexalith.ChatBot.Server.Gateway;

internal static class CommandGatewayServiceCollectionExtensions
{
    /// <summary>The DAPR app id of the EventStore command gateway the dispatcher invokes via service invocation.</summary>
    private const string EventStoreDaprAppId = "eventstore";

    // The DAPR sidecar/proxy endpoints bind the IPv4 loopback (127.0.0.1). On dual-stack hosts (e.g. WSL2)
    // "localhost" resolves to ::1 first and the connection is refused, so every sidecar endpoint the chatbot
    // dials must use the IPv4 literal.
    private static string Ipv4Loopback(string endpoint)
        => endpoint.Replace("localhost", "127.0.0.1", StringComparison.OrdinalIgnoreCase);

    private static Dapr.Client.DaprClient BuildDaprClient()
    {
        Dapr.Client.DaprClientBuilder builder = new();
        string? grpcEndpoint = Environment.GetEnvironmentVariable("DAPR_GRPC_ENDPOINT");
        string resolved = Ipv4Loopback(string.IsNullOrWhiteSpace(grpcEndpoint)
            ? $"http://127.0.0.1:{Environment.GetEnvironmentVariable("DAPR_GRPC_PORT") ?? "50001"}"
            : grpcEndpoint);
        _ = builder.UseGrpcEndpoint(resolved);

        string? httpEndpoint = Environment.GetEnvironmentVariable("DAPR_HTTP_ENDPOINT");
        _ = builder.UseHttpEndpoint(Ipv4Loopback(string.IsNullOrWhiteSpace(httpEndpoint)
            ? $"http://127.0.0.1:{Environment.GetEnvironmentVariable("DAPR_HTTP_PORT") ?? "3500"}"
            : httpEndpoint));

        return builder.Build();
    }

    public static IServiceCollection AddChatBotCommandGateway(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // The real dispatcher routes admitted commands into EventStore through the public gateway client. The
        // submission must authenticate WITHOUT a forged user JWT, so it goes through the chatbot's OWN DAPR
        // sidecar via service invocation tagged `dapr-app-id: eventstore` (the DaprAppIdHandler) — the receiving
        // EventStore sidecar then injects the verified `dapr-caller-app-id: chatbot` header that EventStore's
        // DaprInternal scheme validates against its allow-list (the AppHost grants `chatbot`). BaseAddress is the
        // caller's own sidecar (DAPR_HTTP_ENDPOINT / DAPR_HTTP_PORT, default 3500); the literal localhost host is
        // deliberately opaque to AddServiceDiscovery so it stays a direct sidecar call. In-process tests replace
        // IEventStoreGatewayClient with an accepting fake, so this wiring is exercised only in the live topology.
        string daprHttpEndpoint = Ipv4Loopback(Environment.GetEnvironmentVariable("DAPR_HTTP_ENDPOINT")
            ?? $"http://127.0.0.1:{Environment.GetEnvironmentVariable("DAPR_HTTP_PORT") ?? "3500"}");
        string? daprApiToken = Environment.GetEnvironmentVariable("DAPR_API_TOKEN");
        _ = services
            .AddEventStoreGatewayClient(options => options.BaseAddress = new Uri(daprHttpEndpoint))
            .AddHttpMessageHandler(() => new DaprAppIdHandler(EventStoreDaprAppId, daprApiToken));

        // Host the Pattern-A GovernedOperationAggregate as a real EventStore domain processor. AddEventStore
        // reflection-discovers EventStoreAggregate<TState> subclasses in the domain assembly and registers each
        // as an IDomainProcessor; the EventStore aggregate actor invokes the /process endpoint (mapped in
        // Program.cs) by convention (domain "chatbot" → app id "chatbot" → method "process").
        _ = services.AddEventStore(typeof(GovernedOperationAggregate).Assembly);
        services.TryAddScoped<ChatBotDomainServiceRequestHandler>();

        // M0 read model is projected into an in-memory, tenant-partitioned store (mirrors the Folders default;
        // the DAPR chatbot-statestore-backed store is the production swap). Projection writes stay idempotent
        // and order-tolerant through the handler.
        services.TryAddSingleton<IGovernedOperationProjectionStore, InMemoryGovernedOperationProjectionStore>();
        services.TryAddSingleton<GovernedOperationProjectionHandler>();

        return services
            .AddScoped<IAuthenticationStage, ClaimsAuthenticationStage>()
            .AddScoped<ITenantBindingStage, ClaimsTenantBindingStage>()
            .AddScoped<IAuthorizationStage, PassThroughAuthorizationStage>()
            .AddScoped<IRiskClassifier, PassThroughRiskClassifier>()
            .AddScoped<IApprovalGate, PassThroughApprovalGate>()
            .AddSingleton(static _ => BuildDaprClient())
            .AddSingleton<IIdempotencyStore, DaprCoarseIdempotencyStore>()
            .AddSingleton<InMemoryAuditWriter>()
            .AddSingleton<IAuditWriter>(static services => services.GetRequiredService<InMemoryAuditWriter>())
            .AddSingleton<IAuditHistoryReader>(static services => services.GetRequiredService<InMemoryAuditWriter>())
            .AddSingleton<InMemoryAuditReplayIntentQueue>()
            .AddSingleton<IAuditReplayIntentQueue>(static services => services.GetRequiredService<InMemoryAuditReplayIntentQueue>())
            .AddSingleton<InMemoryOperatorAlertSink>()
            .AddSingleton<IOperatorAlertSink>(static services => services.GetRequiredService<InMemoryOperatorAlertSink>())
            .AddSingleton<IOperationStatusStore, InMemoryOperationStatusStore>()
            .AddSingleton<ISystemClock, SystemClock>()
            .AddSingleton<InMemoryUserFacingMessageTelemetry>()
            .AddSingleton<IUserFacingMessageTelemetry>(static services => services.GetRequiredService<InMemoryUserFacingMessageTelemetry>())
            .AddScoped<IUserFacingRedactionStage, CoarseUserFacingRedactionStage>()
            .AddScoped<IChatBotProblemDetailsFactory, ChatBotProblemDetailsFactory>()
            .AddScoped<ILifecycleTransitionGuard, CommandSubmissionLifecycleTransitionGuard>()
            .AddSingleton<ISpineCommandAllowlist, ChatBotSpineCommandAllowlist>()
            .AddScoped<ICommandDispatcher, AcceptedCommandDispatcher>()
            .AddScoped<CommandGateway>();
    }

    /// <summary>
    /// Production swap (gated on a DAPR sidecar being present): project the governed-operation read model into
    /// the DAPR <c>chatbot-statestore</c> (Redis) instead of the in-memory M0 default, so the durable view
    /// survives across the topology and is inspectable end-to-end. <see cref="DaprGovernedOperationViewStore"/>
    /// resolves the already-registered <c>DaprClient</c>.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection, for chaining.</returns>
    public static IServiceCollection AddChatBotDaprStateStores(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.RemoveAll<IGovernedOperationProjectionStore>();
        return services.AddSingleton<IGovernedOperationProjectionStore, DaprGovernedOperationViewStore>();
    }
}
