using Hexalith.ChatBot.Server.Projections;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Hexalith.ChatBot.Server.Gateway;

/// <summary>
/// Registers the Story 10.6b project-conversation projection-change notification transport (reuse path): replaces the
/// default no-op publisher with the DAPR pub/sub publisher so AI response progress changes relay through EventStore's
/// <c>ProjectionChangedHub</c> to subscribed UI clients. Enabled only in hosts wired to the live DAPR + EventStore
/// topology; the default composition stays on the no-op publisher.
/// </summary>
public static class ProjectConversationChangeNotificationServiceCollectionExtensions
{
    /// <summary>
    /// Replaces the project-conversation change publisher with the DAPR pub/sub implementation. Requires DAPR and the
    /// EventStore host's projection-changed subscription to be present at runtime; the publish itself fails open.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddChatBotProjectionChangeNotifications(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.Replace(ServiceDescriptor.Singleton<IProjectConversationChangePublisher, DaprProjectConversationChangePublisher>());
        return services;
    }
}
