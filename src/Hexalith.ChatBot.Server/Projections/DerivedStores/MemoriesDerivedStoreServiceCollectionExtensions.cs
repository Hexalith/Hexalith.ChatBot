using Hexalith.Memories.Client.Rest;
using Hexalith.ChatBot.Server.Adapters.Projects;
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

        string? configuredEndpoint = configuration[EndpointConfigurationKey];
        if (!Uri.TryCreate(configuredEndpoint, UriKind.Absolute, out Uri? endpoint)
            || (!string.Equals(endpoint.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
                && !string.Equals(endpoint.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
            || !string.IsNullOrEmpty(endpoint.UserInfo)
            || !string.IsNullOrEmpty(endpoint.Query)
            || !string.IsNullOrEmpty(endpoint.Fragment))
        {
            throw new InvalidOperationException(
                $"{EndpointConfigurationKey} must be an absolute HTTP(S) server endpoint without credentials, query, or fragment when live Memories backing is enabled.");
        }

        _ = services.AddMemoriesClient(options =>
        {
            options.Endpoint = endpoint;
            options.ApiToken = configuration["ChatBot:Memories:ApiToken"];
        });

        string? configuredProjectsEndpoint = configuration[ProjectsEndpointConfigurationKey];
        if (!Uri.TryCreate(configuredProjectsEndpoint, UriKind.Absolute, out Uri? projectsEndpoint)
            || (!string.Equals(projectsEndpoint.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
                && !string.Equals(projectsEndpoint.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
            || !string.IsNullOrEmpty(projectsEndpoint.UserInfo)
            || !string.IsNullOrEmpty(projectsEndpoint.Query)
            || !string.IsNullOrEmpty(projectsEndpoint.Fragment))
        {
            throw new InvalidOperationException(
                $"{ProjectsEndpointConfigurationKey} must be an absolute HTTP(S) server endpoint without credentials, query, or fragment when live Memories backing is enabled.");
        }

        string? projectsApiToken = configuration[ProjectsApiTokenConfigurationKey];
        if (string.IsNullOrWhiteSpace(projectsApiToken))
        {
            throw new InvalidOperationException(
                $"{ProjectsApiTokenConfigurationKey} must be configured for authorization-filtered Project Context reads when live Memories backing is enabled.");
        }

        _ = services
            .AddProjectsClient(options => options.BaseAddress = projectsEndpoint)
            .AddHttpMessageHandler(() => new ProjectsBearerTokenHandler(projectsApiToken));

        services.RemoveAll<IDerivedStore>();
        services.RemoveAll<IVectorReindexer>();
        services.RemoveAll<IVectorReindexLedger>();
        services.RemoveAll<IMemoriesCaseResolver>();
        services.AddSingleton<IDerivedStore, MemoriesDerivedStore>();
        services.AddSingleton<IVectorReindexer, MemoriesVectorReindexer>();
        services.AddSingleton<IMemoriesCaseResolver, ProjectsMemoriesCaseResolver>();
        return services;
    }
}
