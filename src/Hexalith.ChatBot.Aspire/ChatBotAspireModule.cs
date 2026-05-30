using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

using CommunityToolkit.Aspire.Hosting.Dapr;

namespace Hexalith.ChatBot.Aspire;

public static class ChatBotAspireModule
{
    public const string AppId = "chatbot";

    public const string EventStoreServiceName = "eventstore";

    public const string EventStoreResourceName = "chatbot-eventstore";

    public const string StateStoreComponentName = "chatbot-statestore";

    public const string PubSubComponentName = "chatbot-pubsub";

    public const string PubSubTopicName = "chatbot.events";

    public const string DeadLetterTopicName = "deadletter.chatbot.events";

    public const string TenantsAppId = "tenants";

    public static (IResourceBuilder<IDaprComponentResource> EventStore, IResourceBuilder<IDaprComponentResource> StateStore, IResourceBuilder<IDaprComponentResource> PubSub)
        AddChatBotSharedDaprComponents(this IDistributedApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        IResourceBuilder<IDaprComponentResource> eventStore = builder
            .AddDaprComponent(EventStoreResourceName, "state.redis")
            .WithMetadata("actorStateStore", "true")
            .WithMetadata("redisHost", "localhost:6379")
            .WithMetadata("keyPrefix", "none");
        IResourceBuilder<IDaprComponentResource> stateStore = builder
            .AddDaprComponent(StateStoreComponentName, "state.redis")
            .WithMetadata("redisHost", "localhost:6379")
            .WithMetadata("keyPrefix", "none");
        IResourceBuilder<IDaprComponentResource> pubSub = builder
            .AddDaprPubSub(PubSubComponentName)
            .WithMetadata("topic", PubSubTopicName)
            .WithMetadata("deadLetterTopic", DeadLetterTopicName);

        return (eventStore, stateStore, pubSub);
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

        (IResourceBuilder<IDaprComponentResource> chatBotEventStore, IResourceBuilder<IDaprComponentResource> stateStore, IResourceBuilder<IDaprComponentResource> pubSub) =
            builder.AddChatBotSharedDaprComponents();

        _ = chatBot
            .WithReference(eventStore)
            .WithReference(tenants)
            .WaitFor(eventStore)
            .WaitFor(tenants)
            .WithDaprSidecar(sidecar => sidecar
                .WithOptions(new DaprSidecarOptions
                {
                    AppId = AppId,
                    Config = daprConfigPath,
                })
                .WithReference(chatBotEventStore)
                .WithReference(stateStore)
                .WithReference(pubSub));

        return new HexalithChatBotResources(chatBotEventStore, stateStore, pubSub, eventStore, tenants, chatBot);
    }
}
