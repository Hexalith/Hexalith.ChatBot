using Hexalith.EventStore.DomainService;

using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace Hexalith.ChatBot.Server.Tests.TrustedAuthority;

/// <summary>
/// Allows in-process handler fixtures to reach ChatBot admission after the SDK's new workload policy is mapped.
/// The production host keeps the SDK policy; live topology tests exercise its transport assertion.
/// </summary>
internal static class SdkRouteFixtureAuthorization
{
    public static void AllowCanonicalRouteHandlers(IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        _ = services.PostConfigure<AuthorizationOptions>(options =>
        {
            AuthorizationPolicy fixturePolicy = new AuthorizationPolicyBuilder()
                .RequireAssertion(static _ => true)
                .Build();
            options.AddPolicy(EventStoreDomainServicePolicies.Process, fixturePolicy);
            options.AddPolicy(EventStoreDomainServicePolicies.Query, fixturePolicy);
            options.AddPolicy(EventStoreDomainServicePolicies.Project, fixturePolicy);
        });
    }
}
