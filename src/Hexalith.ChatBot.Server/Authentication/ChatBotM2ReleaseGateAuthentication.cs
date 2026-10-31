using Microsoft.AspNetCore.Authorization;

namespace Hexalith.ChatBot.Server.Authentication;

/// <summary>
/// Registers the release-gate scheme and policy behind <c>/health/chatbot/periodic-enforcement/m2</c>. The EventStore
/// conforming-host inventory admits no anonymous route other than its exact probes, so the shared secret is checked
/// by a named, scheme-bound policy instead of inside an anonymous handler.
/// </summary>
internal static class ChatBotM2ReleaseGateAuthentication
{
    /// <summary>The authentication scheme that validates the release-gate secret.</summary>
    public const string SchemeName = "ChatBotM2ReleaseGate";

    /// <summary>The policy that admits only a request authenticated by <see cref="SchemeName"/>.</summary>
    public const string PolicyName = "ChatBotM2ReleaseGate";

    /// <summary>The configuration key holding the release-gate secret; the endpoint is unmapped while it is blank.</summary>
    public const string TokenConfigurationKey = "ChatBot:PeriodicEnforcement:M2ReleaseGateToken";

    /// <summary>Registers the scheme and policy when a release-gate secret is configured.</summary>
    /// <param name="services">The application's service collection.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddChatBotM2ReleaseGateAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        string? token = configuration[TokenConfigurationKey];
        if (string.IsNullOrWhiteSpace(token))
        {
            return services;
        }

        _ = services
            .AddAuthentication()
            .AddScheme<ChatBotM2ReleaseGateAuthenticationOptions, ChatBotM2ReleaseGateAuthenticationHandler>(
                SchemeName,
                options => options.Token = token);
        _ = services.AddAuthorization(options => options.AddPolicy(
            PolicyName,
            new AuthorizationPolicyBuilder(SchemeName).RequireAuthenticatedUser().Build()));
        return services;
    }
}
