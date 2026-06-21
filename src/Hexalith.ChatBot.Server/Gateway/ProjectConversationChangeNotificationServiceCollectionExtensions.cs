using Hexalith.ChatBot.Server.Projections;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Hexalith.ChatBot.Server.Gateway;

/// <summary>
/// Registers the Story 10.6b project-conversation projection-change notification transport: adds SignalR and replaces
/// the default no-op publisher with the ChatBot-owned hub broadcaster, so AI response progress changes nudge subscribed
/// UI clients (which then re-query the typed read state). The host must also map
/// <see cref="ChatBotProjectConversationHub"/> at <see cref="ChatBotProjectConversationHub.HubPath"/>. Enabled by the
/// host; the default composition stays on the no-op publisher with no hub mapped.
/// </summary>
public static class ProjectConversationChangeNotificationServiceCollectionExtensions
{
    /// <summary>
    /// Adds SignalR and replaces the project-conversation change publisher with the ChatBot-owned hub broadcaster.
    /// The host must map the hub (see <see cref="ChatBotProjectConversationHub.HubPath"/>). The broadcast itself fails
    /// open.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddChatBotProjectionChangeNotifications(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        _ = services.AddSignalR();
        services.Replace(ServiceDescriptor.Singleton<IProjectConversationChangePublisher, SignalRProjectConversationChangePublisher>());
        return services;
    }
}
