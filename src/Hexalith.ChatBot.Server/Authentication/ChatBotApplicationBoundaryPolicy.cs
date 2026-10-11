using Microsoft.AspNetCore.Authorization;

namespace Hexalith.ChatBot.Server.Authentication;

/// <summary>
/// Lets ChatBot-owned routes reach their existing request-context and owner-authority guards while the
/// EventStore SDK's fallback policy continues to protect every unclaimed route.
/// </summary>
internal static class ChatBotApplicationBoundaryPolicy
{
    /// <summary>The policy applied only to routes that enforce their own ChatBot admission boundary.</summary>
    public const string Name = "ChatBotApplicationBoundary";

    /// <summary>Registers the explicit transport handoff for ChatBot-owned guarded routes.</summary>
    /// <param name="services">The application's service collection.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddChatBotApplicationBoundaryPolicy(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        _ = services.AddAuthorization(options => options.AddPolicy(
            Name,
            new AuthorizationPolicyBuilder().RequireAssertion(static _ => true).Build()));
        return services;
    }
}
