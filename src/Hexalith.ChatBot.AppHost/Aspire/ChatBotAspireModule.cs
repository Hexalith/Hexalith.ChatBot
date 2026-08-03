using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

using CommunityToolkit.Aspire.Hosting.Dapr;

using Microsoft.Extensions.Configuration;

using System.Globalization;

namespace Hexalith.ChatBot.AppHost.Aspire;

internal static class ChatBotAspireModule
{
    public const string AppId = "chatbot";

    /// <summary>
    /// The resource name of the minimal UI core-operations surface. The UI reaches the spine over HTTP via
    /// service discovery (no DAPR sidecar), so it adds no entry to the deny-by-default access-control policy.
    /// </summary>
    public const string ChatBotUiAppId = "chatbot-ui";

    public const string EventStoreServiceName = "eventstore";

    /// <summary>
    /// The EventStore Admin REST API service (operator console backend). It reads the shared EventStore actor
    /// state store directly for stream/projection/tenant inspection and is invoked by the Admin UI over DAPR
    /// service invocation. Canonical name "eventstore-admin": matches the Hexalith.EventStore AppHost so the
    /// Admin.Server's default options (StateStoreName "statestore", EventStoreAppId "eventstore",
    /// TenantServiceAppId "tenants") resolve without override in this topology.
    /// </summary>
    public const string EventStoreAdminAppId = "eventstore-admin";

    /// <summary>
    /// The EventStore Admin Blazor UI (operator console). It reaches <see cref="EventStoreAdminAppId"/> ONLY via
    /// DAPR service invocation (it fails fast without a sidecar), so it carries its own sidecar tagged
    /// "eventstore-admin-ui" and is exposed on an external HTTP endpoint for the browser.
    /// </summary>
    public const string EventStoreAdminUiAppId = "eventstore-admin-ui";

    /// <summary>
    /// The EventStore actor/status/archive/checkpoint state store component. Canonical name "statestore": the
    /// shared Hexalith.EventStore hardcodes this name (its CommandStatus/Archive/Checkpoint options all default
    /// to "statestore"), so the actor host's Dapr clients require a component named exactly this. The chatbot's
    /// own read model + coarse idempotency use the separate <see cref="StateStoreComponentName"/>.
    /// </summary>
    public const string ActorStateStoreComponentName = "statestore";

    public const string StateStoreComponentName = "chatbot-statestore";

    public const string WorkflowStateStoreComponentName = "chatbot-workflow-statestore";

    public const string PubSubComponentName = "chatbot-pubsub";

    public const string PubSubTopicName = "chatbot.events";

    public const string DeadLetterTopicName = "deadletter.chatbot.events";

    public const string TenantsAppId = "tenants";

    public static string GetTenantDeadLetterTopic(string tenantId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        return $"deadletter.{tenantId}.{PubSubTopicName}";
    }

    /// <summary>The IPv4 Redis endpoint provided by <c>dapr init</c>. IPv4 literal, never "localhost" (see remarks at use site).</summary>
    private const string RedisHost = "127.0.0.1:6379";

    /// <summary>
    /// Every app id this module configures a DAPR sidecar for, across <see cref="AddHexalithChatBot"/> and
    /// <see cref="AddEventStoreAdmin"/>. Kept as one list so <see cref="ValidateUniqueInternalGrpcPorts"/> can check
    /// the whole topology's <c>Dapr:InternalGrpcPorts</c> configuration up front, even though the EventStore Admin
    /// resources are composed by a separate call.
    /// </summary>
    private static readonly string[] SidecarAppIds =
    [
        EventStoreServiceName,
        TenantsAppId,
        AppId,
        EventStoreAdminAppId,
        EventStoreAdminUiAppId,
    ];

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
        string? internalGrpcPortValue = builder.Configuration[$"Dapr:InternalGrpcPorts:{appId}"];
        int? internalGrpcPort = null;
        if (!string.IsNullOrWhiteSpace(internalGrpcPortValue))
        {
            if (!int.TryParse(internalGrpcPortValue, NumberStyles.None, CultureInfo.InvariantCulture, out int parsedPort)
                || parsedPort is <= 0 or > 65_535)
            {
                throw new InvalidOperationException(
                    $"Dapr internal gRPC port for app '{appId}' must be an integer from 1 through 65535.");
            }

            internalGrpcPort = parsedPort;
        }

