using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

using CommunityToolkit.Aspire.Hosting.Dapr;

namespace Hexalith.ChatBot.Aspire;

public static class ChatBotAspireModule
{
    public const string AppId = "chatbot";

    /// <summary>
    /// The resource name of the minimal UI core-operations surface. The UI reaches the spine over HTTP via
    /// service discovery (no DAPR sidecar), so it adds no entry to the deny-by-default access-control policy.
    /// </summary>
    public const string ChatBotUiAppId = "chatbot-ui";

    public const string EventStoreServiceName = "eventstore";

    /// <summary>
    /// The EventStore actor/status/archive/checkpoint state store component. Canonical name "statestore": the
    /// shared Hexalith.EventStore hardcodes this name (its CommandStatus/Archive/Checkpoint options all default
    /// to "statestore"), so the actor host's Dapr clients require a component named exactly this. The chatbot's
    /// own read model + coarse idempotency use the separate <see cref="StateStoreComponentName"/>.
    /// </summary>
    public const string ActorStateStoreComponentName = "statestore";

    public const string StateStoreComponentName = "chatbot-statestore";

    public const string PubSubComponentName = "chatbot-pubsub";

    public const string PubSubTopicName = "chatbot.events";

    public const string DeadLetterTopicName = "deadletter.chatbot.events";

    public const string TenantsAppId = "tenants";

    /// <summary>The IPv4 Redis endpoint provided by <c>dapr init</c>. IPv4 literal, never "localhost" (see remarks at use site).</summary>
    private const string RedisHost = "127.0.0.1:6379";

    /// <summary>
    /// Builds sidecar options, applying optional placement/scheduler host-address overrides from configuration
    /// (<c>Dapr:PlacementHostAddress</c> / <c>Dapr:SchedulerHostAddress</c>). When unset, daprd uses its standard
    /// defaults (a conventional <c>dapr init</c>); the override exists for hosts whose <c>dapr init</c> mapped the
    /// placement/scheduler control-plane to non-standard ports.
    /// </summary>
    private static DaprSidecarOptions SidecarOptions(IDistributedApplicationBuilder builder, string appId, string? config = null)
    {
        string? placement = builder.Configuration["Dapr:PlacementHostAddress"];
        string? scheduler = builder.Configuration["Dapr:SchedulerHostAddress"];
        return new DaprSidecarOptions
        {
            AppId = appId,
            Config = config,

            // Force the IPv4 loopback for the sidecar→app channel. The daprd default ("localhost") resolves to
            // both ::1 and 127.0.0.1 in Go's resolver; when the app (Kestrel) is bound to IPv4 only, daprd's ::1
            // attempt fails and service-invocation/pubsub/actor delivery to the app returns gRPC "Unavailable".
            AppChannelAddress = "127.0.0.1",
            PlacementHostAddress = string.IsNullOrWhiteSpace(placement) ? null : placement,
            SchedulerHostAddress = string.IsNullOrWhiteSpace(scheduler) ? null : scheduler,
        };
    }

    public static (IResourceBuilder<IDaprComponentResource> EventStore, IResourceBuilder<IDaprComponentResource> StateStore, IResourceBuilder<IDaprComponentResource> PubSub)
        AddChatBotSharedDaprComponents(this IDistributedApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        // Redis is provided by `dapr init` at 127.0.0.1:6379 (mirrors the canonical Hexalith.EventStore module).
        // Use the IPv4 literal, never "localhost", because on dual-stack hosts (e.g. WSL2) "localhost" resolves to
        // ::1 first and the go-redis client times out before falling back to IPv4 — daprd then fails component init.
        // The EventStore actor/status store, canonically named "statestore" (actorStateStore = true), shared by
        // the eventstore + tenants actor hosts. Use the IPv4 literal redisHost, never "localhost": on dual-stack
        // hosts (e.g. WSL2) "localhost" resolves to ::1 first and the go-redis client times out before falling
        // back to IPv4, so daprd fails component init.
        IResourceBuilder<IDaprComponentResource> actorStateStore = builder
            .AddDaprComponent(ActorStateStoreComponentName, "state.redis")
            .WithMetadata("actorStateStore", "true")
            .WithMetadata("redisHost", RedisHost)
            .WithMetadata("keyPrefix", "none");

        // The chatbot read model + coarse idempotency store (separate from the EventStore actor store).
        IResourceBuilder<IDaprComponentResource> stateStore = builder
            .AddDaprComponent(StateStoreComponentName, "state.redis")
            .WithMetadata("redisHost", RedisHost)
            .WithMetadata("keyPrefix", "none");

        // A REAL Redis pub/sub (not the toolkit's in-memory default, which is per-sidecar and would never carry a
        // governed event from the EventStore publisher across to the chatbot projection subscriber).
        IResourceBuilder<IDaprComponentResource> pubSub = builder
            .AddDaprComponent(PubSubComponentName, "pubsub.redis")
            .WithMetadata("redisHost", RedisHost);

        return (actorStateStore, stateStore, pubSub);
    }

