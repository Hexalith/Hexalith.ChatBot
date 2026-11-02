using Hexalith.EventStore.DomainService;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Hexalith.ChatBot.Server.Tests.TrustedAuthority;

/// <summary>
/// Allows in-process handler fixtures to reach ChatBot admission after the SDK's new workload policy is mapped.
/// The production host keeps the SDK policy; live topology tests exercise its transport assertion. The replacement
/// policies stay scheme-bound and authenticated, because the EventStore conforming-host inventory refuses to start a
/// host, or serve a request, whose named policy is not.
/// </summary>
internal static class SdkRouteFixtureAuthorization
{
    /// <summary>The test-only transport scheme behind the replaced SDK route policies.</summary>
    public const string SchemeName = "ChatBotSdkRouteFixture";

    /// <summary>Supplies actual loaded finite receiver inputs before the production SDK maps its receiver.</summary>
    public static IWebHostBuilder ConfigureFiniteReceiver(this IWebHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        HttpRouteCatalogFixtureFiles files = new();
        files.Configure(builder);
        builder.ConfigureServices(services => services.AddSingleton(_ => files));
        return builder;
    }

    public static void AllowCanonicalRouteHandlers(IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        HttpRouteCatalogFixture.Configure(services);
        if (!services.Any(static service => service.ServiceType == typeof(SdkRouteFixtureAuthenticationHandler)))
        {
            _ = services
                .AddAuthentication()
                .AddScheme<AuthenticationSchemeOptions, SdkRouteFixtureAuthenticationHandler>(SchemeName, null);
        }
        _ = services.PostConfigure<AuthorizationOptions>(options =>
        {
            AuthorizationPolicy fixturePolicy = new AuthorizationPolicyBuilder(SchemeName)
                .RequireAuthenticatedUser()
                .Build();
            options.AddPolicy(EventStoreDomainServicePolicies.Process, fixturePolicy);
            options.AddPolicy(EventStoreDomainServicePolicies.Query, fixturePolicy);
            options.AddPolicy(EventStoreDomainServicePolicies.Project, fixturePolicy);
        });
    }
}