        return new DaprSidecarOptions
        {
            AppId = appId,
            Config = config,
            DaprInternalGrpcPort = internalGrpcPort,

            // Force the IPv4 loopback for the sidecar→app channel. The daprd default ("localhost") resolves to
            // both ::1 and 127.0.0.1 in Go's resolver; when the app (Kestrel) is bound to IPv4 only, daprd's ::1
            // attempt fails and service-invocation/pubsub/actor delivery to the app returns gRPC "Unavailable".
            AppChannelAddress = "127.0.0.1",
            PlacementHostAddress = string.IsNullOrWhiteSpace(placement) ? null : placement,
            SchedulerHostAddress = string.IsNullOrWhiteSpace(scheduler) ? null : scheduler,
        };
    }

    /// <summary>
    /// Fails closed when two of this module's sidecars (<see cref="SidecarAppIds"/>) are configured with the same
    /// <c>Dapr:InternalGrpcPorts:{appId}</c> value. Reads configuration fresh on every call rather than caching
    /// anything in static state, so parallel test compositions with different overrides never observe each other.
    /// </summary>
    /// <param name="configuration">The builder's configuration, read once per composition.</param>
    private static void ValidateUniqueInternalGrpcPorts(IConfiguration configuration)
    {
        Dictionary<int, string> appIdByPort = new();
        foreach (string appId in SidecarAppIds)
        {
            string? configuredPort = configuration[$"Dapr:InternalGrpcPorts:{appId}"];
            if (string.IsNullOrWhiteSpace(configuredPort))
            {
                continue;
            }

            // A malformed value (non-integer or out of range) is reported by SidecarOptions itself when the app
            // id's own sidecar is configured; this pass only needs to compare values that parse as ports.
            if (!int.TryParse(configuredPort, NumberStyles.None, CultureInfo.InvariantCulture, out int port)
                || port is <= 0 or > 65_535)
            {
                continue;
            }

            if (appIdByPort.TryGetValue(port, out string? conflictingAppId))
            {
                throw new InvalidOperationException(
                    $"Dapr internal gRPC port {port} is configured for both '{conflictingAppId}' and '{appId}'. "
                    + "Each Dapr:InternalGrpcPorts entry in this topology must use a distinct port.");
            }

            appIdByPort[port] = appId;
        }
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

        // Validated once per builder composition, from configuration alone (no static mutable state): two sidecars
        // sharing one internal gRPC port fail unpredictably at daprd startup, naming only whichever process lost
        // the bind race, instead of failing closed here with both conflicting app ids.
        ValidateUniqueInternalGrpcPorts(builder.Configuration);

        // Redis is provided by `dapr init` at 127.0.0.1:6379 (mirrors the canonical Hexalith.EventStore module).
        // Use the IPv4 literal redisHost, never "localhost": on dual-stack hosts (e.g. WSL2) "localhost" resolves
        // to ::1 first and the go-redis client times out before falling back to IPv4, so daprd fails component init.
        // The EventStore actor/status store ("statestore", actorStateStore=true) is shared by the eventstore +
        // tenants actor hosts.
        IResourceBuilder<IDaprComponentResource> actorStateStore = builder
            .AddDaprComponent(ActorStateStoreComponentName, "state.redis")
            .WithMetadata("actorStateStore", "true")
            .WithMetadata("redisHost", RedisHost)
            .WithMetadata("keyPrefix", "none");

        // The chatbot read model + coarse idempotency store is logically isolated from every other DAPR state
        // component even though the local topology shares one Redis server.
        IResourceBuilder<IDaprComponentResource> stateStore = builder
            .AddDaprComponent(StateStoreComponentName, "state.redis")
            .WithMetadata("redisHost", RedisHost)
            .WithMetadata("keyPrefix", "name");

        // Hosted Dapr Workflow uses the actor runtime internally. Keep its actor-capable state store separate from
        // the EventStore actor/status store so correction-propagation saga state cannot share EventStore internals.
        IResourceBuilder<IDaprComponentResource> workflowStateStore = builder
            .AddDaprComponent(WorkflowStateStoreComponentName, "state.redis")
            .WithMetadata("actorStateStore", "true")
            .WithMetadata("redisHost", RedisHost)
            .WithMetadata("keyPrefix", "name");

        // A REAL Redis pub/sub (not the toolkit's in-memory default, which is per-sidecar and would never carry a
        // governed event from the EventStore publisher across to the chatbot projection subscriber).
        IResourceBuilder<IDaprComponentResource> pubSub = builder
            .AddDaprComponent(PubSubComponentName, "pubsub.redis")
            .WithMetadata("redisHost", RedisHost);

        // EventStore hosts the aggregate actors and PUBLISHES governed events on the chatbot pub/sub. It trusts the
        // chatbot as an internal DAPR caller (DaprInternal scheme authenticates the verified dapr-caller-app-id
        // against this allow-list, so no user JWT is forged). Disable the Aspire HTTP-endpoint proxy on every
        // sidecar-backed resource so the endpoint's allocated port == its target port == the app's Kestrel listener:
        // the CommunityToolkit Dapr integration sets the sidecar's app-port to the http endpoint's ALLOCATED port,
        // and with proxying on that is the DCP proxy front port (not the app's target port), so daprd dials a port
        // the app is not on and service invocation fails.
        _ = eventStore
            .WithEndpoint("http", endpoint => endpoint.IsProxied = false)
            .WithDaprSidecar(sidecar => sidecar
                .WithOptions(SidecarOptions(builder, EventStoreServiceName, daprConfigPath))
                .WithReference(actorStateStore)
                .WithReference(pubSub))
            .WithEnvironment("Authentication__DaprInternal__AllowedCallers__0", AppId)
            .WithEnvironment("EventStore__Publisher__PubSubName", PubSubComponentName);

        // Tenants is an EventStore domain service sharing the same actor state store + pub/sub (mirrors the
        // canonical EventStore AppHost). It is not on the chatbot command path but completes the spine topology.
        _ = tenants
            .WithEndpoint("http", endpoint => endpoint.IsProxied = false)
            .WithDaprSidecar(sidecar => sidecar
                .WithOptions(SidecarOptions(builder, TenantsAppId, daprConfigPath))
                .WithReference(actorStateStore)
                .WithReference(pubSub));

        // The chatbot reaches EventStore over DAPR service invocation, projects its read model into chatbot-statestore,
        // and subscribes to chatbot-pubsub. It references exactly these three components (chatbot-statestore,
        // chatbot-workflow-statestore, chatbot-pubsub) and never the EventStore "statestore" actor component.
        _ = chatBot
            .WithEndpoint("http", endpoint => endpoint.IsProxied = false)
            .WithReference(eventStore)
            .WithReference(tenants)
            .WaitFor(eventStore)
            .WaitFor(tenants)
            .WithDaprSidecar(sidecar => sidecar
                .WithOptions(SidecarOptions(builder, AppId, daprConfigPath))
                .WithReference(stateStore)
                .WithReference(workflowStateStore)
                .WithReference(pubSub));

        return new HexalithChatBotResources(actorStateStore, stateStore, workflowStateStore, pubSub, eventStore, tenants, chatBot);
    }

    /// <summary>
    /// Adds the EventStore Admin operator console (Admin REST API + Admin Blazor UI) to the local topology,
    /// mirroring the canonical Hexalith.EventStore AppHost. The Admin.Server reads the shared EventStore actor
    /// state store directly (no <c>AdminServer__EventStoreDaprHttpEndpoint</c> is set, so it uses the
    /// state-store actor-key read path rather than cross-sidecar metadata); the Admin.UI invokes the
    /// Admin.Server exclusively over DAPR service invocation. Keycloak/JWT wiring stays in the AppHost
    /// (it owns the identity provider), matching how the spine services are wired.
    /// </summary>
    /// <param name="builder">The distributed application builder.</param>
    /// <param name="resources">The spine resources returned by <see cref="AddHexalithChatBot"/>; supplies the
    /// shared actor state-store component (<see cref="HexalithChatBotResources.EventStore"/>) and the EventStore
    /// project resource the Admin.Server is sequenced behind.</param>
    /// <param name="adminServer">The Admin.Server.Host project resource.</param>
    /// <param name="adminUi">The Admin.UI project resource.</param>
    public static void AddEventStoreAdmin(
        this IDistributedApplicationBuilder builder,
        HexalithChatBotResources resources,
        IResourceBuilder<ProjectResource> adminServer,
        IResourceBuilder<ProjectResource> adminUi,
        string? daprConfigPath = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(resources);
        ArgumentNullException.ThrowIfNull(adminServer);
        ArgumentNullException.ThrowIfNull(adminUi);

        // Admin.Server is an inbound DAPR service-invocation target (the Admin.UI calls it), so disable the
        // Aspire HTTP-endpoint proxy — the sidecar's app-port must equal the app's Kestrel listener (same
        // rationale as the spine resources). It references the actor state store ("statestore") for direct
        // reads and never the pub/sub component (it neither publishes nor subscribes).
        _ = adminServer
            .WithEndpoint("http", endpoint => endpoint.IsProxied = false)
            .WithReference(resources.EventStoreService)
            .WaitFor(resources.EventStoreService)
            .WithDaprSidecar(sidecar => sidecar
                .WithOptions(SidecarOptions(builder, EventStoreAdminAppId, daprConfigPath))
                .WithReference(resources.EventStore));

        // Admin.UI reaches Admin.Server ONLY via DAPR service invocation (it fails fast without a sidecar), so it
        // carries a sidecar whose DaprAppIdHandler tags outbound calls with `dapr-app-id: eventstore-admin`. The
        // sidecar references no state store / pub/sub component — service invocation only, so it has zero direct
        // infrastructure access (same isolation rationale as the chatbot-ui surface). External HTTP endpoint for
        // the browser. WaitFor(adminServer) sequences the UI after its invocation target.
        _ = adminUi
            .WithReference(adminServer)
            .WaitFor(adminServer)
            .WithExternalHttpEndpoints()
            .WithDaprSidecar(sidecar => sidecar
                .WithOptions(SidecarOptions(builder, EventStoreAdminUiAppId, daprConfigPath)));
    }
}
