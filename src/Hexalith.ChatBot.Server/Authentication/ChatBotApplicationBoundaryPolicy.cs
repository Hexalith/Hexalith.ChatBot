using Hexalith.EventStore.ServiceDefaults.Authentication;

using Microsoft.AspNetCore.Authorization;

namespace Hexalith.ChatBot.Server.Authentication;

/// <summary>
/// Admits only authenticated human bearer principals to ChatBot-owned routes, which then apply their existing
/// request-context and owner-authority guards, while the EventStore SDK's fallback policy continues to deny every
/// unclaimed route.
/// </summary>
internal static class ChatBotApplicationBoundaryPolicy
{
    /// <summary>The policy applied only to routes that enforce their own ChatBot admission boundary.</summary>
    public const string Name = "ChatBotApplicationBoundary";

    /// <summary>Registers the scheme-bound policy and its typed ChatBot denial results.</summary>
    /// <param name="services">The application's service collection.</param>
    /// <returns>The service collection.</returns>
    /// <remarks>
    /// The EventStore conforming-host inventory refuses to start a host whose named policy lacks an explicit scheme or
    /// an authentication requirement, and requires a bearer policy to retain the human credential-kind requirement, so
    /// the policy is EventStore's human policy: a workload or channel principal never reaches a ChatBot route.
    /// </remarks>
    public static IServiceCollection AddChatBotApplicationBoundaryPolicy(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        _ = services.AddAuthorization(options => options.AddPolicy(
            Name,
            EventStoreHttpHostSecurityExtensions.CreateHumanPolicy()));
        _ = services.AddSingleton<IAuthorizationMiddlewareResultHandler, ChatBotApplicationBoundaryResultHandler>();
        return services;
    }
}
