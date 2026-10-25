using Hexalith.Memories.Client.Rest;
using Hexalith.ChatBot.Server.Adapters.Projects;
using Hexalith.ChatBot.Server.Adapters.OwnerServices;
using Hexalith.Projects.Client;

using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Hexalith.ChatBot.Server.Projections.DerivedStores;

/// <summary>Registers validated live Memories adapters when required by the hosting environment.</summary>
internal static class MemoriesDerivedStoreServiceCollectionExtensions
{
    public const string EndpointConfigurationKey = "ChatBot:Memories:Endpoint";
    public const string ProjectsEndpointConfigurationKey = "ChatBot:Projects:Endpoint";
    public const string ProjectsApiTokenConfigurationKey = "ChatBot:Projects:ApiToken";
    public const string LiveBackingConfigurationKey = "ChatBot:Memories:UseLiveBacking";
    public const string WorkflowRuntimeConfigurationKey = "ChatBot:UseDaprWorkflowRuntime";

    public static IServiceCollection AddChatBotMemoriesDerivedStores(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(environment);

        bool liveBacking = environment.IsProduction()
            || configuration.GetValue<bool>(LiveBackingConfigurationKey);
        if (!liveBacking)
        {
            return services;
        }

        if (!configuration.GetValue<bool>(WorkflowRuntimeConfigurationKey))
        {
            throw new InvalidOperationException(
                $"{WorkflowRuntimeConfigurationKey} must be true when live Memories backing is enabled; production cannot silently skip durable ingestion or correction workflows.");
        }

        OwnerServiceAuthenticationOptions projectsAuthentication = OwnerServiceAuthenticationOptions.Load(
            configuration, "Projects", "hexalith-eventstore", "projects:read", environment);
        OwnerServiceAuthenticationOptions memoriesAuthentication = OwnerServiceAuthenticationOptions.Load(
            configuration, "Memories", "hexalith-memories-server", "memories:derived-store", environment);
        if ((projectsAuthentication.ApiToken is not null && projectsAuthentication.ApiToken == memoriesAuthentication.ApiToken)
            || (projectsAuthentication.ClientId is not null && memoriesAuthentication.ClientId is not null
            && (projectsAuthentication.ClientId == memoriesAuthentication.ClientId
                || projectsAuthentication.ClientSecret == memoriesAuthentication.ClientSecret)))
            throw new InvalidOperationException("Projects and Memories require distinct owner identities and secrets.");

        _ = services.AddMemoriesClient(options =>
        {
            options.Endpoint = memoriesAuthentication.Endpoint;
            options.ApiToken = memoriesAuthentication.ApiToken;
        }).WithOwnerAuthentication(memoriesAuthentication, environment, "Memories");

        _ = services
            .AddProjectsClient(options => options.BaseAddress = projectsAuthentication.Endpoint)
            .WithOwnerAuthentication(projectsAuthentication, environment, "Projects");

        services.RemoveAll<IDerivedStore>();
        services.RemoveAll<IVectorReindexer>();
        services.RemoveAll<IVectorReindexLedger>();
        services.RemoveAll<IMemoriesCaseResolver>();
        services.AddSingleton<IIngestionBindingSourceAdapter, MemoriesIngestionBindingSourceAdapter>();
        services.AddSingleton<IDerivedStore, MemoriesDerivedStore>();
        services.AddSingleton<IVectorReindexer, MemoriesVectorReindexer>();
        services.AddSingleton<IMemoriesCaseResolver>(provider => new ProjectsMemoriesCaseResolver(
            provider.GetRequiredService<Hexalith.Projects.Client.Generated.IClient>(), projectsAuthentication.TenantId));
        return services;
    }
}