    public static HexalithChatBotResources AddHexalithChatBot(
        this IDistributedApplicationBuilder builder,
        IResourceBuilder<ProjectResource> eventStore,
        IResourceBuilder<ProjectResource> tenants,
        IResourceBuilder<ProjectResource> chatBot,
        string? daprConfigPath = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(eventStore);
        ArgumentNullException.ThrowIfNull(tenants);
        ArgumentNullException.ThrowIfNull(chatBot);

        (IResourceBuilder<IDaprComponentResource> actorStateStore, IResourceBuilder<IDaprComponentResource> stateStore, IResourceBuilder<IDaprComponentResource> pubSub) =
            builder.AddChatBotSharedDaprComponents();

        // EventStore hosts the aggregate actors (it needs the "statestore" actor/status store) and PUBLISHES
        // governed events on the chatbot pub/sub. It trusts the chatbot as an internal DAPR caller — the chatbot
        // submits commands via DAPR service invocation (dapr-app-id: eventstore) and EventStore's DaprInternal
        // scheme authenticates the verified dapr-caller-app-id against this allow-list, so no user JWT is forged
        // (identical to how the tenants domain service submits its bootstrap command). It publishes on the chatbot
        // pub/sub component so the projection subscriber and the publisher share one Redis stream.
        // Disable the Aspire HTTP-endpoint proxy on every sidecar-backed resource so the endpoint's allocated
        // port == its target port == the app's Kestrel listener. The CommunityToolkit Dapr integration sets the
        // sidecar's app-port to the http endpoint's ALLOCATED port; with proxying on, that is the DCP proxy front
        // port (not the app's target port), so daprd dials a port the app is not on and service invocation fails.
        // Keycloak uses the same IsProxied=false pattern for its direct ports in the canonical EventStore AppHost.
        _ = eventStore
            .WithEndpoint("http", endpoint => endpoint.IsProxied = false)
            .WithDaprSidecar(sidecar => sidecar
                .WithOptions(SidecarOptions(builder, EventStoreServiceName))
                .WithReference(actorStateStore)
                .WithReference(pubSub))
            .WithEnvironment("Authentication__DaprInternal__AllowedCallers__0", AppId)
            .WithEnvironment("EventStore__Publisher__PubSubName", PubSubComponentName);

        // Tenants is an EventStore domain service sharing the same actor state store + pub/sub (mirrors the
        // canonical EventStore AppHost). It is not on the chatbot command path but completes the spine topology.
        _ = tenants
            .WithEndpoint("http", endpoint => endpoint.IsProxied = false)
            .WithDaprSidecar(sidecar => sidecar
                .WithOptions(SidecarOptions(builder, TenantsAppId))
                .WithReference(actorStateStore)
                .WithReference(pubSub));

        // The chatbot hosts NO actors; it reaches EventStore over DAPR service invocation, projects its read model
        // into chatbot-statestore, and subscribes to chatbot-pubsub — so it references those two components only
        // (never the EventStore "statestore" actor component).
        _ = chatBot
            .WithEndpoint("http", endpoint => endpoint.IsProxied = false)
            .WithReference(eventStore)
            .WithReference(tenants)
            .WaitFor(eventStore)
            .WaitFor(tenants)
            .WithDaprSidecar(sidecar => sidecar
                .WithOptions(SidecarOptions(builder, AppId, daprConfigPath))
                .WithReference(stateStore)
                .WithReference(pubSub));

        return new HexalithChatBotResources(actorStateStore, stateStore, pubSub, eventStore, tenants, chatBot);
    }
}
