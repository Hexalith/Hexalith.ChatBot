using Aspire.Hosting.ApplicationModel;

using CommunityToolkit.Aspire.Hosting.Dapr;

namespace Hexalith.ChatBot.Aspire;

public sealed record HexalithChatBotResources(
    IResourceBuilder<IDaprComponentResource> EventStore,
    IResourceBuilder<IDaprComponentResource> StateStore,
    IResourceBuilder<IDaprComponentResource> WorkflowStateStore,
    IResourceBuilder<IDaprComponentResource> PubSub,
    IResourceBuilder<ProjectResource> EventStoreService,
    IResourceBuilder<ProjectResource> TenantsService,
    IResourceBuilder<ProjectResource> ChatBotService);
