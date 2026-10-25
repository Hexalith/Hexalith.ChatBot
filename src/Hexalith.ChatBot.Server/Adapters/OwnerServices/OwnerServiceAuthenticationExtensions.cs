using Hexalith.EventStore.ServiceDefaults.Authentication;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Hexalith.ChatBot.Server.Adapters.OwnerServices;

/// <summary>Registers dedicated owner issuers without altering the host's SDK workload options or caller ACLs.</summary>
internal static class OwnerServiceAuthenticationExtensions
{
    /// <summary>Adds a dedicated issuer and a destination-bound bearer handler with redirects disabled.</summary>
    public static IHttpClientBuilder WithOwnerAuthentication(this IHttpClientBuilder client,
        OwnerServiceAuthenticationOptions options, IHostEnvironment environment, string owner)
    {
        IServiceCollection services = client.Services;
        services.AddLogging();
        services.TryAddSingleton(WorkloadSecurityClock.System);
        services.AddHttpClient(JwtWorkloadAssertionIssuer.HttpClientName)
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false });
        if (options.ApiToken is null)
        {
            services.AddKeyedSingleton<OwnerServiceTokenProvider>(owner, (provider, _) => new(options, environment,
                provider.GetRequiredService<IHttpClientFactory>(), provider.GetRequiredService<WorkloadSecurityClock>(),
                provider.GetRequiredService<ILogger<JwtWorkloadAssertionIssuer>>(), owner));
        }
        return client.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false })
            .AddHttpMessageHandler(provider => new OwnerServiceBearerHandler(options,
                options.ApiToken is null ? provider.GetRequiredKeyedService<OwnerServiceTokenProvider>(owner) : null));
    }
}
