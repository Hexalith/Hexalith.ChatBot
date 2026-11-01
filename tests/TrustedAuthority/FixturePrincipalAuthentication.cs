using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;

namespace Hexalith.ChatBot.Tests.TrustedAuthority;

/// <summary>Routes a fixture startup filter's synthetic principal through the host's real bearer scheme name.</summary>
internal static class FixturePrincipalAuthentication
{
    /// <summary>
    /// Replaces the handler behind the host's bearer scheme with <see cref="FixturePrincipalAuthenticationHandler"/>.
    /// The ChatBot boundary policy and the EventStore host inventory still see the production scheme name, policy and
    /// human credential-kind requirement; only the credential source becomes the fixture principal.
    /// </summary>
    /// <param name="services">The test host services.</param>
    /// <returns>The services.</returns>
    public static IServiceCollection AddFixturePrincipalAuthentication(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        _ = services.PostConfigure<AuthenticationOptions>(static options =>
        {
            if (options.SchemeMap.TryGetValue(JwtBearerDefaults.AuthenticationScheme, out AuthenticationSchemeBuilder? bearer))
            {
                bearer.HandlerType = typeof(FixturePrincipalAuthenticationHandler);
            }
        });
        return services;
    }
}
